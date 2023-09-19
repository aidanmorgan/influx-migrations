using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class ImpossibleMigrationTaskException : MigrationException
{
    public ImpossibleMigrationTaskException()
    {
    }

    protected ImpossibleMigrationTaskException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ImpossibleMigrationTaskException(string? message) : base(message)
    {
    }

    public ImpossibleMigrationTaskException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}