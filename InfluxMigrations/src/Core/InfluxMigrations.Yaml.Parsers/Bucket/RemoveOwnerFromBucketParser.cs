using InfluxMigrations.Core;
using InfluxMigrations.Operations.Bucket;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Bucket;

[YamlOperationParser("remove-bucket-owner")]
public class RemoveOwnerFromBucketParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new RemoveOwnerFromBucketBuilder();

        node.Value(CommonTags.UserId, x =>
        {
            builder.WithUserId(x);
        });

        node.Value(CommonTags.UserName, x =>
        {
            builder.WithUserName(x);
        });

        node.Value(CommonTags.BucketId, x =>
        {
            builder.WithBucketId(x);
        });

        node.Value(CommonTags.BucketName, x =>
        {
            builder.WithBucketName(x);
        });


        return builder;
    }
}