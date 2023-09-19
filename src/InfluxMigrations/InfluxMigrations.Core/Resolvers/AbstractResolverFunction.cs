namespace InfluxMigrations.Core.Resolvers;

public abstract class AbstractResolverFunction : IResolverFunction
{
    public string Prefix { get; init; }
    public string Suffix { get; init; }

    protected AbstractResolverFunction(string prefix, string suffix)
    {
        Prefix = prefix;
        Suffix = suffix;
    }

    public abstract IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next);
}