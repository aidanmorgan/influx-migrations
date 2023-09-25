using InfluxMigrations.Commands.Auth;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Outputs;
using InfluxMigrations.Yaml.Parsers;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.Tests;

public class EchoTaskParserTests
{
    [Test]
    public async Task EchoTaskParser_MigrationLevelMultipleLines_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: echo
      expr:
        - Hello world
        - I like cheese
        - Oobn
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.AfterTasks.Count, Is.EqualTo(1));

        var task = result.AfterTasks[0] as EchoTaskBuilder;

        Assert.That(task, Is.Not.Null);
        Assert.That(task.Lines.Count, Is.EqualTo(3));
        Assert.That(task.Lines[0], Is.EqualTo("Hello world"));
        Assert.That(task.Lines[1], Is.EqualTo("I like cheese"));
        Assert.That(task.Lines[2], Is.EqualTo("Oobn"));
    }

    [Test]
    public async Task EchoTaskParser_SingleLines_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: echo
      expr: This is a single line
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.AfterTasks.Count, Is.EqualTo(1));

        var task = result.AfterTasks[0] as EchoTaskBuilder;

        Assert.That(task, Is.Not.Null);
        Assert.That(task.Lines.Count, Is.EqualTo(1));
        Assert.That(task.Lines[0], Is.EqualTo("This is a single line"));
    }

    [Test]
    public async Task EchoTaskParser_MigrationLevelWithMultiplePhases_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: create-bucket
      bucket_name: test-bucket
      organisation_name: test-organisation
      tasks:
        - task: echo
          expr: This is a single line
          phases:
            - commit
            - rollback
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));

        var instance = result.UpOperations[0];

        Assert.That(instance.Operation, Is.InstanceOf<CreateBucketBuilder>());

        Assert.That(instance.AfterCommitTasks.Count, Is.EqualTo(1));
        Assert.That(instance.AfterCommitTasks[0], Is.InstanceOf<EchoTaskBuilder>());
        Assert.That(((EchoTaskBuilder)instance.AfterCommitTasks[0]).Lines[0], Is.EqualTo("This is a single line"));

        Assert.That(instance.AfterRollbackTasks.Count, Is.EqualTo(1));
        Assert.That(instance.AfterRollbackTasks[0], Is.InstanceOf<EchoTaskBuilder>());
        Assert.That(((EchoTaskBuilder)instance.AfterRollbackTasks[0]).Lines[0], Is.EqualTo("This is a single line"));


        Assert.That(instance.AfterExecuteTasks.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task EchoTaskParser_WithOnePhase_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  up:
    - operation: create-bucket
      bucket_name: test-bucket
      organisation_name: test-organisation
      tasks:
        - task: echo
          expr: This is a single line
          phases: commit
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));

        var instance = result.UpOperations[0];

        Assert.That(instance.Operation, Is.InstanceOf<CreateBucketBuilder>());

        Assert.That(instance.AfterCommitTasks.Count, Is.EqualTo(1));
        Assert.That(instance.AfterCommitTasks[0], Is.InstanceOf<EchoTaskBuilder>());
        Assert.That(((EchoTaskBuilder)instance.AfterCommitTasks[0]).Lines[0], Is.EqualTo("This is a single line"));

        Assert.That(instance.AfterRollbackTasks.Count, Is.EqualTo(0));
        Assert.That(instance.AfterExecuteTasks.Count, Is.EqualTo(0));
    }
}