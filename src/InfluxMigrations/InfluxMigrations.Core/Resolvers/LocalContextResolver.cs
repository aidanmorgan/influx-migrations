namespace InfluxMigrations.Core.Resolvers;
using static ResolverFunctionCommon;

[ResolverFunction("local")]
public class LocalContextResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        var key = next(Unwrap(entry, Prefix));
        return key == null ? null : LocalValue(key, entry);        
    }

    
    private static StringResolvable LocalValue(IResolvable<string?> key, string originalString)
    {
        return new StringResolvable(ResolutionType.Local, originalString, (x) =>
        {
            var keyVal = key.Resolve((IOperationExecutionContext)x);
            return string.IsNullOrEmpty(keyVal) ? null : x.Get(keyVal);
        }, x=>
        {
            // TODO : decide if this is correct - does a 'local' make sense in a migration context?
            var keyVal = key.Resolve((IMigrationExecutionContext)x);
            return string.IsNullOrEmpty(keyVal) ? null : x.Get(keyVal);
        });
    }

    public LocalContextResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}