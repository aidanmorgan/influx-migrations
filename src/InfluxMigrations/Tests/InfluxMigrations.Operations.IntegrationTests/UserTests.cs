using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Core;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Impl;
using InfluxMigrations.Operations.User;
using InfluxMigrations.TestCommon;
using NUnit.Framework;

namespace InfluxMigrations.Operations.IntegrationTests;

public class UserTests
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
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();


        CaptureResultBuilder capture = new CaptureResultBuilder();

        var migration = new Migration("0001");
        migration.AddUp("create-user", new CreateUserBuilder()
                .WithUsername(userName)
                .WithPassword(password))
            .AddExecuteTask(TaskPrecedence.After, capture);

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        var user = await _influx.Create()
            .GetUsersApi()
            .FindUserByIdAsync(capture.As<CreateUserResult>().Id);

        Assert.That(user, Is.Not.Null);
        Assert.That(user.Name, Is.EqualTo(userName));
    }

    [Test]
    public async Task CreateUser_Rollback()
    {
        var username = _random.RandomString();
        var capture = new CaptureResultBuilder();

        var migration = new Migration("1");
        migration.AddUp("step 1", new CreateUserBuilder().WithUsername(username))
            .AddExecuteTask(TaskPrecedence.After, capture);
        
        migration.AddUp("step 2", new ForceErrorBuilder().ErrorExecute());

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();


        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationRollback(x);
        
        Assert.That(capture.Result, Is.Not.Null);
        Assert.That(capture.Result, Is.InstanceOf<CreateUserResult>());

        var id = ((CreateUserResult)capture.Result).Id;

        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetUsersApi().FindUserByIdAsync(id);
        });
    }

    [Test]
    public async Task RemoveUserById_Success()
    {
        var userName = _random.RandomString();

        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("1");
        migration.AddUp("step 1", new DeleteUserBuilder().WithUserId(user.Id));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();


        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        NunitExtensions.AssertMigrationSuccess(x);

        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetUsersApi().FindUserByIdAsync(user.Id);
        });
    }
    
    [Test]
    public async Task RemoveUserByName_Success()
    {
        var userName = _random.RandomString();

        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("1");
        migration.AddUp("step 1", new DeleteUserBuilder().WithUserName(userName));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();


        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        NunitExtensions.AssertMigrationSuccess(x);

        Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _influx.Create().GetUsersApi().FindUserByIdAsync(user.Id);
        });
    }

    [Test]
    public async Task RemoveUser_Rollback()
    {
        var userName = _random.RandomString();

        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("1");
        migration.AddUp("step 1", new DeleteUserBuilder().WithUserName(userName));
        migration.AddUp("step 2", new ForceErrorBuilder().ErrorExecute());

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();


        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);
        NunitExtensions.AssertMigrationRollback(x);

        var loaded = await _influx.Create().GetUsersApi().FindUserByIdAsync(user.Id);
        Assert.That(loaded, Is.Not.Null);
    }
    
}