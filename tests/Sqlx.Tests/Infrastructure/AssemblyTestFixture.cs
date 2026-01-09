// -----------------------------------------------------------------------
// <copyright file="AssemblyTestFixture.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;

namespace Sqlx.Tests.Infrastructure;

/// <summary>
/// Assembly级别的测试固件，用于管理所有测试的共享资源
/// 实现单例容器模式：每种数据库只启动一个容器，所有测试共享
/// 每个测试类使用独立的数据库名称来保证隔离
/// </summary>
[TestClass]
public static class AssemblyTestFixture
{
    private static MySqlContainer? _sharedMySqlContainer;
    private static PostgreSqlContainer? _sharedPostgreSqlContainer;
    private static MsSqlContainer? _sharedMsSqlContainer;

    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    private static readonly object _mysqlLock = new object();
    private static readonly object _postgresLock = new object();
    private static readonly object _sqlserverLock = new object();

    /// <summary>
    /// Assembly级别初始化 - 按需启动数据库容器（懒加载）
    /// </summary>
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        lock (_initLock)
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
        }

        context.WriteLine("🚀 Assembly initialized - containers will start on demand");
    }

    /// <summary>
    /// Assembly级别清理 - 停止并清理所有容器
    /// </summary>
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        Console.WriteLine("🧹 Cleaning up shared database containers...");

        // Cleanup E2E test shared connections first
        try
        {
            Sqlx.Tests.E2E.PredefinedInterfacesE2ETestBase.CleanupSharedConnections();
            Console.WriteLine("✅ PredefinedInterfaces E2E test connections cleaned up");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error cleaning up PredefinedInterfaces E2E connections: {ex.Message}");
        }

        try
        {
            Sqlx.Tests.E2E.E2ETestBase.CleanupSharedConnections();
            Console.WriteLine("✅ E2E_FullCoverage test connections cleaned up");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error cleaning up E2E_FullCoverage connections: {ex.Message}");
        }

        var tasks = new System.Collections.Generic.List<Task>();

        if (_sharedMySqlContainer != null)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _sharedMySqlContainer.StopAsync();
                    await _sharedMySqlContainer.DisposeAsync();
                    Console.WriteLine("✅ MySQL container stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error stopping MySQL container: {ex.Message}");
                }
            }));
        }

        if (_sharedPostgreSqlContainer != null)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _sharedPostgreSqlContainer.StopAsync();
                    await _sharedPostgreSqlContainer.DisposeAsync();
                    Console.WriteLine("✅ PostgreSQL container stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error stopping PostgreSQL container: {ex.Message}");
                }
            }));
        }

        if (_sharedMsSqlContainer != null)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _sharedMsSqlContainer.StopAsync();
                    await _sharedMsSqlContainer.DisposeAsync();
                    Console.WriteLine("✅ SQL Server container stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error stopping SQL Server container: {ex.Message}");
                }
            }));
        }

        if (tasks.Count > 0)
        {
            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }
        
        Console.WriteLine("✅ All containers cleaned up");
    }

    // ==================== 容器访问器（懒加载）====================

    public static MySqlContainer? MySqlContainer
    {
        get
        {
            if (_sharedMySqlContainer == null)
            {
                lock (_mysqlLock)
                {
                    if (_sharedMySqlContainer == null)
                    {
                        try
                        {
                            Console.WriteLine("🐬 Starting MySQL container on demand...");
                            var container = new MySqlBuilder()
                                .WithImage("mysql:8.3")
                                .WithUsername("root")
                                .WithPassword("test_password_123")
                                .WithPortBinding(3306, true)
                                .Build();

                            container.StartAsync().GetAwaiter().GetResult();
                            _sharedMySqlContainer = container;
                            Console.WriteLine($"✅ MySQL container started at {container.Hostname}:{container.GetMappedPublicPort(3306)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Failed to start MySQL container: {ex.Message}");
                            return null;
                        }
                    }
                }
            }
            return _sharedMySqlContainer;
        }
    }

    public static PostgreSqlContainer? PostgreSqlContainer
    {
        get
        {
            if (_sharedPostgreSqlContainer == null)
            {
                lock (_postgresLock)
                {
                    if (_sharedPostgreSqlContainer == null)
                    {
                        try
                        {
                            Console.WriteLine("🐘 Starting PostgreSQL container on demand...");
                            var container = new PostgreSqlBuilder()
                                .WithImage("postgres:16")
                                .WithUsername("postgres")
                                .WithPassword("test_password_123")
                                .WithPortBinding(5432, true)
                                .Build();

                            container.StartAsync().GetAwaiter().GetResult();
                            _sharedPostgreSqlContainer = container;
                            Console.WriteLine($"✅ PostgreSQL container started at {container.Hostname}:{container.GetMappedPublicPort(5432)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Failed to start PostgreSQL container: {ex.Message}");
                            return null;
                        }
                    }
                }
            }
            return _sharedPostgreSqlContainer;
        }
    }

    public static MsSqlContainer? MsSqlContainer
    {
        get
        {
            if (_sharedMsSqlContainer == null)
            {
                lock (_sqlserverLock)
                {
                    if (_sharedMsSqlContainer == null)
                    {
                        try
                        {
                            Console.WriteLine("🗄️ Starting SQL Server container on demand...");
                            var container = new MsSqlBuilder()
                                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                                .WithPassword("YourStrong@Passw0rd123")
                                .WithPortBinding(1433, true)
                                .Build();

                            container.StartAsync().GetAwaiter().GetResult();
                            _sharedMsSqlContainer = container;
                            Console.WriteLine($"✅ SQL Server container started at {container.Hostname}:{container.GetMappedPublicPort(1433)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Failed to start SQL Server container: {ex.Message}");
                            return null;
                        }
                    }
                }
            }
            return _sharedMsSqlContainer;
        }
    }
}
