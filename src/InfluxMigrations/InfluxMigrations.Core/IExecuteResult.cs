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

public class EmptyExecuteResult : IExecuteResult
{
}

public interface IExceptionResult
{
    Exception Exception { get; }
}

public class ExceptionExecuteResult : IExecuteResult, IExceptionResult
{
    public Exception Exception { get; private set; }

    public ExceptionExecuteResult(Exception exception)
    {
        Exception = exception;
    }
}

public class ExceptionRollbackResult : IRollbackResult, IExceptionResult
{
    public Exception Exception { get; private set; }
    public IExecuteResult ExecutionResult { get; private set; }

    public ExceptionRollbackResult(IExecuteResult result, Exception exception)
    {
        ExecutionResult = result;
        Exception = exception;
    }
}

public class ExceptionCommitResult : ICommitResult, IExceptionResult
{
    public IExecuteResult ExecuteResult { get; private set; }
    public Exception Exception { get; private set; }

    public ExceptionCommitResult(IExecuteResult result, Exception exception)
    {
        ExecuteResult = result;
        Exception = exception;
    }
}

public class OperationResult<S, R> where S : Enum
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