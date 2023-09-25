namespace InfluxMigrations.Core;

public class MigrationIssue
{
    public string Id { get; init; }
    public MigrationIssueSeverity Severity { get; init; }
    public MigrationPhase Phase { get; init; }
    public MigrationIssueCategory Category { get; init; }
    public Exception? Exception { get; init; }
}