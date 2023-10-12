using InfluxMigrations.Default.Integration;
using InfluxMigrations.Operations.Auth;
using InfluxMigrations.Yaml.Parsers;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.UnitTests;

public class CreateBucketTokenParserTests
{
    [Test]
    public async Task CreateBucketTokenParser_PermissionList_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: create-bucket-token
      id: operation-id
      bucket_name: bucket-name
      bucket_id: bucket-id
      user_name: user-name
      user_id: user-id
      permission:
        - read
        - write
      token_name: test-token
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0].Operation as CreateBucketTokenBuilder;

        Assert.That(operation, Is.Not.Null);
        Assert.That(operation.BucketName, Is.EqualTo("bucket-name"));
        Assert.That(operation.BucketId, Is.EqualTo("bucket-id"));
        Assert.That(operation.UserName, Is.EqualTo("user-name"));
        Assert.That(operation.UserId, Is.EqualTo("user-id"));
        Assert.That(operation.TokenName, Is.EqualTo("test-token"));
        Assert.That(operation.Permissions.Count, Is.EqualTo(2));
        Assert.That(operation.Permissions[0], Is.EqualTo("read"));
        Assert.That(operation.Permissions[1], Is.EqualTo("write"));
    }

    [Test]
    public async Task CreateBucketTokenParser_SinglePermission_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: create-bucket-token
      id: operation-id
      bucket_name: bucket-name
      bucket_id: bucket-id
      user_name: user-name
      user_id: user-id
      permission: read
      token_name: test-token
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0].Operation as CreateBucketTokenBuilder;

        Assert.That(operation, Is.Not.Null);
        Assert.That(operation.BucketName, Is.EqualTo("bucket-name"));
        Assert.That(operation.BucketId, Is.EqualTo("bucket-id"));
        Assert.That(operation.UserName, Is.EqualTo("user-name"));
        Assert.That(operation.UserId, Is.EqualTo("user-id"));
        Assert.That(operation.TokenName, Is.EqualTo("test-token"));
        Assert.That(operation.Permissions.Count, Is.EqualTo(1));
        Assert.That(operation.Permissions[0], Is.EqualTo("read"));
    }
}