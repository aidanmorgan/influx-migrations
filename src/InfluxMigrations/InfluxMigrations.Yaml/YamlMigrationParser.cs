using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml;

public class YamlMigrationParser
{
    private static readonly IDictionary<string, Type> ParseableOperations = new Dictionary<string, Type>();
    private static readonly IDictionary<string, Type> Outputs = new Dictionary<string, Type>();

    public YamlMigrationParser()
    {
    }

    private static List<Tuple<string, Type>> LoadPluggableBuilders(Type attributeType, Type interfaceType,
        Func<object, string> keywordExtractor)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .AsParallel()
            .SelectMany(x => x.GetTypes())
            .Select(x =>
            {
                var attributed = x.GetCustomAttributes(attributeType, true);
                if (attributed.Length > 0 && x.GetInterfaces().Contains(interfaceType))
                {
                    var attr = attributed.First();
                    return new Tuple<string, Type>(keywordExtractor(attr), x);
                }

                return null;
            })
            .Where(x => x != null)
            .ToList()!;
    }

    static YamlMigrationParser()
    {
        LoadPluggableBuilders(typeof(YamlOperationParserAttribute), typeof(IYamlOperationParser),
                (x) => ((YamlOperationParserAttribute)x).Keyword)
            .ForEach(x => ParseableOperations[x.Item1] = x.Item2);

        LoadPluggableBuilders(typeof(YamlTaskParserAttribute), typeof(IYamlTaskParser),
                (x) => ((YamlTaskParserAttribute)x).Keyword)
            .ForEach(x => Outputs[x.Item1] = x.Item2);
    }


    public async Task<IMigration> ParseFile(string inputFile)
    {
        using var file = new StreamReader(File.OpenRead(inputFile));
        return await ParseString(await file.ReadToEndAsync());
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
        root.Value("version", (x) =>
        {
            version = x;
        });

        
        var migration = migrationFactory != null ? migrationFactory(version) : new Migration()
        {
            Version = version
        };

        root.ForEach("up", (x) => { x.
            ForEach((y) =>
            {
                ParseOperation(y, (a, b) => migration.AddUp(a, b));
            }); 
        });
        
        root.ForEach("down", 
            (x) => { x.ForEach((y) =>
            {
                ParseOperation(y, (a, b) => migration.AddDown(a, b));
            }); 
        });
        
        root.ForEach("tasks", (x) =>
        {
            x.ForEach((y) =>
            {
                var outputKey = x.GetStringValue("task");

                if (!Outputs.ContainsKey(outputKey))
                {
                    throw new YamlMigrationParsingException($"Cannot find parser for output type {outputKey}");
                }

                var taskParser = (IYamlTaskParser?)Activator.CreateInstance(Outputs[outputKey]);
                var taskBuilder = taskParser?.Parse((YamlMappingNode)x);

                if (taskBuilder != null)
                {
                    migration.AddTask(taskBuilder);
                }
            });
        });

        return migration;
    }

    private void ParseOperation(YamlNode operationNode, Func<string, IMigrationOperationBuilder, MigrationOperationInstance> addCallback)
    {
        var operationKey = operationNode.GetStringValue("operation");
        if (!ParseableOperations.ContainsKey(operationKey))
        {
            throw new YamlMigrationParsingException($"Could not find parser for operation {operationKey}");
        }

        var id = Guid.NewGuid().ToString();
        operationNode.Value("id", (x) =>
        {
            id = x;
        });

        var commandParser = (IYamlOperationParser?)Activator.CreateInstance(ParseableOperations[operationKey]);
        var commandBuilder = commandParser?.Parse((YamlMappingNode)operationNode);

        if (commandBuilder == null)
        {
            return;
        }

        var operation = addCallback(id, commandBuilder);

        operationNode.ForEach("tasks", (x) =>
        {
            var outputKey = x.GetStringValue("task");
            if (!Outputs.ContainsKey(outputKey))
            {
                throw new YamlMigrationParsingException($"Cannot find parser for output type {outputKey}");
            }

            var taskParser = (IYamlTaskParser?)Activator.CreateInstance(Outputs[outputKey]);
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
                        operation.AddExecuteTask(taskBuilder);
                        break;

                    case "commit":
                    {
                        operation.AddCommitTask(taskBuilder);
                        break;
                    }

                    case "rollback":
                    {
                        operation.AddRollbackTask(taskBuilder);
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            });

            if (!phased)
            {
                operation.AddExecuteTask(taskBuilder);
            }
        });
    }
}