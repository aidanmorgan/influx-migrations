using System.Runtime.Serialization;
using InfluxMigrations.Core;

namespace InfluxMigrations.Yaml;

public class YamlMigrationParsingException : MigrationParsingException
{
    public YamlMigrationParsingException()
    {
    }

    protected YamlMigrationParsingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public YamlMigrationParsingException(string? message) : base(message)
    {
    }

    public YamlMigrationParsingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}