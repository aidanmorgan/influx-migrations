using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

/// <summary>
/// A Resolvable is a placeholder for a value that needs to be evaluated while the migration is running to
/// be able to provide a value.
/// </summary>
public interface IResolvable<out T>
{
    ResolutionType Scope { get; }
    public T? Resolve(IOperationExecutionContext context);

    public T? Resolve(IMigrationExecutionContext executionContext);

    public T? Resolve(IContext context)
    {
        return context switch
        {
            IOperationExecutionContext executionContext => Resolve(executionContext),
            IMigrationExecutionContext migrationExecutionContext => Resolve(migrationExecutionContext),
            _ => throw new MigrationResolutionException(
                $"Cannot determine correct resolver for IContext {context.GetType().FullName}.")
        };
    }
}

/// <summary>
/// Convenience enum that indicates the scope of the IResolvable to aid in debugging.
/// </summary>
public enum ResolutionType
{
    [EnumMember(Value = "fixed")] Fixed,
    [EnumMember(Value = "environment")] Environment,
    [EnumMember(Value = "result")] Result,
    [EnumMember(Value = "migration")] Migration,
    [EnumMember(Value = "step")] Step,
    [EnumMember(Value = "block")] Block,
    [EnumMember(Value = "local")] Local
}

/// <summary>
/// An implementation of the IResolvable interface that chains resolution of the underlying resolvables.
/// </summary>
public class SequentialResolvable : IResolvable<string?>
{
    public ResolutionType Scope => ResolutionType.Block;
    private IList<IResolvable<string?>> _sequence = new List<IResolvable<string?>>();

    public SequentialResolvable Add(IResolvable<string?> resolvable)
    {
        _sequence.Add(resolvable);
        return this;
    }

    public string Resolve(IOperationExecutionContext context)
    {
        return string.Join("", _sequence.Select(x => x.Resolve(context)));
    }

    public string Resolve(IMigrationExecutionContext executionContext)
    {
        return string.Join("", _sequence.Select(x => x.Resolve(executionContext)));
    }
}