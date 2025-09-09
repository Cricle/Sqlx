// -----------------------------------------------------------------------
// <copyright file="PerformanceProfiler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using SqlxPerformanceAnalyzer.Core;
using SqlxPerformanceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SqlxPerformanceAnalyzer.Commands;

/// <summary>
/// Profiles SQL query performance in real-time.
/// </summary>
public class PerformanceProfiler
{
    private readonly ILogger<PerformanceProfiler> _logger;
    private readonly MetricsCollector _metricsCollector;
    private readonly List<QueryExecution> _executions = new();
    private readonly object _lockObject = new();

    public PerformanceProfiler(ILogger<PerformanceProfiler> logger, MetricsCollector metricsCollector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    }

    /// <summary>
    /// Profiles SQL query performance for the specified duration.
    /// </summary>
    public async Task ProfileAsync(string connectionString, int durationSeconds, string? outputFile, int samplingIntervalMs, string? queryFilter)
    {
        _logger.LogInformation("🔬 Starting performance profiling");
        _logger.LogInformation("⏱️ Duration: {Duration} seconds", durationSeconds);
        _logger.LogInformation("📊 Sampling interval: {Interval}ms", samplingIntervalMs);

        if (!string.IsNullOrEmpty(queryFilter))
        {
            _logger.LogInformation("🔍 Query filter: {Filter}", queryFilter);
        }

        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
        var filterRegex = !string.IsNullOrEmpty(queryFilter) ? new Regex(queryFilter, RegexOptions.IgnoreCase) : null;

        try
        {
            // Start metrics collection
            var metricsTask = _metricsCollector.StartCollectionAsync(samplingIntervalMs, cancellationTokenSource.Token);

            // Start query interception (simulated for demo)
            var profilingTask = SimulateQueryProfilingAsync(connectionString, filterRegex, cancellationTokenSource.Token);

            // Wait for completion
            await Task.WhenAll(metricsTask, profilingTask);

            _logger.LogInformation("✅ Profiling completed");
            _logger.LogInformation("📊 Collected {Count} query executions", _executions.Count);

            // Save results
            if (!string.IsNullOrEmpty(outputFile))
            {
                await SaveResultsAsync(outputFile);
            }
            else
            {
                PrintProfilingSummary();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏹️ Profiling stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Profiling failed");
            throw;
        }
    }

    private async Task SimulateQueryProfilingAsync(string connectionString, Regex? filterRegex, CancellationToken cancellationToken)
    {
        // In a real implementation, this would hook into the database driver
        // For now, we'll simulate query executions for demonstration
        
        var random = new Random();
        var sampleQueries = GetSampleQueries();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Simulate a query execution
                var query = sampleQueries[random.Next(sampleQueries.Length)];
                
                if (filterRegex != null && !filterRegex.IsMatch(query))
                {
                    continue;
                }

                var execution = await SimulateQueryExecutionAsync(query, connectionString);
                
                lock (_lockObject)
                {
                    _executions.Add(execution);
                }

                _logger.LogDebug("📝 Captured query: {Duration}ms - {Query}", 
                    execution.ExecutionTimeMs, TruncateQuery(execution.SqlText));

                // Random delay to simulate realistic query patterns
                await Task.Delay(random.Next(50, 500), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during query simulation");
            }
        }
    }

    private async Task<QueryExecution> SimulateQueryExecutionAsync(string sqlQuery, string connectionString)
    {
        var stopwatch = Stopwatch.StartNew();
        var random = new Random();

        // Simulate query execution time based on query complexity
        var baseTime = EstimateQueryComplexity(sqlQuery);
        var jitter = random.Next(-20, 50); // Add realistic variability
        var executionTime = Math.Max(1, baseTime + jitter);

        await Task.Delay(Math.Min(executionTime, 100)); // Cap simulation delay
        stopwatch.Stop();

        var execution = new QueryExecution
        {
            QueryId = Guid.NewGuid().ToString(),
            SqlText = sqlQuery,
            SqlHash = ComputeSqlHash(sqlQuery),
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = executionTime,
            RowsAffected = random.Next(0, 1000),
            BytesReturned = random.Next(1024, 1024 * 100),
            DatabaseName = ExtractDatabaseName(connectionString),
            Status = random.NextDouble() > 0.95 ? ExecutionStatus.Failed : ExecutionStatus.Success,
            ErrorMessage = random.NextDouble() > 0.95 ? "Simulated timeout error" : null,
            CpuTimeMs = executionTime * random.NextDouble(),
            IoTimeMs = executionTime * random.NextDouble() * 0.3,
            LockTimeMs = random.NextDouble() > 0.9 ? random.Next(1, 10) : 0,
            MemoryUsedBytes = random.Next(1024, 1024 * 50),
            ConnectionInfo = connectionString.Split(';').FirstOrDefault() ?? "Unknown",
            StackTrace = "SqlxPerformanceProfiler.SimulateQuery"
        };

        return execution;
    }

