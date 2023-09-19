namespace InfluxMigrations.CodeFirst;

public class InfluxMigrationAttribute
{
    public string Version { get; init; }

    public InfluxMigrationAttribute(string version)
    {
        Version = version;
    }
}