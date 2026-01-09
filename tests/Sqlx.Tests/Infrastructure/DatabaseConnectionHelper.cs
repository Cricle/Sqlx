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
using Sqlx.Annotations;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;

namespace Sqlx.Tests.Infrastructure;

/// <summary>
/// 数据库连接辅助类，使用单例容器模式优化测试性能
/// 核心策略：
/// - 每种数据库类型只启动一个共享容器（通过 AssemblyTestFixture 管理）
/// - 每个测试类使用独立的数据库名称，确保测试隔离
/// - 测试方法之间通过清理数据来隔离
/// </summary>
public static class DatabaseConnectionHelper
{
    // 跟踪每个测试类创建的数据库（确保同一测试类使用相同数据库名）
    private static readonly ConcurrentDictionary<string, string> _databaseMap = new();
    
    // 为每个测试类生成唯一的随机后缀（确保不同测试类不会共享数据库）
    private static readonly ConcurrentDictionary<string, string> _testClassSuffix = new();

    /// <summary>
    /// 清理指定测试类的数据库（注意：不再清理容器，容器由 AssemblyTestFixture 统一管理）
    /// </summary>
    public static async Task CleanupDatabaseAsync(string testClassName)
    {
        if (string.IsNullOrEmpty(testClassName)) return;

        try
        {
            if (_databaseMap.TryRemove(testClassName, out var databaseName))
            {
                Console.WriteLine($"🗑️ [{testClassName}] Database '{databaseName}' cleanup completed");
            }
            _testClassSuffix.TryRemove(testClassName, out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to cleanup database: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 清理指定测试类的容器（向后兼容）
    /// 注意：新的架构不再为每个测试类创建容器，此方法保留以支持旧代码
    /// </summary>
    [Obsolete("Use CleanupDatabaseAsync instead. Containers are now managed by AssemblyTestFixture.")]
    public static async Task CleanupContainerAsync(string testClassName)
    {
        await CleanupDatabaseAsync(testClassName);
    }

    /// <summary>
    /// 生成测试类专属的数据库名称（带随机后缀确保完全隔离）
    /// 同一测试类的所有实例使用相同的数据库名（通过缓存实现）
    /// </summary>
    private static string GetDatabaseName(string testClassName)
    {
        // 如果已经为这个测试类生成过数据库名，直接返回
        if (_databaseMap.TryGetValue(testClassName, out var existingDbName))
        {
            return existingDbName;
        }
        
        // 为测试类生成唯一后缀（每个测试类一次性生成，后续复用）
        var uniqueId = _testClassSuffix.GetOrAdd(testClassName, _ => Guid.NewGuid().ToString("N").Substring(0, 8));
        
        // 将测试类名转换为小写并替换特殊字符，生成合法的数据库名
        var baseName = testClassName.ToLowerInvariant().Replace("_", "").Replace(".", "_");
        
        var dbName = $"sqlx_test_{baseName}_{uniqueId}";
        
        // 限制长度（某些数据库有名称长度限制）
        if (dbName.Length > 64)
        {
            // 如果超长，缩短base name但保留uniqueId
            var maxBaseLength = 64 - 18; // "sqlx_test_" (10) + "_" (1) + uniqueId (8) = 19，留1字符余地
            baseName = baseName.Substring(0, Math.Min(baseName.Length, maxBaseLength));
            dbName = $"sqlx_test_{baseName}_{uniqueId}";
        }
        
        // 缓存这个数据库名
        _databaseMap.TryAdd(testClassName, dbName);
            
        return dbName;
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
    /// 获取PostgreSQL数据库连接（使用共享容器 + 独立数据库）
    /// 每个测试类使用独立的数据库名称，确保测试隔离
    /// </summary>
    public static DbConnection? GetPostgreSQLConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            var container = AssemblyTestFixture.PostgreSqlContainer;
            if (container == null)
            {
                Console.WriteLine($"⚠️ [{testClassName}] PostgreSQL container is not available");
                return null;
            }

            var databaseName = GetDatabaseName(testClassName);
            _databaseMap.TryAdd(testClassName, databaseName);

            // 创建到默认数据库的连接以创建测试数据库
            var adminConnectionString = container.GetConnectionString();
            using (var adminConn = new Npgsql.NpgsqlConnection(adminConnectionString))
            {
                adminConn.Open();
                
                // 检查数据库是否存在，不存在则创建
                using (var cmd = adminConn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
                    var exists = cmd.ExecuteScalar() != null;
                    
                    if (!exists)
                    {
                        cmd.CommandText = $"CREATE DATABASE {databaseName}";
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"✅ [{testClassName}] Created PostgreSQL database: {databaseName}");
                    }
                }
            }

            // 构建到测试数据库的连接字符串
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                Database = databaseName
            };
            
            var connection = new Npgsql.NpgsqlConnection(builder.ConnectionString);
            connection.Open();
            
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to connect to PostgreSQL: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// 获取MySQL数据库连接（使用共享容器 + 独立数据库）
    /// 每个测试类使用独立的数据库名称，确保测试隔离
    /// </summary>
    public static DbConnection? GetMySQLConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            var container = AssemblyTestFixture.MySqlContainer;
            if (container == null)
            {
                Console.WriteLine($"⚠️ [{testClassName}] MySQL container is not available");
                return null;
            }

            var databaseName = GetDatabaseName(testClassName);
            _databaseMap.TryAdd(testClassName, databaseName);

            // 创建到默认数据库的连接以创建测试数据库
            var adminConnectionString = container.GetConnectionString();
            using (var adminConn = new MySqlConnector.MySqlConnection(adminConnectionString))
            {
                adminConn.Open();
                
                // 创建数据库（如果不存在）
                using (var cmd = adminConn.CreateCommand())
                {
                    cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`";
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"✅ [{testClassName}] Ensured MySQL database exists: {databaseName}");
                }
            }

            // 构建到测试数据库的连接字符串
            var builder = new MySqlConnector.MySqlConnectionStringBuilder(adminConnectionString)
            {
                Database = databaseName
            };
            
            var connection = new MySqlConnector.MySqlConnection(builder.ConnectionString);
            connection.Open();
            
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to connect to MySQL: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// 获取SQL Server数据库连接（使用共享容器 + 独立数据库）
    /// 每个测试类使用独立的数据库名称，确保测试隔离
    /// </summary>
    public static DbConnection? GetSqlServerConnection(string testClassName, TestContext? testContext = null)
    {
        try
        {
            var container = AssemblyTestFixture.MsSqlContainer;
            if (container == null)
            {
                Console.WriteLine($"⚠️ [{testClassName}] SQL Server container is not available");
                return null;
            }

            var databaseName = GetDatabaseName(testClassName);
            _databaseMap.TryAdd(testClassName, databaseName);

            // 创建到master数据库的连接以创建测试数据库
            var adminConnectionString = container.GetConnectionString();
            using (var adminConn = new Microsoft.Data.SqlClient.SqlConnection(adminConnectionString))
            {
                adminConn.Open();
                
                // 检查数据库是否存在，不存在则创建
                using (var cmd = adminConn.CreateCommand())
                {
                    cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') CREATE DATABASE [{databaseName}]";
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"✅ [{testClassName}] Ensured SQL Server database exists: {databaseName}");
                }
            }

            // 构建到测试数据库的连接字符串
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(adminConnectionString)
            {
                InitialCatalog = databaseName
            };
            
            var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
            connection.Open();
            
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [{testClassName}] Failed to connect to SQL Server: {ex.GetType().Name}: {ex.Message}");
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

    /// <summary>
    /// 根据数据库方言创建连接
    /// </summary>
    /// <param name="dialect">数据库方言类型</param>
    /// <param name="testClassName">测试类名称（用于生成独立数据库）</param>
    /// <param name="testContext">测试上下文（可选）</param>
    /// <returns>数据库连接，如果不支持或失败则返回null</returns>
    public static DbConnection? CreateConnectionForDialect(SqlDefineTypes dialect, string testClassName, TestContext? testContext = null)
    {
        return dialect switch
        {
            SqlDefineTypes.SQLite => GetSQLiteConnection(),
            SqlDefineTypes.PostgreSql => GetPostgreSQLConnection(testClassName, testContext),
            SqlDefineTypes.MySql => GetMySQLConnection(testClassName, testContext),
            SqlDefineTypes.SqlServer => GetSqlServerConnection(testClassName, testContext),
            _ => null
        };
    }

    /// <summary>
    /// 异步根据数据库方言创建连接
    /// </summary>
    public static async Task<DbConnection?> CreateConnectionForDialectAsync(SqlDefineTypes dialect, string testClassName, TestContext? testContext = null)
    {
        return await Task.FromResult(CreateConnectionForDialect(dialect, testClassName, testContext));
    }
}

