namespace InfluxMigrations.Core.Resolvers;

public abstract class ResolverFunctionCommon
{
    public static StringResolvable FixedValue(string val)
    {
        return new StringResolvable(ResolutionType.Fixed, val, (x) => val, x => val);
    }

    public static string Unwrap(string entry, string key)
    {
        if (entry.Length == key.Length)
        {
            return string.Empty;
        }

        var result = entry.Substring(key.Length, entry.Length - key.Length - 1);

        // not all functions contain a :, so check for it
        return result.StartsWith(":") ? result.Substring(1) : result;
    }
}