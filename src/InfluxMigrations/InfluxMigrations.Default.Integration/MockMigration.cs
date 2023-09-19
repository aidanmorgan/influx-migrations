using InfluxMigrations.Core;

namespace InfluxMigrations.Default.Integration;

public class MockMigration : IMigration
{
    public string Version { get; private set; }
    private MigrationResult? _result;
    
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
        throw new NotImplementedException();
    }

    public MigrationOperationInstance AddDown(string id, IMigrationOperationBuilder operation)
    {
        throw new NotImplementedException();
    }

    public Task<MigrationResult> ExecuteAsync(IMigrationEnvironmentContext env, MigrationDirection direction, MigrationOptions? opts = null)
    {
        this._result ??= new MigrationResult() { Version = this.Version };
        this.Direction = direction;
        
        return Task.FromResult(_result);
    }

    public Migration AddOutput(IMigrationTaskBuilder task)
    {
        throw new NotImplementedException();
    }
}