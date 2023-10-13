using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers;

public class YamlMigrationParser : ParserCommon, IYamlMigrationParser
{
    public YamlMigrationParser()
    {
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

                var taskParser = GetTaskParser(taskKey);
                var taskBuilder = taskParser?.ParseMigrationTask((YamlMappingNode)x);

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

        var id = Guid.NewGuid().ToString();
        operationNode.Value("id", (x) => { id = x; });

        var commandParser = GetOperationParser(operationKey);
        var commandBuilder = commandParser?.Parse((YamlMappingNode)operationNode);

        if (commandBuilder == null)
        {
            return;
        }

        var operation = addCallback(id, commandBuilder);

        operationNode.ForEach("tasks", (x) =>
        {
            var outputKey = x.GetStringValue("task");
            var taskParser = GetTaskParser(outputKey);
            var taskBuilder = taskParser?.ParseOperationTask((YamlMappingNode)x);

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