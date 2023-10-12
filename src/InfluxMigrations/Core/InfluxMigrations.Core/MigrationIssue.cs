namespace InfluxMigrations.Core;

/// <summary>
/// A simple container for any issues that are generated during the migration.
/// </summary>
public class MigrationIssue
{
    public string Id { get; init; }
    public MigrationIssueSeverity Severity { get; init; }
    public MigrationPhase Phase { get; init; }
    public MigrationIssueCategory Category { get; init; }
    public Exception? Exception { get; init; }
}