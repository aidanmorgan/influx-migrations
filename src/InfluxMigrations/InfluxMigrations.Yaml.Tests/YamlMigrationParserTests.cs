using InfluxDB.Client;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;

namespace InfluxMigrations.Yaml.Tests;

public class Tests
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
    public async Task TestCreateBucketLoader()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseFile("Files/0001.yml");
        
        Assert.IsNotNull(result);

        await result.ExecuteAsync(new DefaultEnvironmentContext(_influx) , MigrationDirection.Up);
    }
}