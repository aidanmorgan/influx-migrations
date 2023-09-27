using InfluxMigrations.Default.Integration;
using InfluxMigrations.Operations.Bucket;
using InfluxMigrations.Yaml.Parsers;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.Tests;

public class DeleteBucketParserTests
{
    [Test]
    public async Task DeleteBucketParser_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: delete-bucket
      id: operation-id
      bucket_name: bucket-name
      bucket_id: bucket-id
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0].Operation as DeleteBucketBuilder;

        Assert.That(operation, Is.Not.Null);
        Assert.That(operation.BucketName, Is.EqualTo("bucket-name"));
        Assert.That(operation.BucketId, Is.EqualTo("bucket-id"));
    }
}