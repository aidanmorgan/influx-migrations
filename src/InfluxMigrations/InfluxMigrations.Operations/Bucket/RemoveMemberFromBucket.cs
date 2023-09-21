using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Bucket;

public class RemoveMemberFromBucket : IMigrationOperation
{
    private IOperationExecutionContext _context;

    public InfluxRuntimeIdResolver User { get; private set; }
    public InfluxRuntimeIdResolver Bucket { get; private set; }

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
            var bucketId = await User.GetAsync(_context);

            await _context.Influx.GetBucketsApi().DeleteMemberAsync(userId, bucketId);

            return OperationResults.ExecuteSuccess();
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

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult result)
    {
        try
        {
            var userId = await User.GetAsync(_context);
            var bucketId = await User.GetAsync(_context);

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
    public string UserName { get; init; }
    public string UserId { get; init; }
    public string BucketName { get; init; }
    public string BucketId { get; init; }

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
}