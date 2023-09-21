using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml;

public static class YamlNodeExtensions
{
    public static bool ContainsKey(this YamlNode node, string name)
    {
        try
        {
            var result = node[name];
            return true;
        }
        catch (KeyNotFoundException x)
        {
            return false;
        }
    }

    public static string? GetStringValue(this YamlNode node, string name)
    {
        if (node.ContainsKey(name))
        {
            var value = node[name];
            return value.GetStringValue();
        }

        return null;
    }

    public static string? GetStringValue(this YamlNode node)
    {
        return node is YamlScalarNode scalarNode ? scalarNode.Value : null;
    }

    public static bool Value(this YamlNode node, string name, Action<string> callback)
    {
        if (node.ContainsKey(name))
        {
            var value = node[name].GetStringValue();

            if (!string.IsNullOrEmpty(value))
            {
                callback(value);
                return true;
            }
        }

        return false;
    }

    public static bool ForEach(this YamlNode node, string name, Action<YamlNode> callback)
    {
        if (node.ContainsKey(name))
        {
            if (node[name] is YamlSequenceNode sequenceNode)
            {
                foreach (var child in (sequenceNode.Children))
                {
                    callback(child);
                }

                return true;
            }
            else if (node[name] is YamlMappingNode mappingNode)
            {
                callback(mappingNode);
                return true;
            }
            else if (node[name] is YamlScalarNode scalarNode)
            {
                callback(scalarNode);
                return true;
            }
            else
            {
                throw new YamlMigrationParsingException("Unexpected node type.");
            }
        }

        return false;
    }

    public static void ForEach(this YamlNode node, Action<YamlNode> callback)
    {
        if (node is YamlSequenceNode sequenceNode)
        {
            foreach (var child in (sequenceNode.Children))
            {
                callback(child);
            }
        }
        else if (node is YamlMappingNode mappingNode)
        {
            callback(mappingNode);
        }
        else if (node is YamlScalarNode scalarNode)
        {
            callback(scalarNode);
        }
        else
        {
            throw new YamlMigrationParsingException("Unexpected node type.");
        }
    }
}