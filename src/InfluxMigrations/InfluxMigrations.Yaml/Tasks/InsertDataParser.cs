using InfluxMigrations.Core;
using InfluxMigrations.Outputs;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Tasks;

[YamlTaskParser("insert-data")]
public class InsertDataParser : IYamlTaskParser
{
    public IMigrationTaskBuilder Parse(YamlMappingNode node)
    {
        var builder = new InsertDataBuilder();

        node.Value(CommonTags.OrganisationName, (x) =>
        {
            builder.WithOrganisationName(x);
        });

        node.Value(CommonTags.OrganisationId, (x) =>
        {
            builder.WithOrganisationId(x);
        });

        node.Value(CommonTags.BucketName, (x) =>
        {
            builder.WithBucketName(x);
        });

        node.Value(CommonTags.BucketId, (x) =>
        {
            builder.WithBucketId(x);
        });

        node.ForEach("line", (x) =>
        {
            var val = x.GetStringValue();
            
            if(!string.IsNullOrEmpty(val)) 
            {
                builder.AddLine(val);
            }
        });

        return builder;
    }
}