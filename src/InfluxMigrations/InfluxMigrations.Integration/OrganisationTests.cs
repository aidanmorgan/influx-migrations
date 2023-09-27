using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using InfluxMigrations.Operations.Organisation;
using NUnit.Framework;
using NUnit.Framework.Internal;
using static InfluxMigrations.Default.Integration.NunitExtensions;

namespace InfluxMigrations.IntegrationTests;

public class OrganisationTests
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
    public async Task AddOwnerToOrganisationById_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        
        var migration = new Migration("1");
        migration.AddUp("step1",
            new AddOwnerToOrganisationBuilder().WithUserId(user.Id).WithOrganisationId(org.Id));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();
        
        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetOrganizationsApi().GetOwnersAsync(org.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.True);
    }
    
    [Test]
    public async Task AddOwnerToOrganisationByName_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        
        var migration = new Migration("1");
        migration.AddUp("step1",
            new AddOwnerToOrganisationBuilder().WithUserName(userName).WithOrganisationName(orgName));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetOrganizationsApi().GetOwnersAsync(org.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.True);
    }

    [Test]
    public async Task AddOwnerToOrganisation_Rollback()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        
        var migration = new Migration("1");
        migration.AddUp("step 1",
            new AddOwnerToOrganisationBuilder().WithUserName(userName).WithOrganisationName(orgName));
        migration.AddUp("step 2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationRollback(x);

        var owners = await _influx.Create().GetOrganizationsApi().GetOwnersAsync(org.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.False);
    }

    [Test]
    public async Task RemoveOwnerFromOrganisationById_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        await _influx.Create().GetOrganizationsApi().AddOwnerAsync(user.Id, org.Id);

        var migration = new Migration("1");
        migration.AddUp("step1",
            new RemoveOwnerFromOrganisationBuilder().WithUserId(user.Id).WithOrganisationId(org.Id));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetOrganizationsApi().GetOwnersAsync(org.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.False);
    }

    [Test]
    public async Task RemoveOwnerFromOrganisationByName_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        await _influx.Create().GetOrganizationsApi().AddOwnerAsync(user.Id, org.Id);

        var migration = new Migration("1");
        migration.AddUp("step1",
            new RemoveOwnerFromOrganisationBuilder().WithUserName(userName).WithOrganisationName(orgName));
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var owners = await _influx.Create().GetOrganizationsApi().GetOwnersAsync(org.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.False);
    }

    [Test]
    public async Task RemoveOwnerFromOrganisation_Rollback()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        await _influx.Create().GetOrganizationsApi().AddOwnerAsync(user.Id, org.Id);

        var migration = new Migration("1");
        migration.AddUp("step 1",
            new RemoveOwnerFromOrganisationBuilder().WithUserName(userName).WithOrganisationName(orgName));
        migration.AddUp("step 2", new ForceErrorBuilder().ErrorExecute());
        
        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationRollback(x);

        var owners = await _influx.Create().GetOrganizationsApi().GetOwnersAsync(org.Id);
        Assert.That(owners.Any(y => y.Id == user.Id), Is.True);
    }

    [Test]
    public async Task AddUserToOrganisationById_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("1");
        migration.AddUp("step 1", new AddUserToOrganisationBuilder().WithOrganisationId(org.Id).WithUserId(user.Id));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var members = await _influx.Create().GetOrganizationsApi().GetMembersAsync(org.Id);
        Assert.That(members.Any(y => y.Id == user.Id), Is.True);
    }
    
    [Test]
    public async Task AddUserToOrganisationByName_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("1");
        migration.AddUp("step 1", new AddUserToOrganisationBuilder().WithOrganisationName(orgName).WithUsername(userName));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var members = await _influx.Create().GetOrganizationsApi().GetMembersAsync(org.Id);
        Assert.That(members.Any(y => y.Id == user.Id), Is.True);
    }

    [Test]
    public async Task AddUserToOrganisation_Rollback()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);

        var migration = new Migration("1");
        migration.AddUp("step 1", new AddUserToOrganisationBuilder().WithOrganisationName(orgName).WithUsername(userName));
        migration.AddUp("step 2", new ForceErrorBuilder().ErrorExecute());

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationRollback(x);

        var members = await _influx.Create().GetOrganizationsApi().GetMembersAsync(org.Id);
        Assert.That(members.Any(y => y.Id == user.Id), Is.False);
    }

    [Test]
    public async Task RemoveUserFromOrganisationById_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        await _influx.Create().GetOrganizationsApi().AddMemberAsync(user.Id, org.Id);

        var migration = new Migration("1");
        migration.AddUp("step 1", new RemoveUserFromOrganisationBuilder().WithOrganisationId(org.Id).WithUserId(user.Id));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var members = await _influx.Create().GetOrganizationsApi().GetMembersAsync(org.Id);
        Assert.That(members.Any(y => y.Id == user.Id), Is.False);
        
    }

    [Test]
    public async Task RemoveUserFromOrganisationByName_Success()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        await _influx.Create().GetOrganizationsApi().AddMemberAsync(user.Id, org.Id);

        var migration = new Migration("1");
        migration.AddUp("step 1", new RemoveUserFromOrganisationBuilder().WithOrganisationName(orgName).WithUsername(userName));

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationSuccess(x);

        var members = await _influx.Create().GetOrganizationsApi().GetMembersAsync(org.Id);
        Assert.That(members.Any(y => y.Id == user.Id), Is.False);
    }

    [Test]
    public async Task RemoveUserFromOrganisation_Rollback()
    {
        var orgName = _random.RandomString();
        var userName = _random.RandomString();

        var org = await _influx.Create().GetOrganizationsApi().CreateOrganizationAsync(orgName);
        var user = await _influx.Create().GetUsersApi().CreateUserAsync(userName);
        await _influx.Create().GetOrganizationsApi().AddMemberAsync(user.Id, org.Id);

        var migration = new Migration("1");
        migration.AddUp("step 1", new RemoveUserFromOrganisationBuilder().WithOrganisationName(orgName).WithUsername(userName));
        migration.AddUp("step 2", new ForceErrorBuilder().ErrorExecute());

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();

        var x = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        AssertMigrationRollback(x);

        var members = await _influx.Create().GetOrganizationsApi().GetMembersAsync(org.Id);
        Assert.That(members.Any(y => y.Id == user.Id), Is.True);
    }
    
    
}