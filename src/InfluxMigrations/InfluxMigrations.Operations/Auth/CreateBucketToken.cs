using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Auth;

public class CreateBucketToken : IMigrationOperation
{
    private static readonly IDictionary<string, Permission.ActionEnum> ActionEnums =
        new Dictionary<string, Permission.ActionEnum>()
        {
            { "read", Permission.ActionEnum.Read },
            { "write", Permission.ActionEnum.Write }
        };

    private readonly IOperationExecutionContext _context;
    public InfluxRuntimeIdResolver Bucket { get; set; }
    public InfluxRuntimeIdResolver User { get; set; }

    public IResolvable<string?>? TokenDescription { get; set; }
    public List<IResolvable<string?>> Permissions { get; set; } = new List<IResolvable<string?>>();

    public CreateBucketToken(IOperationExecutionContext context)
    {
        _context = context;

        Bucket = InfluxRuntimeIdResolver.CreateBucket();
        User = InfluxRuntimeIdResolver.CreateUser();
    }

    public CreateBucketToken Initialise(Action<CreateBucketToken> callback)
    {
        callback(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var bucketId = await Bucket.GetAsync(_context);
            if (string.IsNullOrEmpty(bucketId))
            {
                return OperationResults.ExecutionFailed($"Coult not resolve Bucket id.");
            }

            var description = TokenDescription?.Resolve(_context) ??
                              $"Token created via Migration {_context.MigrationExecutionContext.Version}";
            var userId = await User.GetAsync(_context);

            Authorization auth = null;

            var bucket = await _context.Influx.GetBucketsApi().FindBucketByIdAsync(bucketId);

            var actionEnums = Permissions.Select(x => x.Resolve(_context)).Select(x => ActionEnums[x])
                .ToList();

            // if a userid is set, then it should be set as the owner of the authroization, otherwise the authorization
            // is created with the Auth token as the owner.
            if (userId != null)
            {
                auth = await _context.Influx.GetAuthorizationsApi().CreateAuthorizationAsync(
                    new AuthorizationPostRequest(
                        bucket.OrgID,
                        userId,
                        actionEnums.Select(x => new Permission(x,
                            new PermissionResource()
                            {
                                Id = bucketId,
                                Type = PermissionResource.TypeBuckets
                            })
                        ).ToList(),
                        AuthorizationUpdateRequest.StatusEnum.Active,
                        description
                    )
                );
            }
            else
            {
                auth = await _context.Influx.GetAuthorizationsApi().CreateAuthorizationAsync(
                    bucket.OrgID,
                    actionEnums.Select(x => new Permission(x,
                        new PermissionResource()
                        {
                            Id = bucketId,
                            Type = PermissionResource.TypeBuckets
                        })).ToList());
            }

            return OperationResults.ExecuteSuccess(new CreateBucketTokenResult()
            {
                Token = auth.Token,
                TokenId = auth.Id,
                BucketId = bucketId,
                OwnerId = auth.Links?.User
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

public class CreateBucketTokenResult : IExecuteResult
{
    public string BucketId { get; set; }
    public string Token { get; set; }
    public string TokenId { get; set; }
    public string? OwnerId { get; set; }

    public CreateBucketTokenResult()
    {
    }
}

public class CreateBucketTokenBuilder : IMigrationOperationBuilder
{
    public string TokenName { get; private set; }
    public string BucketName { get; private set; }
    public string BucketId { get; private set; }
    public string UserName { get; private set; }
    public string UserId { get; private set; }

    public List<string> Permissions { get; private set; } = new List<string>();

    public CreateBucketTokenBuilder WithTokenName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new MigrationConfigurationException($"Cannot set a null write token name.");
        }

        TokenName = name;
        return this;
    }

    public CreateBucketTokenBuilder WithBucketName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new MigrationConfigurationException("Cannot set a null bucket name.");
        }

        BucketName = name;
        return this;
    }

    public CreateBucketTokenBuilder WithBucketId(string name)
    {
        BucketId = name;
        return this;
    }

    public CreateBucketTokenBuilder WithPermission(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new MigrationConfigurationException("Cannot add a null permission");
        }

        Permissions.Add(name);
        return this;
    }

    public CreateBucketTokenBuilder WithUserName(string username)
    {
        UserName = username;
        return this;
    }

    public CreateBucketTokenBuilder WithUserId(string id)
    {
        UserId = id;
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        var permissions = Permissions.Select(x => StringResolvable.Parse(x) ?? null).Where(x => x != null).ToList();
        if (permissions == null || permissions.Count == 0)
        {
            throw new MigrationOperationBuildingException("No permissions specified.");
        }

        if (string.IsNullOrEmpty(BucketId) && string.IsNullOrEmpty(BucketName))
        {
            throw new MigrationOperationBuildingException("No Bucket specified.");
        }

        if (string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(UserId))
        {
            throw new MigrationOperationBuildingException("No User specified.");
        }

        return new CreateBucketToken(context)
        {
            Permissions = permissions!,
            TokenDescription = StringResolvable.Parse(TokenName)
        }.Initialise(val =>
        {
            val.Bucket.WithId(StringResolvable.Parse(BucketId)).WithName(StringResolvable.Parse(BucketName));
            val.User.WithId(StringResolvable.Parse(UserId)).WithName(StringResolvable.Parse(UserName));
        });
    }
}