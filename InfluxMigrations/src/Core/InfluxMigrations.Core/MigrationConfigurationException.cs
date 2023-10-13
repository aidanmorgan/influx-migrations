using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationConfigurationException : MigrationException
{
    public MigrationConfigurationException()
    {
    }

    protected MigrationConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MigrationConfigurationException(string? message) : base(message)
    {
    }

    public MigrationConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}