namespace InfluxMigrations.Core.Resolvers;

using static ResolverFunctionCommon;

[ResolverFunction("env")]
public class EnvironmentResolver : AbstractResolverFunction
{
    public EnvironmentResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }

    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        var key = next(Unwrap(entry, Prefix));
        return key == null ? null : EnvironmentValue(key, entry);
    }
    
    private static StringResolvable? EnvironmentValue(IResolvable<string?> key, string originalString)
    {
        return new StringResolvable(ResolutionType.Environment, originalString, 
            x =>
            {
                var resolvedKey = key.Resolve((IOperationExecutionContext)x);
                return string.IsNullOrEmpty(resolvedKey) ? null : x.MigrationExecutionContext.EnvironmentContext.Get(resolvedKey);
            },
            x =>
            {
                var resolvedKey = key.Resolve((IMigrationExecutionContext)x);
                return string.IsNullOrEmpty(resolvedKey) ? null : x.EnvironmentContext.Get(resolvedKey);
            });
        
        
    }
}