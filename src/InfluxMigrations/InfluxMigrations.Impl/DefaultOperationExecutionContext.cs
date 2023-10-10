using InfluxDB.Client;
using InfluxMigrations.Core;

namespace InfluxMigrations.Impl;

public class DefaultOperationExecutionContext : IOperationExecutionContext
{
    private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    public IInfluxDBClient Influx => MigrationExecutionContext.Influx;

    public IMigrationExecutionContext MigrationExecutionContext { get; }
    
    public object? ExecuteResult { get; set; }
    public object? CommitResult { get; set; }
    public object? RollbackResult { get; set; }

    public void Accept(IContextVisitor visitor)
    {
        visitor.Visit(this);
    }


    public DefaultOperationExecutionContext(IMigrationExecutionContext migrationExecutionContext, string id)
    {
        Id = id;
        MigrationExecutionContext = migrationExecutionContext;
        ExecuteResult = null;
    }

    public string? Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new MigrationResolutionException($"Cannot retrieve the value of a null key.");
        }

        return _variables.TryGetValue(key, out var variable) ? variable : null;
    }

    public void Set(string key, string? value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new MigrationResolutionException($"Cannot set the value of a null key.");
        }

        // setting a value to null is the same as clearing it
        if (string.IsNullOrEmpty(value))
        {
            _variables.Remove(key);
        }
        else
        {
            _variables[key] = value;
        }
    }
}