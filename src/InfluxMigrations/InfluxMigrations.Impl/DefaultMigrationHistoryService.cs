using System.Net;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using InfluxMigrations.Core;
using NodaTime;
using RestSharp;

namespace InfluxMigrations.Impl;

public class DefaultMigrationHistoryOptions : IMigrationHistoryServiceOptions
{
    public string BucketName { get; init; } = "migration-history";
    public string OrganisationName { get; init; } = "migrations";
    public string MeasurementName { get; init; } = "migration";

    public IMigrationHistoryLogger Logger { get; init; } = new NoOpMigrationHistoryLogger();
}

public class DefaultMigrationHistoryService : IMigrationHistoryService
{
    public DefaultMigrationHistoryOptions Options { get; init; }
    
    // an arbitrary timestamp to calculate range queries from so we don't aggregate everything
    private static readonly DateTimeOffset HistoryEpoch = DateTimeOffset.Parse("2023/01/01T00:00:00.000Z");
    private readonly IInfluxFactory _client;

    public DefaultMigrationHistoryService(IInfluxFactory client, DefaultMigrationHistoryOptions? options = null)
    {
        this._client = client;
        this.Options = options ?? new DefaultMigrationHistoryOptions();
    }
    
    public async Task<List<MigrationHistory>> LoadMigrationHistoryAsync()
    {
        var influx = _client.Create();
        // this is a STUPID hack because influx won't let me create an unbounded query, but I figure nobody will have
        // ever used this code since before the day I created it, so might as well just use that date as a starting point.
        var historyDays = (int)Math.Ceiling((DateTimeOffset.UtcNow - HistoryEpoch).TotalDays);

        try
        {
            var result = await influx
                .GetQueryApi(new MigrationHistoryMapper(Options))
                .QueryAsync<MigrationHistory>(
                    $"from(bucket:\"{Options.BucketName}\") |> range(start: -{(int)historyDays}d)   |> filter(fn: (r) => r._measurement == \"{Options.MeasurementName}\")",
                    Options.OrganisationName);

            return result.OfType<MigrationHistory>().OrderBy(x => x.Timestamp).ToList();
        }
        catch (Exception x)
        {
            if (x is UnauthorizedException)
            {
                return new List<MigrationHistory>();
            }
            
            Options.Logger.LoadException(x);
            return new List<MigrationHistory>();
        }
    }

    public async Task SaveMigrationHistoryAsync(MigrationHistory history)
    {
        var influx = _client.Create();

        await CreateHistoryBucketIfNotExists(influx, Options);
        var list = new List<MigrationHistory>()
        {
            history
        };

        RestResponse? result = null;
        try
        {
            result = await influx.GetWriteApiAsync(new MigrationHistoryMapper(Options))
                .WriteMeasurementsAsyncWithIRestResponse(list,
                    WritePrecision.Ms,
                    Options.BucketName, 
                    Options.OrganisationName);

        }
        catch (Exception x)
        {
            Options.Logger.SaveException(x);
            return;
        }

        if (result == null || result?.StatusCode != HttpStatusCode.NoContent)
        {
            throw new MigrationRunnerException($"Could not save history. Result: {result?.StatusCode} - {result?.Content}");
        }
    }

    public static async Task<Bucket> CreateHistoryBucketIfNotExists(IInfluxDBClient client, DefaultMigrationHistoryOptions opts)
    {
        Bucket historyBucket = null;
        try
        {
            historyBucket = await client.GetBucketsApi().FindBucketByNameAsync(opts.BucketName);
        }
        catch (Exception)
        {
        }

        if (historyBucket == null)
        {
            var organisation =
                (await client.GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
                    x => string.Equals(x?.Name, opts.OrganisationName), null);
            if (organisation == null)
            {
                throw new MigrationRunnerException($"Cannot find Organisation for bucket: {opts.OrganisationName}");
            }

            historyBucket = await client.GetBucketsApi().CreateBucketAsync(opts.BucketName, organisation.Id);
        }

        return historyBucket;

    }
}

public class MigrationHistoryMapper : IDomainObjectMapper
{
    private readonly DefaultMigrationHistoryOptions _opts;

    public MigrationHistoryMapper(DefaultMigrationHistoryOptions opts)
    {
        this._opts = opts;
    }
    
    public T ConvertToEntity<T>(FluxRecord fluxRecord)
    {
        if (typeof(T) != typeof(MigrationHistory))
        {
            throw new NotSupportedException();
        }
        
        var values = fluxRecord.Values;
        var history = new MigrationHistory((string)values["version"],
            Enum.Parse<MigrationDirection>((string)values["direction"]),
            ((Instant)values["_time"]).ToDateTimeOffset(),
            bool.Parse((string)values["success"]));

        return (T)Convert.ChangeType(history, typeof(T));
    }

    public object ConvertToEntity(FluxRecord fluxRecord, Type type)
    {
        if (type != typeof(MigrationHistory))
        {
            throw new NotSupportedException();
        }
        
        var values = fluxRecord.Values;
        var history = new MigrationHistory((string)values["version"],
            Enum.Parse<MigrationDirection>((string)values["direction"]),
            DateTimeOffset.FromUnixTimeMilliseconds(long.Parse((string)values["ts"])),
            bool.Parse((string)values["success"]));

        return Convert.ChangeType(history, type);
    }

    public PointData ConvertToPointData<T>(T entity, WritePrecision precision)
    {
        var entry = entity as MigrationHistory;
        if (entry == null)
        {
            throw new NotSupportedException();
        }

        // going to cheat here and just set one field to an arbitrary value so we only
        // get one record back from the query
        return PointData.Measurement(_opts.MeasurementName)
            .Tag("version", entry.Version)
            .Tag("direction", Enum.GetName(entry.Direction))
            .Tag("success", $"{entry.Success}")
            .Field("utc", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            .Timestamp(entry.Timestamp.ToUnixTimeMilliseconds(), precision);
    }
}