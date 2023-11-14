using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Operations.Auth;

public class ChangeAuthorisationState : IMigrationOperation
{
    public IInfluxRuntimeResolver Bucket { get; private set; } = InfluxRuntimeIdResolver.CreateBucket();
    public IInfluxRuntimeResolver User { get; private set; } = InfluxRuntimeIdResolver.CreateUser();
    public IResolvable<string?>? AuthorisationId { get; set; }
    public IResolvable<string?>? Token { get; set; }
    public IResolvable<string?>? Status { get; set; }

    
    private readonly IContext _context;
    
    public ChangeAuthorisationState(IContext context)
    {
        _context = context;
    }
    
    public ChangeAuthorisationState Initialise(Action<ChangeAuthorisationState> callback)
    {
        callback(this);
        return this;
    }
    
    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var status = TokenCommon.MapStatus(Status?.Resolve(_context));
        if (status == null)
        {
            return OperationResults.ExecuteFailed($"Cannot resolve status, should be ACTIVE or INACTIVE");
        }
        
        var authorisation = AuthorisationId?.Resolve(_context);
        string? bucket = null;

        try
        {
            bucket = await Bucket.GetAsync(_context);
        }
        catch (MigrationResolutionException)
        {
            // ignore - we can continue without this set
        }

        if (!string.IsNullOrEmpty(authorisation))
        {
            return await ChangeAuthorisationStateById(authorisation, status.Value);
        }
        else
        {
            var token = Token?.Resolve(_context);

            if (!string.IsNullOrEmpty(token))
            {
                return await ChangeAuthorisationStateByToken(token, status.Value);
            }
            else
            {
                string? user = null;

                try
                {
                    user = await User.GetAsync(_context);
                }
                catch(MigrationResolutionException) { 
                    // ignore
                }

                if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(bucket))
                {
                    return OperationResults.ExecuteFailed($"Cannot change state, must have user and/or bucket set.");
                }

                if (!string.IsNullOrEmpty(user) && string.IsNullOrEmpty(bucket))
                {
                    return await ChangeAuthorisationStateForUser(user, status.Value);
                }

                if (!string.IsNullOrEmpty(bucket) && string.IsNullOrEmpty(user))
                {
                    return await ChangeAuthorisationStateForBucket(bucket, status.Value);
                }

                return await ChangeAuthorisationStateForUserAndBucket(user, bucket, status.Value);
            }
        }
    }

    private async Task<OperationResult<OperationExecutionState, IExecuteResult>> UpdateStatus(List<Authorization> auths,
        AuthorizationUpdateRequest.StatusEnum status)
    {
        if (auths.Count == 0)
        {
            return OperationResults.ExecuteFailed($"No suitable Authorisations found.");
        }
        
        var success = new List<string>();

        try
        {
            // only bother updating the ones we know we need to update, there's no point setting a status
            // that is already set
            var tasks = auths
                .Where(x => x.Status != status)
                .Select(async x =>
            {
                x.Status = status;
                
                await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(x);

                success.Add(x.Id);
            });

            // TODO : check this throws exceptions as expected
            await Task.WhenAll(tasks);

            return OperationResults.ExecuteSuccess(new ChangeAuthorisationStateResult()
            {
                AuthorisationIds = success,
                Status = status
            });
        }
        catch (Exception x)
        {
            return new OperationResult<OperationExecutionState, IExecuteResult>(OperationExecutionState.Failed,
                new ChangeAuthorisationStateResult()
                {
                    AuthorisationIds = success,
                    Status = status
                });
        }
    }
    
    private async Task<OperationResult<OperationExecutionState,IExecuteResult>> ChangeAuthorisationStateForUserAndBucket(string user, string bucket, AuthorizationUpdateRequest.StatusEnum status)
    {
        try
        {
            var userAuths = await _context.Influx.GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(user);
            var auths = userAuths
                .Where(x =>
                {
                    return x != null && x.Permissions.Any(y =>
                        y.Resource.Type == PermissionResource.TypeBuckets && y.Resource.Id == bucket);
                })
                .ToList();

            return await UpdateStatus(auths, status);
        }
        catch(Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    private async Task<OperationResult<OperationExecutionState,IExecuteResult>> ChangeAuthorisationStateForBucket(string bucket, AuthorizationUpdateRequest.StatusEnum status)
    {
        try
        {
            var auths = (await _context.Influx.GetAuthorizationsApi().FindAuthorizationsAsync())
                .Where(x => x != null && 
                            x.Permissions
                                .Any(y => y.Resource.Type == PermissionResource.TypeBuckets && y.Resource.Id == bucket)
                ).ToList();

            return await UpdateStatus(auths, status);
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    private async Task<OperationResult<OperationExecutionState,IExecuteResult>> ChangeAuthorisationStateForUser(string user, AuthorizationUpdateRequest.StatusEnum status)
    {
        try
        {
            var auths = (await _context.Influx.GetAuthorizationsApi().FindAuthorizationsByUserIdAsync(user)).ToList();
            return await UpdateStatus(auths, status);
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    private async Task<OperationResult<OperationExecutionState,IExecuteResult>> ChangeAuthorisationStateByToken(string? token, AuthorizationUpdateRequest.StatusEnum status)
    {
        try
        {
            var auths = (await _context.Influx.GetAuthorizationsApi().FindAuthorizationsAsync())
                .Where(x => x != null && x.Token == token)
                .ToList();

            return await UpdateStatus(auths, status);
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    private async Task<OperationResult<OperationExecutionState, IExecuteResult>> ChangeAuthorisationStateById(
        string authorisation, AuthorizationUpdateRequest.StatusEnum statusEnum)
    {
        try
        {
            var auth = await _context.Influx.GetAuthorizationsApi().FindAuthorizationByIdAsync(authorisation);
            if (auth == null)
            {
                return OperationResults.ExecuteFailed($"Could not find authorization with provided id.");
            }

            return await UpdateStatus(new List<Authorization>() { auth }, statusEnum);
        }
        catch (Exception x)
        {
            return OperationResults.ExecuteFailed(x);
        }
    }

    public Task<OperationResult<OperationCommitState, ICommitResult>> CommitAsync(IExecuteResult result)
    {
        return OperationResults.CommitUnnecessary(result);
    }

    public async Task<OperationResult<OperationRollbackState, IRollbackResult>> RollbackAsync(IExecuteResult res)
    {
        var failed = new List<string>();
        var result = res as ChangeAuthorisationStateResult;


        foreach (var id in result.AuthorisationIds)
        {
            try
            {
                var auth = await _context.Influx.GetAuthorizationsApi().FindAuthorizationByIdAsync(id);

                if (auth == null)
                {
                    throw new ArgumentException($"Could not find Authorisation");
                }

                auth.Status = TokenCommon.InvertStatus(result.Status);
                await _context.Influx.GetAuthorizationsApi().UpdateAuthorizationAsync(auth);
            }
            catch (Exception x)
            {
                failed.Add(id);
            }            
        }

        if (failed.Count > 0)
        {
            return OperationResults.RollbackFailed(result,
                $"Could not revert authorisation statuses for [{string.Join(",", failed)}]");
        }
        else
        {
            return OperationResults.RollbackSuccess(result);
        }
    }
}

public class ChangeAuthorisationStateResult : IExecuteResult
{
    public List<string> AuthorisationIds { get; init; } = new List<string>();
    public AuthorizationUpdateRequest.StatusEnum Status { get; init; }
}

public class ChangeAuthorisationStateBuilder : IMigrationOperationBuilder
{
    public string BucketId { get; private set; }
    public ChangeAuthorisationStateBuilder WithBucketId(string v)
    {
        this.BucketId = v;
        return this;
    }

    public string BucketName { get; private set; }
    public ChangeAuthorisationStateBuilder WithBucketName(string v)
    {
        this.BucketName = v;
        return this;
    }

    public string UserId { get; private set; }
    public ChangeAuthorisationStateBuilder WithUserId(string v)
    {
        this.UserId = v;
        return this;
    }

    public string UserName { get; private set; }
    public ChangeAuthorisationStateBuilder WithUserName(string v)
    {
        this.UserName = v;
        return this;
    }

    public string Token { get; private set; }
    public ChangeAuthorisationStateBuilder WithToken(string v)
    {
        this.Token = v;
        return this;
    }

    public string AuthorisationId { get; private set; }
    public ChangeAuthorisationStateBuilder WithAuthorisationId(string v)
    {
        this.AuthorisationId = v;
        return this;
    }

    public string Status { get; private set; }
    public ChangeAuthorisationStateBuilder WithState(string v)
    {
        this.Status = v;
        return this;
    }
    
    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        return new ChangeAuthorisationState(context).Initialise(x =>
        {
            x.User.WithId(UserId).WithName(UserName);
            x.Bucket.WithId(BucketId).WithName(BucketName);
            
            x.Token = StringResolvable.Parse(Token);
            x.AuthorisationId = StringResolvable.Parse(AuthorisationId);
            x.Status = StringResolvable.Parse(Status);
        });
    }
}