using System.Reflection;

namespace InfluxMigrations.Core.Resolvers;
using static ResolverFunctionCommon;
[ResolverFunction("result", "execute-result")]
public class ExecuteResultResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        var keyResolvable = next(Unwrap(entry, Prefix));
        return keyResolvable == null ? null : ResultValue(keyResolvable, entry, x => x.ExecuteResult);
   }
    
    internal static StringResolvable ResultValue(IResolvable<string?> key, string originalString, Func<IOperationExecutionContext, object?> getter)
    {
        return new StringResolvable(ResolutionType.Result, originalString,(x) =>
            {
                var result = getter(x);
                if (result == null)
                {
                    return null;
                }
            
                var resultType = result.GetType();
                var val = key.Resolve((IOperationExecutionContext)x);
            
                var property = Enumerable.FirstOrDefault<PropertyInfo?>(resultType.GetProperties(), x => string.Equals(x?.Name, val, StringComparison.InvariantCultureIgnoreCase), null);
                if (property == null)
                {
                    throw new MigrationResolutionException($"Cannot find an appropriate property on {resultType.FullName} for variable {val}");
                }

                if (!property.CanRead)
                {
                    throw new MigrationResolutionException($"Cannot find readable property for variable {val} on {resultType.FullName}");
                }

                if (property.PropertyType != typeof(string))
                {
                    throw new MigrationResolutionException($"Property for value {val} on {resultType.FullName} is not a string.");
                }

                return (string?)property.GetValue(result, null);
            },
            x => throw new MigrationResolutionException($"Cannot resolve a result for a Migration."));
    }

    public ExecuteResultResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}

[ResolverFunction("commit", "commit-result")]
public class CommitResultResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        var keyResolvable = next(Unwrap(entry, Prefix));
        return keyResolvable == null ? null : ExecuteResultResolver.ResultValue(keyResolvable, entry, x=> x.CommitResult);
    }

    public CommitResultResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}

[ResolverFunction("rollback")]
public class RollbackResultResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        var keyResolvable = next(Unwrap(entry, Prefix));
        return keyResolvable == null ? null : ExecuteResultResolver.ResultValue(keyResolvable, entry, x => x.RollbackResult);

    }

    public RollbackResultResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}