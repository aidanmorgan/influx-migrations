namespace InfluxMigrations.Core;

public record MigrationHistory(string Version, MigrationDirection Direction, DateTimeOffset Timestamp, bool? Success);

public interface IMigrationHistoryServiceOptions
{
    IMigrationHistoryLogger Logger { get; init; }
}

/// <summary>
/// Used to load history of migrations that have already been run
/// </summary>
public interface IMigrationHistoryService
{
    Task<List<MigrationHistory>> LoadMigrationHistoryAsync();
    Task SaveMigrationHistoryAsync(MigrationHistory history);
}