using InfluxMigrations.Default.IntegrationTests;
using InfluxMigrations.Operations.User;
using InfluxMigrations.Yaml.Parsers;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.UnitTests;

public class CreateUserParserTests
{
    [Test]
    public async Task CreateUserParser_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: create-user
      id: operation-id
      user_name: test-user
      password: test-password
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0].Operation as CreateUserBuilder;

        Assert.That(operation, Is.Not.Null);
        Assert.That(operation.UserName, Is.EqualTo("test-user"));
        Assert.That(operation.Password, Is.EqualTo("test-password"));
    }
}