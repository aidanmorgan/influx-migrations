using InfluxMigrations.Core;

namespace InfluxMigrations.Yaml;

public interface IYamlMigrationParser
{
    Task<IMigration> ParseFile(string inputFile, Func<string, IMigration>? migrationFactory = null);
    Task<IMigration> ParseString(string content, Func<string, IMigration>? migrationFactory = null);
}