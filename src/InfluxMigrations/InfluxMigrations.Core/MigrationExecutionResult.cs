namespace InfluxMigrations.Core;

public enum ExecutionState
{
    Success,
    Failed,
    Inconsistent,
    Skipped
}

public class MigrationExecutionResult
{
    public ExecutionState ExecutionState { get; set; }
    public ExecutionState CommitState { get; set; }
    public ExecutionState RollbackState { get; set; }
    
    public ExecutionState TaskState { get; set; }
}