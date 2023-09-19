namespace InfluxMigrations.Core;

public enum OperationExecutionState
{
    NotExecuted,
    Success,
    Skipped,
    Failed
}

public enum OperationRollbackState
{
    NotExecuted,
    Unnecessary,
    Impossible,
    Success,
    Skipped,
    Failed
}

public enum OperationCommitState
{
    NotExecuted,
    Unnecessary,    
    Success,
    Skipped,
    Failed
}

public static class OperationResults
{
    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackUnnecessary(IRollbackResult result)
    {
        return Task.FromResult(new OperationResult<OperationRollbackState, IRollbackResult>(OperationRollbackState.Unnecessary, result));
    }
    
    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackUnnecessary(IExecuteResult result)
    {
        return RollbackUnnecessary(new EmptyRollbackResult(result));
    }

    public static Task<OperationResult<OperationCommitState, ICommitResult>> CommitUnnecessary(ICommitResult result)
    {
        return Task.FromResult(new OperationResult<OperationCommitState, ICommitResult>(OperationCommitState.Unnecessary, result));
        
    }
    
    public static Task<OperationResult<OperationCommitState, ICommitResult>> CommitUnnecessary(IExecuteResult result)
    {
        return CommitUnnecessary(new EmptyCommitResult(result));
    } 
    
    public static OperationResult<OperationExecutionState, IExecuteResult> ExecuteSuccess(IExecuteResult result)
    {
        return new OperationResult<OperationExecutionState, IExecuteResult>(OperationExecutionState.Success, result);
    }

    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackImpossible(IRollbackResult result)
    {
        return Task.FromResult(new OperationResult<OperationRollbackState, IRollbackResult>(OperationRollbackState.Impossible, result));
    }
    
    public static Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackImpossible(IExecuteResult result)
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

    public static OperationResult<OperationRollbackState, IRollbackResult> RollbackFailed(IExecuteResult result, Exception x)
    {
        return new OperationResult<OperationRollbackState, IRollbackResult>(OperationRollbackState.Failed, new ExceptionRollbackResult(result, x));
    }

    public static OperationResult<OperationExecutionState,IExecuteResult> ExecutionFailed(string x)
    {
        return ExecutionFailed(new MigrationExecutionException(x));
    }

    public static OperationResult<OperationCommitState,ICommitResult> CommitFailed(IExecuteResult result, string x)
    {
        return CommitFailed(result, new MigrationExecutionException(x));
    }

    public static OperationResult<OperationCommitState,ICommitResult> CommitFailed(IExecuteResult result, Exception x)
    {
        return new OperationResult<OperationCommitState, ICommitResult>(OperationCommitState.Failed, new ExceptionCommitResult(result, x));
    }

    public static OperationResult<OperationCommitState, ICommitResult> CommitSuccess(IExecuteResult result)
    {
        return new OperationResult<OperationCommitState, ICommitResult>(OperationCommitState.Success, new EmptyCommitResult(result));
    }

    public static OperationResult<OperationExecutionState,IExecuteResult> ExecutionFailed(Exception x)
    {
        return new OperationResult<OperationExecutionState, IExecuteResult>(OperationExecutionState.Failed, new ExceptionExecuteResult(x));
    }

    public static OperationResult<OperationRollbackState,IRollbackResult> RollbackFailed(IExecuteResult result, string x)
    {
        return RollbackFailed(result, new MigrationExecutionException(x));
    }
}

public class ExceptionExecuteResult : IExecuteResult
{
    public Exception Exception { get; private set; }

    public ExceptionExecuteResult(Exception exception)
    {
        Exception = exception;
    }
}

public class ExceptionRollbackResult : IRollbackResult
{
    public Exception Exception { get; private set; }
    public IExecuteResult ExecutionResult { get; private set; }

    public ExceptionRollbackResult(IExecuteResult result, Exception exception)
    {
        ExecutionResult = result;
        Exception = exception;
    }
}

public class ExceptionCommitResult : ICommitResult
{
    public IExecuteResult ExecuteResult { get; private set; }
    
    public Exception Exception { get; private set; }

    public ExceptionCommitResult(IExecuteResult result, Exception exception)
    {
        ExecuteResult = result;
        Exception = exception;
    }
}

public class OperationResult<S,R> where S : Enum
{
    public S State { get; private set; }
    public R Result { get; private set; }

    public OperationResult(S state, R result)
    {
        State = state;
        Result = result;
    }
}

/// <summary>
/// Flagging interface for the result of a command invocation.
/// </summary>
public interface IExecuteResult
{
    
}

public interface IRollbackResult
{
    IExecuteResult ExecutionResult { get; }
}

public class EmptyRollbackResult : IRollbackResult
{
    public IExecuteResult ExecutionResult { get; }
    
    public EmptyRollbackResult(IExecuteResult executionResult)
    {
        ExecutionResult = executionResult;
    }
}

public interface ICommitResult
{
    IExecuteResult ExecuteResult { get; }
}

public class EmptyCommitResult : ICommitResult
{
    public IExecuteResult ExecuteResult { get; }
    
    public EmptyCommitResult(IExecuteResult executeResult)
    {
        ExecuteResult = executeResult;
    }
}