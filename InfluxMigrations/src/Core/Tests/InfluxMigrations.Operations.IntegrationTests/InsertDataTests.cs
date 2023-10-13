using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.Operations.Bucket;
using InfluxMigrations.Tasks;
using InfluxMigrations.TestCommon;
using NUnit.Framework;

namespace InfluxMigrations.Operations.IntegrationTests;

public class InsertDataTests
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
    public async Task TestInsert()
    {
        var bucketName = _random.RandomString(8);

        var migration = new Migration("0001");
        var add = migration.AddUp("create-bucket",
            new CreateBucketBuilder().WithBucketName(bucketName).WithOrganisation(InfluxConstants.Organisation));
        add.AddCommitTask(TaskPrecedence.After, new InsertDataBuilder()
            .WithBucketName("${step:create-bucket:${result:name}}")
            .WithOrganisationName(InfluxConstants.Organisation)
            .AddLine("weather,location=us-midwest temperature=82 ${now}"));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        await migration.ExecuteAsync(environment, MigrationDirection.Up);

        var tables = await _influx.Create().GetQueryApi().QueryAsync($"from(bucket:\"{bucketName}\") |> range(start:0)",
            org: InfluxConstants.Organisation);
        var values = tables.SelectMany(table => table.Records.Select(y => y.Values));
        Assert.That(values, Is.Not.Null);
        Assert.That(values.LongCount(), Is.EqualTo(1L));
    }
}