namespace InfluxMigrations.Impl;

public class DefaultVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        var first = long.Parse(x);
        var second = long.Parse(y);

        return first.CompareTo(second);
    }
}