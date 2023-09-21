using InfluxDB.Client;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Core;

public abstract class InfluxRuntimeResolver
{
    private IResolvable<string?> Id { get; set; }
    private IResolvable<string?> Name { get; set; }

    private readonly Func<string?, string?, IInfluxDBClient, Task<string?>> _lookup;

    protected InfluxRuntimeResolver(Func<string?, string?, IInfluxDBClient, Task<string?>> lookup)
    {
        _lookup = lookup;
    }

    public InfluxRuntimeResolver WithName(IResolvable<string?> name)
    {
        Name = name;
        return this;
    }

    public InfluxRuntimeResolver WithName(string? name)
    {
        return !string.IsNullOrEmpty(name) ? WithName(StringResolvable.Parse(name)!) : this;
    }

    public InfluxRuntimeResolver WithId(IResolvable<string?> id)
    {
        Id = id;
        return this;
    }

    public InfluxRuntimeResolver WithId(string? id)
    {
        return !string.IsNullOrEmpty(id) ? WithId(StringResolvable.Parse(id)!) : this;
    }

    public async Task<string?> GetAsync(IOperationExecutionContext ctx)
    {
        var nameValue = Name?.Resolve(ctx);
        var idValue = Id?.Resolve(ctx);

        return await _lookup(idValue, nameValue, ctx.Influx);
    }

    public async Task<string?> GetAsync(IMigrationExecutionContext ctx)
    {
        var nameValue = Name?.Resolve(ctx);
        var idValue = Id?.Resolve(ctx);

        return await _lookup(idValue, nameValue, ctx.Influx);
    }
}

/// <summary>
/// A utility wrapper that can be used to try and resolve an influxdb id given either the id name or a name in a migration,
/// if the name is provided then an influxdb API call is made to lookup the corresponding entry and find it's id.
/// </summary>
public class InfluxRuntimeIdResolver : InfluxRuntimeResolver
{
    private InfluxRuntimeIdResolver(Func<string?, string?, IInfluxDBClient, Task<string?>> lookup) : base(lookup)
    {
    }

    public static InfluxRuntimeIdResolver CreateOrganisation()
    {
        return new InfluxRuntimeIdResolver(async (id, name, influx) =>
        {
            if (!string.IsNullOrEmpty(id))
            {
                // even though we have resolved the organisation id, do the API lookup to check it exists.
                var org = await influx.GetOrganizationsApi().FindOrganizationByIdAsync(id);
                return org?.Id;
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new MigrationResolutionException($"Cannot get Organisation details given no Id or Name value.");
            }

            var result =
                (await influx.GetOrganizationsApi().FindOrganizationsAsync()).FirstOrDefault(
                    z => string.Equals(z?.Name, name, StringComparison.InvariantCultureIgnoreCase), null);
            return result?.Id;
        });
    }

    public static InfluxRuntimeIdResolver CreateBucket()
    {
        return new InfluxRuntimeIdResolver(async (id, name, influx) =>
        {
            if (!string.IsNullOrEmpty(id))
            {
                // even though we have the bucketid we need to check that it actually exists in the database
                var bucket = await influx.GetBucketsApi().FindBucketByIdAsync(id);
                return bucket.Id;
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new MigrationResolutionException($"Cannot get Bucket details given no Id or Name value.");
            }

            var result = await influx.GetBucketsApi().FindBucketByNameAsync(name);
            return result?.Id;
        });
    }

    public static InfluxRuntimeIdResolver CreateUser()
    {
        return new InfluxRuntimeIdResolver(async (id, name, influx) =>
        {
            if (!string.IsNullOrEmpty(id))
            {
                // even though we have resolved the id, we will make the API call to check that it actually exists.
                var user = await influx.GetUsersApi().FindUserByIdAsync(id);
                return user?.Id;
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new MigrationResolutionException($"Cannot find User, no Id or Name has been provided.");
            }

            var result =
                (await influx.GetUsersApi().FindUsersAsync()).FirstOrDefault(x => string.Equals(x?.Name, name), null);

            return result?.Id;
        });
    }
}

public class InfluxRuntimeNameResolver : InfluxRuntimeResolver
{
    private InfluxRuntimeNameResolver(Func<string?, string?, IInfluxDBClient, Task<string?>> lookup) : base(lookup)
    {
    }

    public static InfluxRuntimeNameResolver CreateOrganisation()
    {
        return new InfluxRuntimeNameResolver(async (id, name, influx) =>
        {
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new MigrationResolutionException("No Name or Id for Organisation.");
            }

            try
            {
                var org = await influx.GetOrganizationsApi().FindOrganizationByIdAsync(id);
                return org.Id;
            }
            catch (NotFoundException)
            {
                return null;
            }
        });
    }

    public static InfluxRuntimeNameResolver CreateBucket()
    {
        return new InfluxRuntimeNameResolver(async (id, name, influx) =>
        {
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new MigrationResolutionException("No Name or Id specified.");
            }

            try
            {
                var bucket = await influx.GetBucketsApi().FindBucketByIdAsync(id);
                return bucket?.Name;
            }
            catch (NotFoundException)
            {
                return null;
            }
        });
    }

    public static InfluxRuntimeNameResolver CreateUser()
    {
        return new InfluxRuntimeNameResolver(async (id, name, influx) =>
        {
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new MigrationResolutionException("No Name or Id specified.");
            }

            try
            {
                var user = await influx.GetUsersApi().FindUserByIdAsync(id);
                return user?.Name;
            }
            catch (NotFoundException)
            {
                return null;
            }
        });
    }
}