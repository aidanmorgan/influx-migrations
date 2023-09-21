namespace InfluxMigrations.Core.Resolvers;

using static ResolverFunctionCommon;

[ResolverFunction("migration")]
public class MigrationValueResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        var keyResolvable = next(Unwrap(entry, Prefix));
        return keyResolvable == null ? null : MigrationValue(keyResolvable, entry);
    }

    private static StringResolvable MigrationValue(IResolvable<string?> key, string originalString)
    {
        return new StringResolvable(ResolutionType.Migration, originalString, x =>
            {
                var keyVal = key.Resolve((IOperationExecutionContext)x);
                if (string.IsNullOrEmpty(keyVal))
                {
                    return null;
                }

                // try get the value from our execution context first
                var value = x.Get(keyVal);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                // it's not in the execution context, so try get it from the migration context
                value = x.MigrationExecutionContext.Get(keyVal);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                // now try get it from the environment context
                value = x.MigrationExecutionContext.EnvironmentContext.Get(keyVal);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                throw new MigrationResolutionException($"Could not find a value for key {key} in any context.");
            },
            x =>
            {
                var keyVal = key.Resolve((IMigrationExecutionContext)x);
                if (string.IsNullOrEmpty(keyVal))
                {
                    return null;
                }

                var value = x.Get(keyVal);

                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                value = x.EnvironmentContext.Get(keyVal);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                throw new MigrationResolutionException($"Could not find a value for key {key} in any context.");
            });
    }

    public MigrationValueResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}