using InfluxDB.Client;

namespace InfluxMigrations.Core;

public interface IMigrationRunnerOptions
{
    
}

public interface IMigrationRunnerService
{
    Task<List<MigrationResult>> ExecuteMigrationsAsync(IInfluxFactory influx, string? targetVersion);
}