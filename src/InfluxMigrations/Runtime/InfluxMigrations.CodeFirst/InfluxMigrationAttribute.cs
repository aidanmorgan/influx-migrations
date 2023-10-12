namespace InfluxMigrations.CodeFirst;

public class InfluxMigrationAttribute : Attribute
{
    public string Version { get; init; }

    public InfluxMigrationAttribute(string version)
    {
        Version = version;
    }
}