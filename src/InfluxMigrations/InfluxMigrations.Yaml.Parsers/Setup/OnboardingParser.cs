using InfluxMigrations.Commands.Setup;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Setup;

[YamlOperationParser("onboarding")]
public class OnboardingParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var builder = new OnboardingBuilder();

        node.Value(CommonTags.OrganisationName, (x) => { builder.WithOrganisation(x); });

        node.Value("token", (x) => { builder.WithAdminToken(x); });

        node.Value(CommonTags.BucketName, (x) => { builder.WithBucket(x); });

        node.Value(CommonTags.UserName, (x) => { builder.WithUsername(x); });

        node.Value("password", (x) => { builder.WithPassword(x); });

        return builder;
    }
}