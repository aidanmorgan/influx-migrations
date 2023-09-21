using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public class MigrationOperationBuildingException : MigrationException
{
    public MigrationOperationBuildingException()
    {
    }

    protected MigrationOperationBuildingException(SerializationInfo info, StreamingContext context) : base(info,
        context)
    {
    }

    public MigrationOperationBuildingException(string? message) : base(message)
    {
    }

    public MigrationOperationBuildingException(string? message, Exception? innerException) : base(message,
        innerException)
    {
    }
}