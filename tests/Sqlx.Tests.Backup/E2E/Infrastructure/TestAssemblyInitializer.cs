// <copyright file="TestAssemblyInitializer.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Assembly-level test initializer that starts Docker containers once for all tests.
/// This significantly improves test performance by avoiding repeated container startup/shutdown.
/// </summary>
[TestClass]
public class TestAssemblyInitializer
{
    private static IContainerManager? _containerManager;

    /// <summary>
    /// Initializes the test infrastructure before any tests in the assembly run.
    /// Starts all Docker containers once and keeps them running for all tests.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext context)
    {
        context.WriteLine("=== Assembly Initialize: Starting Docker containers ===");
        
        _containerManager = ContainerManager.Instance;

        // Check if Docker is available
        var dockerAvailable = await _containerManager.IsDockerAvailableAsync();
        if (!dockerAvailable)
        {
            context.WriteLine("WARNING: Docker is not available. Only SQLite E2E tests will run.");
            context.WriteLine("To run tests for MySQL, PostgreSQL, and SQL Server, ensure Docker Desktop is running.");
            return;
        }

        context.WriteLine("Docker is available. Pre-starting all database containers...");

        try
        {
            // Pre-start all containers in parallel for maximum speed
            var startTasks = new[]
            {
                Task.Run(async () =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    await _containerManager.GetConnectionStringAsync(DatabaseType.MySQL);
                    context.WriteLine($"MySQL container started in {sw.ElapsedMilliseconds}ms");
                }),
                Task.Run(async () =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    await _containerManager.GetConnectionStringAsync(DatabaseType.PostgreSQL);
                    context.WriteLine($"PostgreSQL container started in {sw.ElapsedMilliseconds}ms");
                }),
                Task.Run(async () =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    await _containerManager.GetConnectionStringAsync(DatabaseType.SqlServer);
                    context.WriteLine($"SQL Server container started in {sw.ElapsedMilliseconds}ms");
                })
            };

            await Task.WhenAll(startTasks);
            context.WriteLine("All database containers are ready!");
        }
        catch (Exception ex)
        {
            context.WriteLine($"ERROR: Failed to start containers: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Cleans up the test infrastructure after all tests in the assembly complete.
    /// Stops and removes all Docker containers.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        if (_containerManager != null && _containerManager.IsInitialized)
        {
            await _containerManager.DisposeContainersAsync();
        }
    }
}
