using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Bucket;

[YamlOperationParser("create-bucket")]
public class CreateBucketYamlParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode yamlNode)
    {
        CreateBucketBuilder builder = new CreateBucketBuilder();

        yamlNode.Value(CommonTags.BucketName, (x) => { builder.WithBucketName(x); });

        yamlNode.Value("retention", (x) => { builder.WithRetention(x); });

        yamlNode.Value(CommonTags.OrganisationName, (x) => { builder.WithOrganisation(x); });

        yamlNode.Value(CommonTags.OrganisationId, (x) => { builder.WithOrganisationId(x); });

        return builder;
    }
}