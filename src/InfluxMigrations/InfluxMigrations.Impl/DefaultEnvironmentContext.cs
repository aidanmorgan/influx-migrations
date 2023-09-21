using InfluxDB.Client;
using InfluxMigrations.Core;

namespace InfluxMigrations.Impl;

public class DefaultEnvironmentContext : IMigrationEnvironmentContext
{
    private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
    public IInfluxFactory InfluxFactory { get; init; }
    public IMigrationLoggerFactory LoggerFactory { get; set; }

    private readonly IList<IMigrationExecutionContext> _migrationExecutionContexts =
        new List<IMigrationExecutionContext>();


    public DefaultEnvironmentContext(IMigrationLoggerFactory? logger = null)
    {
        LoggerFactory = logger ?? new NoOpMigrationLoggerFactory();
    }

    public DefaultEnvironmentContext(IInfluxFactory client, IMigrationLoggerFactory? logger = null)
    {
        InfluxFactory = client;
        LoggerFactory = logger ?? new NoOpMigrationLoggerFactory();
    }

    public string? Get(string key)
    {
        if (_variables.TryGetValue(key, out var variable))
        {
            return variable;
        }

        var entry = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(entry))
        {
            _variables[key] = entry;
            return entry;
        }

        return null;
    }

    public IMigrationEnvironmentContext Set(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _variables.Remove(key);
        }
        else
        {
            _variables[key] = value;
        }

        return this;
    }

    public IMigrationExecutionContext CreateMigrationContext(string version)
    {
        var ctx = new DefaultMigrationExecutionContext(this, version);
        _migrationExecutionContexts.Add(ctx);
        return ctx;
    }

    public void Accept(IContextVisitor visitor)
    {
        visitor.Visit(this);
    }

    public DefaultEnvironmentContext Add(string key, string value)
    {
        _variables[key] = value;
        return this;
    }
}