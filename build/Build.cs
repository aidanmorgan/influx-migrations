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
    static readonly AbsolutePath TestsRoot = RootDirectory / "tests";

    static readonly AbsolutePath CoreRootDirectory = RootDirectory / "InfluxMigrations";
    static readonly AbsolutePath ExtensionsRootDirectory = RootDirectory / "Extensions";

    static readonly Solution CoreSolution = SolutionModelTasks.ParseSolution(CoreRootDirectory / "InfluxMigrations.sln");

    static readonly List<AbsolutePath> InterfaceProjects = new List<AbsolutePath>()
    {
        CoreRootDirectory / "src" / "Core" / "InfluxMigrations.Core",
        CoreRootDirectory / "src" / "Core"/"InfluxMigrations.Yaml"
    };

    static readonly AbsolutePath PackageOutputDirectory = RootDirectory / "pack";
    static readonly AbsolutePath InterfacesPackDirectory = PackageOutputDirectory / "abstractions";
    static readonly AbsolutePath CorePackDirectory = PackageOutputDirectory / "core";
    static readonly AbsolutePath ExtensionsPackBaseDirectory = PackageOutputDirectory / "extensions";
    
    
    public static int Main () => Execute<Build>(x => x.All);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    Target Clean => _ => _
        .Before(CoreRestore)
        .Executes(() =>
        {
            CoreRootDirectory.GlobDirectories("**/bin", "**/obj")
                .ForEach(x => x.DeleteDirectory());
        });

    Target CoreRestore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(CoreSolution)
                .SetPackageDirectory(CoreRootDirectory / "packages"));    
        });

    Target CoreCompile => _ => _
        .DependsOn(CoreRestore)
        .Executes(() =>
        {
                DotNetBuild(s => s
                    .SetNoWarns()
                    .SetProjectFile(CoreSolution)
                    .SetConfiguration(Configuration)
                    .SetFileVersion(GitVersion.AssemblySemFileVer)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
                    .SetPackageDirectory(CoreRootDirectory / "packages")
                    .EnableNoRestore());
        });

    Target CoreUnitTests => _ => _
        .DependsOn(CoreCompile)
        .Before(CorePackage)
        .Executes(() =>
        {
            foreach (var testProject in CoreSolution.GetUnitTestProjects())
            {
                DotNetTest(s => s
                    .SetProjectFile(testProject)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetResultsDirectory(TestsRoot / "results" / testProject.Solution.Name)
                );
            }
        });

    Target CoreIntegrationTests => _ => _
        .DependsOn(CoreCompile)
        .Before(CorePackage)
        .Executes(() =>
        {
            foreach (var testProject in CoreSolution.GetIntegrationTestProjects())
            {
                DotNetTest(s => s
                    .SetProjectFile(testProject)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetResultsDirectory(TestsRoot / "results" / testProject.Solution.Name)
                );
            }
        });
    
    Target CorePackage => _ => _
        .DependsOn(CoreCompile)
        .Executes(() =>
        {
            InterfacesPackDirectory.CreateOrCleanDirectory();
            
            foreach (var interfaceProject in InterfaceProjects)
            {
                DotNetPack(s => 
                    s.SetProject(interfaceProject)
                        .SetNoRestore(true)
                        .SetNoBuild(true)
                        .SetVersion(GitVersion.AssemblySemVer)
                        .SetOutputDirectory(InterfacesPackDirectory)
                        .SetAuthors("Aidan Morgan")
                        .SetRepositoryUrl("https://github.com/aidanmorgan/influx-migrations")
                );
            }

            var projectsToIgnoreWhenPacking = new List<AbsolutePath>();
            projectsToIgnoreWhenPacking.AddRange(InterfaceProjects);
            projectsToIgnoreWhenPacking.AddRange(CoreSolution.GetUnitTestProjects().Select(x => x.Path));
            projectsToIgnoreWhenPacking.AddRange(CoreSolution.GetIntegrationTestProjects().Select(x => x.Path));
            
            // skip tests and any interface projects
            var coreProjects = CoreSolution.GetAllProjects("*")
                .Where(x => ! projectsToIgnoreWhenPacking.Contains(x.Path));

            CorePackDirectory.CreateOrCleanDirectory();
            
            foreach (var pkg in coreProjects)
            {
                DotNetPack(s => 
                    s.SetProject(pkg)
                        .SetNoRestore(true)
                        .SetNoBuild(true)
                        .SetVersion(GitVersion.AssemblySemVer)
                        .SetOutputDirectory(CorePackDirectory)
                        .SetAuthors("Aidan Morgan")
                        .SetRepositoryUrl("https://github.com/aidanmorgan/influx-migrations")
                );
                
            }
        });

    Target Core => _ => _
        .DependsOn(CoreRestore)
        .DependsOn(CoreCompile)
        .DependsOn(CoreUnitTests)
        .DependsOn(CorePackage);

    private IEnumerable<Solution> FindExtensionSolutions()
    {
        return ExtensionsRootDirectory.GlobFiles("**/*.sln").Select(SolutionModelTasks.ParseSolution);
    }
    
    Target ExtensionRestore => _ => _
        .Executes(() =>
        {
            foreach (var extensionSolution in FindExtensionSolutions())
            {
                DotNetRestore(s => s
                    .SetProjectFile(extensionSolution)
                    .SetPackageDirectory(extensionSolution.Directory / "packages")
                    .AddSources(InterfacesPackDirectory)
                    .AddSources("https://api.nuget.org/v3/index.json")
                );
            }
        });

    Target ExtensionsCompile => _ => _
        .DependsOn(ExtensionRestore)
        .Executes(() =>
        {
            foreach (var extensionSolution in FindExtensionSolutions())
            {
                DotNetBuild(s =>
                    s.SetProjectFile(extensionSolution)
                        .SetNoRestore(true)
                        .SetPackageDirectory(extensionSolution.Directory / "packages")
                    );
            }
        });

    Target ExtensionsUnitTests => _ => _
        .DependsOn(ExtensionsCompile)
        .Executes(() =>
        {
            var projects = FindExtensionSolutions().SelectMany(x => x.GetUnitTestProjects());
            
            foreach (var project in projects)
            {
                DotNetTest(s => s
                    .SetProjectFile(project)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetResultsDirectory(TestsRoot / "results" / project.Solution.Name));
            }
        });

    Target ExtensionsIntegrationTests => _ => _
        .Executes(() =>
        {
            var projects = FindExtensionSolutions().SelectMany(x => x.GetIntegrationTestProjects());
            
            foreach (var project in projects)
            {
                DotNetTest(s => s
                    .SetProjectFile(project)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetResultsDirectory(TestsRoot / "results" / project.Solution.Name));
            }

        });

    Target ExtensionsPackage => _ => _
        .DependsOn(ExtensionsUnitTests)
        .Executes(() =>
        {
            foreach (var extensionSolution in FindExtensionSolutions())
            {
                DotNetPack(s =>
                    s.SetProject(extensionSolution)
                        .SetNoBuild(true)
                        .SetVersion(GitVersion.AssemblySemVer)
                        .SetOutputDirectory(ExtensionsPackBaseDirectory / extensionSolution.Name)
                        .SetAuthors("Aidan Morgan")
                        .SetRepositoryUrl("https://github.com/aidanmorgan/influx-migrations")
                );
            }
        });

    Target Extensions => _ => _
        .DependsOn(ExtensionRestore)
        .DependsOn(ExtensionsCompile)
        .DependsOn(ExtensionsPackage);

    Target All => _ => _
        .DependsOn(Core)
        .Triggers(Extensions);
}


public static class SolutionExtensions
{
    public static IEnumerable<Project> GetUnitTestProjects(this Solution solution)
    {
        return solution.GetAllProjects("*UnitTests");
    }

    public static IEnumerable<Project> GetIntegrationTestProjects(this Solution solution)
    {
        return solution.GetAllProjects("*IntegrationTests");

    }
}