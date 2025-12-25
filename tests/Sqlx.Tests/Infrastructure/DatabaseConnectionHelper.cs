// -----------------------------------------------------------------------
// <copyright file="DatabaseConnectionHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;

namespace Sqlx.Tests.Infrastructure;

/// <summary>
/// 数据库连接辅助类，使用 Testcontainers 自动管理数据库容器
/// </summary>
public static class DatabaseConnectionHelper
{
    // Testcontainers 实例（单例模式，避免重复启动容器）
    private static MySqlContainer? _mySqlContainer;
    private static PostgreSqlContainer? _postgreSqlContainer;
    private static MsSqlContainer? _msSqlContainer;
    private static readonly object _lock = new object();

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
    /// </summary>
    public static DbConnection? GetPostgreSQLConnection(TestContext? testContext = null)
    {
        try
        {
            lock (_lock)
            {
                if (_postgreSqlContainer == null)
                {
                    Console.WriteLine("🐳 Starting PostgreSQL container...");
                    _postgreSqlContainer = new PostgreSqlBuilder()
                        .WithImage("postgres:16")
                        .WithDatabase("sqlx_test")
                        .WithUsername("postgres")
                        .WithPassword("postgres")
                        .Build();
                    
                    _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();
                    Console.WriteLine("✅ PostgreSQL container started successfully");
                }
            }

            var connection = new Npgsql.NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to start PostgreSQL container: {ex.GetType().Name}: {ex.Message}");
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
    /// </summary>
    public static DbConnection? GetMySQLConnection(TestContext? testContext = null)
    {
        try
        {
            lock (_lock)
            {
                if (_mySqlContainer == null)
                {
                    Console.WriteLine("🐳 Starting MySQL container...");
                    _mySqlContainer = new MySqlBuilder()
                        .WithImage("mysql:8.3")
                        .WithDatabase("sqlx_test")
                        .WithUsername("root")
                        .WithPassword("root")
                        .Build();
                    
                    _mySqlContainer.StartAsync().GetAwaiter().GetResult();
                    Console.WriteLine("✅ MySQL container started successfully");
                }
            }

            var connection = new MySqlConnector.MySqlConnection(_mySqlContainer.GetConnectionString());
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to start MySQL container: {ex.GetType().Name}: {ex.Message}");
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
    /// </summary>
    public static DbConnection? GetSqlServerConnection(TestContext? testContext = null)
    {
        try
        {
            lock (_lock)
            {
                if (_msSqlContainer == null)
                {
                    Console.WriteLine("🐳 Starting SQL Server container...");
                    _msSqlContainer = new MsSqlBuilder()
                        .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                        .WithPassword("YourStrong@Passw0rd")
                        .Build();
                    
                    _msSqlContainer.StartAsync().GetAwaiter().GetResult();
                    Console.WriteLine("✅ SQL Server container started successfully");
                }
            }

            var connection = new Microsoft.Data.SqlClient.SqlConnection(_msSqlContainer.GetConnectionString());
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to start SQL Server container: {ex.GetType().Name}: {ex.Message}");
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

