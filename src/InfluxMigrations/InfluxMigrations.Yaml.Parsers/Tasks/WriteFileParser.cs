using InfluxMigrations.Core;
using InfluxMigrations.Tasks;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Tasks;

[YamlTaskParser("write-file")]
public class WriteFileParser : IYamlTaskParser
{
    private WriteFileTaskBuilder Parse(YamlMappingNode node)
    {
        var builder = new WriteFileTaskBuilder();

        node.Value("file", (x) => { builder.WithFile(x); });

        node.ForEach("content", (x) =>
        {
            var value = x.GetStringValue();
            if (!string.IsNullOrEmpty(value))
            {
                builder.WithContent(value);
            }
        });

        return builder;
    }

    public IMigrationTaskBuilder ParseMigrationTask(YamlMappingNode node)
    {
        return Parse(node);
    }

    public IEnvironmentTaskBuilder ParseEnvironmentTask(YamlMappingNode node)
    {
        return Parse(node);
    }

    IOperationTaskBuilder IYamlTaskParser.ParseOperationTask(YamlMappingNode node)
    {
        return Parse(node);
    }
}