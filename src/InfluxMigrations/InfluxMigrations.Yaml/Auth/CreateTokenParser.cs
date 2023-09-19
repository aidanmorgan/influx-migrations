using System.Runtime.Serialization;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Auth;

[YamlOperationParser("create-token")]
public class CreateTokenParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode yamlNode)
    {
        throw new NotImplementedException();
    }
}
