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
/// 数据库连接辅助类
/// CI环境：使用GitHub Actions提供的数据库服务（通过环境变量配置）
/// 本地环境：使用Testcontainers自动管理数据库容器，每个测试类使用独立的容器实例
/// </summary>
public static class DatabaseConnectionHelper
{
    // 使用 ConcurrentDictionary 跟踪测试类和对应的容器
    private static readonly ConcurrentDictionary<string, IAsyncDisposable> _containerMap = new();
    
    /// <summary>
    /// 判断当前是否在CI环境
    /// </summary>
    private static bool IsCI => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
                                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

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
    /// 获取PostgreSQL数据库连接
    /// CI环境：使用GitHub Actions提供的数据库服务
    /// 本地环境：使用Testcontainers，每个测试类使用独立的容器实例
    /// </summary>
    public static DbConnection? GetPostgreSQLConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            // CI环境：使用环境变量中的连接字符串
            if (IsCI)
            {
                var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine($"⚠️ [{testClassName}] POSTGRESQL_CONNECTION environment variable not set in CI");
                    return null;
                }
                
                Console.WriteLine($"🔗 [{testClassName}] Using CI PostgreSQL service");
                var ciConnection = new Npgsql.NpgsqlConnection(connectionString);
                ciConnection.Open();
                return ciConnection;
            }
            
            // 本地环境：使用Testcontainers
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
            var localConnection = new Npgsql.NpgsqlConnection(existingContainer.GetConnectionString());
            localConnection.Open();
            
            return localConnection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to connect to PostgreSQL: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// 获取MySQL数据库连接
    /// CI环境：使用GitHub Actions提供的数据库服务
    /// 本地环境：使用Testcontainers，每个测试类使用独立的容器实例
    /// </summary>
    public static DbConnection? GetMySQLConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            // CI环境：使用环境变量中的连接字符串
            if (IsCI)
            {
                var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine($"⚠️ [{testClassName}] MYSQL_CONNECTION environment variable not set in CI");
                    return null;
                }
                
                Console.WriteLine($"🔗 [{testClassName}] Using CI MySQL service");
                var ciConnection = new MySqlConnector.MySqlConnection(connectionString);
                ciConnection.Open();
                return ciConnection;
            }
            
            // 本地环境：使用Testcontainers
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
            var localConnection = new MySqlConnector.MySqlConnection(existingContainer.GetConnectionString());
            localConnection.Open();
            
            return localConnection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to connect to MySQL: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// 获取SQL Server数据库连接
    /// CI环境：使用GitHub Actions提供的数据库服务
    /// 本地环境：使用Testcontainers，每个测试类使用独立的容器实例
    /// </summary>
    public static DbConnection? GetSqlServerConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            // CI环境：使用环境变量中的连接字符串
            if (IsCI)
            {
                var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine($"⚠️ [{testClassName}] SQLSERVER_CONNECTION environment variable not set in CI");
                    return null;
                }
                
                Console.WriteLine($"🔗 [{testClassName}] Using CI SQL Server service");
                var ciConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                ciConnection.Open();
                return ciConnection;
            }
            
            // 本地环境：使用Testcontainers
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
            var localConnection = new Microsoft.Data.SqlClient.SqlConnection(existingContainer.GetConnectionString());
            localConnection.Open();
            
            return localConnection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to connect to SQL Server: {ex.GetType().Name}: {ex.Message}");
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

