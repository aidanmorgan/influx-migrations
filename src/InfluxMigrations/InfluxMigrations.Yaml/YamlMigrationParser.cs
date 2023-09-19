using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml;

public class YamlMigrationParser
{
    private static IDictionary<string, Type> _parseableOperations = new Dictionary<string, Type>();
    private static IDictionary<string, Type> _outputs = new Dictionary<string, Type>();
    
    public YamlMigrationParser()
    {
        
    }

    private static List<Tuple<string, Type>> LoadPluggableBuilders(Type attributeType, Type interfaceType, Func<object, string> keywordExtractor)
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
            LoadPluggableBuilders(typeof(YamlOperationParserAttribute), typeof(IYamlOperationParser), (x) => ((YamlOperationParserAttribute)x).Keyword)
                .ForEach(x => _parseableOperations[x.Item1] = x.Item2);
            
            LoadPluggableBuilders(typeof(YamlOutputParserAttribute), typeof(IYamlTaskParser), (x) => ((YamlOutputParserAttribute)x).Keyword)
                .ForEach(x => _outputs[x.Item1] = x.Item2);
    }
    
    
    public async Task<Migration> ParseFile(string inputFile)
    {
        using var file = new StreamReader(File.OpenRead(inputFile));
        return await ParseString(await file.ReadToEndAsync());
    }

    public async Task<Migration> ParseString(string content)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(content));

        var root = stream.Documents.First().RootNode["migration"];
        if (root == null)
        {
            throw new YamlMigrationParsingException();
        }

        var migration = new Migration();
        if (root.ContainsKey("version"))
        {
            migration.Version = ((YamlScalarNode)root["version"]).Value ?? string.Empty;
        }

        if (root.ContainsKey("up"))
        {
            var upOperations = ((YamlSequenceNode)root["up"]);
            foreach (var taskNode in upOperations)
            {
                ParseOperation(taskNode, (a, b) => migration.AddUp(a, b));
            }
        }

        if (root.ContainsKey("down"))
        {
            var downOperations = ((YamlSequenceNode)root["down"]);
            foreach (var taskNode in downOperations)
            {
                ParseOperation(taskNode, (a, b) => migration.AddDown(a, b));
            }
        }

        return migration;
    }

    private void ParseOperation(YamlNode operationNode, Func<string, IMigrationOperationBuilder, MigrationOperationInstance> _addCallback )
    {
            var operationKey = ((YamlScalarNode)operationNode["operation"]).Value;

            if (!_parseableOperations.ContainsKey(operationKey))
            {
                throw new YamlMigrationParsingException($"Could not find parser for operation {operationKey}");
            }

            var id = Guid.NewGuid().ToString();
            if (operationNode.ContainsKey("id"))
            {
                id = ((YamlScalarNode)operationNode["id"]).Value!;
            }

            var commandParser = (IYamlOperationParser)Activator.CreateInstance(_parseableOperations[operationKey]);
            var commandBuilder = commandParser.Parse((YamlMappingNode)operationNode);

            var operation = _addCallback(id, commandBuilder);

            if (!operationNode.ContainsKey("tasks"))
            {
                return;
            }
            else
            {
                var tasksNode = ((YamlSequenceNode)operationNode["tasks"]);
                
                foreach (var output in tasksNode.Children)
                {
                    var outputKey = ((YamlScalarNode)((YamlMappingNode)output)["task"]).Value;

                    if (!_outputs.ContainsKey(outputKey))
                    {
                        throw new YamlMigrationParsingException($"Cannot find parser for output type {outputKey}");
                    }
                    
                    var outputParser = (IYamlTaskParser)Activator.CreateInstance(_outputs[outputKey]);
                    var outputBuilder = outputParser.Parse((YamlMappingNode)output);

                    if (output.ContainsKey("phases"))
                    {
                        foreach (var phase in ((YamlSequenceNode)output["phases"]))
                        {
                            switch (((YamlScalarNode)phase).Value)
                            {
                                case  "execute":
                                    operation.AddExecuteTask(outputBuilder);
                                    break;

                                case "commit":
                                {
                                    operation.AddCommitTask(outputBuilder);
                                    break;
                                }

                                case "rollback":
                                {
                                    operation.AddRollbackTask(outputBuilder);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // we assume if no phases are specified then it's an execute output task
                        operation.AddExecuteTask(outputBuilder);
                    }
                }
            }
    }
}

public static class YamlNodeExtensions
{
    public static bool ContainsKey(this YamlNode node, string name)
    {
        try
        {
            var result = node[name];
            return true;
        }
        catch (Exception x)
        {
            return false;
        }
    }
}
