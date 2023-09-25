namespace InfluxMigrations.Core;

public interface IResolverFunction
{
    IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next);
}