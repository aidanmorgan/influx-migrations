using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationTaskExecutionException : MigrationException
{
    public MigrationTaskExecutionException()
    {
    }

    protected MigrationTaskExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MigrationTaskExecutionException(string? message) : base(message)
    {
    }

    public MigrationTaskExecutionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}