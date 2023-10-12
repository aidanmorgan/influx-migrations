namespace InfluxMigrations.Core;

public static class OperationResults
{
    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackUnnecessary(
        IRollbackResult result)
    {
        return Task.FromResult(
            new OperationResult<OperationRollbackState, IRollbackResult>(OperationRollbackState.Unnecessary, result));
    }

    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackUnnecessary(
        IExecuteResult result)
    {
        return RollbackUnnecessary(new EmptyRollbackResult(result));
    }

    public static Task<OperationResult<OperationCommitState, ICommitResult>> CommitUnnecessary(ICommitResult result)
    {
        return Task.FromResult(
            new OperationResult<OperationCommitState, ICommitResult>(OperationCommitState.Unnecessary, result));
    }

    public static Task<OperationResult<OperationCommitState, ICommitResult>> CommitUnnecessary(IExecuteResult result)
    {
        return CommitUnnecessary(new EmptyCommitResult(result));
    }

    public static OperationResult<OperationExecutionState, IExecuteResult> ExecuteSuccess(IExecuteResult result)
    {
        return new OperationResult<OperationExecutionState, IExecuteResult>(OperationExecutionState.Success, result);
    }

    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackImpossible(
        IRollbackResult result)
    {
        return Task.FromResult(
            new OperationResult<OperationRollbackState, IRollbackResult>(OperationRollbackState.Impossible, result));
    }

    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackImpossible(
        IExecuteResult result)
    {
        return RollbackImpossible(new EmptyRollbackResult(result));
    }

    public static OperationResult<OperationRollbackState, IRollbackResult> RollbackSuccess(IExecuteResult result)
    {
        return RollbackSuccess(new EmptyRollbackResult(result));
    }

    public static OperationResult<OperationRollbackState, IRollbackResult> RollbackSuccess(IRollbackResult result)
    {
        return new OperationResult<OperationRollbackState, IRollbackResult>(OperationRollbackState.Success, result);
    }

    public static OperationResult<OperationRollbackState, IRollbackResult> RollbackFailed(IExecuteResult result,
        Exception x)
    {
        return new OperationResult<OperationRollbackState, IRollbackResult>(OperationRollbackState.Failed,
            new ExceptionRollbackResult(result, x));
    }

    public static OperationResult<OperationExecutionState, IExecuteResult> ExecuteFailed(string x)
    {
        return ExecuteFailed(new MigrationExecutionException(x));
    }

    public static OperationResult<OperationCommitState, ICommitResult> CommitFailed(IExecuteResult result, string x)
    {
        return CommitFailed(result, new MigrationExecutionException(x));
    }

    public static OperationResult<OperationCommitState, ICommitResult> CommitFailed(IExecuteResult result, Exception x)
    {
        return new OperationResult<OperationCommitState, ICommitResult>(OperationCommitState.Failed,
            new ExceptionCommitResult(result, x));
    }

    public static OperationResult<OperationCommitState, ICommitResult> CommitSuccess(IExecuteResult result)
    {
        return new OperationResult<OperationCommitState, ICommitResult>(OperationCommitState.Success,
            new EmptyCommitResult(result));
    }

    public static OperationResult<OperationExecutionState, IExecuteResult> ExecuteFailed(Exception x)
    {
        return new OperationResult<OperationExecutionState, IExecuteResult>(OperationExecutionState.Failed,
            new ExceptionExecuteResult(x));
    }

    public static OperationResult<OperationRollbackState, IRollbackResult> RollbackFailed(IExecuteResult result,
        string x)
    {
        return RollbackFailed(result, new MigrationExecutionException(x));
    }

    public static OperationResult<OperationExecutionState, IExecuteResult> ExecuteSuccess()
    {
        return new OperationResult<OperationExecutionState, IExecuteResult>(OperationExecutionState.Success,
            new EmptyExecuteResult());
    }
}