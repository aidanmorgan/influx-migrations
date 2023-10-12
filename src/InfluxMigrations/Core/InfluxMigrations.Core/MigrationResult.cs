namespace InfluxMigrations.Core;

public class MigrationResult
{
    public string Version { get; init; }

    private readonly List<MigrationIssue> _issues = new List<MigrationIssue>();
    public List<MigrationIssue> Issues => new List<MigrationIssue>(_issues);

    /// <summary>
    /// Adds a MigrationIssue to the list of issues about this Migration.
    /// </summary>
    public MigrationIssue AddIssue(MigrationIssue issue)
    {
        _issues.Add(issue);
        return issue;
    }

    public MigrationIssue AddIssue<TS, TR>(string id, MigrationIssueCategory category, MigrationPhase phase, MigrationIssueSeverity severity, OperationResult<TS, TR> result = null) where TS : Enum
    {
        var issue = new MigrationIssue()
        {
            Id = id,
            Category = category,
            Phase = phase,
            Severity = severity,
            Exception = result.Result is IExceptionResult exceptionResult ? exceptionResult.Exception : null
        };

        _issues.Add(issue);
        return issue;
    }

    /// <summary>
    /// Returns true if the migration was a success, false if any issues were encountered.
    /// </summary>
    public bool Success => _issues.Count == 0;

    /// <summary>
    /// Returns true if the underlying database is now in an inconsistent state, false otherwise
    /// </summary>
    public bool Inconsistent => _issues.Any(x => x.Phase == MigrationPhase.Rollback);

    /// <summary>
    /// Which direction is the operation going - forwards or backwards?
    /// </summary>
    public MigrationDirection Direction { get; set; }
    
    /// <summary>
    /// Any options that were used to configure the Migration.
    /// </summary>
    public MigrationOptions Options { get; set; }
}