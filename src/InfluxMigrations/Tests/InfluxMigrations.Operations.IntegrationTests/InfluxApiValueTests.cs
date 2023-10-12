using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.TestCommon;
using NUnit.Framework;

namespace InfluxMigrations.Operations.IntegrationTests;

public class InfluxApiValueTests
{
    private InfluxFixture _influxFixture;
    private IInfluxFactory _influx;

    [SetUp]
    public async Task SetUp()
    {
        _influxFixture = new InfluxFixture();
        _influx = await _influxFixture.Setup();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _influxFixture.TearDown();
    }

    [Test]
    public async Task OrganisationName()
    {
        var o = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        Assert.That(o, Is.Not.Null);

        var org = InfluxRuntimeIdResolver.CreateOrganisation().WithName(InfluxConstants.Organisation);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var migrationContext = environment.CreateMigrationContext("0001");
        var executionContext = migrationContext.CreateExecutionContext(nameof(BucketName));

        var orgId = await org.GetAsync(executionContext);

        Assert.That(orgId, Is.Not.Null);
        Assert.That(orgId, Is.EqualTo(o!.Id));
    }

    [Test]
    public async Task OrganisationId()
    {
        var o = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var org = InfluxRuntimeIdResolver.CreateOrganisation().WithId(o.Id);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var migrationContext = environment.CreateMigrationContext("0001");
        var executionContext = migrationContext.CreateExecutionContext(nameof(BucketName));

        var orgId = await org.GetAsync(executionContext);

        Assert.That(orgId, Is.Not.Null);
        Assert.That(orgId, Is.EqualTo(o.Id));
    }

    [Test]
    public async Task BucketName()
    {
        var o = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var b = await _influx.Create().GetBucketsApi()
            .CreateBucketAsync(nameof(BucketName), o!.Id);

        var bucket = InfluxRuntimeIdResolver.CreateBucket().WithName(nameof(BucketName));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var migrationContext = environment.CreateMigrationContext("0001");
        var executionContext = migrationContext.CreateExecutionContext(nameof(BucketName));

        var bucketId = await bucket.GetAsync(executionContext);

        Assert.That(bucketId, Is.Not.Null);
        Assert.That(bucketId, Is.EqualTo(b.Id));
    }

    [Test]
    public async Task BucketId()
    {
        var o = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var b = await _influx.Create().GetBucketsApi()
            .CreateBucketAsync(nameof(BucketId), o!.Id);

        var org = InfluxRuntimeIdResolver.CreateOrganisation().WithName(InfluxConstants.Organisation);
        var bucket = InfluxRuntimeIdResolver.CreateBucket().WithId(b.Id);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var migrationContext = environment.CreateMigrationContext("0001");
        var executionContext = migrationContext.CreateExecutionContext(nameof(BucketId));
        var bucketId = await bucket.GetAsync(executionContext);

        Assert.That(bucketId, Is.Not.Null);
        Assert.That(bucketId, Is.EqualTo(b.Id));
    }
}