using InfluxDB.Client;
using InfluxMigrations.Core;

namespace InfluxMigrations.Commands.Organisation;

public class RemoveUserFromOrganisation : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public InfluxRuntimeIdResolver Organisation { get; private set; }
    public InfluxRuntimeIdResolver User { get; private set; }

    public RemoveUserFromOrganisation(IOperationExecutionContext context)
    {
        _context = context;
        
        Organisation = InfluxRuntimeIdResolver.CreateOrganisation();
        User = InfluxRuntimeIdResolver.CreateUser();
    }
    
    public IMigrationOperation Initialise(Action<RemoveUserFromOrganisation> action)
    {
        action(this);
        return this;
    }
    

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var organisationId = await Organisation.GetAsync(_context);
            if (string.IsNullOrEmpty(organisationId))
            {
                return OperationResults.ExecutionFailed(
                    $"Cannot remove User for Organisation, no Organisation id set.");
            }

            var userId = await User.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecutionFailed($"Cannot remove User from Organisation, no User id set.");
            }

            var result = await _context.Influx.GetOrganizationsApi().AddMemberAsync(userId, organisationId);
            
            return OperationResults.ExecuteSuccess(new RemoveUserFromOrganisationResult()
            {
                OrganisationId = organisationId,
                UserId = userId
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecutionFailed(x);
        }
    }

    public async Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult? r)
    {
        var result = (RemoveUserFromOrganisationResult?)r;

        if (string.IsNullOrEmpty(result.OrganisationId) || string.IsNullOrEmpty(result.UserId))
        {
            return OperationResults.CommitFailed(result, 
                $"Cannot commit {typeof(RemoveUserFromOrganisationResult).FullName}, Organisation and User Id's are required.");
        }

        try
        {
            await _context.Influx.GetOrganizationsApi().DeleteMemberAsync(result.UserId, result.OrganisationId);
            return OperationResults.CommitSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.CommitFailed(result, x);
        }
    }

    public Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult? r)
    {
        return OperationResults.RollbackUnnecessary(r);
    }

}

public class RemoveUserFromOrganisationResult : IExecuteResult
{
    public string OrganisationId { get; set; }
    public string UserId { get; set; }
}

public class RemoveUserFromOrganisationBuilder : IMigrationOperationBuilder
{
    private string? _organisationId;
    private string? _organisationName;

    private string? _userId;
    private string? _userName;

    public RemoveUserFromOrganisationBuilder WithOrganisationName(string name)
    {
        _organisationName = name;
        return this;
    }

    public RemoveUserFromOrganisationBuilder WithOrganisationId  (string id)
    {
        _organisationId = id;
        return this;
    }
    
    public RemoveUserFromOrganisationBuilder WithUsername(string username)
    {
        _userName = username;
        return this;
    }
    
    public RemoveUserFromOrganisationBuilder WithUserId(string id)
    {
        _userId = id;
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(_organisationId) && string.IsNullOrEmpty(_organisationName))
        {
            throw new MigrationOperationBuildingException("No Organisation specified.");
        }

        if (string.IsNullOrEmpty(_userId) && string.IsNullOrEmpty(_userName))
        {
            throw new MigrationOperationBuildingException("No User specified.");
        }
        
        return new RemoveUserFromOrganisation(context)
            .Initialise(x =>
            {
                x.Organisation.WithId(_organisationId)
                    .WithName(_organisationName);
                x.User.WithId(_userId).WithName(_userName);
            });
    }
}