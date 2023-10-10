using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Operations.Auth;

public class DeleteBucketTokenPermissions : IMigrationOperation
{
    private readonly IOperationExecutionContext _context;
    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();
    public IInfluxRuntimeResolver Bucket { get; private set; } = InfluxRuntimeIdResolver.CreateBucket();

    public IList<IResolvable<string?>> Permissions { get; init; } = new List<IResolvable<string?>>();

    public DeleteBucketTokenPermissions(IOperationExecutionContext context)
    {
        _context = context;
    }

    public DeleteBucketTokenPermissions Initialise(Action<DeleteBucketTokenPermissions> op)
    {
        op(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var userId = await User?.GetAsync(_context);
        if (string.IsNullOrEmpty(userId))
        {
            return OperationResults.ExecuteFailed($"Cannot remove User permissions for Bucket, User cannot be resolved.");
        }

        var bucketId = await Bucket?.GetAsync(_context);
        if (string.IsNullOrEmpty(bucketId))
        {
            return OperationResults.ExecuteFailed($"Cannot remove User permissions for Bucket, Bucket cannot be resolved.");
        }

        var actions = Permissions?.Select(x => x.Resolve(_context)).Select(TokenCommon.MapPermission).ToList() ?? new List<Permission.ActionEnum?>();
        if (actions.Any(x => x == null))
        {
            return OperationResults.ExecuteFailed($"Cannot resolve Permission to remove from Bucket for User.");
        }

        // if there are no actions listed by the user then we'll assume it's a both read and write key
        // TODO : reconsider if this is a good idea or if we should default to READ 
        if (actions.Count == 0)
        {
            actions.Add(Permission.ActionEnum.Read);
            actions.Add(Permission.ActionEnum.Write);
        }
        
        try
        {
            var currentAuthorizations = await GetAuthorizationsForExecute(userId, bucketId, actions);

            var resourceIdsToRemove = new List<Tuple<string, Permission.ActionEnum>>();
            foreach (var action in actions.Cast<Permission.ActionEnum>())
            {
                var y = currentAuthorizations
                    .SelectMany(x => x.Permissions)
                    .Where(x => x.Action == action)
                    .Select(x => x.Resource.Id)
                    .FirstOrDefault((string?)null);

                if (string.IsNullOrEmpty(y))
                {
                    return OperationResults.ExecuteFailed($"Cannot find Permission for User and Bucket to remove.");
                }

                resourceIdsToRemove.Add(new Tuple<string, Permission.ActionEnum>(y, action));
            }

            // if there's any Authorizations where all the permissions are going to be removed then set them to inactive so they can be deleted in the commit phase.
            var inactivatedIds = new List<string>();
            foreach(var x in currentAuthorizations) 
            {
                var resourceIds = x
                    .Permissions
                    .Select(y => new Tuple<string, Permission.ActionEnum>(y.Resource.Id, y.Action))
                    .ToList();

                // if all of the resources are in the list, then we should deactivate the permission
                if(resourceIds.All(y => resourceIdsToRemove.Contains(y)))
                {
                    x.Status = AuthorizationUpdateRequest.StatusEnum.Inactive;
                    await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(x);
                    
                    inactivatedIds.Add(x.Id);
                }
            }
                
            return OperationResults.ExecuteSuccess(new DeleteBucketTokenPermissionsResult()
            {
                BucketId = bucketId,
                UserId = userId,
                Actions = actions.Cast<Permission.ActionEnum>().ToList(),
                PermissionResourceIds = resourceIdsToRemove,
                InactivatedAuthorizationIds = inactivatedIds
            });
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }


    public async Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult r)
    {
        var result = r as DeleteBucketTokenPermissionsResult;

        try
        {
            var authorisations = await GetAuthorizationsForCommitOrRollback(result);

            foreach (var authorisation in authorisations)
            {
                if (result.InactivatedAuthorizationIds.Contains(authorisation.Id))
                {
                    await _context.Influx.GetAuthorizationsApi().DeleteAuthorizationAsync(authorisation.Id);
                }
                else
                {
                    var count = authorisation.Permissions.RemoveAll(x => result.PermissionResourceIds.Contains(new Tuple<string, Permission.ActionEnum>(x.Resource.Id, x.Action)));
                    if (count > 0)
                    {
                        await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(authorisation);
                    }
                }
            }

            return OperationResults.CommitSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.CommitFailed(result, x);
        }
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult r)
    {
        var result = r as DeleteBucketTokenPermissionsResult;

        try
        {
            var authorisations = await GetAuthorizationsForCommitOrRollback(result);

            foreach (var authorisation in authorisations.Where(authorisation => result.InactivatedAuthorizationIds.Contains(authorisation.Id)))
            {
                authorisation.Status = AuthorizationUpdateRequest.StatusEnum.Active;
                await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(authorisation);
            }

            return OperationResults.RollbackSuccess(result);
        }
        catch (Exception x)
        {
            return OperationResults.RollbackFailed(result, x);
        }
    }
    
    private async Task<List<Authorization>> GetAuthorizationsForExecute(string userId, string bucketId, List<Permission.ActionEnum?> actions)
    {
        var currentAuthorizations = (await _context.Influx.GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(userId))
            .Where(x =>
            {
                return x.UserID == userId &&
                       x.Permissions.Any(y =>
                           y.Resource.Id == bucketId && y.Resource.Type == PermissionResource.TypeBuckets &&
                           actions.Contains(y.Action));
            })
            .ToList();
        return currentAuthorizations;
    }
    
    private async Task<List<Authorization>> GetAuthorizationsForCommitOrRollback(DeleteBucketTokenPermissionsResult? result)
    {
        return (await _context.Influx.GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(result.UserId))
            .Where(x =>
            {
                return result.InactivatedAuthorizationIds.Contains(x.Id) ||
                       (x.Status == AuthorizationUpdateRequest.StatusEnum.Active &&
                        x.Permissions.Any(y => result.PermissionResourceIds.Contains(new Tuple<string, Permission.ActionEnum>(y.Resource.Id, y.Action))));
            })
            .ToList();
    }
    
}

public class DeleteBucketTokenPermissionsResult : IExecuteResult
{
    public string BucketId { get; init; }
    public string UserId { get; init; }
    public List<Tuple<string, Permission.ActionEnum>> PermissionResourceIds { get; init; }
    public List<string> InactivatedAuthorizationIds { get; set; }
    public List<Permission.ActionEnum> Actions { get; set; }
}

public class DeleteBucketTokenPermissionsBuilder : IMigrationOperationBuilder
{
    public string UserId { get; private set; }
    public DeleteBucketTokenPermissionsBuilder WithUserId(string v)
    {
        this.UserId = v;
        return this;
    }

    public string UserName { get; private set; }
    public DeleteBucketTokenPermissionsBuilder WithUserName(string v)
    {
        this.UserName = v;
        return this;
    }

    public string BucketId { get; private set; }
    public DeleteBucketTokenPermissionsBuilder WithBucketId(string v)
    {
        this.BucketId = v;
        return this;
    }

    public string BucketName { get; private set; }
    public DeleteBucketTokenPermissionsBuilder WithBucketName(string v)
    {
        this.BucketName = v;
        return this;
    }

    private List<string> Permissions = new List<string>();

    public DeleteBucketTokenPermissionsBuilder WithPermission(string permission)
    {
        Permissions.Add(permission);
        return this;
    }

    public DeleteBucketTokenPermissionsBuilder WithPermissions(List<string> permissions)
    {
        Permissions.AddRange(permissions);
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        var task = new DeleteBucketTokenPermissions(context)
            {
                Permissions = Permissions.Select(StringResolvable.Parse).ToList()
            }
            .Initialise(x =>
            {
                x.User.WithId(UserId).WithName(UserName);
                x.Bucket.WithId(BucketId).WithName(BucketName);
            });

        return task;
    }
}