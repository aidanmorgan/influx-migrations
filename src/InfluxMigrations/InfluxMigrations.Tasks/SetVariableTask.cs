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
    private static readonly IDictionary<string, VariableScope> VariableScopeLookup = new Dictionary<string, VariableScope>()
    {
        { "local", VariableScope.Local },
        { "migration", VariableScope.Migration },
        { "global", VariableScope.Global }
    };
    
    public  IResolvable<string> Key { get; set; }
    public IResolvable<string> Value { get; set; }
    public IResolvable<string> Scope { get; set; } = StringResolvable.Parse("local");

    public SetVariableTask()
    {
    }

    public Task ExecuteAsync(IOperationExecutionContext context)
    {
        var key = Key.Resolve(context);
        var value = Value.Resolve(context);

        var scope = VariableScopeLookup[Scope.Resolve(context)];
        
        switch (scope)
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
        var scope = VariableScopeLookup[Scope.Resolve(executionContext)];

        switch (scope)
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
    public string Key { get;  private set; }
    public string Value { get; private set; }
    public string Scope { get; private set; } = "local";
    
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

    public SetVariableTaskBuilder WithScope(string scope)
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
            Scope = StringResolvable.Parse(Scope),
        };
    }
}