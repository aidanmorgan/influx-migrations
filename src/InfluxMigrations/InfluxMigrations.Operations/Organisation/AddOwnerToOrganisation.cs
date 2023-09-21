using InfluxMigrations.Core;

namespace InfluxMigrations.Commands.Organisation;

public class AddOwnerToOrganisation : IMigrationOperation
{
    private IOperationExecutionContext _context;

    public IInfluxRuntimeResolver Organisation { get; private set; } = InfluxRuntimeIdResolver.CreateOrganisation();
    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();

    public AddOwnerToOrganisation(IOperationExecutionContext ctx)
    {
        this._context = ctx;
    }

    public AddOwnerToOrganisation Initialise(Action<AddOwnerToOrganisation> callback)
    {
        callback(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var userId = await User.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecuteFailed($"Cannot add User as owner of Organisation, User not found.");
            }

            var organisationId = await Organisation.GetAsync(_context);
            if (string.IsNullOrEmpty(organisationId))
            {
                return OperationResults.ExecuteFailed(
                    $"Cannot add User as an owner of Organisation, Organisation not found.");
            }

            var result = await _context.Influx.GetOrganizationsApi().AddOwnerAsync(userId, organisationId);

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
        try
        {
            var userId = await User.GetAsync(_context);
            var orgId = await Organisation.GetAsync(_context);

            await _context.Influx.GetOrganizationsApi().DeleteOwnerAsync(userId, orgId);

            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class AddOwnerToOrganisationBuilder : IMigrationOperationBuilder
{
    public string UserId { get; private set; }
    public string UserName { get; private set; }

    public string OrganisationName { get; private set; }

    public AddOwnerToOrganisationBuilder WithOrganisationName(string v)
    {
        this.OrganisationName = v;
        return this;
    }

    public string OrganisationId { get; private set; }

    public AddOwnerToOrganisationBuilder WithOrganisationId(string v)
    {
        this.OrganisationId = v;
        return this;
    }

    public AddOwnerToOrganisationBuilder WithUserId(string id)
    {
        UserId = id;
        return this;
    }

    public AddOwnerToOrganisationBuilder WithUserName(string name)
    {
        UserName = name;
        return this;
    }
    
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new AddOwnerToOrganisation(context).Initialise(x =>
        {
            x.User.WithName(UserName).WithId(UserId);
            x.Organisation.WithName(OrganisationName).WithId(OrganisationId);
        });
    }
}