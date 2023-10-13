using InfluxMigrations.Default.IntegrationTests;
using InfluxMigrations.Operations.Setup;
using InfluxMigrations.Yaml.Parsers;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.UnitTests;

public class OnboardingParserTests
{
    [Test]
    public async Task OnboardingParser_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: onboarding
      id: operation-id
      organisation_name: organisation-name
      token: test-token
      bucket_name: test-bucket
      user_name: user-name
      password: test-password
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0].Operation as OnboardingBuilder;

        Assert.That(operation, Is.Not.Null);

        Assert.That(operation.OrganisationName, Is.EqualTo("organisation-name"));
        Assert.That(operation.Token, Is.EqualTo("test-token"));
        Assert.That(operation.BucketName, Is.EqualTo("test-bucket"));
        Assert.That(operation.UserName, Is.EqualTo("user-name"));
        Assert.That(operation.Password, Is.EqualTo("test-password"));
    }
}