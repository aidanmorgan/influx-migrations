using InfluxMigrations.Core;

namespace InfluxMigrations.Operations.IntegrationTests;

public class ForceError : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public bool ErrorOnExecute { get; init; } = false;
    public bool ErrorOnCommit { get; init; } = false;
    public bool ErrorOnRollback { get; init; } = false; 

    public ForceError(IOperationExecutionContext context)
    {
        _context = context;
    }

    public Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        return Task.FromResult(ErrorOnExecute
            ? OperationResults.ExecuteFailed("Force thrown.")
            : OperationResults.ExecuteSuccess(new ForceErrorResult()));
    }

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result)
    {
        return Task.FromResult(ErrorOnCommit
            ? OperationResults.CommitFailed(result, "Force thrown.")
            : OperationResults.CommitSuccess(result));
    }

    public Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        return Task.FromResult(ErrorOnRollback
            ? OperationResults.RollbackFailed(result, "Force thrown.")
            : OperationResults.RollbackSuccess(result));
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
        return new ForceError(context)
        {
            ErrorOnCommit = _errorCommit,
            ErrorOnExecute = _errorExecute,
            ErrorOnRollback = _errorRollback
        };
    }
}