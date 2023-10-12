using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Operations.Bucket;

public class RemoveMemberFromBucket : IMigrationOperation
{
    private IOperationExecutionContext _context;

    public IInfluxRuntimeResolver User { get; private set; }
    public IInfluxRuntimeResolver Bucket { get; private set; }

    public RemoveMemberFromBucket(IOperationExecutionContext context)
    {
        _context = context;

        User = InfluxRuntimeIdResolver.CreateUser();
        Bucket = InfluxRuntimeIdResolver.CreateBucket();
    }

    public RemoveMemberFromBucket Initialise(Action<RemoveMemberFromBucket> callback)
    {
        callback(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var userId = await User.GetAsync(_context);
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResults.ExecuteFailed("Cannot remove Membership, cannot find User.");
            }
            
            var bucketId = await Bucket.GetAsync(_context);
            if (string.IsNullOrEmpty(bucketId))
            {
                return OperationResults.ExecuteFailed($"Cannot remove Membership, cannot find Bucket.");
            }

            var membership = await _context.Influx.GetBucketsApi().GetMembersAsync(bucketId);

            if (membership.All(x => x.Id != userId))
            {
                return OperationResults.ExecuteFailed($"Could not find ResourceMember for User in Bucket.");
            }
            
            await _context.Influx.GetBucketsApi().DeleteMemberAsync(userId, bucketId);

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
            var userId = await User.GetAsync(_context);
            var bucketId = await Bucket.GetAsync(_context);

            await _context.Influx.GetBucketsApi().AddMemberAsync(userId, bucketId);

            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
}

public class RemoveMemberFromBucketBuilder : IMigrationOperationBuilder
{
    public string UserName { get; private set; }
    public string UserId { get; private set; }
    public string BucketName { get; private set; }
    public string BucketId { get; private set; }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new RemoveMemberFromBucket(context).Initialise(x =>
        {
            x.Bucket
                .WithName(StringResolvable.Parse(BucketName))
                .WithId(StringResolvable.Parse(BucketId));
            x.User
                .WithName(StringResolvable.Parse(UserName))
                .WithId(StringResolvable.Parse(UserId));
        });
    }

    public RemoveMemberFromBucketBuilder WithBucketName(string s)
    {
        BucketName = s;
        return this;
    }

    public RemoveMemberFromBucketBuilder WithBucketId(string s)
    {
        BucketId = s;
        return this;
    }

    public RemoveMemberFromBucketBuilder WithUserName(string s)
    {
        UserName = s;
        return this;
    }

    public RemoveMemberFromBucketBuilder WithUserId(string s)
    {
        UserId = s;
        return this;
    }
}