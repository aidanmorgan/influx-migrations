using InfluxMigrations.Core;

namespace InfluxMigrations.Yaml;

public class YamlOperationParserAttribute : Attribute
{
    public string Keyword { get; private set; }
    public List<string> Fields { get; private set; }

    public YamlOperationParserAttribute(string keyword, params string[] fields)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            throw new MigrationParsingException(
                $"Cannot use a {nameof(YamlOperationParserAttribute)} with no keyword.");
        }

        Keyword = keyword;
        Fields = fields?.ToList() ?? new List<string>();
    }
}