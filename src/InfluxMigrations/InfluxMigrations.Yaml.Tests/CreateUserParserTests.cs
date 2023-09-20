using InfluxMigrations.Commands.Auth;
using InfluxMigrations.Commands.User;
using InfluxMigrations.Default.Integration;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.Tests;

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