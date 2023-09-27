using System.Globalization;
using System.Text.RegularExpressions;

namespace InfluxMigrations.Core;

/// <summary>
/// Used to parse a string in a influx db format into a TimeSpan.
/// </summary>
public static class TimeSpanParser
{
    const string Quantity = "quantity";
    const string Unit = "unit";

    const string Days = @"(d(ays?)?)";
    const string Hours = @"(h((ours?)|(rs?))?)";
    const string Minutes = @"(m((inutes?)|(ins?))?)";
    const string Seconds = @"(s((econds?)|(ecs?))?)";

    public static TimeSpan? ParseTimeSpan(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }

        var timeSpanRegex = new Regex(
            string.Format(@"\s*(?<{0}>\d+)\s*(?<{1}>({2}|{3}|{4}|{5}|\Z))",
                Quantity, Unit, Days, Hours, Minutes, Seconds),
            RegexOptions.IgnoreCase);
        var matches = timeSpanRegex.Matches(s);

        var ts = new TimeSpan();
        foreach (Match match in matches)
        {
            if (Regex.IsMatch(match.Groups[Unit].Value, @"\A" + Days))
            {
                ts = ts.Add(TimeSpan.FromDays(double.Parse(match.Groups[Quantity].Value)));
            }
            else if (Regex.IsMatch(match.Groups[Unit].Value, Hours))
            {
                ts = ts.Add(TimeSpan.FromHours(double.Parse(match.Groups[Quantity].Value)));
            }
            else if (Regex.IsMatch(match.Groups[Unit].Value, Minutes))
            {
                ts = ts.Add(TimeSpan.FromMinutes(double.Parse(match.Groups[Quantity].Value)));
            }
            else if (Regex.IsMatch(match.Groups[Unit].Value, Seconds))
            {
                ts = ts.Add(TimeSpan.FromSeconds(double.Parse(match.Groups[Quantity].Value)));
            }
            else
            {
                // Quantity given but no unit, default to Hours
                ts = ts.Add(TimeSpan.FromHours(double.Parse(match.Groups[Quantity].Value)));
            }
        }

        return ts;
    }
}