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

        yamlNode.Value("bucket_name", (x) =>
        {
            builder.WithBucketName(x);
        });

        yamlNode.Value("bucket_id", (x) =>
        {
            builder.WithBucketId(x);
        });

        return builder;
    }
}