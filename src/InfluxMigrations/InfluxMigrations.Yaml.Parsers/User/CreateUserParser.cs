using InfluxMigrations.Core;
using InfluxMigrations.Operations.User;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.User;

[YamlOperationParser("create-user")]
public class CreateUserParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode yamlNode)
    {
        var builder = new CreateUserBuilder();

        yamlNode.Value(CommonTags.UserName, x => { builder.WithUsername(x); });

        yamlNode.Value("password", x => { builder.WithPassword(x); });

        return builder;
    }
}