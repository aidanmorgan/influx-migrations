using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Operations.Auth;

public class CreateBucketToken : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public IInfluxRuntimeResolver Bucket { get; set; }
    public IInfluxRuntimeResolver User { get; set; }

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
            var bucketId = await Bucket?.GetAsync(_context);
            if (string.IsNullOrEmpty(bucketId))
            {
                return OperationResults.ExecuteFailed($"Coult not resolve Bucket id.");
            }

            var description = TokenDescription?.Resolve(_context) ??
                              $"Token created via Migration {_context.MigrationExecutionContext.Version}";
            var userId = await User?.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecuteFailed("Could not resolve User id.");
            }

            var bucket = await _context.Influx.GetBucketsApi().FindBucketByIdAsync(bucketId);

            var actionEnums = Permissions
                .Select(x => x.Resolve(_context))
                .Select(TokenCommon.MapPermission)
                .ToList();

            if (actionEnums.Any(x => x == null))
            {
                return OperationResults.ExecuteFailed($"Cannot find permission for provided value.");
            }

            var permissions = actionEnums
                .Select(x => new Permission((Permission.ActionEnum)x!,
                    new PermissionResource(type: PermissionResource.TypeBuckets, id: bucketId)))
                .ToList();
            
            var existing = await _context.Influx.GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(userId);

            var auth = await _context.Influx.GetAuthorizationsApi().CreateAuthorizationAsync(
                new AuthorizationPostRequest(
                    bucket.OrgID,
                    userId,
                    permissions,
                    AuthorizationUpdateRequest.StatusEnum.Inactive,
                    description
                )
            );

            return OperationResults.ExecuteSuccess(new CreateBucketTokenResult()
            {
                Token = auth.Token,
                TokenId = auth.Id,
                BucketId = bucketId,
                OwnerId = userId
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    public async Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult r)
    {
        var result = (CreateBucketTokenResult)r;

        try
        {
            var token = await _context.Influx.GetAuthorizationsApi().FindAuthorizationByIdAsync(result.TokenId);
            token.Status = AuthorizationUpdateRequest.StatusEnum.Active;
            await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(token);

            return OperationResults.CommitSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.CommitFailed(result, x);
        }
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult r)
    {
        var result = (CreateBucketTokenResult)r;

        try
        {
            await _context.Influx.GetAuthorizationsApi().DeleteAuthorizationAsync(result.TokenId);
            return OperationResults.RollbackSuccess(r);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }


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