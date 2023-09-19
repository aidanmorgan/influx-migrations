using InfluxMigrations.Core;
using InfluxMigrations.Outputs;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Output;

[YamlTaskParser("echo")]
public class EchoTaskParser : IYamlTaskParser
{
    public IMigrationTaskBuilder Parse(YamlMappingNode node)
    {
        EchoTaskBuilder builder = new EchoTaskBuilder();

        if (!node.ContainsKey("expr"))
        {
            throw new YamlMigrationParsingException($"Cannot create a ${typeof(EchoTaskBuilder)} with no expr fields.");
        }
        var exprNode = node["expr"];

        if (exprNode is YamlScalarNode)
        {
            var value = ((YamlScalarNode)exprNode).Value;
            builder.WithString(value);
        }
        else if (exprNode is YamlSequenceNode)
        {
            foreach (var lineNode in (((YamlSequenceNode)exprNode).Children))
            {
                builder.WithString(((YamlScalarNode)lineNode).Value);
                builder.WithString("\n");
            }
        }

        return builder;
    }
}