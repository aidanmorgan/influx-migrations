using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;



class Build : NukeBuild
{
    [GitVersion] readonly GitVersion GitVersion;
    static readonly AbsolutePath PackageOutputDirectory = RootDirectory / "packages";
    static readonly AbsolutePath CoreInterfacesPackageOutputDirectory = PackageOutputDirectory / "core-interfaces";
    static readonly AbsolutePath CorePackagesDirectory = PackageOutputDirectory / "core";
    static readonly AbsolutePath ExtensionsPackageOutputDirectory = PackageOutputDirectory / "extensions";

    static readonly AbsolutePath TestsRoot = RootDirectory / "tests";

    static readonly AbsolutePath CorePath = RootDirectory / "src" / "InfluxMigrations";
    static readonly Solution CoreSolutionFile = (CorePath / "InfluxMigrations.sln").ReadSolution();
    static readonly AbsolutePath CoreTestsDirectory = TestsRoot / "core";

    static readonly List<AbsolutePath> InterfaceProjects = new List<AbsolutePath>()
    {
        CorePath / "Core" / "InfluxMigrations.Core",
        CorePath / "Yaml" / "InfluxMigrations.Yaml"
    };

    static readonly AbsolutePath ExtensionsPath = RootDirectory / "src" / "InfluxMigrationsExtensions";
    static readonly Solution ExtensionsSolutionFile = (ExtensionsPath / "InfluxMigrations.Extensions.sln").ReadSolution();

    static readonly List<AbsolutePath> AllSolutionDirectories = new List<AbsolutePath>()
    {
        CorePath,
        ExtensionsPath
    };

    public static int Main () => Execute<Build>(x => x.All);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    Target Clean => _ => _
        .Before(CoreRestore)
        .Executes(() =>
        {
            foreach (var project in AllSolutionDirectories)
            {
                project.GlobDirectories("**/bin", "**/obj")
                    .ForEach(x => x.DeleteDirectory());
            }
            
            PackageOutputDirectory.DeleteDirectory();
            TestsRoot.DeleteDirectory();
        });

    Target CoreRestore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s.SetProjectFile(CoreSolutionFile));    
        });

    Target CoreCompile => _ => _
        .DependsOn(CoreRestore)
        .Executes(() =>
        {
            Console.WriteLine(CoreSolutionFile);
            DotNetBuild(s => s
                .SetProjectFile(CoreSolutionFile)
                .SetConfiguration(Configuration)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .EnableNoRestore());
            
        });

    Target CoreUnitTests => _ => _
        .DependsOn(CoreCompile)
        .Before(CorePackage)
        .Executes(() =>
        {
            foreach (var testProject in CoreSolutionFile.GetAllProjects("*UnitTests"))
            {
                DotNetTest(s => s
                    .SetProjectFile(testProject)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetResultsDirectory(CoreTestsDirectory / "results")
                );
            }
        });

    Target CoreIntegrationTests => _ => _
        .DependsOn(CoreCompile)
        .Before(CorePackage)
        .Executes(() =>
        {
            foreach (var testProject in CoreSolutionFile.GetAllProjects("*IntegrationTests"))
            {
                DotNetTest(s => s
                    .SetProjectFile(testProject)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetResultsDirectory(CoreTestsDirectory / "results")
                );
            }
        });
    
    Target CorePackage => _ => _
        .DependsOn(CoreCompile)
        .Executes(() =>
        {
            foreach (var interfaceProject in InterfaceProjects)
            {
                DotNetPack(s => 
                    s.SetProject(interfaceProject)
                        .SetNoRestore(true)
                        .SetNoBuild(true)
                        .SetOutputDirectory(CoreInterfacesPackageOutputDirectory)
                        .SetAuthors("Aidan Morgan")
                        .SetRepositoryUrl("https://github.com/aidanmorgan/influx-migrations")
                );
            }

            var coreProjects = CoreSolutionFile.GetAllProjects("*")
                .Where(x => !x.Directory.Contains("Test"))
                .Where(x => !InterfaceProjects.Contains(x.Directory));

            foreach (var pkg in coreProjects)
            {
                DotNetPack(s => 
                    s.SetProject(pkg)
                        .SetNoRestore(true)
                        .SetNoBuild(true)
                        .SetOutputDirectory(CorePackagesDirectory)
                        .SetAuthors("Aidan Morgan")
                        .SetRepositoryUrl("https://github.com/aidanmorgan/influx-migrations")
                );
                
            }
        });

    Target Core => _ => _
        .DependsOn(CoreRestore)
        .DependsOn(CoreCompile)
        .DependsOn(CoreUnitTests)
//        .DependsOn(CoreIntegrationTests)
        .DependsOn(CorePackage);

    Target ExtensionRestore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(ExtensionsSolutionFile)
                .AddSources(CoreInterfacesPackageOutputDirectory));
        });

    Target ExtensionsCompile => _ => _
        .DependsOn(ExtensionRestore)
        .Executes(() =>
        {
            DotNetBuild(s =>
                s.SetProjectFile(ExtensionsSolutionFile));
        });

    Target ExtensionsPackage => _ => _
        .DependsOn(ExtensionsCompile)
        .Executes(() =>
        {
            DotNetPack(s =>
                s.SetProject(ExtensionsPath)
                    .SetNoBuild(true)
                    .SetOutputDirectory(ExtensionsPackageOutputDirectory)
                    .SetAuthors("Aidan Morgan")
                    .SetRepositoryUrl("https://github.com/aidanmorgan/influx-migrations")
            );
        });

    Target Extensions => _ => _
        .DependsOn(ExtensionRestore)
        .DependsOn(ExtensionsCompile)
        .DependsOn(ExtensionsPackage);

    Target All => _ => _
        .DependsOn(Core)
        .Triggers(Extensions);
}
