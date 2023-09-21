using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Outputs;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.Tests;

public class WriteFileTaskParserTests
{
    [Test]
    public async Task WriteFileParser_MigrationLevelMultipleLines_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: write-file
      file: test-file
      content:
        - line 1
        - line 2
        - line 3 

", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.Tasks.Count, Is.EqualTo(1));

        var task = result.Tasks[0] as WriteFileTaskBuilder;

        Assert.That(task, Is.Not.Null);
        Assert.That(task.File, Is.EqualTo("test-file"));
        Assert.That(task.Content.Count, Is.EqualTo(3));
        Assert.That(task.Content[0], Is.EqualTo("line 1"));
        Assert.That(task.Content[1], Is.EqualTo("line 2"));
        Assert.That(task.Content[2], Is.EqualTo("line 3"));
    }

    [Test]
    public async Task WriteFileParser_MigrationLevelSingleLine_Success()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: write-file
      file: test-file
      content: line 1
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.Tasks.Count, Is.EqualTo(1));

        var task = result.Tasks[0] as WriteFileTaskBuilder;

        Assert.That(task, Is.Not.Null);
        Assert.That(task.File, Is.EqualTo("test-file"));
        Assert.That(task.Content.Count, Is.EqualTo(1));
        Assert.That(task.Content[0], Is.EqualTo("line 1"));
    }

    [Test]
    public async Task WriteFileParser_TaskLevelLevelMultipleLinesMultiplePhases_Success()
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
        - task: write-file
          file: test-file
          content:
            - line 1
            - line 2
            - line 3 
          phases:
            - rollback
            - commit

", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var op = result.UpOperations[0];

        Assert.That(op.Operation, Is.InstanceOf<CreateBucketBuilder>());

        Assert.That(op.ExecuteTasks.Count, Is.EqualTo(0));
        Assert.That(op.CommitTasks.Count, Is.EqualTo(1));
        Assert.That(op.CommitTasks[0], Is.InstanceOf<WriteFileTaskBuilder>());
        Assert.That(((WriteFileTaskBuilder)op.CommitTasks[0]).File, Is.EqualTo("test-file"));
        Assert.That(((WriteFileTaskBuilder)op.CommitTasks[0]).Content.Count, Is.EqualTo(3));

        Assert.That(op.RollbackTasks.Count, Is.EqualTo(1));
        Assert.That(op.RollbackTasks[0], Is.InstanceOf<WriteFileTaskBuilder>());
        Assert.That(((WriteFileTaskBuilder)op.RollbackTasks[0]).File, Is.EqualTo("test-file"));
        Assert.That(((WriteFileTaskBuilder)op.RollbackTasks[0]).Content.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task WriteFileParser_TaskLevelSingleLine_Success()
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
        - task: write-file
          file: test-file
          content:
            - line 1
            - line 2
            - line 3 
          phases: rollback

", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.UpOperations.Count, Is.EqualTo(1));

        var op = result.UpOperations[0];

        Assert.That(op.Operation, Is.InstanceOf<CreateBucketBuilder>());

        Assert.That(op.ExecuteTasks.Count, Is.EqualTo(0));
        Assert.That(op.CommitTasks.Count, Is.EqualTo(0));

        Assert.That(op.RollbackTasks.Count, Is.EqualTo(1));
        Assert.That(op.RollbackTasks[0], Is.InstanceOf<WriteFileTaskBuilder>());
        Assert.That(((WriteFileTaskBuilder)op.RollbackTasks[0]).File, Is.EqualTo("test-file"));
        Assert.That(((WriteFileTaskBuilder)op.RollbackTasks[0]).Content.Count, Is.EqualTo(3));
    }
}