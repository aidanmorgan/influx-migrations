using InfluxMigrations.Core;

namespace InfluxMigrations.Default.Integration;

public class MockMigrationHistoryService : IMigrationHistoryService
{
    private readonly List<MigrationHistory> _histories = new List<MigrationHistory>();

    public MockMigrationHistoryService AddHistory(string version, MigrationDirection? dir = null, DateTimeOffset? time = null, bool success = true)
    {
        return AddHistory(new MigrationHistory(version, dir ?? MigrationDirection.Up, time ?? DateTimeOffset.UtcNow, success));
    }
    
    public MockMigrationHistoryService AddHistory(MigrationHistory hist)
    {
        _histories.Add(hist);
        return this;
    }

    public MockMigrationHistoryService AddHistories(List<MigrationHistory> h)
    {
        _histories.AddRange(h);
        return this;
    }
    
    public Task<List<MigrationHistory>> LoadMigrationHistoryAsync()
    {
        return Task.FromResult(_histories);
    }

    public Task SaveMigrationHistoryAsync(MigrationHistory history)
    {
        _histories.Add(history);
        return Task.CompletedTask;
    }
}