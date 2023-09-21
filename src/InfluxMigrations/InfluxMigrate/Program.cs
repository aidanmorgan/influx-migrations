﻿// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using CommandLine;
using InfluxDB.Client;
using InfluxMigrations.Core;
using InfluxMigrations.Impl;
using Pastel;

namespace InfluxMigrate;

public class Program
{
    public class Options
    {
        [Option('h', "host", Required = true, HelpText = "Influx host URL")]
        public string InfluxHost { get; set; }

        [Option('b', "bucket", Required = false, HelpText = "Influx Bucket for saving migration history.",
            Default = "migrations_history")]
        public string InfluxBucket { get; set; }

        [Option('m', "measurement", Required = false, HelpText = "Influx Measurement name for  migration history.",
            Default = "migrations")]
        public string InfluxMeasurement { get; set; }

        [Option('o', "org", Required = true, HelpText = "Influx Organisation for saving migration history.")]
        public string InfluxOrganisation { get; set; }

        [Option('t', "token", Required = true, HelpText = "Influx admin token.")]
        public string InfluxAdminToken { get; set; }

        [Option('d', "directory", Required = true, HelpText = "Migrations YAML directory.")]
        public string MigrationsDirectory { get; set; }

        [Option(shortName: 't', longName: "target", Required = false, HelpText = "Specific version to migrate to.")]
        public string? TargetVersion { get; set; } = null;
    }

    public static async Task<int> Main(string[] args)
    {
        var result = Parser.Default.ParseArguments<Options>(args).MapResult(Do, Error);

        return (await result);
    }

    private static Task<int> Error(IEnumerable<Error> arg)
    {
        foreach (var error in arg)
        {
            Console.Error.WriteLine(error);
        }

        return Task.FromResult(-1);
    }

    private static async Task<int> Do(Options o)
    {
        Console.WriteLine($"Starting Migrations.".Pastel(ConsoleColor.Cyan));
        Console.WriteLine($"Migration Directory: {o.MigrationsDirectory}");

        var target = string.IsNullOrEmpty(o.TargetVersion) ? "LATEST" : o.TargetVersion;
        Console.WriteLine($"Target Version: {target}");

        var influx = new InfluxFactory(o.InfluxHost, o.InfluxAdminToken);

        var migrationRunner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
        {
            Logger = new TextWriterRunnerLogger(Console.Out),
            Loader = new DefaultMigrationLoaderService(o.MigrationsDirectory,
                new DefaultMigrationLoaderOptions()
                {
                    Logger = new TextWriterMigrationLoaderLogger(Console.Out)
                }),
            History = new DefaultMigrationHistoryService(influx, new DefaultMigrationHistoryOptions()
            {
                Logger = new TextWriterMigrationHistoryLogger(Console.Out),
                BucketName = o.InfluxBucket,
                OrganisationName = o.InfluxOrganisation,
                MeasurementName = o.InfluxMeasurement
            }),
            MigrationOptions = new MigrationOptions()
            {
                Logger = new TextWriterMigrationLoggerFactory(Console.Out)
            }
        });

        try
        {
            var results = await migrationRunner
                .ExecuteMigrationsAsync(influx, o.TargetVersion);
            var unpacked = results
                .SelectMany(x => x.Issues)
                .ToList();

            if (unpacked.Count > 0)
            {
                if (results.Any(x => x.Inconsistent))
                {
                    Console.WriteLine(
                        $"Migrations [{string.Join(",", results.Select(x => x.Version))}] FAILED INCONSISTENT"
                            .Pastel(ConsoleColor.Magenta));
                }
                else if (!results.Any(x => x.Success))
                {
                    Console.WriteLine($"Migrations [{string.Join(",", results.Select(x => x.Version))}] FAILED"
                        .Pastel(ConsoleColor.Red));

                    foreach (var failed in results.Where(x => !x.Success))
                    {
                        Console.WriteLine($"Migration: {failed.Version}: ".Pastel(ConsoleColor.Red));
                        foreach (var issue in failed.Issues)
                        {
                            Console.WriteLine(
                                $"\tCategory: {issue.Category} : Phase: {issue.Phase} : Severity: {issue.Severity}"
                                    .Pastel(ConsoleColor.Red));
                            if (issue.Exception != null)
                            {
                                var stackTrace = new StackTrace(issue.Exception, true);

                                Console.WriteLine($"\t{issue.Exception.Message}".Pastel(ConsoleColor.DarkRed));
                                Console.WriteLine($"\t{stackTrace}".Pastel(ConsoleColor.DarkRed));
                            }
                        }
                    }

                    return -1;
                }
                else
                {
                    Console.WriteLine($"Migrations [{string.Join(",", results.Select(x => x.Version))}] SUCCESS"
                        .Pastel(ConsoleColor.Green));
                    return 0;
                }
            }
        }
        catch (MigrationRunnerException x)
        {
            Console.WriteLine($"Exception thrown running Migrations.".Pastel(ConsoleColor.Red));
            Console.WriteLine($"{x.Message}".Pastel(ConsoleColor.DarkRed));
            Console.WriteLine($"{new StackTrace(x)}".Pastel(ConsoleColor.DarkRed));

            return -1;
        }

        return 0;
    }
}