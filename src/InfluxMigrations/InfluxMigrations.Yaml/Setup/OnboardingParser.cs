using InfluxMigrations.Commands.Setup;
using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Setup;

[YamlOperationParser("onboarding")]
public class OnboardingParser : IYamlOperationParser
{
    public IMigrationOperationBuilder Parse(YamlMappingNode node)
    {
        var org = ((YamlScalarNode)node["organisation"]).Value;
        var token = ((YamlScalarNode)node["token"]).Value;
        var bucket = ((YamlScalarNode)node["bucket"]).Value;
        var username = ((YamlScalarNode)node["username"]).Value;
        var password = ((YamlScalarNode)node["password"]).Value;
        
        
        
        var result = new OnboardingBuilder()
            .WithBucket(bucket)
            .WithOrganisation(org)
            .WithAdminToken(token)
            .WithUsername(username)
            .WithPassword(password);
        return result;
    }
}