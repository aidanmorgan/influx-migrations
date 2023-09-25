using System.Reflection;
using InfluxMigrations.Commands;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers;

public class YamlMigrationParser : IYamlMigrationParser
{
    private static readonly IDictionary<string, Type> OperationParsers = new Dictionary<string, Type>();
    private static readonly IDictionary<string, Type> TaskParsers = new Dictionary<string, Type>();
    
    public YamlMigrationParser()
    {
    }

    private static List<Tuple<string, Type>> LoadPluggableBuilders(Type attributeType, Type interfaceType,
        Func<Attribute, string> keywordExtractor)
    {
        var coreTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .ToList()
            .WithAttributeAndInterface(attributeType, interfaceType);

        var extensionTypes = AppDomain.CurrentDomain
            .GetExtensionService()?
            .GetExtensionTypes()
            .WithAttributeAndInterface(attributeType, interfaceType);

        var all = new List<Tuple<Attribute, Type>>().Concat(coreTypes).Concat(extensionTypes).ToList();

        return all.Select(x => new Tuple<string, Type>(keywordExtractor(x.Item1), x.Item2)).ToList();
    }

    static YamlMigrationParser()
    {
        AppDomain.CurrentDomain.GetExtensionService()?.AddSharedTypes(typeof(IYamlMigrationParser).Assembly);
        
        LoadPluggableBuilders(typeof(YamlOperationParserAttribute), typeof(IYamlOperationParser),
                (x) => ((YamlOperationParserAttribute)x).Keyword)
            .ForEach(x => OperationParsers[x.Item1] = x.Item2);

        LoadPluggableBuilders(typeof(YamlTaskParserAttribute), typeof(IYamlTaskParser),
                (x) => ((YamlTaskParserAttribute)x).Keyword)
            .ForEach(x => TaskParsers[x.Item1] = x.Item2);
    }


    public async Task<IMigration> ParseFile(string inputFile, Func<string, IMigration>? migrationFactory = null)
    {
        using var file = new StreamReader(File.OpenRead(inputFile));
        return await ParseString(await file.ReadToEndAsync(), migrationFactory);
    }

    public async Task<IMigration> ParseString(string content, Func<string, IMigration>? migrationFactory = null)
    { 
        var stream = new YamlStream();
        stream.Load(new StringReader(content));

        var root = stream.Documents.First().RootNode["migration"];
        if (root == null)
        {
            throw new YamlMigrationParsingException();
        }

        var version = string.Empty;
        root.Value("version", (x) => { version = x; });
        
        var migration = migrationFactory != null ? migrationFactory(version) : new Migration() { Version = version };

        root.ForEach("up", (x) => { 
            x.ForEach((y) =>
            {
                ParseOperation(y, (a, b) => migration.AddUp(a, b));
            }); 
        });

        root.ForEach("down", (x) =>
        {
            x.ForEach((y) =>
            {
                ParseOperation(y, (a, b) => migration.AddDown(a, b));
            });
        });

        root.ForEach("tasks", (x) =>
        {
            x.ForEach((y) =>
            {
                var taskKey = x.GetStringValue("task");

                if (!TaskParsers.ContainsKey(taskKey))
                {
                    throw new YamlMigrationParsingException($"Cannot find parser for Task type {taskKey}. Registered Task Parsers: [{string.Join(",", TaskParsers.Keys)}]");
                }

                var taskParser = (IYamlTaskParser?)Activator.CreateInstance(TaskParsers[taskKey]);
                var taskBuilder = taskParser?.Parse((YamlMappingNode)x);

                if (taskBuilder != null)
                {
                    var phase = "after";

                    y.Value("phase", x =>
                    {
                        phase = x;
                    });

                    if (string.IsNullOrEmpty(phase) ||
                        string.Equals(phase, "after", StringComparison.InvariantCultureIgnoreCase))
                    {
                        migration.AddAfterTask(taskBuilder);
                    }
                    else
                    {
                        migration.AddBeforeTask(taskBuilder);
                    }
                }
            });
        });

        return migration;
    }

    private void ParseOperation(YamlNode operationNode,
        Func<string, IMigrationOperationBuilder, MigrationOperationInstance> addCallback)
    {
        var operationKey = operationNode.GetStringValue("operation");
        if (!OperationParsers.ContainsKey(operationKey))
        {
            throw new YamlMigrationParsingException($"Could not find parser for operation {operationKey}");
        }

        var id = Guid.NewGuid().ToString();
        operationNode.Value("id", (x) => { id = x; });

        var commandParser = (IYamlOperationParser?)Activator.CreateInstance(OperationParsers[operationKey]);
        var commandBuilder = commandParser?.Parse((YamlMappingNode)operationNode);

        if (commandBuilder == null)
        {
            return;
        }

        var operation = addCallback(id, commandBuilder);

        operationNode.ForEach("tasks", (x) =>
        {
            var outputKey = x.GetStringValue("task");
            if (!TaskParsers.ContainsKey(outputKey))
            {
                throw new YamlMigrationParsingException($"Cannot find parser for output type {outputKey}");
            }

            var taskParser = (IYamlTaskParser?)Activator.CreateInstance(TaskParsers[outputKey]);
            var taskBuilder = taskParser?.Parse((YamlMappingNode)x);

            if (taskBuilder == null)
            {
                return;
            }

            var phased = x.ForEach("phases", (x) =>
            {
                switch (x.GetStringValue())
                {
                    case "execute":
                    case "execute-after":
                        operation.AddExecuteTask(TaskPrecedence.After, taskBuilder);
                        break;
                    
                    case "execute-before":
                        operation.AddExecuteTask(TaskPrecedence.Before, taskBuilder);
                        break;

                    case "commit":
                    case "commit-after":
                        operation.AddCommitTask(TaskPrecedence.After, taskBuilder);
                        break;
                    
                    case "commit-before":
                        operation.AddCommitTask(TaskPrecedence.Before, taskBuilder);
                        break;
                    
                    case "rollback":
                    case "rollback-after":
                        operation.AddRollbackTask(TaskPrecedence.After, taskBuilder);
                        break;
                    
                    case "rollback-before":
                        operation.AddRollbackTask(TaskPrecedence.Before, taskBuilder);
                        break;
                    
                    default:
                    {
                        break;
                    }
                }
            });

            if (!phased)
            {
                operation.AddExecuteTask(TaskPrecedence.After, taskBuilder);
            }
        });
    }
}