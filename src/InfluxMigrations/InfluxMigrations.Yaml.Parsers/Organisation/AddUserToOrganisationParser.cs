using InfluxMigrations.Commands.Organisation;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Organisation;

[YamlOperationParser("add-user-to-organisation")]
public class AddUserToOrganisationParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new AddUserToOrganisationBuilder();

        node.Value(CommonTags.OrganisationName, x => { builder.WithOrganisationName(x); });

        node.Value(CommonTags.OrganisationId, x => { builder.WithOrganisationId(x); });

        node.Value(CommonTags.UserName, x => { builder.WithUsername(x); });

        node.Value(CommonTags.UserId, x => { builder.WithUserId(x); });

        return builder;
    }
}