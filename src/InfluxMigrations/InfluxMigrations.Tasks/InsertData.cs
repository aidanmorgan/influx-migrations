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

    public Dictionary<IResolvable<string?>, IResolvable<string?>> Tags { get; } = new Dictionary<IResolvable<string?>, IResolvable<string?>>();
    public Dictionary<IResolvable<string?>, IResolvable<string?>> Fields { get; } = new Dictionary<IResolvable<string?>, IResolvable<string?>>();

}

public class InsertData : IMigrationTask
{
    public InfluxRuntimeNameResolver Bucket { get; private set; } = InfluxRuntimeNameResolver.CreateBucket();
    public InfluxRuntimeNameResolver Organisation { get; private set; } = InfluxRuntimeNameResolver.CreateOrganisation();
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
    
    public async Task ExecuteAsync(IOperationExecutionContext ctx)
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
        catch (BadRequestException x)
        {
            throw new MigrationExecutionException("Error inserting.", x);
        }
    }

    public async Task ExecuteAsync(IMigrationExecutionContext ctx)
    {
        var bucket = await Bucket.GetAsync(ctx);
        var organisation = await Organisation.GetAsync(ctx);
        
        var pointData = Data.Select(x =>
        {
            var line = x.Resolve(ctx);
            return line;
        }).ToList();

        if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(organisation))
        {
            await ctx.Influx.GetWriteApiAsync().WriteRecordsAsync(pointData, WritePrecision.Ms);
        }
        else
        {
            await ctx.Influx.GetWriteApiAsync().WriteRecordsAsync(pointData, WritePrecision.Ms, bucket, organisation);
        }
    }
}

public class InsertDataBuilder : IMigrationTaskBuilder
{
    private List<string> _lines = new List<string>();
    private string _bucketId;
    private string _bucketName;

    private string _organisationId;
    private string _organisationName;

    public InsertDataBuilder WithBucketId(string id)
    {
        this._bucketId = id;
        return this;
    }

    public InsertDataBuilder WithBucketName(string id)
    {
        this._bucketName = id;
        return this;
    }

    public InsertDataBuilder WithOrganisationId(string id)
    {
        this._organisationId = id;
        return this;
    }

    public InsertDataBuilder WithOrganisationName(string name)
    {
        this._organisationName = name;
        return this;
    }

    public InsertDataBuilder AddLine(string line)
    {
        this._lines.Add(line);
        return this;
    }

    public InsertDataBuilder AddLines(List<string> lines)
    {
        this._lines.AddRange(lines);
        return this;
    }
    
    public IMigrationTask Build()
    {
        return new InsertData()
            {
                Data = _lines.Select(StringResolvable.Parse).ToList(),
                WritePrecision = WritePrecision.Ms
            }
            .Initialise(x =>
            {
                x.Bucket.WithName(_bucketName).WithId(_bucketId);
                x.Organisation.WithName(_organisationName).WithId(_organisationId);
            });
    }
}