using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationLoadingException : MigrationException
{
    public MigrationLoadingException()
    {
    }

    protected MigrationLoadingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MigrationLoadingException(string? message) : base(message)
    {
    }

    public MigrationLoadingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}