using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using InfluxDB.Client;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Commands.Setup;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.Outputs;

namespace InfluxMigrations.IntegrationTests;

public class OnboardingTests
{
    private IInfluxFactory _influx;
    private IContainerService _container;
    private Random _random;

    [SetUp]
    public void SetUp()
    {
        // we have to use a different startup here to test this scenario in comparison to the others
        _container =
            new Builder().UseContainer()
                .UseImage("influxdb:latest")
                .ExposePort(8086, 8086)
                .Build()
                .Start();
        
        _container.WaitForMessageInLogs("service=tcp-listener transport=http addr=:8086 port=8086", (long)TimeSpan.FromSeconds(30).TotalMilliseconds);
        
        _random = new Random();

        _influx = new InfluxFactory().WithHost("http://localhost:8086");
    }

    [TearDown]
    public void TearDown()
    { 
        _container.Stop();
        _container.Remove(force: true);
        _container.Dispose();
    }

    [Test]
    public async Task TestOnboarding()
    {
        var org = _random.RandomString(8);
        var user = _random.RandomString(8);
        var pass = _random.RandomString(8);
        var bucket = _random.RandomString(8);
        var token = _random.RandomString(32);
        
        
        var migration = new Migration("0001");
        var add = migration.AddUp("onboarding",
            new OnboardingBuilder()
                .WithOrganisation(org)
                .WithUsername(user)
                .WithPassword(pass)
                .WithBucket(bucket)
                .WithAdminToken("${env:admin_token}"));

        var result = await migration.ExecuteAsync(new DefaultEnvironmentContext(_influx).Set("admin_token", token), MigrationDirection.Up);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.EqualTo(true));
        Assert.That(result.Inconsistent, Is.EqualTo(false));
    }

    [Test]
    public async Task DoOnboarding_ThenCreateBucket()
    {
        var org = _random.RandomString(8);
        var user = _random.RandomString(8);
        var pass = _random.RandomString(8);
        var bucket = _random.RandomString(8);
        var token = _random.RandomString(32);
        
        
        var migration = new Migration("0001");
        var add = migration.AddUp("onboarding",
            new OnboardingBuilder()
                .WithOrganisation(org)
                .WithUsername(user)
                .WithPassword(pass)
                .WithBucket(bucket)
                .WithAdminToken("${env:admin_token}"));
        var b2 = migration.AddUp("another-bucket",
            new CreateBucketBuilder()
                .WithBucketName(_random.RandomString(8))
                .WithOrganisation(org));
        migration.AddTask(new EchoTaskBuilder().WithString("Admin token: ${env:admin_token}"));

        var result = await migration.ExecuteAsync(new DefaultEnvironmentContext(_influx).Set("admin_token", token), MigrationDirection.Up);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.EqualTo(true));
        Assert.That(result.Inconsistent, Is.EqualTo(false));
    }
    
    
}