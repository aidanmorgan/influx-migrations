using InfluxMigrations.Core;
using InfluxMigrations.Operations.Bucket;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Bucket;

[YamlOperationParser("delete-bucket")]
public class DeleteBucketParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode yamlNode)
    {
        var builder = new DeleteBucketBuilder();

        yamlNode.Value(CommonTags.BucketName, (x) => { builder.WithBucketName(x); });

        yamlNode.Value(CommonTags.BucketId, (x) => { builder.WithBucketId(x); });

        return builder;
    }
}