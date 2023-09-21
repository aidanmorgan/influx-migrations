﻿using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Setup;

public class Onboarding : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    
    public IResolvable<string> Username { get; init; }
    public IResolvable<string> Password { get; init; }
    public IResolvable<string> Organisation { get; init; }
    public IResolvable<string> Bucket { get; init; }
    public IResolvable<string> Token { get; init; }

    public Onboarding(IOperationExecutionContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var allowed = await _context.Influx.IsOnboardingAllowedAsync();

            if (!allowed)
            {
                return OperationResults.ExecutionFailed("Cannot onboard Influx, it is not allowed.");
            }

            var adminToken = Token.Resolve(_context);

            var result = await _context.Influx.OnboardingAsync(
                new OnboardingRequest(
                    Username.Resolve(_context),
                    Password.Resolve(_context),
                    Organisation.Resolve(_context),
                    Bucket.Resolve(_context),
                    token: Token.Resolve(_context)
                ));

            if (!string.IsNullOrEmpty(adminToken))
            {
                _context.Accept(new ChangeAdminTokenVisitor(adminToken));
            }

            return OperationResults.ExecuteSuccess(new OnboardingResult()
            {
                BucketId = result.Bucket.Id,
                BucketName = result.Bucket.Name,
                OrganisationId = result.Org.Id,
                OrganisationName = result.Org.Name,
                User = result.User.Id,
                UserName = result.User.Name
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecutionFailed(x);
        }
    }

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result)
    {
        return OperationResults.CommitUnnecessary(result);
    }

    public Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        return OperationResults.RollbackImpossible(result);
    }
}

public class OnboardingResult : IExecuteResult
{
    public string BucketId { get; init; }
    public string BucketName { get; init; }
    public string OrganisationId { get; init; }
    public string OrganisationName { get; init; }
    public string User { get; init; }
    public string UserName { get; init; }
}

public class OnboardingBuilder : IMigrationOperationBuilder
{
    public string OrganisationName { get; private set; }
    public string BucketName { get; private set; }
    public string UserName { get; private set; }
    public string Password { get; private set; }
    public string Token { get; private set; }

    public OnboardingBuilder WithOrganisation(string name)
    {
        this.OrganisationName = name;
        return this;
    }

    public OnboardingBuilder WithBucket(string name)
    {
        this.BucketName = name;
        return this;
    }

    public OnboardingBuilder WithUsername(string name)
    {
        this.UserName = name;
        return this;
    }

    public OnboardingBuilder WithPassword(string password)
    {
        this.Password = password;
        return this;
    }

    public OnboardingBuilder WithAdminToken(string token)
    {
        this.Token = token;
        return this;
    }
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new Onboarding(context)
        {
            Bucket = StringResolvable.Parse(BucketName),
            Organisation = StringResolvable.Parse(OrganisationName),
            Password = StringResolvable.Parse(Password),
            Username = StringResolvable.Parse(UserName),
            Token = StringResolvable.Parse(Token)
        };
    }
}

/// <summary>
/// A IContextVisitor implementation that will change the admin token used by influx.
/// </summary>
public class ChangeAdminTokenVisitor : IContextVisitor
{
    private readonly string _token;

    public ChangeAdminTokenVisitor(string token)
    {
        _token = token;
    }
    
    public void Visit(IOperationExecutionContext ctx)
    {
        ctx.MigrationExecutionContext.Accept(this);
    }

    public void Visit(IMigrationExecutionContext ctx)
    {
        ctx.EnvironmentContext.Accept(this);
    }

    public void Visit(IMigrationEnvironmentContext ctx)
    {
        ctx.InfluxFactory.WithToken(_token);
    }
}