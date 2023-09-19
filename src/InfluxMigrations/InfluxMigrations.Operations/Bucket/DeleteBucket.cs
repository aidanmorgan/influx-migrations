using InfluxDB.Client;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Bucket;

public class DeleteBucket : IMigrationOperation
{
    public const string DeletePrefix = "DEL_";
    
    private readonly IOperationExecutionContext _context;
    public InfluxRuntimeIdResolver Bucket { get; private set; }

    
    public DeleteBucket(IOperationExecutionContext context)
    {
        _context = context;
        Bucket = InfluxRuntimeIdResolver.CreateBucket();
    }

    public DeleteBucket Initalise(Action<DeleteBucket> bucket)
    {
        bucket(this);
        return this;
    }

    
    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var bucketId = await Bucket.GetAsync(_context);
        if (string.IsNullOrEmpty(bucketId))
        {
            return OperationResults.ExecutionFailed($"Cannot delete Bucket, no Bucket id provided.");
        }

        var bucket = await _context.Influx.GetBucketsApi().FindBucketByIdAsync(bucketId);
        var oldName = bucket.Name;
        bucket.Name = $"{DeletePrefix}{bucket.Name}";

        try
        {
            var result = await _context.Influx.GetBucketsApi().UpdateBucketAsync(bucket);

            return OperationResults.ExecuteSuccess(new DeleteBucketResult()
            {
                Id = result.Id,
                NewName = result.Name,
                OldName = oldName
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecutionFailed(x);
        }
    }
    
    public async Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult r)
    {
        var result = (DeleteBucketResult?)r;

        if (string.IsNullOrEmpty(result?.Id))
        {
            return OperationResults.CommitFailed(result, $"Cannot commit delete of bucket, cannot find bucket id.");
        }

        try
        {
            await _context.Influx.GetBucketsApi().DeleteBucketAsync(result.Id);
        }
        catch (Exception x)
        {
            return OperationResults.CommitFailed(result, x);
        }

        return OperationResults.CommitSuccess(result);
    }
    
    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult r)
    {
        var result = (DeleteBucketResult)r;
        if (string.IsNullOrEmpty(result?.Id))
        {
            return OperationResults.RollbackFailed(result, $"Cannot commit delete of bucket, cannot find bucket id.");
        }

        var bucket = await _context.Influx.GetBucketsApi().FindBucketByIdAsync(result.Id);
        bucket.Name = result.OldName;

        try
        {
            await _context.Influx.GetBucketsApi().UpdateBucketAsync(bucket);
            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }

}

public class DeleteBucketResult : IExecuteResult
{
    public string Id { get; set; }
    public string OldName { get; set; }
    public string NewName { get; set; }

    public string Name => NewName;
}

public class DeleteBucketBuilder : IMigrationOperationBuilder
{
    private string _bucketId;
    private string _bucketName;

    public DeleteBucketBuilder() {}

    public DeleteBucketBuilder WithBucketId(string id)
    {
        _bucketId = id;
        return this;
    }

    public DeleteBucketBuilder WithBucketName(string id)
    {
        _bucketName = id;
        return this;
    }
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(_bucketId) && string.IsNullOrEmpty(_bucketName))
        {
            throw new MigrationOperationBuildingException("No Bucket specified.");
        }
        
        return new DeleteBucket(context).Initalise(x =>
        {
            x.Bucket
                .WithId(StringResolvable.Parse(_bucketId))
                .WithName(StringResolvable.Parse(_bucketName));
        });
    }
}