using InfluxMigrations.Core;

namespace InfluxMigrations.Yaml.Parsers;

public class ParserCommon
{
    private static readonly IDictionary<string, Type> OperationParsers = new Dictionary<string, Type>();
    private static readonly IDictionary<string, Type> TaskParsers = new Dictionary<string, Type>();

    private static List<Tuple<string, Type>> LoadPluggableBuilders(Type attributeType, Type interfaceType,
        Func<Attribute, string> keywordExtractor)
    {
        var coreTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .ToList()
            .WithAttributeAndInterface(attributeType, interfaceType);

        var extensionTypes = AppDomain.CurrentDomain
            .GetExtensionService()?
            .GetExtensionTypes()
            .WithAttributeAndInterface(attributeType, interfaceType);

        var all = new List<Tuple<Attribute, Type>>().Concat(coreTypes).Concat(extensionTypes).ToList();

        return all.Select(x => new Tuple<string, Type>(keywordExtractor(x.Item1), x.Item2)).ToList();
    }

    static ParserCommon()
    {
        AppDomain.CurrentDomain.GetExtensionService()?.AddSharedTypes(typeof(IYamlMigrationParser).Assembly);
        
        LoadPluggableBuilders(typeof(YamlOperationParserAttribute), typeof(IYamlOperationParser),
                (x) => ((YamlOperationParserAttribute)x).Keyword)
            .ForEach(x => OperationParsers[x.Item1] = x.Item2);

        LoadPluggableBuilders(typeof(YamlTaskParserAttribute), typeof(IYamlTaskParser),
                (x) => ((YamlTaskParserAttribute)x).Keyword)
            .ForEach(x => TaskParsers[x.Item1] = x.Item2);
    }

    protected IYamlTaskParser GetTaskParser(string key)
    {
        if (TaskParsers.TryGetValue(key, out var parser))
        {
            return (IYamlTaskParser)Activator.CreateInstance(parser);
        }

        throw new YamlMigrationParsingException($"Cannot find YamlTaskParser for key {key}");
    }

    protected IYamlOperationParser GetOperationParser(string key)
    {
        if (OperationParsers.TryGetValue(key, out var parser))
        {
            return (IYamlOperationParser)Activator.CreateInstance(parser);
        }

        throw new YamlMigrationParsingException($"Cannot find IYamlOperationParser for key {key}");
    }
}