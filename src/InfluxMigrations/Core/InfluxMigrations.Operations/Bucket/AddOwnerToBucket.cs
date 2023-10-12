using InfluxMigrations.Core;

namespace InfluxMigrations.Operations.Bucket;

public class AddOwnerToBucket : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public IInfluxRuntimeResolver Bucket { get; private set; } = InfluxRuntimeIdResolver.CreateBucket();
    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();

    public AddOwnerToBucket(IOperationExecutionContext context)
    {
        _context = context;
    }
    
    public AddOwnerToBucket Initialise(Action<AddOwnerToBucket> callback)
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
                return OperationResults.ExecuteFailed($"Cannot add owner to Bucket, Bucket cannot be found.");
            }
            
            var userId = await User.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecuteFailed($"Cannot add Owner to Bucket, User cannot be found.");
            }

            await _context.Influx.GetBucketsApi().AddOwnerAsync(userId, bucketId);
            
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
        try
        {
            var bucketId = await Bucket.GetAsync(_context);
            var userId = await User.GetAsync(_context);

            await _context.Influx.GetBucketsApi().DeleteOwnerAsync(userId, bucketId);
            
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class AddOwnerToBucketBuilder : IMigrationOperationBuilder
{
    public string BucketId { get; private set; }
    public string BucketName { get; private set; }
    
    public string UserId { get; private set; }
    public string UserName { get; private set; }

    public AddOwnerToBucketBuilder WithBucketId(string id)
    {
        BucketId = id;
        return this;
    }

    public AddOwnerToBucketBuilder WithBucketName(string name)
    {
        BucketName = name;
        return this;
    }

    public AddOwnerToBucketBuilder WithUserId(string id)
    {
        UserId = id;
        return this;
    }

    public AddOwnerToBucketBuilder WithUserName(string name)
    {
        UserName = name;
        return this;
    }
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new AddOwnerToBucket(context).Initialise(x =>
        {
            x.Bucket.WithId(BucketId).WithName(BucketName);
            x.User.WithId(UserId).WithName(UserName);
        });
    }
}