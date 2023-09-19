using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Bucket;

[YamlOperationParser("create-bucket")]
public class CreateBucketYamlParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode yamlNode)
    {
        CreateBucketBuilder builder = new CreateBucketBuilder();
        builder.WithBucketName(((YamlScalarNode)yamlNode["name"]).Value);

        if (yamlNode.ContainsKey("retention"))
        {
            builder.WithRetention(((YamlScalarNode)yamlNode["retention"]).Value);
        }

        if (yamlNode.ContainsKey("organisation"))
        {
            builder.WithOrganisation(((YamlScalarNode)yamlNode["organisation"]).Value);
        }

        return builder;
    }
}