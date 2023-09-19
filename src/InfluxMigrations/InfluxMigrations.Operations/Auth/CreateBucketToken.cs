using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxMigrations.Commands.Bucket;
using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Commands.Auth;

public class CreateBucketToken : IMigrationOperation
{
    private static readonly IDictionary<string, Permission.ActionEnum> _actionEnums =
        new Dictionary<string, Permission.ActionEnum>()
        {
            { "read", Permission.ActionEnum.Read },
            { "write", Permission.ActionEnum.Write }
        };
    
    private readonly IOperationExecutionContext _context;
    public InfluxRuntimeIdResolver Bucket { get;  set; }
    public InfluxRuntimeIdResolver User { get;  set; }
    
    public IResolvable<string?>? TokenDescription { get; set; }
    public List<IResolvable<string?>> Permissions { get; set; } = new List<IResolvable<string?>>();
    
    public CreateBucketToken(IOperationExecutionContext context)
    {
        _context = context;

        Bucket = InfluxRuntimeIdResolver.CreateBucket();
        User = InfluxRuntimeIdResolver.CreateUser();
    }

    public CreateBucketToken Initialise(Action<CreateBucketToken> callback)
    {
        callback(this);
        return this;
    }

    public async Task<OperationResult<OperationExecutionState, IExecuteResult>> ExecuteAsync()
    {
        var bucketId = await Bucket.GetAsync(_context);
        if (string.IsNullOrEmpty(bucketId))
        {
            throw new MigrationExecutionException($"Cannot retrieve Bucket id for provided id/name");
        }
        
        var description = TokenDescription?.Resolve(_context) ??  $"Token created via Migration {_context.MigrationExecutionContext.Version}";
        var userId = await User.GetAsync(_context);

        Authorization auth = null;

        var bucket = await _context.Influx.GetBucketsApi().FindBucketByIdAsync(bucketId);
        
        var actionEnums = Permissions.Select(x => x.Resolve(_context)).Select(x => _actionEnums[x])
            .ToList();

        // if a userid is set, then it should be set as the owner of the authroization, otherwise the authorization
        // is created with the Auth token as the owner.
        if (userId != null)
        {
            
            auth = await _context.Influx.GetAuthorizationsApi().CreateAuthorizationAsync(
                new AuthorizationPostRequest(
                    bucket.OrgID,
                    userId,
                    actionEnums.Select(x => new Permission(x, 
                        new PermissionResource()
                        {
                            Id = bucketId,
                            Type = PermissionResource.TypeBuckets
                        })
                    ).ToList(),
                    AuthorizationUpdateRequest.StatusEnum.Active,
                    description
                )
            );
        }
        else
        {
            auth = await _context.Influx.GetAuthorizationsApi().CreateAuthorizationAsync(
                bucket.OrgID,
                actionEnums.Select(x => new Permission(x, 
                    new PermissionResource()
                    {
                        Id = bucketId,
                        Type = PermissionResource.TypeBuckets
                    })).ToList());
        }
        
        return OperationResults.ExecuteSuccess(new CreateBucketTokenResult()
        {
            Token = auth.Token,
            TokenId = auth.Id,
            BucketId = bucketId,
            OwnerId = auth.Links?.User
        });
    }
    
    public Task<OperationResult<OperationCommitState, ICommitResult?>> CommitAsync(IExecuteResult result)
    {
        return OperationResults.CommitUnnecessary(result);
    }

    public Task<OperationResult<OperationRollbackState, IRollbackResult?>> RollbackAsync(IExecuteResult result)
    {
        return OperationResults.RollbackImpossible(result);
    }
    
}

public class CreateBucketTokenResult : IExecuteResult
{
    public string BucketId { get; set; }
    public string Token { get; set; }
    public string TokenId { get; set; }
    public string? OwnerId { get; set; }

    public CreateBucketTokenResult()
    {
    }
}


public class CreateBucketTokenBuilder : IMigrationOperationBuilder
{
    private string _tokenName;
    private string _bucket;
    private string _bucketId;
    private string _user;
    private string _userId;
    
    private List<string> _permissions = new List<string>();

    public CreateBucketTokenBuilder WithTokenName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new MigrationConfigurationException($"Cannot set a null write token name.");
        }
        
        _tokenName = name;
        return this;
    }

    public CreateBucketTokenBuilder WithBucket(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new MigrationConfigurationException("Cannot set a null bucket name.");
        }

        _bucket = name;
        return this;
    }

    public CreateBucketTokenBuilder WithBucketId(string name)
    {
        _bucketId = name;
        return this;
    }

    public CreateBucketTokenBuilder WithPermission(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new MigrationConfigurationException("Cannot add a null permission");
        }

        _permissions.Add(name);
        return this;
    }
    
    public CreateBucketTokenBuilder WithUser(string username)
    {
        _user = username;
        return this;
    }

    public CreateBucketTokenBuilder WithUserId(string id)
    {
        _userId = id;
        return this;
    }

    public IMigrationOperation Build(IOperationExecutionContext context)
    {
        var permissions = _permissions.Select(x => StringResolvable.Parse(x) ?? null).Where(x => x != null).ToList();
        if (permissions == null || permissions.Count == 0)
        {
            throw new MigrationOperationBuildingException("No permissions specified.");
        }

        if (string.IsNullOrEmpty(_bucketId) && string.IsNullOrEmpty(_bucket))
        {
            throw new MigrationOperationBuildingException("No Bucket specified.");
        }

        if (string.IsNullOrEmpty(_user) && string.IsNullOrEmpty(_userId))
        {
            throw new MigrationOperationBuildingException("No User specified.");
        }
        
        return new CreateBucketToken(context)
        {
            Permissions = permissions!,
            TokenDescription = StringResolvable.Parse(_tokenName)
        }.Initialise(val =>
        {
            val.Bucket.WithId(StringResolvable.Parse(_bucketId)).WithName(StringResolvable.Parse(_bucket));
            val.User.WithId(StringResolvable.Parse(_userId)).WithName(StringResolvable.Parse(_user));
        });
    }
}