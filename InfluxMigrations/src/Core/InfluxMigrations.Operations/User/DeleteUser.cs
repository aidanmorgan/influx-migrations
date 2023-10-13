using InfluxMigrations.Core;

namespace InfluxMigrations.Operations.User;

public class DeleteUser : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    
    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();

    public DeleteUser(IOperationExecutionContext context)
    {
        _context = context;
    }

    public DeleteUser Initialise(Action<DeleteUser> act)
    {
        act(this);
        return this;
    }
    
    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var userId = await User.GetAsync(_context);

        if (string.IsNullOrEmpty(userId))
        {
            return OperationResults.ExecuteFailed($"Cannot remove User, cannot resolve User id.");
        }

        try
        {
            var user = await _context.Influx.GetUsersApi().FindUserByIdAsync(userId);
            user.Status = InfluxDB.Client.Api.Domain.User.StatusEnum.Inactive;

            await _context.Influx.GetUsersApi().UpdateUserAsync(user);
            return OperationResults.ExecuteSuccess();
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    public async Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result)
    {
        var userId = await User.GetAsync(_context);

        try
        {
            await _context.Influx.GetUsersApi().DeleteUserAsync(userId);
            return OperationResults.CommitSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.CommitFailed(result, x);
        }
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        var userId = await User.GetAsync(_context);

        try
        {
            var user = await _context.Influx.GetUsersApi().FindUserByIdAsync(userId);
            user.Status = InfluxDB.Client.Api.Domain.User.StatusEnum.Active;

            await _context.Influx.GetUsersApi().UpdateUserAsync(user);
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class DeleteUserBuilder : IMigrationOperationBuilder
{
    public string UserId { get; private set; }

    public DeleteUserBuilder WithUserId(string v)
    {
        this.UserId = v;
        return this;
    }

    public string UserName { get; private set; }

    public DeleteUserBuilder WithUserName(string v)
    {
        this.UserName = v;
        return this;
    }
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new DeleteUser(context).Initialise(x =>
        {
            x.User.WithId(UserId).WithName(UserName);
        });
    }
}