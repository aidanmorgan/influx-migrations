using InfluxDB.Client;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using NUnit.Framework;

namespace InfluxMigrations.Default.Integration;

public class DefaultMigrationRunnerServiceTests
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
    public async Task Empty_DoesNothing()
    {
        var runner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
            {
                History = new MockMigrationHistoryService(),
                Loader = new MockMigrationLoaderService()
            }
        );

        var result = await runner.ExecuteMigrationsAsync(null);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }
    
    [Test]
    public async Task NoMigrations_AppliesAll()
    {
        var migrations = new List<MockMigration>()
        {
            new MockMigration("0001"),
            new MockMigration("0002"),
            new MockMigration("0003"),
        };
            
        var runner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
            {
                History = new MockMigrationHistoryService(),
                Loader = new MockMigrationLoaderService().AddMigrations(migrations.OfType<IMigration>().ToList())
            }
        );

        var result = await runner.ExecuteMigrationsAsync(null);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3));
    }
    
    [Test]
    public async Task SomeHistory_AppliesNew()
    {
        var migrations = new List<MockMigration>()
        {
            new MockMigration("0001"),
            new MockMigration("0002"),
            new MockMigration("0003")
        };
            
        var runner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
            {
                History = new MockMigrationHistoryService().AddHistory("0001").AddHistory("0002"),
                Loader = new MockMigrationLoaderService().AddMigrations(migrations.OfType<IMigration>().ToList())
            }
        );

        var result = await runner.ExecuteMigrationsAsync(null);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Version, Is.EqualTo("0003"));
    }
    
    [Test]
    public async Task MoreMigrationsThanEntries()
    {
        var migrations = new List<MockMigration>()
        {
            new MockMigration("0001"),
            new MockMigration("0002"),
            new MockMigration("0003")
        };
            
        var runner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
            {
                History = new MockMigrationHistoryService().AddHistory("0001").AddHistory("0002").AddHistory("0003").AddHistory("0004").AddHistory("0005"),
                Loader = new MockMigrationLoaderService().AddMigrations(migrations.OfType<IMigration>().ToList())
            }
        );

        var result = await runner.ExecuteMigrationsAsync(null);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }
    
    [Test]
    public async Task TargetVersion_RollForward()
    {
        var migrations = new List<MockMigration>()
        {
            new MockMigration("0001"),
            new MockMigration("0002"),
            new MockMigration("0003"),
            new MockMigration("0004"),
            new MockMigration("0005"),
            new MockMigration("0006"),
            new MockMigration("0007"),
            new MockMigration("0008"),
            new MockMigration("0009")
        };
            
        var runner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
            {
                History = new MockMigrationHistoryService().AddHistory("0001").AddHistory("0002").AddHistory("0003").AddHistory("0004"),
                Loader = new MockMigrationLoaderService().AddMigrations(migrations.OfType<IMigration>().ToList())
            }
        );

        var result = await runner.ExecuteMigrationsAsync(null, "0007");
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result[0].Version, Is.EqualTo("0005"));
        Assert.That(result[1].Version, Is.EqualTo("0006"));
        Assert.That(result[2].Version, Is.EqualTo("0007"));
    }        
    
    [Test]
    public async Task TargetVersion_RollBackwards()
    {
        var migrations = new List<MockMigration>()
        {
            new MockMigration("0001"),
            new MockMigration("0002"),
            new MockMigration("0003"),
            new MockMigration("0004"),
            new MockMigration("0005"),
        };

        var runner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
            {
                History = new MockMigrationHistoryService()
                    .AddHistory("0001")
                    .AddHistory("0002")
                    .AddHistory("0003")
                    .AddHistory("0004")
                    .AddHistory("0005"), 
                Loader = new MockMigrationLoaderService().AddMigrations(migrations.OfType<IMigration>().ToList())
            }
        );

        var result = await runner.ExecuteMigrationsAsync(null, "0003");
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Version, Is.EqualTo("0005"));
        Assert.That(result[1].Version, Is.EqualTo("0004"));
    }        
    
    
}