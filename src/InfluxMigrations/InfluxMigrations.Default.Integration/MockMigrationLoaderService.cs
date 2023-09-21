using InfluxMigrations.Core;

namespace InfluxMigrations.Default.Integration;

public class MockMigrationLoaderService : IMigrationLoaderService
{
    private readonly List<IMigration> _migrations = new List<IMigration>();

    public MockMigrationLoaderService AddMigration(IMigration m)
    {
        _migrations.Add(m);
        return this;
    }

    public MockMigrationLoaderService AddMigrations(List<IMigration> m)
    {
        _migrations.AddRange(m);
        return this;
    }

    public Task<List<IMigration>> LoadMigrationsAsync()
    {
        return Task.FromResult(_migrations);
    }
}