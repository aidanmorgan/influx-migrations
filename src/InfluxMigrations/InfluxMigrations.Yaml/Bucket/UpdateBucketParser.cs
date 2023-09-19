using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Bucket;

[YamlOperationParser("update-bucket")]
public class UpdateBucketParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode yamlNode)
    {
        throw new NotImplementedException();
    }
}