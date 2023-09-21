using InfluxMigrations.Core;
using InfluxMigrations.Outputs;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Tasks;

[YamlTaskParser("write-file")]
public class WriteFileParser : IYamlTaskParser
{
    public IMigrationTaskBuilder Parse(YamlMappingNode node)
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
}