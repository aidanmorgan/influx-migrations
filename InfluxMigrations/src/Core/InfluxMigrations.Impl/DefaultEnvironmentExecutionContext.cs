using InfluxDB.Client;
using InfluxMigrations.Core;
using Microsoft.VisualBasic.CompilerServices;

namespace InfluxMigrations.Impl;

public class DefaultEnvironmentExecutionContext : IEnvironmentExecutionContext
{
    private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
    public IInfluxFactory InfluxFactory { get; init; }
    public IMigrationLoggerFactory LoggerFactory { get; set; }
    
    private readonly IList<IMigrationExecutionContext> _migrationExecutionContexts = new List<IMigrationExecutionContext>();
    private readonly List<IEnvironmentTaskBuilder> _beforeTasks = new List<IEnvironmentTaskBuilder>();
    private readonly List<IEnvironmentTaskBuilder> _afterTasks = new List<IEnvironmentTaskBuilder>();
    public EnvironmentState State { get; private set; } = EnvironmentState.Invalid;


    public DefaultEnvironmentExecutionContext(IMigrationLoggerFactory? logger = null)
    {
        LoggerFactory = logger ?? new NoOpMigrationLoggerFactory();
    }

    public DefaultEnvironmentExecutionContext(IInfluxFactory client, MigrationOptions options = null)
    {
        InfluxFactory = client;
        LoggerFactory = options?.Logger ?? new NoOpMigrationLoggerFactory();
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

    public IEnvironmentExecutionContext Set(string key, string? value)
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
        if (State != EnvironmentState.Ready)
        {
            throw new MigrationExecutionException($"Cannot execute Migration, Environment is not initialised.");
        }

        var ctx = new DefaultMigrationExecutionContext(this, version);
        _migrationExecutionContexts.Add(ctx);
        
        return ctx;
    }

    public IEnvironmentExecutionContext AddTask(TaskPrecedence precedence, IEnvironmentTaskBuilder builder)
    {
        switch (precedence)
        {
            case TaskPrecedence.After:
            {
                _afterTasks.Add(builder);
                return this;
            }

            case TaskPrecedence.Before:
            {
                _beforeTasks.Add(builder);
                return this;
            }

            default:
            {
                throw new MigrationConfigurationException($"Unrecognised task precedence.");
            }
        }
    }


    private List<TaskResult>? _initialiseResults = null;
    public async Task<List<TaskResult>> Initialise(IMigrationRunnerLogger? l = null)
    {
        if (_initialiseResults != null)
        {
            return _initialiseResults;
        }

        var logger = l ?? new NoOpMigrationRunnerLogger();
        
        var results = new List<TaskResult>();
        foreach (var task in _beforeTasks.Select(x => x.BuildEnvironment()))
        {
            var log = logger.StartTask(task);
            var result = await task.ExecuteAsync(this);

            if (result.State == TaskState.Success)
            {
                log.Complete();
            }
            else
            {
                log.Failed(result.Exception);
            }

            results.Add(result);
        }
        
        _initialiseResults = results;

        State = EnvironmentState.Ready;
        return results;
    }

    private List<TaskResult>? _finaliseResults = null;
    public async Task<List<TaskResult>> Finalise(IMigrationRunnerLogger? l = null)
    {
        if (_finaliseResults != null)
        {
            return _finaliseResults;
        }
        
        var logger = l ?? new NoOpMigrationRunnerLogger();
        
        var results = new List<TaskResult>();
        foreach (var task in _afterTasks.Select(x => x.BuildEnvironment()))
        {
            var log = logger.StartTask(task);
            var result = await task.ExecuteAsync(this);

            if (result.State == TaskState.Success)
            {
                log.Complete();
            }
            else
            {
                log.Failed(result.Exception);
            }
            
            results.Add(result);
        }

        _finaliseResults = results;

        State = EnvironmentState.Finalised;
        
        return results;
    }
    
    public void Accept(IContextVisitor visitor)
    {
        visitor.Visit(this);
    }

    public IInfluxDBClient Influx => InfluxFactory.Create();

    public DefaultEnvironmentExecutionContext Add(string key, string value)
    {
        _variables[key] = value;
        return this;
    }
}