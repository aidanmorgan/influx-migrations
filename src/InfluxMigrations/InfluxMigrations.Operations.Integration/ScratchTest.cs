using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using InfluxMigrations.IntegrationCommon;
using InfluxMigrations.Operations.Auth;
using InfluxMigrations.Operations.Bucket;
using InfluxMigrations.Operations.Organisation;
using InfluxMigrations.Operations.User;
using InfluxMigrations.Tasks;
using NUnit.Framework;

namespace InfluxMigrations.Operations.IntegrationTests;

public class ScratchTest
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
    public async Task ChainTest()
    {
        var organisationName = _random.RandomString(16);
        var randomBucketNames = new List<string>
        {
            _random.RandomString(16),
            _random.RandomString(16),
            _random.RandomString(16)
        };

        var randomUserNames = new List<string>
        {
            _random.RandomString(16),
            _random.RandomString(16),
            _random.RandomString(16)
        };

        var environment = new DefaultEnvironmentExecutionContext(_influx);
        await environment.Initialise();


        var migration = new Migration("0001");
        migration.AddUp("create-organisation", new CreateOrganisationBuilder().WithName(organisationName));
        migration.AddUp("create-bucket-1", new CreateBucketBuilder()
            .WithOrganisation(organisationName)
            .WithBucketName(randomBucketNames[0]));
        migration.AddUp("create-user1", new CreateUserBuilder()
            .WithUsername(randomUserNames[0])
            .WithPassword("p@ssw0rd"));
        migration.AddUp("add-user1-to-organisation", new AddUserToOrganisationBuilder()
            .WithOrganisationId("${step:create-organisation:${result:id}}")
            .WithUserId("${step:create-user1:${result:id}}"));
        migration.AddUp("assign-testuser1-test-bucket-1", new CreateBucketTokenBuilder()
            .WithBucketId("${step:create-bucket-1:${result:id}}")
            .WithUserId("${step:create-user1:${result:id}}")
            .WithPermission("read"));

        migration.AddUp("create-bucket-2", new CreateBucketBuilder()
            .WithOrganisation(organisationName)
            .WithBucketName(randomBucketNames[1]));
        migration.AddUp("create-user2", new CreateUserBuilder()
            .WithUsername(randomUserNames[1])
            .WithPassword("p@ssw0rd"));
        migration.AddUp("add-user2-to-organisation", new AddUserToOrganisationBuilder()
            .WithOrganisationId("${step:create-organisation:${result:id}}")
            .WithUserId("${step:create-user2:${result:id}}"));
        migration.AddUp("assign-testuser2-test-bucket-2", new CreateBucketTokenBuilder()
            .WithBucketId("${step:create-bucket-2:${result:id}}")
            .WithUserId("${step:create-user2:${result:id}}")
            .WithPermission("read"));


        migration.AddUp("create-bucket-3", new CreateBucketBuilder()
            .WithOrganisation(organisationName)
            .WithBucketName(randomBucketNames[2]));
        migration.AddUp("create-user3", new CreateUserBuilder()
            .WithUsername(randomUserNames[2])
            .WithPassword("p@ssw0rd"));
        migration.AddUp("add-user3-to-organisation", new AddUserToOrganisationBuilder()
            .WithOrganisationId("${step:create-organisation:${result:id}}")
            .WithUserId("${step:create-user3:${result:id}}"));
        migration.AddUp("assign-testuser3-test-bucket-3", new CreateBucketTokenBuilder()
            .WithBucketId("${step:create-bucket-3:${result:id}}")
            .WithUserId("${step:create-user3:${result:id}}")
            .WithPermission("read"));

        var stringWriter = new StringWriter();
        var echoTask = new EchoTaskBuilder().WithWriter(stringWriter);
        migration.AddAfterTask(echoTask
            .WithString(
                "Username: ${step:create-user1:${result:name}} Bucket: ${step:create-bucket-1:${result:name}} Token: ${step:assign-testuser1-test-bucket-1:${result:token}}")
            .WithString(
                "Username: ${step:create-user2:${result:name}} Bucket: ${step:create-bucket-2:${result:name}} Token: ${step:assign-testuser2-test-bucket-2:${result:token}}")
            .WithString(
                "Username: ${step:create-user3:${result:name}} Bucket: ${step:create-bucket-3:${result:name}} Token: ${step:assign-testuser3-test-bucket-3:${result:token}}"));


        var result = await migration.ExecuteAsync(environment, MigrationDirection.Up);

        Assert.That(result.Success, Is.EqualTo(true));
        Assert.That(result.Inconsistent, Is.EqualTo(false));
        Assert.That(result.Issues.Count, Is.EqualTo(0));

        Console.WriteLine(stringWriter);
        var writer = stringWriter.ToString()
            .Split("\n")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();
        
        Assert.That(writer.Count, Is.EqualTo(3));
        Assert.That(writer[0], Does.Contain(randomUserNames[0]));
        Assert.That(writer[1], Does.Contain(randomUserNames[1]));
        Assert.That(writer[2], Does.Contain(randomUserNames[2]));
        
        Assert.That(writer[0], Does.Contain(randomBucketNames[0]));
        Assert.That(writer[1], Does.Contain(randomBucketNames[1]));
        Assert.That(writer[2], Does.Contain(randomBucketNames[2]));
    }
}

