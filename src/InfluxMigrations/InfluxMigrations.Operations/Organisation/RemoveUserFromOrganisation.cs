using InfluxDB.Client;
using InfluxMigrations.Core;

namespace InfluxMigrations.Commands.Organisation;

public class RemoveUserFromOrganisation : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public IInfluxRuntimeResolver Organisation { get; private set; }
    public IInfluxRuntimeResolver User { get; private set; }

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
                return OperationResults.ExecuteFailed(
                    $"Cannot remove User for Organisation, no Organisation id set.");
            }

            var userId = await User.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecuteFailed($"Cannot remove User from Organisation, no User id set.");
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
            return OperationResults.ExecuteFailed(x);
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
    public string OrganisationId { get; private set; }
    public string OrganisationName { get; private set; }

    public string? UserId { get; private set; }
    public string? UserName { get; private set; }

    public RemoveUserFromOrganisationBuilder WithOrganisationName(string name)
    {
        OrganisationName = name;
        return this;
    }

    public RemoveUserFromOrganisationBuilder WithOrganisationId(string id)
    {
        OrganisationId = id;
        return this;
    }

    public RemoveUserFromOrganisationBuilder WithUsername(string username)
    {
        UserName = username;
        return this;
    }

    public RemoveUserFromOrganisationBuilder WithUserId(string id)
    {
        UserId = id;
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(OrganisationId) && string.IsNullOrEmpty(OrganisationName))
        {
            throw new MigrationOperationBuildingException("No Organisation specified.");
        }

        if (string.IsNullOrEmpty(UserId) && string.IsNullOrEmpty(UserName))
        {
            throw new MigrationOperationBuildingException("No User specified.");
        }

        return new RemoveUserFromOrganisation(context)
            .Initialise(x =>
            {
                x.Organisation.WithId(OrganisationId)
                    .WithName(OrganisationName);
                x.User.WithId(UserId).WithName(UserName);
            });
    }
}