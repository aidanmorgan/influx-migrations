using System.Reflection;
using DotNet.Testcontainers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Core;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using InfluxMigrations.Operations.Auth;
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
    public async Task CreateBucketAuthorizationByName_Success()
    {
        var bucketName = _random.RandomString();
        var userName = _random.RandomString();
        var orgName = _random.RandomString();


        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);

        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var createBucketResult = new CaptureResultBuilder();

        var migration = new Migration("1");
        migration.AddUp("step 1",
                new CreateBucketTokenBuilder()
                    .WithBucketName(bucketName)
                    .WithPermission("read")
                    .WithPermission("write")
                    .WithTokenName("new-user-token")
                    .WithUserName(userName))
            .AddExecuteTask(TaskPrecedence.After, createBucketResult);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var result = (CreateBucketTokenResult?)createBucketResult.Result;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.Not.Null);
    }

    [Test]
    public async Task CreateBucketAuthorizationById_Success()
    {
        var bucketName = _random.RandomString();
        var userName = _random.RandomString();
        var orgName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);

        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var createBucketResult = new CaptureResultBuilder();

        var migration = new Migration("1");
        migration.AddUp("step 1",
                new CreateBucketTokenBuilder()
                    .WithBucketId(bucket.Id)
                    .WithPermission("read")
                    .WithPermission("write")
                    .WithTokenName("new-user-token")
                    .WithUserId(user.Id))
            .AddExecuteTask(TaskPrecedence.After, createBucketResult);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var result = (CreateBucketTokenResult?)createBucketResult.Result;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.Not.Null);
    }

    [Test]
    public async Task CreateBucketAuthorization_Rollback()
    {
        var bucketName = _random.RandomString();
        var userName = _random.RandomString();
        var orgName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);

        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var createBucketResult = new CaptureResultBuilder();

        var migration = new Migration("1");
        migration.AddUp("step 1",
                new CreateBucketTokenBuilder()
                    .WithUserId(user.Id)
                    .WithBucketId(bucket.Id)
                    .WithPermission("read")
                    .WithPermission("write")
                    .WithTokenName("new-user-token"))
            .AddExecuteTask(TaskPrecedence.After, createBucketResult);
        migration.AddUp("step 2", new ForceErrorBuilder().ErrorExecute());

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationRollback(x);

        var result = (CreateBucketTokenResult?)createBucketResult.Result;

        var bucketMembers = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(user.Id);
        Assert.That(bucketMembers.Count, Is.EqualTo(0));
    }

    
}