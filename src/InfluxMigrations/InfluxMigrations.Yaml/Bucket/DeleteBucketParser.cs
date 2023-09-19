using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Bucket;

[YamlOperationParser("delete-bucket")]
public class DeleteBucketParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode yamlNode)
    {
        var builder = new DeleteBucketBuilder();

        if (yamlNode.ContainsKey("name"))
        {
            builder.WithBucketName(((YamlScalarNode)yamlNode["name"]).Value);
        }

        if (yamlNode.ContainsKey("id"))
        {
            builder.WithBucketName(((YamlScalarNode)yamlNode["id"]).Value);
        }
        
        return builder;
    }
}