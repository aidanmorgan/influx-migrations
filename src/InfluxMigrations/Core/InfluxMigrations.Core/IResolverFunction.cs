namespace InfluxMigrations.Core;

/// <summary>
/// Represents a function that implements a specific resolver behaviour, intended to be pluggable so additional
/// types can be added if required.
/// </summary>
public interface IResolverFunction
{
    IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next);
}