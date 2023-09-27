namespace InfluxMigrations.Core.Resolvers;

[ResolverFunction("random-number")]
public class RandomNumberResolver : AbstractResolverFunction
{ 
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        throw new NotImplementedException();
    }
    
    public RandomNumberResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}