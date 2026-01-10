// -----------------------------------------------------------------------
// <copyright file="AssemblyTestFixture.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;
using Xunit;

namespace Sqlx.Tests.Infrastructure;

/// <summary>
/// Assembly-level test fixture for managing shared database containers
/// Containers are started once per test assembly and shared across all tests
/// </summary>
public class AssemblyTestFixture : IAsyncLifetime
{
    /// <summary>
    /// Shared MySQL container instance
    /// </summary>
    public static MySqlContainer? MySqlContainer { get; private set; }

    /// <summary>
    /// Shared PostgreSQL container instance
    /// </summary>
    public static PostgreSqlContainer? PostgreSqlContainer { get; private set; }

    /// <summary>
    /// Shared SQL Server container instance
    /// </summary>
    public static MsSqlContainer? MsSqlContainer { get; private set; }

    /// <summary>
    /// Initialize containers before any tests run
    /// </summary>
    public async Task InitializeAsync()
    {
        Console.WriteLine("🚀 Starting database containers...");

        try
        {
            // Start MySQL container
            MySqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.0")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            // Start PostgreSQL container
            PostgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            // Start SQL Server container
            MsSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong@Passw0rd")
                .Build();

            // Start all containers in parallel
            await Task.WhenAll(
                MySqlContainer.StartAsync(),
                PostgreSqlContainer.StartAsync(),
                MsSqlContainer.StartAsync()
            );

            Console.WriteLine("✅ All database containers started successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to start database containers: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Cleanup containers after all tests complete
    /// </summary>
    public async Task DisposeAsync()
    {
        Console.WriteLine("🧹 Stopping database containers...");

        try
        {
            var disposeTasks = new System.Collections.Generic.List<Task>();

            if (MySqlContainer != null)
            {
                disposeTasks.Add(MySqlContainer.DisposeAsync().AsTask());
            }

            if (PostgreSqlContainer != null)
            {
                disposeTasks.Add(PostgreSqlContainer.DisposeAsync().AsTask());
            }

            if (MsSqlContainer != null)
            {
                disposeTasks.Add(MsSqlContainer.DisposeAsync().AsTask());
            }

            await Task.WhenAll(disposeTasks);

            Console.WriteLine("✅ All database containers stopped successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error stopping database containers: {ex.Message}");
        }
    }
}
