namespace InfluxMigrations.Core.Resolvers;
using static ResolverFunctionCommon;
[ResolverFunction("now")]
public class TimestampResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        var key = Unwrap(entry, Prefix);

        var split = key.Split(":");

        if (split.Length == 1)
        {
            return ResolverFunctionCommon.FixedValue($"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        }
        else
        {
            throw new MigrationResolutionException("Unsupported now format.");
        }
        
    }

    public TimestampResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}