namespace InfluxMigrations.Core;

public enum MigrationPhase
{
    Execute,
    Commit,
    Rollback,
    Task,
    Migration
}