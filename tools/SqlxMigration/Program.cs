// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using SqlxMigration.Commands;
using SqlxMigration.Core;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace SqlxMigration;

/// <summary>
/// Main program entry point for Sqlx Migration Tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🔄 Sqlx Migration Tool v1.0.0");
        Console.WriteLine("===============================");
        Console.WriteLine("Convert Dapper and Entity Framework Core code to Sqlx");
        Console.WriteLine();

        var rootCommand = new RootCommand("Sqlx Migration Tool - Convert Dapper/EF Core to Sqlx")
        {
            CreateAnalyzeCommand(),
            CreateMigrateCommand(),
            CreateGenerateCommand(),
            CreateValidateCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateAnalyzeCommand()
    {
        var pathArgument = new Argument<string>("path", "Path to the project or solution file");
        var outputOption = new Option<string>("--output", "Output file for analysis report") { IsRequired = false };
        var formatOption = new Option<AnalysisFormat>("--format", () => AnalysisFormat.Console, "Output format");

        var command = new Command("analyze", "Analyze existing code for migration opportunities")
        {
            pathArgument,
            outputOption,
            formatOption
        };

        command.SetHandler(async (path, output, format) =>
        {
            var logger = CreateLogger();
            var analyzer = new CodeAnalyzer(logger);
            await analyzer.AnalyzeAsync(path, output, format);
        }, pathArgument, outputOption, formatOption);

        return command;
    }

    private static Command CreateMigrateCommand()
    {
        var pathArgument = new Argument<string>("path", "Path to the project or solution file");
        var sourceOption = new Option<MigrationSource>("--source", () => MigrationSource.Auto, "Source framework");
        var targetOption = new Option<string>("--target", "Target directory for migrated code") { IsRequired = false };
        var dryRunOption = new Option<bool>("--dry-run", "Perform a dry run without making changes");
        var backupOption = new Option<bool>("--backup", () => true, "Create backup of original files");

        var command = new Command("migrate", "Migrate Dapper/EF Core code to Sqlx")
        {
            pathArgument,
            sourceOption,
            targetOption,
            dryRunOption,
            backupOption
        };

        command.SetHandler(async (path, source, target, dryRun, backup) =>
        {
            var logger = CreateLogger();
            var migrator = new CodeMigrator(logger);
            await migrator.MigrateAsync(path, source, target, dryRun, backup);
        }, pathArgument, sourceOption, targetOption, dryRunOption, backupOption);

        return command;
    }

    private static Command CreateGenerateCommand()
    {
        var projectArgument = new Argument<string>("project", "Path to the target project");
        var entityOption = new Option<string>("--entity", "Entity class name") { IsRequired = true };
        var tableOption = new Option<string>("--table", "Database table name") { IsRequired = false };
        var dialectOption = new Option<string>("--dialect", () => "SqlServer", "Database dialect");

        var command = new Command("generate", "Generate Sqlx repository from entity")
        {
            projectArgument,
            entityOption,
            tableOption,
            dialectOption
        };

        command.SetHandler(async (project, entity, table, dialect) =>
        {
            var logger = CreateLogger();
            var generator = new RepositoryGenerator(logger);
            await generator.GenerateAsync(project, entity, table ?? entity, dialect);
        }, projectArgument, entityOption, tableOption, dialectOption);

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var pathArgument = new Argument<string>("path", "Path to the project or solution file");
        var strictOption = new Option<bool>("--strict", "Enable strict validation mode");

        var command = new Command("validate", "Validate migrated Sqlx code")
        {
            pathArgument,
            strictOption
        };

        command.SetHandler(async (path, strict) =>
        {
            var logger = CreateLogger();
            var validator = new CodeValidator(logger);
            await validator.ValidateAsync(path, strict);
        }, pathArgument, strictOption);

        return command;
    }

    private static ILogger CreateLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        return loggerFactory.CreateLogger<Program>();
    }
}

/// <summary>
/// Analysis output format.
/// </summary>
public enum AnalysisFormat
{
    Console,
    Json,
    Xml,
    Html
}

/// <summary>
/// Migration source framework.
/// </summary>
public enum MigrationSource
{
    Auto,
    Dapper,
    EntityFramework,
    Both
}
