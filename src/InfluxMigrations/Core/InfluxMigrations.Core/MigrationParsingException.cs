using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationParsingException : MigrationException
{
    public MigrationParsingException()
    {
    }

    protected MigrationParsingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MigrationParsingException(string? message) : base(message)
    {
    }

    public MigrationParsingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}