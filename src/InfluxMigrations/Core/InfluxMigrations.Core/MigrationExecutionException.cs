using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationExecutionException : MigrationException
{
    public MigrationExecutionException()
    {
    }

    protected MigrationExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MigrationExecutionException(string? message) : base(message)
    {
    }

    public MigrationExecutionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}