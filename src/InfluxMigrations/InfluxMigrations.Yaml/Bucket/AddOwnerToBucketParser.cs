using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Bucket;

[YamlOperationParser("add-owner-to-bucket")]
public class AddOwnerToBucketParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new AddOwnerToBucketBuilder();

        node.Value(CommonTags.BucketId, x =>
        {
            builder.WithBucketId(x);
        });

        node.Value(CommonTags.BucketName, x =>
        {
            builder.WithBucketName(x);
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