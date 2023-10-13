using InfluxDB.Client;

namespace InfluxMigrations.Core;

/// <summary>
/// Contains scoped instances for a specific operation invocation.
/// </summary>
public interface IOperationExecutionContext : IContext
{
    public string? Id { get; }
    public string? Get(string key);
    public void Set(string key, string? value);
    public IInfluxDBClient Influx => MigrationExecutionContext.Influx;

    public IMigrationExecutionContext MigrationExecutionContext { get; }

    public object? ExecuteResult { get; set; }
    public object? CommitResult { get; set; }
    public object? RollbackResult { get; set; }
}