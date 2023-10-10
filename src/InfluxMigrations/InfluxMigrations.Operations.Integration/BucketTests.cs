using System.Runtime.CompilerServices;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using InfluxMigrations.Operations.Auth;
using InfluxMigrations.Operations.Bucket;
using NUnit.Framework;
using static InfluxMigrations.Default.Integration.NunitExtensions;

namespace InfluxMigrations.Operations.IntegrationTests;

public class BucketTests
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
    public async Task CreateBucket_Success()
    {
        var bucketName = _random.RandomString(8);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var resultCapture = new CaptureResultBuilder();

        var migration = new Migration("0001");
        var step = migration.AddUp("create-bucket",
            new CreateBucketBuilder()
                .WithBucketName(bucketName)
                .WithOrganisation(InfluxConstants.Organisation));
        step.AddExecuteTask(TaskPrecedence.After, resultCapture);

        var mig = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        var result = resultCapture.As<CreateBucketResult>();

        AssertMigrationSuccess(mig);
        Assert.That(result, Is.Not.Null);
        
        var loaded = await _influx.Create().GetBucketsApi().FindBucketByIdAsync(result.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.Name, Is.EqualTo(bucketName));
    }
    
    [Test]
    public async Task CreateBucket_Rollback() 
    {
        var bucketName = _random.RandomString(8);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise(new NoOpMigrationRunnerLogger());

        var resultCapture = new CaptureResultBuilder();

        var migration = new Migration("0001");
        var step = migration.AddUp("create-bucket",
            new CreateBucketBuilder()
                .WithBucketName(bucketName)
                .WithOrganisation(InfluxConstants.Organisation)).AddExecuteTask(TaskPrecedence.After, resultCapture);
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());

        var mig = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        
        AssertMigrationRollback(mig);

        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetBucketsApi()
                .FindBucketByIdAsync(((CreateBucketResult)resultCapture.Result).Id);
        });
    }

    [Test]
    public async Task DeleteBucket_ByName()
    {
        var bucketName = _random.RandomString(8);

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync())
            .FirstOrDefault(x => x.Name == InfluxConstants.Organisation);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var migration = new Migration("0001");
        var step = migration.AddUp("delete-bucket-name", new DeleteBucketBuilder().WithBucketName(bucketName));

        var mig = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(mig);
        
        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetBucketsApi().FindBucketByIdAsync(bucket.Id);
        });
    }

    [Test]
    public async Task DeleteBucket_ById()
    {
        var bucketName = _random.RandomString(8);

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(x =>
            x.Name == InfluxConstants.Organisation);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var migration = new Migration("0001");
        var step = migration.AddUp("delete-bucket-name", new DeleteBucketBuilder().WithBucketId(bucket.Id));

        var mig = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        AssertMigrationSuccess(mig);

        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetBucketsApi().FindBucketByIdAsync(bucket.Id);
        });
    }

    [Test]
    public async Task DeleteBucket_RollbackSuccess()
    {
        var bucketName = _random.RandomString(8);

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(x =>
            x.Name == InfluxConstants.Organisation);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org);

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var migration = new Migration("0001");
        migration.AddUp("delete-bucket-name", new DeleteBucketBuilder().WithBucketId(bucket.Id));
        migration.AddUp("force-throw", new ForceErrorBuilder().ErrorExecute());

        var mig = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        
        AssertMigrationRollback(mig);


        var loaded = await _influx.Create().GetBucketsApi().FindBucketByIdAsync(bucket.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.Name, Is.EqualTo(bucketName));
    }
    
    [Test]
    public async Task AddMemberToBucketByName_Success()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var result = new CaptureResultBuilder();
        
        var migration = new Migration("0001");
        migration
            .AddUp("1", new AddMemberToBucketBuilder()
                .WithBucketName(bucketName)
                .WithUserName(userName))
            .AddExecuteTask(TaskPrecedence.After, result);
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);
        Assert.That(result.Result, Is.Not.Null);
        Assert.That(result.Result, Is.InstanceOf<EmptyExecuteResult>());

        var members = await _influx.Create().GetBucketsApi().GetMembersAsync(bucket.Id);
        Assert.That(members.Any(y => y.Id == user.Id));
    }
    
    [Test]
    public async Task AddMemberToBucketById_Success()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var result = new CaptureResultBuilder();
        
        var migration = new Migration("0001");
        migration
            .AddUp("1", new AddMemberToBucketBuilder()
                .WithBucketId(bucket.Id)
                .WithUserId(user.Id))
            .AddExecuteTask(TaskPrecedence.After, result);
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);
        Assert.That(result.Result, Is.Not.Null);
        Assert.That(result.Result, Is.InstanceOf<EmptyExecuteResult>());

        var members = await _influx.Create().GetBucketsApi().GetMembersAsync(bucket.Id);
        Assert.That(members.Any(y => y.Id == user.Id));
    }
    
    
    [Test]
    public async Task AddMemberToBucket_Rollback()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        
        var migration = new Migration("0001");
        migration
            .AddUp("1", new AddMemberToBucketBuilder()
                .WithBucketName(bucketName)
                .WithUserName(userName));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
            
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationRollback(x);

        var members = await _influx.Create().GetBucketsApi().GetMembersAsync(bucket.Id);
        Assert.That(members.All(y => y.Id != user.Id));
    }

    [Test]
    public async Task RemoveMemberFromBucketByName_Success()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var role = await _influx.Create().GetBucketsApi().AddMemberAsync(user.Id, bucket.Id);
        
        var migration = new Migration("0001");
        migration
            .AddUp("1", new RemoveMemberFromBucketBuilder()
                .WithBucketName(bucketName)
                .WithUserName(userName));
            
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var members = await _influx.Create().GetBucketsApi().GetMembersAsync(bucket.Id);
        Assert.That(members.All(y => y.Id != user.Id));
    }
    
    [Test]
    public async Task RemoveMemberFromBucketById_Success()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var role = await _influx.Create().GetBucketsApi().AddMemberAsync(user.Id, bucket.Id);

        var migration = new Migration("0001");
        migration
            .AddUp("1", new RemoveMemberFromBucketBuilder()
                .WithBucketId(bucket.Id)
                .WithUserId(user.Id));
            
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var members = await _influx.Create().GetBucketsApi().GetMembersAsync(bucket.Id);
        Assert.That(members.All(y => y.Id != user.Id));
    }

    [Test]
    public async Task RemoveMemberFromBucket_Rollback()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var role = await _influx.Create().GetBucketsApi().AddMemberAsync(user.Id, bucket.Id);
        
        var migration = new Migration("0001");
        migration
            .AddUp("1", new RemoveMemberFromBucketBuilder()
                .WithBucketId(bucket.Id)
                .WithUserId(user.Id));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationRollback(x);

        var members = await _influx.Create().GetBucketsApi().GetMembersAsync(bucket.Id);
        Assert.That(members.Any(y => y.Id == user.Id));
    }

    [Test]
    public async Task AddOwnerToBucketByName_Success()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("0001");
        migration.AddUp("1", new AddOwnerToBucketBuilder().WithBucketName(bucket.Name).WithUserName(user.Name));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetBucketsApi().GetOwnersAsync(bucket.Id);
        Assert.That(owners.Any(x => x.Id == user.Id));
    }
    
    [Test]
    public async Task AddOwnerToBucketById_Success()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("0001");
        migration.AddUp("1", new AddOwnerToBucketBuilder().WithBucketId(bucket.Id).WithUserId(user.Id));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetBucketsApi().GetOwnersAsync(bucket.Id);
        Assert.That(owners.Any(x => x.Id == user.Id));
    }

    [Test]
    public async Task AddOwnerToBucketId_Rollback()
    {
        var userName = _random.RandomString();
        var bucketName = _random.RandomString();
        
        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org!.Id);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("0001");
        migration.AddUp("1", new AddOwnerToBucketBuilder().WithBucketId(bucket.Id).WithUserId(user.Id));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationRollback(x);

        var owners = await _influx.Create().GetBucketsApi().GetOwnersAsync(bucket.Id);
        Assert.That(owners.All(x => x.Id != user.Id));
    }

    [Test]
    public async Task RemoveOwnerFromBucketById_Success()
    {
        var bucketName = _random.RandomString();
        var userName = _random.RandomString();

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);

        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org.Id);
        await _influx.Create().GetBucketsApi().AddOwnerAsync(user.Id, bucket.Id);

        var migration = new Migration("1");
        migration.AddUp("1", new RemoveOwnerFromBucketBuilder().WithBucketId(bucket.Id).WithUserId(user.Id));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        
        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetBucketsApi().GetOwnersAsync(bucket.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.False);
    }

    [Test]
    public async Task RemoveOwnerFromBucketByName_Success()
    {
        var bucketName = _random.RandomString();
        var userName = _random.RandomString();

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);

        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org.Id);
        await _influx.Create().GetBucketsApi().AddOwnerAsync(user.Id, bucket.Id);

        var migration = new Migration("1");
        migration.AddUp("1", new RemoveOwnerFromBucketBuilder().WithBucketName(bucketName).WithUserName(userName));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        
        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetBucketsApi().GetOwnersAsync(bucket.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.False);
    }

    [Test]
    public async Task RemoveOwnerFromBucket_Rollback()
    {
        var bucketName = _random.RandomString();
        var userName = _random.RandomString();

        var org = (await _influx.Create().GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
            x => string.Equals(x?.Name, InfluxConstants.Organisation, StringComparison.InvariantCultureIgnoreCase),
            null);

        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        var bucket = await _influx.Create().GetBucketsApi().CreateBucketAsync(bucketName, org.Id);
        await _influx.Create().GetBucketsApi().AddOwnerAsync(user.Id, bucket.Id);

        var migration = new Migration("1");
        migration.AddUp("1", new RemoveOwnerFromBucketBuilder().WithBucketName(bucketName).WithUserName(userName));
        migration.AddUp("2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        
        AssertMigrationRollback(x);

        var owners = await _influx.Create().GetBucketsApi().GetOwnersAsync(bucket.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.True);
    }


}