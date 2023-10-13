using InfluxMigrations.Core;
using InfluxMigrations.Operations.Organisation;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Organisation;

[YamlOperationParser("remove-user-from-organisation")]
public class RemoveUserFromOrganisationParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new RemoveUserFromOrganisationBuilder();

        node.Value(CommonTags.OrganisationName, x => { builder.WithOrganisationName(x); });

        node.Value(CommonTags.OrganisationId, x => { builder.WithOrganisationId(x); });

        node.Value(CommonTags.UserName, x => { builder.WithUsername(x); });

        node.Value(CommonTags.UserId, x => { builder.WithUserId(x); });


        return builder;
    }
}