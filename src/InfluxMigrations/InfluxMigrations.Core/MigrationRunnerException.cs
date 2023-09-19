using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationRunnerException : MigrationException
{
    public MigrationRunnerException()
    {
    }

    protected MigrationRunnerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MigrationRunnerException(string? message) : base(message)
    {
    }

    public MigrationRunnerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}