using DotNet.Testcontainers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Commands.Auth;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Commands.User;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using Microsoft.Extensions.Logging;
using Testcontainers.InfluxDb;

namespace InfluxMigrations.IntegrationTests;

public class AuthManagementTests
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
    public async Task CreateUser()
    {
        var environment = new DefaultEnvironmentContext(_influx, new TextWriterMigrationLoggerFactory(Console.Out));

        CaptureResultBuilder createdUserResult = new CaptureResultBuilder();

        var migration = new Migration("0001");
        migration.AddUp("create-user", new CreateUserBuilder()
                .WithUsername("testuser1")
                .WithPassword("p@ssw0rd"))
            .AddExecuteTask(createdUserResult);

        await migration.ExecuteAsync(environment, MigrationDirection.Up);

        var user = await _influx.Create()
            .GetUsersApi()
            .FindUserByIdAsync(createdUserResult.As<CreateUserResult>().Id);

        Assert.That(user, Is.Not.Null);
        Assert.That(user.Name, Is.EqualTo("testuser1"));
    }
    
    [Test]
    public async Task CreateBucketAuthorization_SpecificUser()
    {
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase), null);
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
            .AddExecuteTask(createBucketResult);
        
        var environment = new DefaultEnvironmentContext(_influx, new TextWriterMigrationLoggerFactory(Console.Out));
        await migration.ExecuteAsync(environment, MigrationDirection.Up);
        
        var result = (CreateBucketTokenResult?)createBucketResult.Result;
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.Not.Null);
    }
}