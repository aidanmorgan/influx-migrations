using InfluxMigrations.Core;

namespace InfluxMigrations.Operations.Auth;

public class AddBucketTokenPermission
{
    private readonly IOperationExecutionContext _context;
    
    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();
    public IInfluxRuntimeResolver  Bucket { get; private set; } = InfluxRuntimeIdResolver.CreateBucket();

    public AddBucketTokenPermission(IOperationExecutionContext context)
    {
        _context = context;
    }

    public AddBucketTokenPermission Initialise(Action<AddBucketTokenPermission> callback)
    {
        callback(this);
        return this;
    }
}

public class AddBucketTokenPermissionResult : IExecuteResult 
{

}

public class AddBucketTokenPermissionBuilder : IMigrationOperationBuilder
{
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        throw new NotImplementedException();
    }
}