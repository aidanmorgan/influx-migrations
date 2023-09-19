namespace InfluxMigrations.Yaml;

public class YamlTaskParserAttribute : Attribute
{
    public string Keyword { get; private set; }

    public YamlTaskParserAttribute(string keyword)
    {
        Keyword = keyword;
    }
}