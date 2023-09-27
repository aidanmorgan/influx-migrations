using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;

namespace InfluxMigrations.Core;

public enum EnvironmentState
{
    Invalid,
    Ready,
    Finalised
}

/// <summary>
/// Contains "global" scoped information
/// </summary>
public interface IEnvironmentExecutionContext : IContext
{
    public EnvironmentState State { get; }
    public IInfluxFactory InfluxFactory { get; }
    
    public IInfluxDBClient Influx => InfluxFactory.Create();

    public string? Get(string key);
    
    public IEnvironmentExecutionContext Set(string key, string? value);

    public IMigrationLoggerFactory LoggerFactory { get; }

    public IMigrationExecutionContext CreateMigrationContext(string version);

    public IEnvironmentExecutionContext AddTask(TaskPrecedence precedence, IEnvironmentTaskBuilder builder);
    Task<List<TaskResult>> Initialise(IMigrationRunnerLogger? logger = null);
    Task<List<TaskResult>> Finalise(IMigrationRunnerLogger? logger = null);
}