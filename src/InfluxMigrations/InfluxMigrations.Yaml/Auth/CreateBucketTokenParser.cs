using System.Runtime.Serialization;
using InfluxMigrations.Commands.Auth;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Auth;

[YamlOperationParser("create-bucket-token")]
public class CreateBucketTokenParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new CreateBucketTokenBuilder();

        node.Value("token_name", x => { builder.WithTokenName(x); });

        node.Value(CommonTags.BucketName, x => { builder.WithBucketName(x); });

        node.Value(CommonTags.BucketId, x => { builder.WithBucketId(x); });

        node.ForEach("permission", x => { builder.WithPermission(x.GetStringValue()); });

        node.Value("token_name", x => { builder.WithTokenName(x); });

        node.Value(CommonTags.UserName, x => { builder.WithUserName(x); });

        node.Value(CommonTags.UserId, x => { builder.WithUserId(x); });

        return builder;
    }
}