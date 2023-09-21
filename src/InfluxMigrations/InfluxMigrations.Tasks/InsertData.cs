using System.Runtime.Serialization;
using System.Web;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Writes;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Outputs;

public class Data
{
    public IResolvable<string?> Measurement { get; init; }
    public IResolvable<string?> Timestamp { get; init; }

    public Dictionary<IResolvable<string?>, IResolvable<string?>> Tags { get; } =
        new Dictionary<IResolvable<string?>, IResolvable<string?>>();

    public Dictionary<IResolvable<string?>, IResolvable<string?>> Fields { get; } =
        new Dictionary<IResolvable<string?>, IResolvable<string?>>();
}

public class InsertData : IMigrationTask
{
    public InfluxRuntimeNameResolver Bucket { get; private set; } = InfluxRuntimeNameResolver.CreateBucket();

    public InfluxRuntimeNameResolver Organisation { get; private set; } =
        InfluxRuntimeNameResolver.CreateOrganisation();

    public WritePrecision WritePrecision { get; set; } = WritePrecision.Ms;
    public List<IResolvable<string?>> Data { get; init; } = new List<IResolvable<string?>>();

    public InsertData()
    {
    }

    public InsertData Initialise(Action<InsertData> data)
    {
        data(this);
        return this;
    }

    public async Task<TaskResult> ExecuteAsync(IOperationExecutionContext ctx)
    {
        var bucket = await Bucket.GetAsync(ctx);
        var organisation = await Organisation.GetAsync(ctx);

        var pointData = Data.Select(x =>
        {
            var line = x.Resolve(ctx);
            return line;
        }).ToList();

        try
        {
            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(organisation))
            {
                await ctx.Influx.GetWriteApiAsync().WriteRecordsAsync(pointData, WritePrecision.Ms);
            }
            else
            {
                await ctx.Influx.GetWriteApiAsync()
                    .WriteRecordsAsync(pointData, WritePrecision.Ms, bucket, organisation);
            }
        }
        catch (InfluxException x)
        {
            return TaskResults.TaskFailure(x);
        }

        return TaskResults.TaskSuccess();
    }

    public async Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx)
    {
        var bucket = await Bucket.GetAsync(ctx);
        var organisation = await Organisation.GetAsync(ctx);

        var pointData = Data.Select(x =>
        {
            var line = x.Resolve(ctx);
            return line;
        }).ToList();

        try
        {
            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(organisation))
            {
                await ctx.Influx.GetWriteApiAsync().WriteRecordsAsync(pointData, WritePrecision.Ms);
            }
            else
            {
                await ctx.Influx.GetWriteApiAsync()
                    .WriteRecordsAsync(pointData, WritePrecision.Ms, bucket, organisation);
            }
        }
        catch (InfluxException x)
        {
            return TaskResults.TaskFailure(x);
        }

        return TaskResults.TaskSuccess();
    }
}

public class InsertDataBuilder : IMigrationTaskBuilder
{
    public List<string> Lines { get; private set; } = new List<string>();
    public string BucketId { get; private set; }
    public string BucketName { get; private set; }
    public string OrganisationId { get; private set; }
    public string OrganisationName { get; private set; }

    public InsertDataBuilder WithBucketId(string id)
    {
        this.BucketId = id;
        return this;
    }

    public InsertDataBuilder WithBucketName(string id)
    {
        this.BucketName = id;
        return this;
    }

    public InsertDataBuilder WithOrganisationId(string id)
    {
        this.OrganisationId = id;
        return this;
    }

    public InsertDataBuilder WithOrganisationName(string name)
    {
        this.OrganisationName = name;
        return this;
    }

    public InsertDataBuilder AddLine(string line)
    {
        this.Lines.Add(line);
        return this;
    }

    public InsertDataBuilder AddLines(List<string> lines)
    {
        this.Lines.AddRange(lines);
        return this;
    }

    public IMigrationTask Build()
    {
        return new InsertData()
            {
                Data = Lines.Select(StringResolvable.Parse).ToList(),
                WritePrecision = WritePrecision.Ms
            }
            .Initialise(x =>
            {
                x.Bucket.WithName(BucketName).WithId(BucketId);
                x.Organisation.WithName(OrganisationName).WithId(OrganisationId);
            });
    }
}