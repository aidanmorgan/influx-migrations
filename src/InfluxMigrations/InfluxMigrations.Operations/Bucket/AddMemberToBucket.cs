using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Bucket;

public class AddMemberToBucket : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;

    public IInfluxRuntimeResolver User { get; private set; }
    public IInfluxRuntimeResolver Bucket { get; private set; }


    public AddMemberToBucket(IOperationExecutionContext context)
    {
        _context = context;

        User = InfluxRuntimeIdResolver.CreateUser();
        Bucket = InfluxRuntimeIdResolver.CreateBucket();
    }

    public AddMemberToBucket Initialise(Action<AddMemberToBucket> callback)
    {
        callback(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            var userId = await User.GetAsync(_context);
            var bucketId = await Bucket.GetAsync(_context);

            await _context.Influx.GetBucketsApi().AddMemberAsync(userId, bucketId);

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

            await _context.Influx.GetBucketsApi().DeleteMemberAsync(userId, bucketId);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }

        return OperationResults.RollbackSuccess(result);
    }
}

public class AddMemberToBucketBuilder : IMigrationOperationBuilder
{
    public string UserName { get; private set; }
    public string UserId { get; private set; }
    public string BucketName { get; private set; }
    public string BucketId { get; private set; }


    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new AddMemberToBucket(context).Initialise(x =>
        {
            x.Bucket
                .WithName(StringResolvable.Parse(BucketName))
                .WithId(StringResolvable.Parse(BucketId));
            x.User
                .WithName(StringResolvable.Parse(UserName))
                .WithId(StringResolvable.Parse(UserId));
        });
    }

    public AddMemberToBucketBuilder WithBucketId(string s)
    {
        BucketId = s;
        return this;
    }

    public AddMemberToBucketBuilder WithBucketName(string s)
    {
        BucketName = s;
        return this;
    }

    public AddMemberToBucketBuilder WithUserId(string s)
    {
        UserId = s;
        return this;
    }

    public AddMemberToBucketBuilder WithUserName(string s)
    {
        UserName = s;
        return this;
    }
}