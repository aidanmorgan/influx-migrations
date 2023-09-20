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
    public List<IMigrationTaskBuilder> Tasks { get; private set; } = 
        new List<IMigrationTaskBuilder>();
    
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

    public Task<MigrationResult> ExecuteAsync(IMigrationEnvironmentContext env, MigrationDirection direction, MigrationOptions? opts = null)
    {
        this._result ??= new MigrationResult() { Version = this.Version };
        this.Direction = direction;
        
        return Task.FromResult(_result);
    }

    public IMigration AddTask(IMigrationTaskBuilder task)
    {
        Tasks.Add(task);
        return this;
    }
}