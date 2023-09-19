using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationResolutionException : MigrationException
{
    public MigrationResolutionException()
    {
    }

    protected MigrationResolutionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MigrationResolutionException(string? message) : base(message)
    {
    }

    public MigrationResolutionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}