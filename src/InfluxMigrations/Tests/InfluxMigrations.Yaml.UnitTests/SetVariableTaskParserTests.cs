using InfluxMigrations.Default.Integration;
using InfluxMigrations.Operations.Bucket;
using InfluxMigrations.Tasks;
using InfluxMigrations.Yaml.Parsers;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.UnitTests;

public class SetVariableTaskParserTests
{
    [Test]
    public async Task SetVariable_SeparateKeyValueMigrationLevel_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: set
      key: test-key
      value: test-value
      scope: global
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.AfterTasks.Count, Is.EqualTo(1));

        var task = result.AfterTasks[0] as SetVariableTaskBuilder;

        Assert.That(task, Is.Not.Null);
        Assert.That(task.Key, Is.EqualTo("test-key"));
        Assert.That(task.Value, Is.EqualTo("test-value"));
        Assert.That(task.Scope, Is.EqualTo("global"));
    }

    [Test]
    public async Task SetVariable_ExprValueMigrationLevel_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: set
      expr: test-key = test-value
      scope: global
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.AfterTasks.Count, Is.EqualTo(1));

        var task = result.AfterTasks[0] as SetVariableTaskBuilder;

        Assert.That(task, Is.Not.Null);
        Assert.That(task.Key, Is.EqualTo("test-key"));
        Assert.That(task.Value, Is.EqualTo("test-value"));
        Assert.That(task.Scope, Is.EqualTo("global"));
    }

    [Test]
    public async Task SetVariable_SeparateKeyValueTaskLevelWithMultiplePhases_Success()
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
        - task: set
          key: test-key
          value: test-value
          scope: global
          phases:
            - commit
            - rollback
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0];

        Assert.That(operation.Operation, Is.InstanceOf<CreateBucketBuilder>());
        Assert.That(operation.AfterExecuteTasks.Count, Is.EqualTo(0));
        Assert.That(operation.AfterCommitTasks.Count, Is.EqualTo(1));
        Assert.That(operation.AfterCommitTasks[0], Is.InstanceOf<SetVariableTaskBuilder>());
        Assert.That(((SetVariableTaskBuilder)operation.AfterCommitTasks[0]).Key, Is.EqualTo("test-key"));
        Assert.That(((SetVariableTaskBuilder)operation.AfterCommitTasks[0]).Value, Is.EqualTo("test-value"));
        Assert.That(((SetVariableTaskBuilder)operation.AfterCommitTasks[0]).Scope, Is.EqualTo("global"));

        Assert.That(operation.AfterRollbackTasks.Count, Is.EqualTo(1));
        Assert.That(operation.AfterRollbackTasks[0], Is.InstanceOf<SetVariableTaskBuilder>());
        Assert.That(((SetVariableTaskBuilder)operation.AfterRollbackTasks[0]).Key, Is.EqualTo("test-key"));
        Assert.That(((SetVariableTaskBuilder)operation.AfterRollbackTasks[0]).Value, Is.EqualTo("test-value"));
        Assert.That(((SetVariableTaskBuilder)operation.AfterRollbackTasks[0]).Scope, Is.EqualTo("global"));
    }

    [Test]
    public async Task SetVariable_SeparateKeyValueTaskLevelWithSinglePhase_Success()
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
        - task: set
          key: test-key
          value: test-value
          scope: global
          phases: rollback
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var operation = result.UpOperations[0];

        Assert.That(operation.Operation, Is.InstanceOf<CreateBucketBuilder>());
        Assert.That(operation.AfterExecuteTasks.Count, Is.EqualTo(0));
        Assert.That(operation.AfterCommitTasks.Count, Is.EqualTo(0));

        Assert.That(operation.AfterRollbackTasks.Count, Is.EqualTo(1));
        Assert.That(operation.AfterRollbackTasks[0], Is.InstanceOf<SetVariableTaskBuilder>());
        Assert.That(((SetVariableTaskBuilder)operation.AfterRollbackTasks[0]).Key, Is.EqualTo("test-key"));
        Assert.That(((SetVariableTaskBuilder)operation.AfterRollbackTasks[0]).Value, Is.EqualTo("test-value"));
        Assert.That(((SetVariableTaskBuilder)operation.AfterRollbackTasks[0]).Scope, Is.EqualTo("global"));
    }
}