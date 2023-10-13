namespace InfluxMigrations.Core;

/// <summary>
/// A IMigrationOperation is a step in a Migration that performs an action against an InfluxDb.
/// </summary>
public interface IMigrationOperation
{
    public Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync();

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result);

    public Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result);
}