namespace InfluxMigrations.Core.Resolvers;

using static ResolverFunctionCommon;

[ResolverFunction("step")]
public class StepContextResolver : AbstractResolverFunction
{
    public override IResolvable<string?> Parse(string entry, Func<string, IResolvable<string?>> next)
    {
        // step keys are a special case in that they need a step id as well
        var split = Unwrap(entry, "${step").SplitFirst(":");
        if (split.Length != 2)
        {
            throw new MigrationResolutionException($"Cannot determine how to resolve step reference {entry}");
        }

        var keyValue = next(split[1]);
        return keyValue == null ? null : StepValue(split[0], keyValue, entry);
    }

    private static StringResolvable StepValue(string stepId, IResolvable<string?> key, string originalString)
    {
        /*
         * A step value resolver is a special case, it basically, when used, will change the context that the
         * resolutions are being made into the step with the provided id to try and work out the value
         * to use. This allows references to other steps to be performed.
         *
         * I don't know if i've made this too complicated by trying to infer where the scope is attempting to be performed.
         */
        return new StringResolvable(ResolutionType.Step, originalString, (x) =>
            {
                var context = x.MigrationExecutionContext.GetExecutionContext(stepId);
                if (context == null)
                {
                    throw new MigrationResolutionException(
                        $"Could not find Execution Context for Operation with id {stepId}");
                }

                var result = key.Resolve((IOperationExecutionContext)context);
                return result;
            },
            x =>
            {
                var context = x.GetExecutionContext(stepId);
                if (context == null)
                {
                    throw new MigrationResolutionException(
                        $"Could not find Execution Context for Operation with id {stepId}");
                }

                var result = key.Resolve((IOperationExecutionContext)context);
                return result;
            },
            x => throw new MigrationResolutionException("Cannot use a step result on an Environment context."));
    }

    public StepContextResolver(string prefix, string suffix) : base(prefix, suffix)
    {
    }
}