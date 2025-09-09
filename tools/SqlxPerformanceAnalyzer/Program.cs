// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlxPerformanceAnalyzer.Commands;
using SqlxPerformanceAnalyzer.Core;
using SqlxPerformanceAnalyzer.Models;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace SqlxPerformanceAnalyzer;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🔬 Sqlx Performance Analyzer v1.0.0");
        Console.WriteLine("====================================");
        Console.WriteLine("Monitor and optimize your Sqlx application performance");
        Console.WriteLine();

        var rootCommand = new RootCommand("Sqlx Performance Analyzer - Monitor SQL query performance")
        {
            CreateProfileCommand(),
            CreateAnalyzeCommand(),
            CreateMonitorCommand(),
            CreateReportCommand(),
            CreateBenchmarkCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateProfileCommand()
    {
        var connectionOption = new Option<string>("--connection", "Database connection string") { IsRequired = true };
        var durationOption = new Option<int>("--duration", () => 30, "Profiling duration in seconds");
        var outputOption = new Option<string>("--output", "Output file for profiling data");
        var samplingOption = new Option<int>("--sampling", () => 100, "Sampling interval in milliseconds");
        var queryFilterOption = new Option<string>("--filter", "Filter queries by pattern");

        var command = new Command("profile", "Profile SQL query performance in real-time")
        {
            connectionOption,
            durationOption,
            outputOption,
            samplingOption,
            queryFilterOption
        };

        command.SetHandler(async (connection, duration, output, sampling, filter) =>
        {
            using var host = CreateHost();
            var profiler = host.Services.GetRequiredService<PerformanceProfiler>();
            await profiler.ProfileAsync(connection, duration, output, sampling, filter);
        }, connectionOption, durationOption, outputOption, samplingOption, queryFilterOption);

        return command;
    }

    private static Command CreateAnalyzeCommand()
    {
        var inputOption = new Option<string>("--input", "Input profiling data file") { IsRequired = true };
        var outputOption = new Option<string>("--output", "Output analysis report file");
        var formatOption = new Option<ReportFormat>("--format", () => ReportFormat.Console, "Report format");
        var thresholdOption = new Option<double>("--threshold", () => 1000.0, "Slow query threshold (ms)");

        var command = new Command("analyze", "Analyze profiling data and generate performance insights")
        {
            inputOption,
            outputOption,
            formatOption,
            thresholdOption
        };

        command.SetHandler(async (input, output, format, threshold) =>
        {
            using var host = CreateHost();
            var analyzer = host.Services.GetRequiredService<PerformanceAnalyzer>();
            await analyzer.AnalyzeAsync(input, output, format, threshold);
        }, inputOption, outputOption, formatOption, thresholdOption);

        return command;
    }

    private static Command CreateMonitorCommand()
    {
        var connectionOption = new Option<string>("--connection", "Database connection string") { IsRequired = true };
        var intervalOption = new Option<int>("--interval", () => 5, "Monitoring interval in seconds");
        var alertThresholdOption = new Option<double>("--alert-threshold", () => 2000.0, "Alert threshold (ms)");
        var outputOption = new Option<string>("--output", "Output directory for monitoring logs");

        var command = new Command("monitor", "Continuous monitoring of SQL performance")
        {
            connectionOption,
            intervalOption,
            alertThresholdOption,
            outputOption
        };

        command.SetHandler(async (connection, interval, alertThreshold, output) =>
        {
            using var host = CreateHost();
            var monitor = host.Services.GetRequiredService<PerformanceMonitor>();
            await monitor.MonitorAsync(connection, interval, alertThreshold, output);
        }, connectionOption, intervalOption, alertThresholdOption, outputOption);

        return command;
    }

    private static Command CreateReportCommand()
    {
        var inputOption = new Option<string>("--input", "Input data directory") { IsRequired = true };
        var outputOption = new Option<string>("--output", "Output report file");
        var formatOption = new Option<ReportFormat>("--format", () => ReportFormat.Html, "Report format");
        var periodOption = new Option<ReportPeriod>("--period", () => ReportPeriod.LastDay, "Report period");

        var command = new Command("report", "Generate comprehensive performance reports")
        {
            inputOption,
            outputOption,
            formatOption,
            periodOption
        };

        command.SetHandler(async (input, output, format, period) =>
        {
            using var host = CreateHost();
            var reporter = host.Services.GetRequiredService<PerformanceReporter>();
            await reporter.GenerateReportAsync(input, output, format, period);
        }, inputOption, outputOption, formatOption, periodOption);

        return command;
    }

    private static Command CreateBenchmarkCommand()
    {
        var connectionOption = new Option<string>("--connection", "Database connection string") { IsRequired = true };
        var queryOption = new Option<string>("--query", "SQL query to benchmark") { IsRequired = true };
        var iterationsOption = new Option<int>("--iterations", () => 1000, "Number of iterations");
        var concurrencyOption = new Option<int>("--concurrency", () => 1, "Concurrent connections");
        var outputOption = new Option<string>("--output", "Output benchmark report file");

        var command = new Command("benchmark", "Benchmark specific SQL queries")
        {
            connectionOption,
            queryOption,
            iterationsOption,
            concurrencyOption,
            outputOption
        };

        command.SetHandler(async (connection, query, iterations, concurrency, output) =>
        {
            using var host = CreateHost();
            var benchmarker = host.Services.GetRequiredService<PerformanceBenchmarker>();
            await benchmarker.BenchmarkAsync(connection, query, iterations, concurrency, output);
        }, connectionOption, queryOption, iterationsOption, concurrencyOption, outputOption);

        return command;
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                services.AddSingleton<PerformanceProfiler>();
                services.AddSingleton<PerformanceAnalyzer>();
                services.AddSingleton<PerformanceMonitor>();
                services.AddSingleton<PerformanceReporter>();
                services.AddSingleton<PerformanceBenchmarker>();
                services.AddSingleton<QueryOptimizer>();
                services.AddSingleton<MetricsCollector>();
            })
            .Build();
    }
}

public enum ReportFormat
{
    Console,
    Json,
    Html,
    Csv,
    Excel
}

public enum ReportPeriod
{
    LastHour,
    LastDay,
    LastWeek,
    LastMonth,
    All
}

