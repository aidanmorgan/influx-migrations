using System.ComponentModel;
using System.Runtime.Serialization;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Outputs;

public enum VariableScope
{
    [EnumMember(Value = "local")]Local,
    [EnumMember(Value = "migration")]Migration,
    [EnumMember(Value = "global")]Global
}

public class SetVariableTask : IMigrationTask
{
    public  IResolvable<string> Key { get; set; }
    public IResolvable<string> Value { get; set; }
    public VariableScope Scope { get; set; } = VariableScope.Local;

    public SetVariableTask()
    {
    }

    public Task ExecuteAsync(IOperationExecutionContext context)
    {
        var key = Key.Resolve(context);
        var value = Value.Resolve(context);

        switch (Scope)
        {
            case VariableScope.Local:
            {
                context.Set(key, value);
                break;
            }

            case VariableScope.Migration:
            {
                context.MigrationExecutionContext.Set(key, value);
                break;
            }

            case VariableScope.Global:
            {
                context.MigrationExecutionContext.EnvironmentContext.Set(key, value);
                break;
            }
        }
        
        return Task.CompletedTask;
    }
    
    public Task ExecuteAsync(IMigrationExecutionContext executionContext)
    {
        var key = Key.Resolve(executionContext);
        var value = Value.Resolve(executionContext);

        switch (Scope)
        {
            case VariableScope.Local:
            {
                throw new MigrationExecutionException($"Cannot resolve a Local scope variable for a Migration.");
                break;
            }

            case VariableScope.Migration:
            {
                executionContext.Set(key, value);
                break;
            }

            case VariableScope.Global:
            {
                executionContext.EnvironmentContext.Set(key, value);
                break;
            }
        }
        
        return Task.CompletedTask;
    }    
}

public class SetVariableTaskBuilder : IMigrationTaskBuilder
{
    private string Key { get;  set; }
    private string Value { get; set; }
    private VariableScope Scope { get; set; } = VariableScope.Local;


    
    public SetVariableTaskBuilder WithKey(string key)
    {
        this.Key = key;
        return this;
    }

    public SetVariableTaskBuilder WithValue(string value)
    {
        this.Value = value;
        return this;
    }

    public SetVariableTaskBuilder WithScope(VariableScope scope)
    {
        this.Scope = scope;
        return this;
    }

    public IMigrationTask Build()
    {
        return new SetVariableTask()
        {
            Key = StringResolvable.Parse(Key),
            Value = StringResolvable.Parse(Value),
            Scope = Scope,
        };
    }
}