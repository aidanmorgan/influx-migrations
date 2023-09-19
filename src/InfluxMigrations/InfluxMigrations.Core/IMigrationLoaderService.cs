namespace InfluxMigrations.Core;

public interface IMigrationLoaderServiceOptions
{
    IMigrationLoaderLogger Logger { get; init; }
}

public interface IMigrationLoaderService
{
    Task<List<IMigration>> LoadMigrationsAsync();
}