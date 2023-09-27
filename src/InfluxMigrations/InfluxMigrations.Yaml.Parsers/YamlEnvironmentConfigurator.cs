using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers;

public class YamlEnvironmentConfigurator : ParserCommon, IEnvironmentConfigurator
{
    private static readonly List<string> EnvironmentConfigurationFilenames = new List<string>()
    {
        "global.yml",
        "env.yml"
    };
    
    private readonly YamlStream _stream;

    public static IEnvironmentConfigurator CreateFromDirectory(string directory)
    {
        foreach (var name in EnvironmentConfigurationFilenames)
        {
            var file = Path.Combine(directory, name);
            if (Path.Exists(file))
            {
                return CreateFromFile(file);
            }
        }

        return new NoOpEnvironmentConfigurator();
    }
    
    public static IEnvironmentConfigurator CreateFromFile(string inputFile)
    {
        using var file = new StreamReader(File.OpenRead(inputFile));
        var stream = new YamlStream();
        stream.Load(new StringReader(file.ReadToEnd()));

        return new YamlEnvironmentConfigurator(stream);
    }

    public static IEnvironmentConfigurator CreateFromString(string content)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(content));

        return new YamlEnvironmentConfigurator(stream);
    }

    private YamlEnvironmentConfigurator(YamlStream content)
    {
        _stream = content;
    }
    
    public Task ConfigureEnvironmentAsync(IEnvironmentExecutionContext ctx, IMigrationRunnerOptions opts)
    {
        _stream.Documents.First().RootNode.ForEach("before", x =>
        {
            var task = x.GetStringValue("task");
            var builder = GetTaskParser(task);
            
            ctx.AddTask(TaskPrecedence.Before, builder.ParseEnvironmentTask((YamlMappingNode)x));
        });
        
        _stream.Documents.First().RootNode.ForEach("after", x =>
        {
            var task = x.GetStringValue("task");
            var builder = GetTaskParser(task);

            ctx.AddTask(TaskPrecedence.After, builder.ParseEnvironmentTask((YamlMappingNode)x));
        });

        return Task.CompletedTask;
    }
}