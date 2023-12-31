﻿using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Tasks;

public class InsertData : IMigrationTask, IOperationTask, IEnvironmentTask
{
    private static readonly IDictionary<string, WritePrecision> PrecisionLookup =
        new Dictionary<string, WritePrecision>()
        {
            { "ms", WritePrecision.Ms },
            { "ns", WritePrecision.Ns },
            { "us", WritePrecision.Us },
            { "s", WritePrecision.S }
        };

    public IInfluxRuntimeResolver  Bucket { get; private set; } = InfluxRuntimeNameResolver.CreateBucket();

    public IInfluxRuntimeResolver Organisation { get; private set; } =
        InfluxRuntimeNameResolver.CreateOrganisation();

    public IResolvable<string> Precision { get; init; } = StringResolvable.Parse("ms");
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
        return await Execute(ctx);
    }

    public async Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx)
    {
        return await Execute(ctx);
    }

    public async Task<TaskResult> ExecuteAsync(IEnvironmentExecutionContext ctx)
    {
        return await Execute(ctx);
    }

    private async Task<TaskResult> Execute(IContext ctx)
    {
        var precision = Precision.Resolve((ctx));
        if (string.IsNullOrEmpty(precision) || !PrecisionLookup.ContainsKey(precision.ToLowerInvariant()))
        {
            return TaskResults.TaskFailure($"Cannot resolve WritePrecision.");
        }
        
        var writePrecision = PrecisionLookup[precision.ToLowerInvariant()];
       
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
                await ctx.Influx.GetWriteApiAsync().WriteRecordsAsync(pointData, writePrecision);
            }
            else
            {
                await ctx.Influx.GetWriteApiAsync()
                    .WriteRecordsAsync(pointData, writePrecision, bucket, organisation);
            }
        }
        catch (InfluxException x)
        {
            return TaskResults.TaskFailure(x);
        }

        return TaskResults.TaskSuccess();
        
    }
}

public class InsertDataBuilder : IMigrationTaskBuilder, IOperationTaskBuilder, IEnvironmentTaskBuilder
{
    public List<string> Lines { get; private set; } = new List<string>();
    public string BucketId { get; private set; }
    public string BucketName { get; private set; }
    public string OrganisationId { get; private set; }
    public string OrganisationName { get; private set; }
    public string Precision { get; private set; } = "ms";

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

    public InsertDataBuilder WithPrecision(string precision)
    {
        this.Precision = precision;
        return this;
    }

    public IMigrationTask BuildMigration()
    {
        return new InsertData()
            {
                Data = Lines.Select(StringResolvable.Parse).ToList(),
                Precision = StringResolvable.Parse(Precision)
            }
            .Initialise(x =>
            {
                x.Bucket.WithName(BucketName).WithId(BucketId);
                x.Organisation.WithName(OrganisationName).WithId(OrganisationId);
            });
    }

    public IOperationTask BuildOperation()
    {
        return new InsertData()
            {
                Data = Lines.Select(StringResolvable.Parse).ToList(),
                Precision = StringResolvable.Parse(Precision)
            }
            .Initialise(x =>
            {
                x.Bucket.WithName(BucketName).WithId(BucketId);
                x.Organisation.WithName(OrganisationName).WithId(OrganisationId);
            });
    }

    public IEnvironmentTask BuildEnvironment()
    {
        return new InsertData()
            {
                Data = Lines.Select(StringResolvable.Parse).ToList(),
                Precision = StringResolvable.Parse(Precision)
            }
            .Initialise(x =>
            {
                x.Bucket.WithName(BucketName).WithId(BucketId);
                x.Organisation.WithName(OrganisationName).WithId(OrganisationId);
            });
    }
}