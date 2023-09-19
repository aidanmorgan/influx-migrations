// See https://aka.ms/new-console-template for more information

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
        
        [Option('b', "bucket", Required = false, HelpText = "Influx Bucket for saving migration history.", Default = "migrations_history")]
        public string InfluxBucket { get; set; }
        
        [Option('m', "measurement", Required = false, HelpText = "Influx Measurement name for  migration history.", Default = "migrations")]
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
        var result = await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync<Options>(async o =>
            {
                Console.WriteLine($"Starting Migrations.".Pastel(ConsoleColor.Cyan));
                Console.WriteLine($"Migration Directory: {o.MigrationsDirectory}");

                var target = string.IsNullOrEmpty(o.TargetVersion) ? "LATEST" : o.TargetVersion;
                Console.WriteLine($"Target Version: {target}");

                var influx = new InfluxFactory(o.InfluxHost, o.InfluxAdminToken);

                var migrationRunner = new DefaultMigrationRunnerService(new DefaultMigrationRunnerOptions()
                {
                    Logger = new TextWriterRunnerLogger(Console.Out),
                    Loader = new DefaultMigrationLoaderService(o.MigrationsDirectory, new DefaultMigrationLoaderOptions()
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

                var results = await migrationRunner.ExecuteMigrationsAsync(influx, o.TargetVersion);
                var unpacked = results.Select(x => new Tuple<MigrationResult, List<MigrationIssue>>(x, x.Issues)).ToList();

                if (unpacked.Any(x => !x.Item1.Success || x.Item1.Inconsistent))
                {
                    Console.WriteLine($"Migrations [{string.Join(",", results.Select(x => x.Version))}] FAILED".Pastel(ConsoleColor.Red));

                    foreach (var issue in unpacked)
                    {
                        if (!issue.Item1.Success || issue.Item1.Inconsistent)
                        {
                            Console.Write($"{issue.Item1.Version} ".Pastel(ConsoleColor.Red));
                            if (issue.Item1.Inconsistent)
                            {
                                Console.Write($" INFLUX INCONSISTENT".Pastel(ConsoleColor.Magenta));
                            }
                            
                            Console.WriteLine();

                            foreach (var mi in issue.Item2)
                            {
                                Console.WriteLine($"{mi.Category} - {mi.Phase} - {mi.Severity} - {mi.Id}".Pastel(ConsoleColor.Red));
                                if (mi.Exception != null)
                                {
                                    Console.WriteLine(mi.Exception.Message.Pastel(ConsoleColor.DarkRed));
                                    Console.WriteLine(mi.Exception.StackTrace.Pastel(ConsoleColor.DarkRed));
                                }
                            }
                        }
                    }
                }
            });

        return -1;
    }
}