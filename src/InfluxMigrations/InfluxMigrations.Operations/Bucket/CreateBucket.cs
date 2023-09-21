using System.Diagnostics;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Commands.Auth;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Bucket;

public class CreateBucket : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public InfluxRuntimeIdResolver Organisation { get; private set; }
    public IResolvable<string?> Bucket { get; set; }
    public IResolvable<string?>? RetentionPeriodSeconds { get; set; }


    public CreateBucket(IOperationExecutionContext context)
    {
        _context = context;

        Organisation = InfluxRuntimeIdResolver.CreateOrganisation();
    }

    /// <summary>
    /// Conenienve method to allow the InfluxApiValues to be updated as they are order-dependent when they are
    /// created.
    /// </summary>
    public CreateBucket Initialise(Action<CreateBucket> func)
    {
        func(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        try
        {
            InfluxDB.Client.Api.Domain.Bucket result = null;

            var organisationId = await Organisation.GetAsync(_context);
            if (string.IsNullOrEmpty(organisationId))
            {
                return OperationResults.ExecutionFailed($"Cannot create Bucket, cannot resolve Organisation id.");
            }

            var bucketName = Bucket.Resolve(_context);

            var bucket = await _context.Influx.GetBucketsApi().FindBucketByNameAsync(bucketName);
            if (bucket != null)
            {
                return OperationResults.ExecutionFailed(
                    $"Cannot create Bucket with name {bucketName}, one already exists.");
            }

            if (RetentionPeriodSeconds != null)
            {
                var ts = TimeSpanParser.ParseTimeSpan(RetentionPeriodSeconds.Resolve(_context));

                var retention = ts == null
                    ? new BucketRetentionRules()
                    : new BucketRetentionRules(BucketRetentionRules.TypeEnum.Expire, (long)ts.Value.TotalSeconds);

                result = await _context.Influx.GetBucketsApi()
                    .CreateBucketAsync(bucketName, retention, organisationId);
            }
            else
            {
                result = await _context.Influx.GetBucketsApi()
                    .CreateBucketAsync(bucketName, organisationId);
            }

            return OperationResults.ExecuteSuccess(new CreateBucketResult()
            {
                OrganisationId = organisationId,
                Id = result.Id,
                Name = bucketName,
                CreatedTime = result.CreatedAt
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

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult r)
    {
        var createBucketResult = (CreateBucketResult)r;

        await _context.Influx.GetBucketsApi().FindBucketByIdAsync(createBucketResult.Id);

        try
        {
            await _context.Influx.GetBucketsApi().DeleteBucketAsync(createBucketResult.Id);
            return OperationResults.RollbackSuccess(r);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(r, x);
        }
    }
}

public class CreateBucketResult : IExecuteResult
{
    public string Id { get; set; }
    public string OrganisationId { get; set; }
    public DateTime? CreatedTime { get; set; }
    public string Name { get; set; }

    public CreateBucketResult()
    {
    }
}

public class CreateBucketBuilder : IMigrationOperationBuilder
{
    public string OrganisationName { get; private set; }
    public string OrganisationId { get; private set; }
    public string BucketName { get; private set; }
    public string Retention { get; private set; }

    public CreateBucketBuilder()
    {
    }

    public CreateBucketBuilder WithOrganisation(string orgName)
    {
        this.OrganisationName = orgName;
        return this;
    }

    public CreateBucketBuilder WithOrganisationId(string id)
    {
        this.OrganisationId = id;
        return this;
    }

    public CreateBucketBuilder WithBucketName(string bucketName)
    {
        this.BucketName = bucketName;
        return this;
    }

    public CreateBucketBuilder WithRetention(string retention)
    {
        this.Retention = retention;
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(OrganisationId) && string.IsNullOrEmpty(OrganisationName))
        {
            throw new MigrationOperationBuildingException("No Organisation specified.");
        }

        if (string.IsNullOrEmpty(BucketName))
        {
            throw new MigrationOperationBuildingException("No Bucket specified.");
        }

        var cmd = new CreateBucket(context)
        {
            RetentionPeriodSeconds = string.IsNullOrEmpty(Retention) ? null : StringResolvable.Parse(Retention)
        }.Initialise((x) =>
        {
            x.Organisation
                .WithId(StringResolvable.Parse(OrganisationId))
                .WithName(StringResolvable.Parse(OrganisationName));

            x.Bucket = StringResolvable.Parse(BucketName);
        });

        return cmd;
    }
}