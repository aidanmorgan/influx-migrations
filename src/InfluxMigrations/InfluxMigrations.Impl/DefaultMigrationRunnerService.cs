using InfluxMigrations.Core;

namespace InfluxMigrations.Impl;

public class DefaultMigrationRunnerOptions : IMigrationRunnerOptions
{
    public IComparer<string> VersionComparer { get; init; } = new DefaultVersionComparer();

    public MigrationOptions MigrationOptions { get; init; } = new MigrationOptions()
    {
        Logger = new TextWriterMigrationLoggerFactory(Console.Out)
    };

    public Func<IInfluxFactory, MigrationOptions, Task<IEnvironmentExecutionContext>> EnvironmentExecutionContextFactory { get; init; } 
        = (influx, options) => Task.FromResult<IEnvironmentExecutionContext>(new DefaultEnvironmentExecutionContext(influx, options));
    
    public IMigrationLoaderService Loader { get; init; }
    public IEnvironmentConfigurator? EnvironmentConfigurator { get; init; } = new NoOpEnvironmentConfigurator();
    public IMigrationHistoryService History { get; init; }

    public IMigrationRunnerLogger Logger { get; init; } = new TextWriterMigrationRunnerLogger(Console.Out);

}

public class DefaultMigrationRunnerService : IMigrationRunnerService
{
    private readonly DefaultMigrationRunnerOptions _options;

    public DefaultMigrationRunnerService(DefaultMigrationRunnerOptions opts)
    {
        _options = opts;

        if (opts.History == null)
        {
            throw new MigrationRunnerException(
                $"Cannot create a {GetType().FullName} with no {typeof(IMigrationHistoryService).FullName} specified.");
        }

        if (opts.Loader == null)
        {
            throw new MigrationRunnerException(
                $"Cannot create a {GetType().FullName} with no {typeof(IMigrationLoaderService).FullName} specified.");
        }
    }

    public async Task<List<MigrationResult>> ExecuteMigrationsAsync(IInfluxFactory influx, string? targetVersion = null)
    {
        var logger = _options.Logger;

        var history = await _options.History.LoadMigrationHistoryAsync();
        var loaded = await _options.Loader.LoadMigrationsAsync();

        if (loaded
            .GroupBy(x => x.Version, new ComparerAdapter<string>(_options.VersionComparer))
            .Any(x => x.Count() > 1))
        {
            throw new MigrationRunnerException(
                $"Cannot execute Migration(s), duplicate Migrations found: [{string.Join(",", loaded.Select(x => x.Version))}]");
        }

        var alreadyExecuted = history.Where(x => x.Success.HasValue && x.Success.Value)
            .Select(x => x.Version)
            .ToList();

        if (!string.IsNullOrEmpty(targetVersion))
        {
            // check that we have the target migration to actually migrate to/from
            if (loaded.All(x => _options.VersionComparer.Compare(x.Version, targetVersion) != 0))
            {
                throw new MigrationRunnerException($"Cannot find target version {targetVersion} to migrate to.");
            }

            if (alreadyExecuted.Contains(targetVersion))
            {
                // get the version numbers of the entries that we need to rollback
                var toRollback = history.Where(x =>
                    _options.VersionComparer.Compare(x.Version, targetVersion) > 0).Select(x => x.Version).ToList();

                // convert those into IMigrations from the loader
                var toExecute = loaded
                    .Where(x => toRollback.Any(y => _options.VersionComparer.Compare(y, x.Version) == 0)).ToList();

                return await Execute(influx, toExecute, MigrationDirection.Down, _options.MigrationOptions, logger);
            }
            else
            {
                var toExecute = new List<IMigration>(loaded);
                toExecute.RemoveAll(x => alreadyExecuted.Any(y => _options.VersionComparer.Compare(y, x.Version) == 0));
                toExecute.RemoveAll(x => _options.VersionComparer.Compare(x.Version, targetVersion) > 0);
                
                return await Execute(influx, toExecute, MigrationDirection.Up, _options.MigrationOptions, logger);
            }
        }
        else
        {
            // go through and remove any migrations that have been previously successfully applied
            var toExecute = new List<IMigration>(loaded);
            toExecute.RemoveAll(x =>
                alreadyExecuted.FirstOrDefault(y => _options.VersionComparer.Compare(x.Version, y) == 0, null) != null);


            return await Execute(influx, toExecute, MigrationDirection.Up, _options.MigrationOptions, logger);
        }
    }

    private async Task<List<MigrationResult>> Execute(IInfluxFactory influx, List<IMigration> migrations,
        MigrationDirection direction, MigrationOptions options, IMigrationRunnerLogger logger)
    {
        if (migrations.Count == 0)
        {
            logger.NoMigrations();
            return new List<MigrationResult>();
        }
        
        var results = new List<MigrationResult>();

        var plan = direction == MigrationDirection.Up
            ? migrations.OrderBy(x => x.Version, _options.VersionComparer).ToList()
            : migrations.OrderByDescending(x => x.Version, _options.VersionComparer).ToList();

        logger.ExecutionPlan(plan, direction);

        var environment = await _options.EnvironmentExecutionContextFactory(influx, _options.MigrationOptions);
        if (_options.EnvironmentConfigurator != null)
        {
            await _options.EnvironmentConfigurator.ConfigureEnvironmentAsync(environment, _options);
        }

        await environment.Initialise(_options.Logger);

        foreach (var migration in plan)
        {
            var r = await migration.ExecuteAsync(environment, direction, options);

            results.Add(r);

            if (r.Success)
            {
                var entry = new MigrationHistory(
                    migration.Version,
                    direction,
                    DateTimeOffset.UtcNow,
                    true);

                await _options.History.SaveMigrationHistoryAsync(entry);

                _options.Logger.MigrationSaved(entry);
            }
            else
            {
                var entry = new MigrationHistory(
                    migration.Version,
                    direction,
                    DateTimeOffset.UtcNow,
                    false);

                await _options.History.SaveMigrationHistoryAsync(entry);
                _options.Logger.MigrationSaveFailed(entry);

                return results;
            }
        }

        await environment.Finalise(_options.Logger);

        return results;
    }
}

public class ComparerAdapter<T> : IEqualityComparer<T>
{
    private readonly IComparer<T> _comparer;

    public ComparerAdapter(IComparer<T> comparer)
    {
        _comparer = comparer;
    }

    public bool Equals(T? x, T? y)
    {
        return _comparer.Compare(x, y) == 0;
    }

    public int GetHashCode(T obj)
    {
        return obj.GetHashCode();
    }
}