using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Organisation;

public class CreateOrganisation : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;

    public IResolvable<string> OrganisationName { get; set; }

    public CreateOrganisation(IOperationExecutionContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult?>> ExecuteAsync()
    {
        try
        {
            var name = OrganisationName.Resolve(_context);

            if (string.IsNullOrEmpty(name))
            {
                return OperationResults.ExecutionFailed("Cannot create organisation, name has not been specified.");
            }

            var result = await _context.Influx.GetOrganizationsApi().CreateOrganizationAsync(name);
            result.Status = Organization.StatusEnum.Active;

            await _context.Influx.GetOrganizationsApi().UpdateOrganizationAsync(result);

            return OperationResults.ExecuteSuccess(new CreateOrganisationResult()
            {
                Id = result.Id,
                Name = result.Name
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecutionFailed(x);
        }
    }

    public Task<OperationResult<OperationCommitState, ICommitResult?>> CommitAsync(IExecuteResult? result)
    {
        return OperationResults.CommitUnnecessary(result);
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult?>> RollbackAsync(IExecuteResult? r)
    {
        var result = (CreateOrganisationResult?)r;

        try
        {
            if (string.IsNullOrEmpty(result?.Id))
            {
                return OperationResults.RollbackFailed(result,
                    $"Cannot rollback {typeof(CreateOrganisation)}, no Organisation id was created.");
            }

            await _context.Influx.GetOrganizationsApi().DeleteOrganizationAsync(result.Id);
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class CreateOrganisationResult : IExecuteResult
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class CreateOrganisationBuilder : IMigrationOperationBuilder
{
    private string _name;

    public CreateOrganisationBuilder WithName(string name)
    {
        this._name = name;
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(_name))
        {
            throw new MigrationOperationBuildingException("No Organisation specified.");
        }

        return new CreateOrganisation(context)
        {
            OrganisationName = StringResolvable.Parse(_name)
        };
    }
}