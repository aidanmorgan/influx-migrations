using InfluxDB.Client;

namespace InfluxMigrations.Core;

/// <summary>
/// Contains scoped instances for an entire migration file
/// </summary>
public interface IMigrationExecutionContext : IContext
{
    public string Version { get; }
    public IInfluxDBClient Influx => EnvironmentExecutionContext.Influx;

    public void Set(string key, string? value);
    public string? Get(string name);
    public IEnvironmentExecutionContext EnvironmentExecutionContext { get; }
    
    /// <summary>
    /// Creates a new IOperationExecutionContext for a specified step id.
    /// </summary>
    public IOperationExecutionContext CreateExecutionContext(string operationInstanceId);
    
    /// <summary>
    /// Returns the IOperationExecutionContext that is assocated with a provided step id. 
    /// </summary>
    public IOperationExecutionContext? GetExecutionContext(string operationInstanceId);
}