    private int EstimateQueryComplexity(string sqlQuery)
    {
        var complexity = 10; // Base time
        var upperQuery = sqlQuery.ToUpperInvariant();

        // Add time based on query features
        if (upperQuery.Contains("JOIN")) complexity += 20;
        if (upperQuery.Contains("ORDER BY")) complexity += 15;
        if (upperQuery.Contains("GROUP BY")) complexity += 25;
        if (upperQuery.Contains("HAVING")) complexity += 10;
        if (Regex.Matches(upperQuery, @"\bSELECT\b").Count > 1) complexity += 30; // Subqueries
        if (upperQuery.Contains("UNION")) complexity += 40;
        if (upperQuery.Contains("DISTINCT")) complexity += 15;

        // Add time based on operations
        if (upperQuery.StartsWith("INSERT")) complexity += 5;
        if (upperQuery.StartsWith("UPDATE")) complexity += 8;
        if (upperQuery.StartsWith("DELETE")) complexity += 6;

        return complexity;
    }

    private string ComputeSqlHash(string sqlQuery)
    {
        // Normalize the query by removing parameters and whitespace variations
        var normalized = Regex.Replace(sqlQuery, @"'[^']*'", "'?'", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\b\d+\b", "?");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        normalized = normalized.Trim().ToUpperInvariant();

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash)[..16]; // First 16 characters
    }

    private string ExtractDatabaseName(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';');
            var dbPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase) ||
                                                 p.Trim().StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase));
            
            if (dbPart != null)
            {
                return dbPart.Split('=')[1].Trim();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return "Unknown";
    }

    private string[] GetSampleQueries()
    {
        return new[]
        {
            "SELECT * FROM Users WHERE IsActive = 1",
            "SELECT u.Id, u.Name, p.Title FROM Users u JOIN Posts p ON u.Id = p.UserId WHERE u.CreatedDate > '2023-01-01'",
            "INSERT INTO Users (Name, Email, CreatedDate) VALUES ('John Doe', 'john@example.com', GETDATE())",
            "UPDATE Users SET LastLoginDate = GETDATE() WHERE Id = 123",
            "DELETE FROM Sessions WHERE ExpiryDate < GETDATE()",
            "SELECT COUNT(*) FROM Orders WHERE OrderDate >= '2023-01-01' AND OrderDate < '2024-01-01'",
            "SELECT p.ProductName, SUM(oi.Quantity) as TotalSold FROM Products p JOIN OrderItems oi ON p.Id = oi.ProductId GROUP BY p.ProductName ORDER BY TotalSold DESC",
            "SELECT * FROM Users WHERE Email LIKE '%@gmail.com' ORDER BY CreatedDate DESC",
            "UPDATE Inventory SET Quantity = Quantity - 1 WHERE ProductId = 456 AND Quantity > 0",
            "SELECT u.Name, COUNT(o.Id) as OrderCount FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId GROUP BY u.Id, u.Name HAVING COUNT(o.Id) > 5"
        };
    }

    private async Task SaveResultsAsync(string outputFile)
    {
        try
        {
            var json = JsonSerializer.Serialize(_executions, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputFile, json);
            _logger.LogInformation("💾 Results saved to: {Path}", outputFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save results to {Path}", outputFile);
            throw;
        }
    }

    private void PrintProfilingSummary()
    {
        if (!_executions.Any())
        {
            Console.WriteLine("No query executions captured during profiling period.");
            return;
        }

        var avgTime = _executions.Average(e => e.ExecutionTimeMs);
        var maxTime = _executions.Max(e => e.ExecutionTimeMs);
        var errorCount = _executions.Count(e => e.Status == ExecutionStatus.Failed);
        var uniqueQueries = _executions.GroupBy(e => e.SqlHash).Count();

        Console.WriteLine("📊 PROFILING SUMMARY");
        Console.WriteLine("===================");
        Console.WriteLine($"Total Executions: {_executions.Count:N0}");
        Console.WriteLine($"Unique Queries: {uniqueQueries:N0}");
        Console.WriteLine($"Average Time: {avgTime:F2}ms");
        Console.WriteLine($"Max Time: {maxTime:F2}ms");
        Console.WriteLine($"Error Count: {errorCount:N0}");
        Console.WriteLine($"Error Rate: {(double)errorCount / _executions.Count * 100:F1}%");
        Console.WriteLine();

        var slowQueries = _executions.Where(e => e.ExecutionTimeMs > avgTime * 2).Take(5);
        if (slowQueries.Any())
        {
            Console.WriteLine("🐌 SLOWEST QUERIES:");
            foreach (var query in slowQueries)
            {
                Console.WriteLine($"  {query.ExecutionTimeMs:F2}ms - {TruncateQuery(query.SqlText)}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("💡 Use 'sqlx-perf analyze' to get detailed performance insights");
    }

    private string TruncateQuery(string query)
    {
        const int maxLength = 60;
        if (query.Length <= maxLength) return query;
        return query.Substring(0, maxLength - 3) + "...";
    }
}

