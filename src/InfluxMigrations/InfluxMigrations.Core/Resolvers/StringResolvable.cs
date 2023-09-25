namespace InfluxMigrations.Core.Resolvers;

public class StringResolvable : IResolvable<string?>, IEquatable<StringResolvable>
{
    public const string KeyOpening = "${";
    public const string KeyClosing = "}";

    private static readonly Dictionary<Tuple<string, string>, IResolverFunction> ResolverFunctions = new Dictionary<Tuple<string, string>, IResolverFunction>();

    static StringResolvable()
    {
        // register the shared types from this assembly with the extensions context before attempting to load
        // extensions.
        AppDomain.CurrentDomain.GetExtensionService()?.AddSharedTypes(typeof(IResolverFunction).Assembly);

        var coreTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .ToList()
            .WithAttributeAndInterface(typeof(ResolverFunctionAttribute), typeof(IResolverFunction));
            
        var extensionTypes = AppDomain.CurrentDomain
            .GetExtensionService()?
            .GetExtensionTypes()
            .WithAttributeAndInterface(typeof(ResolverFunctionAttribute), typeof(IResolverFunction));

        var allTypes = new List<Tuple<Attribute, Type>>().Concat(coreTypes).Concat(extensionTypes);
        
        foreach (var x in allTypes)
        {
            var attr = (ResolverFunctionAttribute)x.Item1;

            var keys = new List<string>()
            {
                attr.Key
            };
            keys.AddRange(attr.Aliases);

            foreach (var key in keys)
            {
                var prefix = KeyOpening + key;
                var suffix = KeyClosing;

                try
                {
                    var instance = (IResolverFunction?)Activator.CreateInstance(x.Item2, prefix, suffix);
                    if (instance != null)
                    {
                        ResolverFunctions[new Tuple<string, string>(prefix, suffix)] = instance;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }             
    }

    /// <summary>
    /// A convenience method that breaks the provided string down into corresponding tokens based on the ${} language that
    /// has been implemented.
    /// </summary>
    public static List<string> Tokenize(string input)
    {
        // NOTE: This code is a little odd at first, but we want to be able to support nesting of interpreter instructions
        // so we can't just do a split based on ${ and } values as they can be multiple deep.
        //
        // Basically the approach to this is to try and pull off all non-interpreter characters then recursively call this method
        // to pull off each interperter instruction block. That way the approach is always either processing interpreter commands
        // or plain text entries at a time which _should_ simplify the code.
        //
        // There are unit tests, i recommend looking at those.
        var tokens = new List<string>();

        if (!input.StartsWith(KeyOpening))
        {
            if (!input.Contains(KeyOpening))
            {
                return new List<string>()
                {
                    input
                };
            }
            else
            {
                tokens.Add(input.Substring(0, input.IndexOf(KeyOpening, StringComparison.Ordinal)));
                tokens.AddNonEmptyRange(Tokenize(input.Substring(input.IndexOf(KeyOpening, StringComparison.Ordinal))));
                return tokens;
            }
        }

        // we now know we start with a ${, so now we need to scan until we find the corresponding closing } remembering
        // that the interpreter commands can be nested within each other.
        var open = 0;
        for (int i = 1; i < input.Length; i++)
        {
            if (input[i - 1] == '$' && input[i] == '{')
            {
                open++;
                continue;
            }

            if (input[i] == '}')
            {
                if (open == 1)
                {
                    tokens.Add(input.Substring(0, i + 1).Trim());
                    tokens.AddNonEmptyRange(Tokenize(input.Substring(i + 1)));
                    return tokens;
                }
                else
                {
                    open--;
                }
            }
        }

        throw new MigrationResolutionException($"Could not tokenize string \"{input}\".");
    }

    public static IResolvable<string?> Parse(string entry)
    {
        if (string.IsNullOrEmpty(entry))
        {
            return null;
        }

        var tokenized = Tokenize(entry);

        if (tokenized.Count > 1)
        {
            var sequence = new SequentialResolvable();
            tokenized.Select(_Parse).ToList().ForEach(x => sequence.Add(x));
            return sequence;
        }

        return _Parse(tokenized[0]);
    }

    /// <summary>
    /// Performs the parse operation for a token from the Tokenize method.
    /// </summary>
    private static IResolvable<string?> _Parse(string entry)
    {
        foreach (var pair in ResolverFunctions.Keys)
        {
            if (entry.StartsWith(pair.Item1) && entry.EndsWith(pair.Item2))
            {
                var function = ResolverFunctions[pair];
                return function.Parse(entry, _Parse);
            }
        }

        // this is a special case that assumes anything that is between ${ and } that isn't a match is a local varaiable
        if (entry.StartsWith(KeyOpening) && entry.EndsWith(KeyClosing))
        {
            var key = _Parse(ResolverFunctionCommon.Unwrap(entry, KeyOpening));
            return key == null
                ? null
                : new StringResolvable(ResolutionType.Local, entry, (x) =>
                {
                    var keyVal = key.Resolve(x);
                    return string.IsNullOrEmpty(keyVal) ? null : x.Get(keyVal);
                }, x =>
                {
                    // TODO : decide if this is correct - does a 'local' make sense in a migration context?
                    var keyVal = key.Resolve(x);
                    return string.IsNullOrEmpty(keyVal) ? null : x.Get(keyVal);
                });
            ;
        }

        // This isn't recognised as a variable for resolution, so just return a fixed string value
        return ResolverFunctionCommon.FixedValue(entry);
    }

    private readonly Func<IOperationExecutionContext, string?> _operationFunc;
    private readonly Func<IMigrationExecutionContext, string?> _migrationFunc;
    private readonly string _id = Guid.NewGuid().ToString();
    public ResolutionType Scope { get; private init; }
    private readonly string _expression;

    public StringResolvable(ResolutionType resolutionType, string expression,
        Func<IOperationExecutionContext, string?> operationFunc,
        Func<IMigrationExecutionContext, string?> migrationFunc)
    {
        this.Scope = resolutionType;
        this._operationFunc = operationFunc;
        this._migrationFunc = migrationFunc;
        this._expression = expression;
    }

    public override string ToString()
    {
        return $"{nameof(_expression)}: {_expression}, {nameof(Scope)}: {Scope}";
    }

    public string? Resolve(IOperationExecutionContext context)
    {
        return _operationFunc(context);
    }

    public string? Resolve(IMigrationExecutionContext executionContext)
    {
        return _migrationFunc(executionContext);
    }

    public bool Equals(StringResolvable? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _id.Equals(other._id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((StringResolvable)obj);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(StringResolvable? left, StringResolvable? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StringResolvable? left, StringResolvable? right)
    {
        return !Equals(left, right);
    }
}

public static class StringExtensions
{
    public static string[] SplitFirst(this string entry, string sep)
    {
        var index = entry.IndexOf(sep);

        if (index < 0)
        {
            return new[]
            {
                entry
            };
        }

        return new[]
        {
            entry.Substring(0, index),
            entry.Substring(index + 1, entry.Length - index - 1)
        };
    }
}

public static class ListExtensions
{
    public static void AddNonEmptyRange(this List<string> list, List<string> toAdd)
    {
        var pruned = toAdd.Where(x => !string.IsNullOrEmpty(x)).ToList();
        if (pruned.Count > 0)
        {
            list.AddRange(pruned);
        }
    }
}