namespace InfluxMigrations.Core;

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