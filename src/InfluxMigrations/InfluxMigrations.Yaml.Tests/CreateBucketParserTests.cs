using InfluxDB.Client;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.Tests;

public class Tests
{
    [Test]
    public async Task CreateBucketParser_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: create-bucket
      id: operation-id
      bucket_name: bucket-name
      organisation_name: organisation-name
      organisation_id: organisation-id
      retention: 5d
", x => new MockMigration(x)) as MockMigration;


        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0].Operation as CreateBucketBuilder;

        Assert.That(operation, Is.Not.Null);
        Assert.That(operation.Retention, Is.EqualTo("5d"));
        Assert.That(operation.BucketName, Is.EqualTo("bucket-name"));
        Assert.That(operation.OrganisationName, Is.EqualTo("organisation-name"));
        Assert.That(operation.OrganisationId, Is.EqualTo("organisation-id"));
    }
}