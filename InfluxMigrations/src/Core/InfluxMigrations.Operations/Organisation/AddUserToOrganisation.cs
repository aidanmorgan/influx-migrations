﻿using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Operations.Organisation;

public class AddUserToOrganisation : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public IInfluxRuntimeResolver Organisation { get; private set; }
    public IInfluxRuntimeResolver User { get; private set; }


    public AddUserToOrganisation(IOperationExecutionContext context)
    {
        _context = context;

        Organisation = InfluxRuntimeIdResolver.CreateOrganisation();
        User = InfluxRuntimeIdResolver.CreateUser();
    }

    public IMigrationOperation Initialise(Action<AddUserToOrganisation> callback)
    {
        callback(this);
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
                    $"Cannot add user to Organisation, cannot find Organisation id.");
            }

            var userId = await User.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecuteFailed($"Cannot add user to Organisation, cannot find User id");
            }

            var result = await _context.Influx.GetOrganizationsApi().AddMemberAsync(userId, organisationId);

            return OperationResults.ExecuteSuccess(new AddUserToOrganisationResult()
            {
                UserId = userId,
                OrganisationId = organisationId,
                ResourceMemberId = result.Id,
                Role = result.Role
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult r)
    {
        return OperationResults.CommitUnnecessary(r);
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult r)
    {
        var result = (AddUserToOrganisationResult?)r;

        try
        {
            if (string.IsNullOrEmpty(result?.ResourceMemberId))
            {
                return OperationResults.RollbackFailed(result,
                    $"Cannot rollback {typeof(AddUserToOrganisation)}, resource member id not provided.");
            }

            await _context.Influx.GetOrganizationsApi().DeleteMemberAsync(result.UserId, result.OrganisationId);
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class AddUserToOrganisationResult : IExecuteResult
{
    public string UserId { get; set; }
    public string OrganisationId { get; set; }
    public string ResourceMemberId { get; set; }
    public ResourceMember.RoleEnum? Role { get; set; }
}

public class AddUserToOrganisationBuilder : IMigrationOperationBuilder
{
    private string _organisationName;
    private string _organisationId;

    private string _userName;
    private string _userId;

    public AddUserToOrganisationBuilder WithUsername(string name)
    {
        _userName = name;
        return this;
    }

    public AddUserToOrganisationBuilder WithUserId(string id)
    {
        _userId = id;
        return this;
    }

    public AddUserToOrganisationBuilder WithOrganisationName(string id)
    {
        _organisationName = id;
        return this;
    }

    public AddUserToOrganisationBuilder WithOrganisationId(string id)
    {
        _organisationId = id;
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

        return new AddUserToOrganisation(context).Initialise((x) =>
        {
            x.Organisation.WithName(StringResolvable.Parse(_organisationName))
                .WithId(StringResolvable.Parse(_organisationId));
            x.User.WithName(StringResolvable.Parse(_userName)).WithId(StringResolvable.Parse(_userId));
        });
    }
}