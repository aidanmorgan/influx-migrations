using System.Diagnostics;
using DotNet.Testcontainers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Testcontainers.InfluxDb;

namespace InfluxMigrations.IntegrationTests;

public class BucketManagementTests
{
    private InfluxFixture _influxFixture;
    private IInfluxFactory _influx;
    private Random _random;

    [SetUp]
    public async Task SetUp()
    {
        _influxFixture = new InfluxFixture();
        _influx = await _influxFixture.Setup();

        _random = new Random();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _influxFixture.TearDown();
    }

    [Test]
    public async Task CreateBucket()
    {
        var bucketName = _random.RandomString(8);

        var environment = new DefaultEnvironmentContext(_influx);
        var resultCapture = new CaptureResultBuilder();

        var migration = new Migration("0001");
        var step = migration.AddUp("create-bucket",
            new CreateBucketBuilder()
                .WithBucketName(bucketName)
                .WithOrganisation(InfluxConstants.Organisation));
        step.AddExecuteTask(resultCapture);

        await migration.ExecuteAsync(environment, MigrationDirection.Up);

        var result = resultCapture.As<CreateBucketResult>();

        Assert.That(result, Is.Not.Null);
        var loaded = await _influx.Create().GetBucketsApi().FindBucketByIdAsync(result.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.Name, Is.EqualTo(bucketName));
    }

    [Test]
    public async Task DeleteBucket_ByName()
    {
        var bucketName = _random.RandomString(8);

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync())
            .FirstOrDefault(x => x.Name == InfluxConstants.Organisation);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org);

        var environment = new DefaultEnvironmentContext(_influx);
        var migration = new Migration("0001");
        var step = migration.AddUp("delete-bucket-name", new DeleteBucketBuilder().WithBucketName(bucketName));

        await migration.ExecuteAsync(environment, MigrationDirection.Up);

        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetBucketsApi().FindBucketByIdAsync(bucket.Id);
        });
    }

    [Test]
    public async Task DeleteBucket_ById()
    {
        var bucketName = _random.RandomString(8);

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(x =>
            x.Name == InfluxConstants.Organisation);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org);

        var environment = new DefaultEnvironmentContext(_influx);
        var migration = new Migration("0001");
        var step = migration.AddUp("delete-bucket-name", new DeleteBucketBuilder().WithBucketId(bucket.Id));

        await migration.ExecuteAsync(environment, MigrationDirection.Up);

        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetBucketsApi().FindBucketByIdAsync(bucket.Id);
        });
    }

    [Test]
    public async Task DeleteBucket_ExecuteError_ForcesRollback()
    {
        var bucketName = _random.RandomString(8);

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(x =>
            x.Name == InfluxConstants.Organisation);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org);

        var environment = new DefaultEnvironmentContext(_influx);
        var migration = new Migration("0001");
        migration.AddUp("delete-bucket-name", new DeleteBucketBuilder().WithBucketId(bucket.Id));
        migration.AddUp("force-throw", new ForceErrorBuilder().ErrorExecute());

        await migration.ExecuteAsync(environment, MigrationDirection.Up);

        var loaded = await _influx.Create().GetBucketsApi().FindBucketByIdAsync(bucket.Id);
        Assert.That(loaded, Is.Not.Null);
    }
}