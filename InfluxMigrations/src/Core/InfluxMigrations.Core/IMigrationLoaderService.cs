namespace InfluxMigrations.Core;

public interface IMigrationLoaderServiceOptions
{
    IMigrationLoaderLogger Logger { get; init; }
}

/// <summary>
/// Used to load Migrations from storage that may want to be run.
/// </summary>
public interface IMigrationLoaderService
{
    Task<List<IMigration>> LoadMigrationsAsync();
}