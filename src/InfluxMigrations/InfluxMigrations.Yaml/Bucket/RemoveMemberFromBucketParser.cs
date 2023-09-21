using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Bucket;

[YamlOperationParser("remove-member-from-bucket")]
public class RemoveMemberFromBucketParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new RemoveMemberFromBucketBuilder();

        node.Value(CommonTags.BucketName, x =>
        {
            builder.WithBucketName(x);
        });

        node.Value(CommonTags.BucketId, x =>
        {
            builder.WithBucketId(x);
        });

        node.Value(CommonTags.UserName, x =>
        {
            builder.WithUserName(x);
        });

        node.Value(CommonTags.UserId, x =>
        {
            builder.WithUserId(x);
        });

        return builder;
    }
}