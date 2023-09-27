using InfluxMigrations.Core;

namespace InfluxMigrations.Default.Integration;

public class MockMigration : IMigration
{
    public string Version { get; set; }
    private MigrationResult? _result;

    public List<MigrationOperationInstance> UpOperations { get; private set; } =
        new List<MigrationOperationInstance>();

    public List<MigrationOperationInstance> DownOperations { get; private set; } =
        new List<MigrationOperationInstance>();

    public List<IMigrationTaskBuilder> AfterTasks { get; private set; } =
        new List<IMigrationTaskBuilder>();

    public List<IMigrationTaskBuilder> BeforeTasks { get; private set; } = new List<IMigrationTaskBuilder>();

    public MigrationDirection Direction { get; private set; }

    public MockMigration(string ver)
    {
        Version = ver;
    }

    public MockMigration WithResult(Action<MigrationResult> res)
    {
        _result = new MigrationResult()
        {
            Version = this.Version
        };

        res(_result);
        return this;
    }

    public MigrationOperationInstance AddUp(string id, IMigrationOperationBuilder operation)
    {
        var entry = new MigrationOperationInstance(id, operation);
        UpOperations.Add(entry);
        return entry;
    }

    public MigrationOperationInstance AddDown(string id, IMigrationOperationBuilder operation)
    {
        var entry = new MigrationOperationInstance(id, operation);
        DownOperations.Add(entry);
        return entry;
    }

    public Task<MigrationResult> ExecuteAsync(IEnvironmentExecutionContext env, MigrationDirection direction,
        MigrationOptions? opts = null)
    {
        _result ??= new MigrationResult() { Version = this.Version };
        Direction = direction;

        return Task.FromResult(_result);
    }

    public IMigration AddAfterTask(IMigrationTaskBuilder task)
    {
        AfterTasks.Add(task);
        return this;
    }

    public IMigration AddBeforeTask(IMigrationTaskBuilder task)
    {
        BeforeTasks.Add(task);
        return this;
    }
}