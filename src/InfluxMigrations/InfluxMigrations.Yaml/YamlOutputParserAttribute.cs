namespace InfluxMigrations.Yaml;

public class YamlOutputParserAttribute : Attribute
{
    public string Keyword { get; private set; }

    public YamlOutputParserAttribute(string keyword)
    {
        Keyword = keyword;
    }
}