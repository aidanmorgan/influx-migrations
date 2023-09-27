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

    public DeleteBucketToken Initialise(Action<DeleteBucketToken> op)
    {
        op(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var userId = await User?.GetAsync(_context);
        if (string.IsNullOrEmpty(userId))
        {
            return OperationResults.ExecuteFailed(
                $"Cannot remove User permissions for Bucket, User cannot be resolved.");
        }

        var bucketId = await Bucket?.GetAsync(_context);
        if (string.IsNullOrEmpty(bucketId))
        {
            return OperationResults.ExecuteFailed(
                $"Cannot remove User permissions for Bucket, Bucket cannot be resolved.");
        }

        try
        {
            var permissions = (await _context.Influx.GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(userId)).Where(x =>
            {
                return x.UserID == userId && x.Permissions.Any(y => y.Resource.Id == bucketId && y.Resource.Type == PermissionResource.TypeBuckets);
            })
            .ToList();

            foreach (var authorization in permissions)
            {
                // TODO : what do we do here?!?
            }

            return OperationResults.ExecuteSuccess(new DeleteBucketTokenResult()
            {
                BucketId = bucketId,
                UserId = userId,
                PermissionIds = new List<string>()
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result)
    {
        throw new NotImplementedException();
    }

    public Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        throw new NotImplementedException();
    }
}

public class DeleteBucketTokenResult : IExecuteResult
{
    public string BucketId { get; init; }
    public string UserId { get; init; }
    public List<string> PermissionIds { get; init; }
}