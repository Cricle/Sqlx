// -----------------------------------------------------------------------
// <copyright file="DatabaseConnectionHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;

namespace Sqlx.Tests.Infrastructure;

/// <summary>
/// 数据库连接辅助类，使用 Testcontainers 自动管理数据库容器
/// 每个测试类使用独立的容器实例，测试方法之间通过清理数据来隔离
/// </summary>
public static class DatabaseConnectionHelper
{
    // 使用 ConcurrentDictionary 跟踪测试类和对应的容器
    private static readonly ConcurrentDictionary<string, IAsyncDisposable> _containerMap = new();

    /// <summary>
    /// 清理指定测试类的容器
    /// </summary>
    public static async Task CleanupContainerAsync(string testClassName)
    {
        if (string.IsNullOrEmpty(testClassName)) return;

        try
        {
            // 如果有关联的容器，停止并删除它
            if (_containerMap.TryRemove(testClassName, out var container))
            {
                await container.DisposeAsync();
                Console.WriteLine($"🗑️ [{testClassName}] Container stopped and removed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to cleanup container: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取SQLite内存数据库连接
    /// </summary>
    public static DbConnection GetSQLiteConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// 获取PostgreSQL数据库连接（使用 Testcontainers）
    /// 每个测试类使用独立的容器实例，测试方法之间通过清理数据来隔离
    /// </summary>
    public static DbConnection? GetPostgreSQLConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            // 检查是否已经为这个测试类创建了容器
            if (!_containerMap.ContainsKey(testClassName))
            {
                Console.WriteLine($"🐳 [{testClassName}] Starting new PostgreSQL container...");
                var container = new PostgreSqlBuilder()
                    .WithImage("postgres:16")
                    .WithDatabase("sqlx_test")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();
                
                container.StartAsync().GetAwaiter().GetResult();
                Console.WriteLine($"✅ [{testClassName}] PostgreSQL container started successfully");
                
                // 记录测试类和容器的关联
                _containerMap[testClassName] = container;
            }

            var existingContainer = (PostgreSqlContainer)_containerMap[testClassName];
            var connection = new Npgsql.NpgsqlConnection(existingContainer.GetConnectionString());
            connection.Open();
            
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to start PostgreSQL container: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// 获取MySQL数据库连接（使用 Testcontainers）
    /// 每个测试类使用独立的容器实例，测试方法之间通过清理数据来隔离
    /// </summary>
    public static DbConnection? GetMySQLConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            // 检查是否已经为这个测试类创建了容器
            if (!_containerMap.ContainsKey(testClassName))
            {
                Console.WriteLine($"🐳 [{testClassName}] Starting new MySQL container...");
                var container = new MySqlBuilder()
                    .WithImage("mysql:8.3")
                    .WithDatabase("sqlx_test")
                    .WithUsername("root")
                    .WithPassword("root")
                    .Build();
                
                container.StartAsync().GetAwaiter().GetResult();
                Console.WriteLine($"✅ [{testClassName}] MySQL container started successfully");
                
                // 记录测试类和容器的关联
                _containerMap[testClassName] = container;
            }

            var existingContainer = (MySqlContainer)_containerMap[testClassName];
            var connection = new MySqlConnector.MySqlConnection(existingContainer.GetConnectionString());
            connection.Open();
            
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to start MySQL container: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// 获取SQL Server数据库连接（使用 Testcontainers）
    /// 每个测试类使用独立的容器实例，测试方法之间通过清理数据来隔离
    /// </summary>
    public static DbConnection? GetSqlServerConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            // 检查是否已经为这个测试类创建了容器
            if (!_containerMap.ContainsKey(testClassName))
            {
                Console.WriteLine($"🐳 [{testClassName}] Starting new SQL Server container...");
                var container = new MsSqlBuilder()
                    .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                    .WithPassword("YourStrong@Passw0rd")
                    .Build();
                
                container.StartAsync().GetAwaiter().GetResult();
                Console.WriteLine($"✅ [{testClassName}] SQL Server container started successfully");
                
                // 记录测试类和容器的关联
                _containerMap[testClassName] = container;
            }

            var existingContainer = (MsSqlContainer)_containerMap[testClassName];
            var connection = new Microsoft.Data.SqlClient.SqlConnection(existingContainer.GetConnectionString());
            connection.Open();
            
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to start SQL Server container: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// 获取Oracle数据库连接（暂未实现）
    /// </summary>
    public static DbConnection? GetOracleConnection(TestContext? testContext = null)
    {
        // Oracle 支持暂未实现
        Console.WriteLine("⚠️ Oracle database is not yet supported");
        return null;
    }
}

