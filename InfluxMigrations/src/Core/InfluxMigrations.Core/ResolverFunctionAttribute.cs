namespace InfluxMigrations.Core;

/// <summary>
/// Flagging attribute to allow disovery of IResolverFunction instances at runtime.
/// </summary>
public class ResolverFunctionAttribute : Attribute
{
    public string Key { get; init; }
    public List<string> Aliases { get; init; }

    public ResolverFunctionAttribute(string key, params string[] others)
    {
        Key = key;
        Aliases = others.ToList();
    }
}