namespace InfluxMigrations.Impl;

/// <summary>
/// Implements a comparator that assumes that all version strings are Long's and orders them in increasing order.
/// </summary>
public class DefaultVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        var first = long.Parse(x);
        var second = long.Parse(y);

        return first.CompareTo(second);
    }
}