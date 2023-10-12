using NUnit.Framework;

namespace InfluxMigrations.Core.UnitTests;

public class ExtensionsLoadingTests
{
    [Test]
    public void TestExtensionLoading_Success()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        while (Directory.GetFiles(currentDirectory, "*.sln").Length == 0)
        {
            currentDirectory = Directory.GetParent(currentDirectory).FullName;
        }

        var extensionsContext = new ExtensionsService($"{currentDirectory}{Path.DirectorySeparatorChar}Extensions");
        
        var types = extensionsContext.GetExtensionTypes();
        
        Assert.That(types, Is.Not.Null);
    }
}