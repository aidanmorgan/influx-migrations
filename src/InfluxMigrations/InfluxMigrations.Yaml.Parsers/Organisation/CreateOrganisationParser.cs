using InfluxMigrations.Commands.Organisation;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Organisation;

[YamlOperationParser("create-organisation")]
public class CreateOrganisationParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new CreateOrganisationBuilder();

        node.Value(CommonTags.OrganisationName, x => { builder.WithName(x); });

        return builder;
    }
}