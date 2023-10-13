namespace InfluxMigrations.Core.Resolvers;

public abstract class ResolverFunctionCommon
{
    /// <summary>
    /// Returns a StringResolvable that always returns the same value.
    /// </summary>
    public static StringResolvable FixedValue(string val)
    {
        return new StringResolvable(ResolutionType.Fixed, val, (x) => val, x => val, x=> val);
    }

    /// <summary>
    /// Removes the resolver function definition of the provided key from the provided entry. Used to support
    /// nesting of functions.
    /// </summary>
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