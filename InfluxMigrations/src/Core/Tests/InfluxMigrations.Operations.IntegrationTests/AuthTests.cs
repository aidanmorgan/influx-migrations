using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;
using InfluxMigrations.Default.IntegrationTests;
using InfluxMigrations.Impl;
using InfluxMigrations.Operations.Auth;
using InfluxMigrations.TestCommon;
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
    public async Task ChangeAuthorizationState_ByBucketAndUserName_Success()
    {
        var result = await CreateBucketAuth();

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithBucketName(result.Bucket.Name)
                .WithUserName(result.User.Name)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }
    
    [Test]
    public async Task ChangeAuthorizationState_ByBucketAndUserNameWithMultipleActions_Success()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithBucketName(result.Bucket.Name)
                .WithUserName(result.User.Name)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }
    

    [Test]
    public async Task ChangeAuthorizationState_ByToken_Success()
    {
        var result = await CreateBucketAuth();

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithToken(result.Authorization.Token)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }
    
    [Test]
    public async Task ChangeAuthorizationState_ByTokenWithMultipleActions_Success()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithToken(result.Authorization.Token)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }    
    
    [Test]
    public async Task ChangeAuthorizationState_ByUser_Success()
    {
        var result = await CreateBucketAuth();

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithUserName(result.User.Name)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }    
    
    [Test]
    public async Task ChangeAuthorizationState_ByUserWithMultipleActions_Success()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithUserName(result.User.Name)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }        

    [Test]
    public async Task ChangeAuthorizationState_ByBucket_Success()
    {
        var result = await CreateBucketAuth();

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithBucketName(result.Bucket.Name)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }    
    
    [Test]
    public async Task ChangeAuthorizationState_ByBucketWithMultipleActions_Success()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithBucketName(result.Bucket.Name)
                .WithState("inactive"));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Inactive));
    }
    
    [Test]
    public async Task ChangeAuthorizationState_ByBucketAndUserName_Rollback()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithUserName(result.User.Name)
                .WithBucketName(result.Bucket.Name)
                .WithState("inactive"));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationRollback(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Active));
    }
    
    [Test]
    public async Task ChangeAuthorizationState_ByUser_Rollback()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithUserName(result.User.Name)
                .WithState("inactive"));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationRollback(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Active));
    }

    [Test]
    public async Task ChangeAuthorizationState_ByBucket_Rollback()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithBucketName(result.Bucket.Name)
                .WithState("inactive"));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationRollback(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Active));
    }
    
    [Test]
    public async Task ChangeAuthorizationState_ByToken_Rollback()
    {
        var result = await CreateBucketAuth(Permission.ActionEnum.Read, Permission.ActionEnum.Write);

        var migration = new Migration("1");
        migration.AddUp("1",
            new ChangeAuthorisationStateBuilder()
                .WithToken(result.Authorization.Token)
                .WithState("inactive"));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationRollback(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded.First().Status, Is.EqualTo(AuthorizationUpdateRequest.StatusEnum.Active));
    }

    [Test]
    public async Task DeleteBucketToken_Success()
    {
        var result = await CreateBucketAuth();

        var migration = new Migration("1");
        migration.AddUp("1",
            new DeleteBucketTokenBuilder().WithBucketName(result.Bucket.Name).WithUserName(result.User.Name));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteBucketToken_Rollback()
    {
        var result = await CreateBucketAuth();

        var migration = new Migration("1");
        migration.AddUp("1",
            new DeleteBucketTokenBuilder().WithBucketName(result.Bucket.Name).WithUserName(result.User.Name));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationRollback(x);

        var loaded = await _influx.Create().GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.User.Id);
        Assert.That(loaded.Count, Is.EqualTo(1));
    }
    
    
    
    record TestAuthCreateResult(
        Organization Organization, 
        InfluxDB.Client.Api.Domain.Bucket Bucket, 
        InfluxDB.Client.Api.Domain.User User,
        Authorization Authorization
    );
    
    private async Task<TestAuthCreateResult> CreateBucketAuth(params Permission.ActionEnum[] actions)
    {
        var bucketName = _random.RandomString();
        var userName = _random.RandomString();
        var orgName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);

        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        if (actions.Length == 0)
        {
            actions = new[] { Permission.ActionEnum.Read };
        }

        var auth = await _influx.Create().GetAuthorizationsApi().CreateAuthorizationAsync(new AuthorizationPostRequest()
        {
            OrgID = org.Id,
            Status = AuthorizationUpdateRequest.StatusEnum.Active,
            UserID = user.Id,
            Permissions = actions.Select(x => new Permission(x, new PermissionResource(type: PermissionResource.TypeBuckets, id: bucket.Id))).ToList()
        });
        
        Assert.That(auth.Token, Is.Not.Null);
        Assert.That(auth.Token, Is.Not.Empty);

        return new TestAuthCreateResult(org, bucket, user, auth);
    }
}