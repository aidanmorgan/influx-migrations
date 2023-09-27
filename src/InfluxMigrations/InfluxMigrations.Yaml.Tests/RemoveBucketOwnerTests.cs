using System.Reflection;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Operations.Bucket;
using InfluxMigrations.Yaml.Parsers;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.Tests;

public class RemoveBucketOwnerTests
{
    [Test]
    public async Task RemoveOwnerFromBucket_Success()
    {
        var parser = new YamlMigrationParser();
        var result = await parser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: remove-bucket-owner
      user_name: user-name
      user_id: user-id
      bucket_name: bucket-name
      bucket_id: bucket-id
", x => new MockMigration(x));

        Assert.That(result, Is.Not.Null);
        var builder = ((MockMigration)result).UpOperations[0].Operation as RemoveOwnerFromBucketBuilder;
        
        Assert.That(builder.BucketId, Is.EqualTo("bucket-id"));
        Assert.That(builder.BucketName, Is.EqualTo("bucket-name"));
        Assert.That(builder.UserId, Is.EqualTo("user-id"));
        Assert.That(builder.UserName, Is.EqualTo("user-name"));
    }
}