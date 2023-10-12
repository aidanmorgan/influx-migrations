using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;

namespace InfluxMigrations.Operations.Auth;

public class DeleteBucketToken : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;

    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();
    public IInfluxRuntimeResolver Bucket { get; private set; } = InfluxRuntimeIdResolver.CreateBucket();

    public DeleteBucketToken(IOperationExecutionContext context)
    {
        _context = context;
    }

    public DeleteBucketToken Initialise(Action<DeleteBucketToken> callback)
    {
        callback(this);
        return this;
    }
    
    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var userId = await User?.GetAsync(_context);
        if (string.IsNullOrEmpty(userId))
        {
            return OperationResults.ExecuteFailed($"Cannot find User.");
        }

        var bucketId = await Bucket?.GetAsync(_context);
        if (string.IsNullOrEmpty(bucketId))
        {
            return OperationResults.ExecuteFailed($"Cannot find Bucket.");
        }

        try
        {
            var authorizationIds = await GetAuthorizationIds(userId, bucketId);
            if (authorizationIds.Count == 0)
            {
                return OperationResults.ExecuteFailed(
                    $"Cannot find any active Authorization(s) for the User and Bucket.");
            }

            foreach (var authorizationId in authorizationIds)
            {
                var entry = await _context.Influx.GetAuthorizationsApi().FindAuthorizationByIdAsync(authorizationId);
                entry.Status = AuthorizationUpdateRequest.StatusEnum.Inactive;

                await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(entry);
            }

            return OperationResults.ExecuteSuccess(new DeleteBucketTokenResult()
            {
                AuthorizationIds = authorizationIds
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }


    public async Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult r)
    {
        var result = r as DeleteBucketTokenResult;

        try
        {
            foreach (var authorizationId in result.AuthorizationIds)
            {
                await _context.Influx.GetAuthorizationsApi().DeleteAuthorizationAsync(authorizationId);
            }

            return OperationResults.CommitSuccess(r);
        }
        catch (Exception x)
        {
            return OperationResults.CommitFailed(result, x);
        }
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult r)
    {
        var result = r as DeleteBucketTokenResult;

        try
        {
            foreach (var authorizationId in result.AuthorizationIds)
            {
                var auth = await _context.Influx.GetAuthorizationsApi().FindAuthorizationByIdAsync(authorizationId);

                if (auth.Status == AuthorizationUpdateRequest.StatusEnum.Inactive)
                {
                    auth.Status = AuthorizationUpdateRequest.StatusEnum.Active;
                    await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(auth);
                }
            }

            return OperationResults.RollbackSuccess(r);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
    
    
    private async Task<List<string>> GetAuthorizationIds(string userId, string bucketId)
    {
        var auth = (await _context.Influx.GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(userId));
        return auth.SelectMany(x =>
            {
                return x.Permissions.Select(y => new Tuple<Authorization, Permission>(x, y));
            })
            .Where(x => x.Item2.Resource.Id == bucketId &&
                        x.Item2.Resource.Type == PermissionResource.TypeBuckets &&
                        x.Item1.Status == AuthorizationUpdateRequest.StatusEnum.Active)
            .Select(x => x.Item1.Id)
            .ToList();
    }
}

public class DeleteBucketTokenResult : IExecuteResult 
{ 
    public List<string> AuthorizationIds { get; set; }
}

public class DeleteBucketTokenBuilder : IMigrationOperationBuilder
{
    public string UserId { get; private set; }

    public DeleteBucketTokenBuilder WithUserId(string v)
    {
        this.UserId = v;
        return this;
    }

    public string UserName { get; private set; }

    public DeleteBucketTokenBuilder WithUserName(string v)
    {
        this.UserName = v;
        return this;
    }

    public string BucketId { get; private set; }

    public DeleteBucketTokenBuilder WithBucketId(string v)
    {
        this.BucketId = v;
        return this;
    }

    public string BucketName { get; private set; }

    public DeleteBucketTokenBuilder WithBucketName(string v)
    {
        this.BucketName = v;
        return this;
    }
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new DeleteBucketToken(context).Initialise(x =>
        {
            x.Bucket.WithId(BucketId).WithName(BucketName);
            x.User.WithId(UserId).WithName(UserName);
        });
    }
}