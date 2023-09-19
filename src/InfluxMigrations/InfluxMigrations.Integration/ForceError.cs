using InfluxMigrations.Core;

namespace InfluxMigrations.IntegrationTests;

public class ForceError : IMigrationOperation
{
    private IOperationExecutionContext _context;
    private readonly bool _throwExecute = false;
    private readonly bool _throwCommit = false;
    private readonly bool _throwRollback = false;

    public ForceError(IOperationExecutionContext context, bool throwExecute, bool throwCommit, bool throwRollback)
    {
        _context = context;
        _throwExecute = throwExecute;
        _throwCommit = throwCommit;
        _throwRollback = throwRollback;
    }

    public Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        return Task.FromResult(_throwExecute ? OperationResults.ExecutionFailed("Force thrown.") : OperationResults.ExecuteSuccess(new ForceErrorResult()));
    }

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result)
    {
        return Task.FromResult(_throwCommit ? OperationResults.CommitFailed(result, "Force thrown.") : OperationResults.CommitSuccess(result));
    }

    public Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        return Task.FromResult(_throwRollback ? OperationResults.RollbackFailed(result, "Force thrown.") : OperationResults.RollbackSuccess(result));
    }
}

public class ForceErrorResult : IExecuteResult
{
    
}

public class ForceErrorBuilder : IMigrationOperationBuilder
{
    private bool _errorExecute;
    private bool _errorCommit;
    private bool _errorRollback;

    public ForceErrorBuilder()
    {
        
    }

    public ForceErrorBuilder ErrorExecute()
    {
        _errorExecute = true;
        return this;
    }
    
    public ForceErrorBuilder ErrorCommit()
    {
        _errorCommit = true;
        return this;
    }
    
    public ForceErrorBuilder ErrorRollback()
    {
        _errorRollback = true;
        return this;
    }
    
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new ForceError(context, _errorExecute, _errorCommit, _errorRollback);
    }
}