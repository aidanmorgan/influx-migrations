using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Bucket;

[YamlOperationParser("add-member-to-bucket")]
public class AddMemberToBucketParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new AddMemberToBucketBuilder();

        node.Value(CommonTags.BucketId, x =>
        {
            builder.WithBucketId(x);
        });

        node.Value(CommonTags.BucketName, x =>
        {
            builder.WithBucketName(x);
        });

        node.Value(CommonTags.UserId, x =>
        {
            builder.WithUserId(x);
        });

        node.Value(CommonTags.UserName, x =>
        {
            builder.WithUserName(x);
        });

        return builder;
    }
}