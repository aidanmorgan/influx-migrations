using InfluxMigrations.Core;
using InfluxMigrations.Outputs;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Tasks;

[YamlTaskParser("echo")]
public class EchoTaskParser : IYamlTaskParser
{
    public IMigrationTaskBuilder Parse(YamlMappingNode node)
    {
        EchoTaskBuilder builder = new EchoTaskBuilder();

        node.ForEach("expr", (x) =>
        {
            var strVal = x.GetStringValue();
            if (!string.IsNullOrEmpty(strVal))
            {
                builder.WithString(strVal);
            }
        });

        return builder;
    }
}