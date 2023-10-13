using System.Runtime.Serialization;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Tasks;

public enum VariableScope
{
    [EnumMember(Value = "local")] Local,
    [EnumMember(Value = "migration")] Migration,
    [EnumMember(Value = "global")] Global
}

public class SetVariableTask : IMigrationTask, IOperationTask, IEnvironmentTask
{
    private static readonly IDictionary<string, VariableScope> VariableScopeLookup =
        new Dictionary<string, VariableScope>()
        {
            { "local", VariableScope.Local },
            { "migration", VariableScope.Migration },
            { "global", VariableScope.Global }
        };

    public IResolvable<string> Key { get; set; }
    public IResolvable<string> Value { get; set; }
    public IResolvable<string> Scope { get; set; } = StringResolvable.Parse("local");

    public SetVariableTask()
    {
    }

    public Task<TaskResult> ExecuteAsync(IOperationExecutionContext context)
    {
        var key = Key.Resolve(context);

        if (string.IsNullOrEmpty(key))
        {
            return TaskResults.TaskFailureAsync("Cannot set variable, cannot resolve Key value.");
        }
        
        // variable is allowed to be null, it will cause the value (if it exists) to be unset
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
                context.MigrationExecutionContext.EnvironmentExecutionContext.Set(key, value);
                break;
            }

            default:
            {
                context.Set(key, value);
                break;
            }
        }

        return TaskResults.TaskSuccessAsync();
    }

    public Task<TaskResult> ExecuteAsync(IMigrationExecutionContext executionContext)
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
                executionContext.EnvironmentExecutionContext.Set(key, value);
                break;
            }
        }

        return TaskResults.TaskSuccessAsync();
    }

    public Task<TaskResult> ExecuteAsync(IEnvironmentExecutionContext executionContext)
    {
        var key = Key.Resolve(executionContext);
        var value = Value.Resolve(executionContext);
        var scope = VariableScopeLookup[Scope.Resolve(executionContext)];

        switch (scope)
        {
            case VariableScope.Local:
            {
                throw new MigrationExecutionException($"Cannot resolve a Local scope variable for an Environment.");
                break;
            }

            case VariableScope.Migration:
            {
                throw new MigrationExecutionException($"Cannot resolve a Migration scope variable for an Environment.");
                break;
            }

            case VariableScope.Global:
            {
                executionContext.Set(key, value);
                break;
            }
        }

        return TaskResults.TaskSuccessAsync();
        
    }
}

public class SetVariableTaskBuilder : IMigrationTaskBuilder, IOperationTaskBuilder, IEnvironmentTaskBuilder
{
    public string Key { get; private set; }
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

    public IMigrationTask BuildMigration()
    {
        return new SetVariableTask()
        {
            Key = StringResolvable.Parse(Key),
            Value = StringResolvable.Parse(Value),
            Scope = StringResolvable.Parse(Scope),
        };
    }

    public IOperationTask BuildOperation()
    {
        return new SetVariableTask()
        {
            Key = StringResolvable.Parse(Key),
            Value = StringResolvable.Parse(Value),
            Scope = StringResolvable.Parse(Scope),
        };
    }

    public IEnvironmentTask BuildEnvironment()
    {
        return new SetVariableTask()
        {
            Key = StringResolvable.Parse(Key),
            Value = StringResolvable.Parse(Value),
            Scope = StringResolvable.Parse(Scope),
        };
    }
}