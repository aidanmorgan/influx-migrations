using InfluxDB.Client;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using NUnit.Framework;

namespace InfluxMigrations.Default.Integration;

public class DefaultMigrationHistoryServiceTests
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
    public async Task TestLoadEmptyHistory_BucketDoesNotExist()
    {
        var loader = new DefaultMigrationHistoryService(_influx, new DefaultMigrationHistoryOptions()
        {
            OrganisationName = InfluxConstants.Organisation,
            BucketName = InfluxConstants.HistoryBucket
        });

        var result = await loader.LoadMigrationHistoryAsync();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task TestLoadEmptyHistory_BucketExists()
    {
        var opts = new DefaultMigrationHistoryOptions()
        {
            OrganisationName = InfluxConstants.Organisation,
            BucketName = InfluxConstants.HistoryBucket
        };

        await DefaultMigrationHistoryService.CreateHistoryBucketIfNotExists(_influx.Create(), opts);
        var loader = new DefaultMigrationHistoryService(_influx, opts);

        var result = await loader.LoadMigrationHistoryAsync();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }


    [Test]
    public async Task TestHistoryBucketCreatedIfNotExists()
    {
        var historyBucket = await DefaultMigrationHistoryService.CreateHistoryBucketIfNotExists(_influx.Create(),
            new DefaultMigrationHistoryOptions()
            {
                BucketName = InfluxConstants.HistoryBucket,
                OrganisationName = InfluxConstants.Organisation
            });

        Assert.That(historyBucket, Is.Not.Null);
    }

    [Test]
    public async Task TestInsertHistory()
    {
        var loader = new DefaultMigrationHistoryService(_influx, new DefaultMigrationHistoryOptions()
        {
            OrganisationName = InfluxConstants.Organisation,
            BucketName = InfluxConstants.HistoryBucket,
            MeasurementName = InfluxConstants.HistoryMeasurement
        });

        var now = DateTimeOffset.UtcNow;

        await loader.SaveMigrationHistoryAsync(new MigrationHistory("0001", MigrationDirection.Up, now, false));

        var loaded = await loader.LoadMigrationHistoryAsync();
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.Count, Is.EqualTo(1));

        var history = loaded[0];
        Assert.That(history.Direction, Is.EqualTo(MigrationDirection.Up));
        Assert.That(history.Success, Is.EqualTo(false));
        Assert.That(history.Version, Is.EqualTo("0001"));
        Assert.That(history.Timestamp.ToUnixTimeMilliseconds(), Is.EqualTo(now.ToUnixTimeMilliseconds()));
    }
}