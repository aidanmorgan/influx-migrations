using InfluxMigrations.Core;
using InfluxMigrations.Outputs;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Output;

[YamlOutputParser("file")]
public class WriteFileParser : IYamlTaskParser
{
    public IMigrationTaskBuilder Parse(YamlMappingNode node)
    {
        var children = node.Children;
        var builder = new WriteFileTaskBuilder();

        builder.WithFile(((YamlScalarNode)node["file"]).Value);

        if (!children.ContainsKey("content"))
        {
            throw new YamlMigrationParsingException(
                $"Cannot create a ${typeof(WriteFileTask)} with no content tags.");
        }

        var contentNode = children["content"];

        if (contentNode is YamlScalarNode)
        {
            var line = ((YamlScalarNode)contentNode).Value;
            builder.WithContent(line);
        }
        else if (contentNode is YamlSequenceNode)
        {
            foreach (var childNode in ((YamlSequenceNode)contentNode).Children)
            {
                var line = ((YamlScalarNode)childNode).Value;
                builder.WithContent(line);
                builder.WithContent("\n");
            }
        }

        return builder;
    }
}