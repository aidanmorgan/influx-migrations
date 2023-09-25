using System.Reflection;
using System.Runtime.Loader;
using McMaster.NETCore.Plugins;
using Newtonsoft.Json;

namespace InfluxMigrations.Core;


// Allows the registration of additional types that can be loaded at runtime to extend the core migrations framework.
public interface IExtensionsService
{
    // intentionally register the extension service with a different name each time it is run for reasons I can't remember
    // why I chose to do previously.
    public static readonly string AppDomainName = $"{nameof(IExtensionsService)}--{Guid.NewGuid()}";
    
    List<Type> GetExtensionTypes();
    void AddSharedTypes(Assembly ass);
}

public static class AppDomainExtensions
{
    public static IExtensionsService GetExtensionService(this AppDomain domain)
    {
        var svc = (IExtensionsService?)domain.GetData(IExtensionsService.AppDomainName);
        return svc ?? NoOpExtensionService.Instance;
    }

    public static void RegisterExtensionService(this AppDomain domain, IExtensionsService? context)
    {
        if (context != null)
        {
            domain.SetData(IExtensionsService.AppDomainName, context!);
        }
    }
}

// implementation of the IExtensionsService interface that allows extensions to be loaded
// from assemblies that are below a specific directory.
public class ExtensionsService : IExtensionsService
{
    private readonly string _directory;
    private readonly Func<string, bool> _filter;
    private List<PluginLoader>? _loaders = new List<PluginLoader>();
    private readonly HashSet<Type> _sharedTypes = new HashSet<Type>();

    public ExtensionsService(string dir, Func<string, bool>? filter = null)
    {
        _directory = dir;
        _filter = filter ?? (s => true);
    }
    
    // the plugin framework will load types into a separate application context, so we need to be able to tell it which
    // types from the host application we want to consider "shared" so the types from the 
    public void AddSharedTypes(Assembly ass)
    {
        foreach (var type in ass.GetTypes())
        {
            _sharedTypes.Add(type);
        }

        _loaders?.ForEach(x => x.Dispose());
        _loaders = null;
    }


    private IEnumerable<PluginLoader> Initialise()
    {
        if (_loaders != null)
        {
            return _loaders;
        }
        
        var result = Directory.GetDirectories(_directory).Select(
                x =>
                {
                    if (!_filter(x)) return null;
                    
                    try
                    {
                        var dirName = Path.GetFileName(x);
                        var pluginDll = Path.Combine(x, dirName + ".dll");

                        if (File.Exists(pluginDll))
                        {
                            var loader = PluginLoader.CreateFromAssemblyFile(
                                pluginDll,
                                isUnloadable:true,
                                sharedTypes: _sharedTypes.ToArray());

                            return loader;
                        }

                    }
                    catch (Exception)
                    {
                        return null;
                    }

                    return null;
                })
            .Where(x => x != null)
            .ToList();

        _loaders = result!;
        return _loaders;
    }
    
    public List<Type> GetExtensionTypes()
    {
        var loaders = Initialise();
        var allTypes = loaders
            .Select(x => x.LoadDefaultAssembly())
            .SelectMany(x => x.GetTypes())
            .ToList();

        return allTypes;
    }
}

public class NoOpExtensionService : IExtensionsService
{
    public static readonly NoOpExtensionService Instance = new NoOpExtensionService();
    public List<Type> GetExtensionTypes()
    {
        return new List<Type>();
    }

    public void AddSharedTypes(Assembly ass)
    {
        // intentionally no-op
    }
}

public static class ExtensionExtensions
{
    public static List<Tuple<Attribute, Type>> WithAttributeAndInterface(this List<Type> types, Type attributeType, Type interfaceType)
    {
        return types.Select(x =>
            {
                var attribute = x.GetCustomAttributes()
                    .FirstOrDefault(y => y.GetType() == attributeType, null);
                
                if (attribute != null && x.GetInterfaces().Contains(interfaceType))
                {
                    return new Tuple<Attribute, Type>(attribute, x);
                }
                return null;
            }).Where(x => x != null)
            .ToList() ?? new List<Tuple<Attribute, Type>>();
    }
    
    public static List<Tuple<Attribute, Type>> WithAttribute(this List<Type> types, Type attributeType)
    {
        return  types.Select(x =>
            {
                var attribute = x.GetCustomAttributes().FirstOrDefault(y => y.GetType() == attributeType, null);
                return attribute != null ? new Tuple<Attribute, Type>(attribute, x) : null;
            }).Where(x => x != null)
            .ToList() ?? new List<Tuple<Attribute, Type>>();
    }

    public static List<Type> WithInterface(this List<Type> types, Type interfaceType)
    {
        return types
            .Select(x => x.GetInterfaces().Contains(interfaceType) ? x : null).Where(x => x != null)
            .ToList() ?? new List<Type>();
    }
    
}