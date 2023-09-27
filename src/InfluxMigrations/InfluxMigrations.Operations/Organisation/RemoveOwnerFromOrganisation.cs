using InfluxMigrations.Core;

namespace InfluxMigrations.Operations.Organisation;

public class RemoveOwnerFromOrganisation : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();
    public IInfluxRuntimeResolver Organisation { get; private set; } = InfluxRuntimeIdResolver.CreateOrganisation();

    public RemoveOwnerFromOrganisation(IOperationExecutionContext context)
    {
        _context = context;
    }

    public RemoveOwnerFromOrganisation Initialise(Action<RemoveOwnerFromOrganisation> op)
    {
        op(this);
        return this;
    }
    
    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var userId = await User.GetAsync(_context);
        if (string.IsNullOrEmpty(userId))
        {
            return OperationResults.ExecuteFailed($"Cannot remove Owner from Organisation, cannot resolve User.");
        }

        var orgId = await Organisation.GetAsync(_context);
        if (string.IsNullOrEmpty(orgId))
        {
            return OperationResults.ExecuteFailed($"Cannot remove Owner from Organisation, cannot resolve Organisation.");
        }

        try
        {
            await _context.Influx.GetOrganizationsApi().DeleteOwnerAsync(userId, orgId);
            return OperationResults.ExecuteSuccess();
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result)
    {
        return OperationResults.CommitUnnecessary(result);
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        var userId = await User.GetAsync(_context);
        var orgId = await Organisation.GetAsync(_context);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(orgId))
        {
            return OperationResults.RollbackSuccess(result);
        }

        try
        {
            await _context.Influx.GetOrganizationsApi().AddOwnerAsync(userId, orgId);
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class RemoveOwnerFromOrganisationBuilder : IMigrationOperationBuilder
{
    public string UserId { get; private set; }

    public RemoveOwnerFromOrganisationBuilder WithUserId(string v)
    {
        this.UserId = v;
        return this;
    }

    public string UserName { get; private set; }

    public RemoveOwnerFromOrganisationBuilder WithUserName(string v)
    {
        this.UserName = v;
        return this;
    }

    public string OrganisationId { get; private set; }

    public RemoveOwnerFromOrganisationBuilder WithOrganisationId(string v)
    {
        this.OrganisationId = v;
        return this;
    }

    public string OrganisationName { get; private set; }

    public RemoveOwnerFromOrganisationBuilder WithOrganisationName(string v)
    {
        this.OrganisationName = v;
        return this;
    }
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new RemoveOwnerFromOrganisation(context).Initialise(x =>
        {
            x.Organisation.WithId(OrganisationId).WithName(OrganisationName);
            x.User.WithId(UserId).WithName(UserName);
        });
    }
}