using InfluxMigrations.Core;
using NUnit.Framework;

namespace InfluxMigrations.Default.Integration;

public static class NunitExtensions
{
    public static void AssertMigrationSuccess(MigrationResult res)
    {
        Assert.That(res.Success, Is.True);
        Assert.That(res.Inconsistent, Is.False);
        Assert.That(res.Issues.Count, Is.Zero);
    }

    public static void AssertMigrationRollback(MigrationResult migrationResult)
    {
        Assert.That(migrationResult.Success, Is.False);
        Assert.That(migrationResult.Inconsistent, Is.False);
    }
}