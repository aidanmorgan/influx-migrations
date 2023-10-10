using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using InfluxMigrations.Operations.Auth;
using NUnit.Framework;

namespace InfluxMigrations.Operations.IntegrationTests;

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

    [Test]
    public async Task DeleteBucketToken_SeparateTokens()
    {
        var bucketName = _random.RandomString();
        var organisationName = _random.RandomString();
        var userName = _random.RandomString();

        var organisation = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(organisationName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, organisation);

        await CreateToken(bucket, user, Permission.ActionEnum.Read);
        await CreateToken(bucket, user, Permission.ActionEnum.Write);

        var migration = new Migration("1.0");
        migration.AddUp("step 1",
            new DeleteBucketTokenPermissionsBuilder()
                .WithBucketId(bucket.Id)
                .WithUserId(user.Id)
                .WithPermission("read"));

        var env = new DefaultEnvironmentExecutionContext(_influx);
        await env.Initialise();
        
        await migration.ExecuteAsync(env, MigrationDirection.Up);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(user.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Permissions.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Permissions.First().Action, Is.EqualTo(Permission.ActionEnum.Write));
    }
    
    [Test]
    public async Task DeleteBucketToken_IndividualToken()
    {
        var bucketName = _random.RandomString();
        var organisationName = _random.RandomString();
        var userName = _random.RandomString();

        var organisation = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(organisationName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, organisation);

        await CreateToken(bucket, user, Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1.0");
        migration.AddUp("step 1",
            new DeleteBucketTokenPermissionsBuilder()
                .WithBucketId(bucket.Id)
                .WithUserId(user.Id)
                .WithPermission("read"));

        var env = new DefaultEnvironmentExecutionContext(_influx);
        await env.Initialise();
        
        await migration.ExecuteAsync(env, MigrationDirection.Up);

        var permissions = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(user.Id);
        Assert.That(permissions.Count, Is.EqualTo(1));
        Assert.That(permissions.First().Permissions.Count, Is.EqualTo(1));
        Assert.That(permissions.First().Permissions.First().Action, Is.EqualTo(Permission.ActionEnum.Write));
    }    
    
    [Test]
    public async Task DeleteBucketToken_MultipleToken_DeleteAll()
    {
        var bucketName = _random.RandomString();
        var organisationName = _random.RandomString();
        var userName = _random.RandomString();

        var organisation = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(organisationName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, organisation);

        await CreateToken(bucket, user, Permission.ActionEnum.Read);
        await CreateToken(bucket, user, Permission.ActionEnum.Write);

        var migration = new Migration("1.0");
        migration.AddUp("step 1",
            new DeleteBucketTokenPermissionsBuilder()
                .WithBucketId(bucket.Id)
                .WithUserId(user.Id)
                .WithPermission("read").WithPermission("write"));

        var env = new DefaultEnvironmentExecutionContext(_influx);
        await env.Initialise();
        
        await migration.ExecuteAsync(env, MigrationDirection.Up);

        var permissions = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(user.Id);
        Assert.That(permissions.Count, Is.EqualTo(0));
    }       
    
    [Test]
    public async Task DeleteBucketToken_IndividualToken_DeleteAll()
    {
        var bucketName = _random.RandomString();
        var organisationName = _random.RandomString();
        var userName = _random.RandomString();

        var organisation = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(organisationName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, organisation);

        await CreateToken(bucket, user, Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1.0");
        migration.AddUp("step 1",
            new DeleteBucketTokenPermissionsBuilder()
                .WithBucketId(bucket.Id)
                .WithUserId(user.Id)
                .WithPermission("read").WithPermission("write"));

        var env = new DefaultEnvironmentExecutionContext(_influx);
        await env.Initialise();
        
        await migration.ExecuteAsync(env, MigrationDirection.Up);

        var permissions = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(user.Id);
        Assert.That(permissions.Count, Is.EqualTo(0));
    }        

    private async Task<Authorization> CreateToken(InfluxDB.Client.Api.Domain.Bucket bucket, InfluxDB.Client.Api.Domain.User user, params Permission.ActionEnum[] actions)
    {
        var permissions = actions.Select(x => new Permission(x, new PermissionResource(type: PermissionResource.TypeBuckets, id: bucket.Id)));
        
        var token = await _influx.Create().GetAuthorizationsApi().CreateAuthorizationAsync(
            new AuthorizationPostRequest(
                bucket.OrgID,
                user.Id,
                permissions.ToList())
        );

        return token;
    }
    
    
}