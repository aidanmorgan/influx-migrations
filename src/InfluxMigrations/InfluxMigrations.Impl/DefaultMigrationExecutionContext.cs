using System.Windows.Input;
using InfluxDB.Client;
using InfluxMigrations.Core;

namespace InfluxMigrations.Impl;

public class DefaultMigrationExecutionContext : IMigrationExecutionContext
{
    public string Version { get; private set; }
    public IEnvironmentExecutionContext EnvironmentExecutionContext { get; private set; }

    private Dictionary<string, string> _variables = new Dictionary<string, string>();

    public DefaultMigrationExecutionContext(IEnvironmentExecutionContext env, string version)
    {
        this.EnvironmentExecutionContext = env;
        this.Version = version;
    }

    public void Set(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _variables.Remove(key);
        }
        else
        {
            _variables[key] = value;
        }
    }

    public string? Get(string name)
    {
        return _variables.TryGetValue(name, out var variable) ? variable : null;
    }

    private readonly Dictionary<string, IOperationExecutionContext> _executionContexts =  new Dictionary<string, IOperationExecutionContext>();

    public IOperationExecutionContext CreateExecutionContext(string id)
    {
        var context = new DefaultOperationExecutionContext(this, id);
        _executionContexts[id] = context;
        return context;
    }

    public IOperationExecutionContext? GetExecutionContext(string id)
    {
        return _executionContexts.TryGetValue(id, out var context) ? context : null;
    }

    public void Accept(IContextVisitor visitor)
    {
        visitor.Visit(this);
    }

    public IInfluxDBClient Influx => EnvironmentExecutionContext.Influx;
}