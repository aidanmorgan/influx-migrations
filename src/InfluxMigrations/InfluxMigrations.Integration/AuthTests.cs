using DotNet.Testcontainers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Commands.Auth;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Commands.Organisation;
using InfluxMigrations.Commands.User;
using InfluxMigrations.Core;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Testcontainers.InfluxDb;

namespace InfluxMigrations.IntegrationTests;

public class AuthTests
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
    public async Task CreateUser()
    {
        var userName = _random.RandomString();
        var password = _random.RandomString();
        
        var environment = new DefaultEnvironmentContext(_influx, new TextWriterMigrationLoggerFactory(Console.Out));

        CaptureResultBuilder createdUserResult = new CaptureResultBuilder();

        var migration = new Migration("0001");
        migration.AddUp("create-user", new CreateUserBuilder()
                .WithUsername(userName)
                .WithPassword(password))
            .AddExecuteTask(TaskPrecedence.After, createdUserResult);

        await migration.ExecuteAsync(environment, MigrationDirection.Up);

        var user = await _influx.Create()
            .GetUsersApi()
            .FindUserByIdAsync(createdUserResult.As<CreateUserResult>().Id);

        Assert.That(user, Is.Not.Null);
        Assert.That(user.Name, Is.EqualTo(userName));
    }

    [Test]
    public async Task CreateBucketAuthorization_SpecificUser()
    {
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        Assert.That(org, Is.Not.Null);

        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync("specific_user_bucket", org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync("username");

        var createBucketResult = new CaptureResultBuilder();

        var migration = new Migration("0002");
        migration.AddUp("create-bucket-token",
                new CreateBucketTokenBuilder()
                    .WithBucketName("specific_user_bucket")
                    .WithPermission("read")
                    .WithPermission("write")
                    .WithTokenName("new-user-token")
                    .WithUserName("username"))
            .AddExecuteTask(TaskPrecedence.After, createBucketResult);

        var environment = new DefaultEnvironmentContext(_influx, new TextWriterMigrationLoggerFactory(Console.Out));
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var result = (CreateBucketTokenResult?)createBucketResult.Result;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.Not.Null);
    }

    
}