using InfluxMigrations.Core;

namespace InfluxMigrations.Operations.Bucket;

public class RemoveOwnerFromBucket : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;

    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();
    public IInfluxRuntimeResolver Bucket { get; private set; } = InfluxRuntimeIdResolver.CreateBucket();

    public RemoveOwnerFromBucket(IOperationExecutionContext ctx)
    {
        _context = ctx;
    }

    public RemoveOwnerFromBucket Initialise(Action<RemoveOwnerFromBucket> op)
    {
        op(this);
        return this;
    }
    
    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var userId = await User.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecuteFailed($"Cannot remove Owner from Bucket, cannot resolve User.");
            }
            
            var bucketId = await Bucket.GetAsync(_context);
            if (string.IsNullOrEmpty(bucketId))
            {
                return OperationResults.ExecuteFailed($"Cannot remove Owner from Bucket, cannot resolve Bucket.");
            }
            
            await _context.Influx.GetBucketsApi().DeleteOwnerAsync(userId, bucketId);
            
            return OperationResults.ExecuteSuccess();
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

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        var userId = await User.GetAsync(_context);
        if (string.IsNullOrEmpty(userId))
        {
            return OperationResults.RollbackSuccess(result);
        }
            
        var bucketId = await Bucket.GetAsync(_context);
        if (string.IsNullOrEmpty(bucketId))
        {
            return OperationResults.RollbackSuccess(result);
        }

        try
        {
            await _context.Influx.GetBucketsApi().AddOwnerAsync(userId, bucketId);
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }

    }
}

public class RemoveOwnerFromBucketBuilder : IMigrationOperationBuilder
{
    public string UserId { get; private set; }

    public RemoveOwnerFromBucketBuilder WithUserId(string v)
    {
        this.UserId = v;
        return this;
    }

    public string UserName { get; private set; }

    public RemoveOwnerFromBucketBuilder WithUserName(string v)
    {
        this.UserName = v;
        return this;
    }

    public string BucketId { get; private set; }

    public RemoveOwnerFromBucketBuilder WithBucketId(string v)
    {
        this.BucketId = v;
        return this;
    }

    public string BucketName { get; private set; }

    public RemoveOwnerFromBucketBuilder WithBucketName(string v)
    {
        this.BucketName = v;
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new RemoveOwnerFromBucket(context).Initialise(x =>
        {
            x.User.WithId(UserId).WithName(UserName);
            x.Bucket.WithId(BucketId).WithName(BucketName);
        });
    }
}