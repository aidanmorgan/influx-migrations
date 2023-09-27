namespace InfluxMigrations.Core.Resolvers;

using static ResolverFunctionCommon;

[ResolverFunction("random-string")]
public class RandomStringResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        throw new NotImplementedException();
    }

    public RandomStringResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}