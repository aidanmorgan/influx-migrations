using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Default.Integration;
using InfluxMigrations.Outputs;
using NUnit.Framework;

namespace InfluxMigrations.Yaml.Tests;

public class InsertDataTaskParserTests
{
    [Test]
    public async Task ParseInsertData_MigrationLevelMultipleLines()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: insert-data
      organisation_name: organisation-name
      organisation_id: organisation-id
      bucket_name: bucket-name
      bucket_id: bucket-id
      line:
        - line 1
        - line 2
        - line 3
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.Tasks.Count, Is.EqualTo(1));

        var task = result.Tasks[0] as InsertDataBuilder;
        Assert.That(task, Is.Not.Null);

        Assert.That(task.OrganisationName, Is.EqualTo("organisation-name"));
        Assert.That(task.OrganisationId, Is.EqualTo("organisation-id"));
        Assert.That(task.BucketName, Is.EqualTo("bucket-name"));
        Assert.That(task.BucketId, Is.EqualTo("bucket-id"));
        Assert.That(task.Lines.Count, Is.EqualTo(3));
        Assert.That(task.Lines[0], Is.EqualTo("line 1"));
        Assert.That(task.Lines[1], Is.EqualTo("line 2"));
        Assert.That(task.Lines[2], Is.EqualTo("line 3"));
    }

    [Test]
    public async Task ParseInsertData_MigrationLevelSingleLine()
    {
        var yamlParser = new YamlMigrationParser();
        var result = await yamlParser.ParseString(
@"migration:
  version: 0001
  tasks:
    - task: insert-data
      organisation_name: organisation-name
      organisation_id: organisation-id
      bucket_name: bucket-name
      bucket_id: bucket-id
      line: line 1
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));
        Assert.That(result.Tasks.Count, Is.EqualTo(1));

        var task = result.Tasks[0] as InsertDataBuilder;
        Assert.That(task, Is.Not.Null);

        Assert.That(task.OrganisationName, Is.EqualTo("organisation-name"));
        Assert.That(task.OrganisationId, Is.EqualTo("organisation-id"));
        Assert.That(task.BucketName, Is.EqualTo("bucket-name"));
        Assert.That(task.BucketId, Is.EqualTo("bucket-id"));
        Assert.That(task.Lines.Count, Is.EqualTo(1));
        Assert.That(task.Lines[0], Is.EqualTo("line 1"));
    }

    [Test]
    public async Task ParseInsertData_OperationLevelWithMultiplePhases()
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
        - task: insert-data
          organisation_name: organisation-name
          organisation_id: organisation-id
          bucket_name: bucket-name
          bucket_id: bucket-id
          line: line 1
          phases:
            - commit
            - rollback 
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));

        var operation = result.UpOperations[0];
        Assert.That(operation.Operation, Is.InstanceOf<CreateBucketBuilder>());

        Assert.That(operation.CommitTasks.Count, Is.EqualTo(1));
        Assert.That(operation.CommitTasks[0], Is.InstanceOf<InsertDataBuilder>());
        Assert.That(((InsertDataBuilder)operation.CommitTasks[0]).Lines.Count, Is.EqualTo(1));
        Assert.That(((InsertDataBuilder)operation.CommitTasks[0]).Lines[0], Is.EqualTo("line 1"));

        Assert.That(operation.RollbackTasks.Count, Is.EqualTo(1));
        Assert.That(operation.RollbackTasks[0], Is.InstanceOf<InsertDataBuilder>());
        Assert.That(((InsertDataBuilder)operation.RollbackTasks[0]).Lines.Count, Is.EqualTo(1));
        Assert.That(((InsertDataBuilder)operation.RollbackTasks[0]).Lines[0], Is.EqualTo("line 1"));

        Assert.That(operation.ExecuteTasks.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ParseInsertData_OperationLevelWithOnePhase()
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
        - task: insert-data
          organisation_name: organisation-name
          organisation_id: organisation-id
          bucket_name: bucket-name
          bucket_id: bucket-id
          line: line 1
          phases: commit
", x => new MockMigration(x)) as MockMigration;

        Assert.That(result, Is.Not.Null);

        Assert.That(result.Version, Is.EqualTo("0001"));

        var operation = result.UpOperations[0];
        Assert.That(operation.Operation, Is.InstanceOf<CreateBucketBuilder>());

        Assert.That(operation.CommitTasks.Count, Is.EqualTo(1));
        Assert.That(operation.CommitTasks[0], Is.InstanceOf<InsertDataBuilder>());
        Assert.That(((InsertDataBuilder)operation.CommitTasks[0]).Lines.Count, Is.EqualTo(1));
        Assert.That(((InsertDataBuilder)operation.CommitTasks[0]).Lines[0], Is.EqualTo("line 1"));

        Assert.That(operation.RollbackTasks.Count, Is.EqualTo(0));
        Assert.That(operation.ExecuteTasks.Count, Is.EqualTo(0));
    }
}