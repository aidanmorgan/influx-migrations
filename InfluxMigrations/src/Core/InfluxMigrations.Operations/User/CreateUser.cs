using Flurl.Http;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Operations.User;

public class CreateUser : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;

    public IResolvable<string?> Username { get; set; }
    public IResolvable<string?> Password { get; set; }


    public CreateUser(IOperationExecutionContext ctx)
    {
        this._context = ctx;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var username = Username?.Resolve(_context);

            if (string.IsNullOrEmpty(username))
            {
                return OperationResults.ExecuteFailed($"Cannot create user, cannot resolve Username.");
            }

            var user = await _context.Influx.GetUsersApi().CreateUserAsync(username);

            var requestedPassword = Password?.Resolve(_context);
            if (!string.IsNullOrEmpty(requestedPassword))
            {
                var response = await _context.Influx.Raw()
                    .AppendPathSegment($"/api/v2/users/{user.Id}/password")
                    .PostJsonAsync(new
                    {
                        password = requestedPassword
                    });

                if (response.StatusCode != 204)
                {
                    return OperationResults.ExecuteFailed(
                        $"Could not set initial user password for User with id {user.Id}.");
                }
            }

            return OperationResults.ExecuteSuccess(new CreateUserResult()
            {
                Name = user.Name,
                Id = user.Id
            });
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

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult r)
    {
        var result = (CreateUserResult)r;
        try
        {
            if (string.IsNullOrEmpty(result?.Id))
            {
                return OperationResults.RollbackFailed(result,
                    $"Cannot roll back {typeof(CreateUser).FullName}, no User Id provided.");
            }

            await _context.Influx.GetUsersApi().DeleteUserAsync(result.Id);
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class CreateUserResult : IExecuteResult
{
    public string Id { get; init; }
    public string Name { get; init; }
}

public class CreateUserBuilder : IMigrationOperationBuilder
{
    public string UserName { get; private set; }
    public string Password { get; private set; }

    private readonly List<IMigrationTaskBuilder> _output = new List<IMigrationTaskBuilder>();

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(UserName))
        {
            throw new MigrationOperationBuildingException("No Username specified.");
        }

        return new CreateUser(context)
        {
            Username = StringResolvable.Parse(UserName),
            Password = StringResolvable.Parse(Password)
        };
    }

    public CreateUserBuilder WithUsername(string username)
    {
        this.UserName = username;
        return this;
    }

    public CreateUserBuilder WithPassword(string password)
    {
        this.Password = password;
        return this;
    }
}