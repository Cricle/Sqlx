// -----------------------------------------------------------------------
// <copyright file="DatabaseConnectionHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Infrastructure;

/// <summary>
/// 数据库连接辅助类，根据环境变量提供不同的数据库连接
/// </summary>
public static class DatabaseConnectionHelper
{
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
    /// </summary>
    public static DbConnection? GetPostgreSQLConnection(TestContext? testContext = null)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION") ??
                               testContext?.Properties["PostgreSQLConnection"]?.ToString() ??
                               "Host=localhost;Port=5432;Database=sqlx_test;Username=postgres;Password=postgres;Pooling=false;Timeout=5";

        try
        {
            var connection = new Npgsql.NpgsqlConnection(connectionString);
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to connect to PostgreSQL: {ex.Message}");
            Console.WriteLine($"Connection string (masked): Host=localhost;Port=5432;Database=sqlx_test;Username=***;Password=***");
            return null;
        }
    }

    /// <summary>
    /// 获取MySQL数据库连接
    /// </summary>
    public static DbConnection? GetMySQLConnection(TestContext? testContext = null)
    {
        var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ??
                               testContext?.Properties["MySQLConnection"]?.ToString() ??
                               "Server=localhost;Port=3306;Database=sqlx_test;Uid=root;Pwd=root";

        try
        {
            var connection = new MySqlConnector.MySqlConnection(connectionString);
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to connect to MySQL: {ex.Message}");
            Console.WriteLine($"Connection string (masked): Server=localhost;Port=3306;Database=sqlx_test;Uid=***;Pwd=***");
            return null;
        }
    }

    /// <summary>
    /// 获取SQL Server数据库连接
    /// </summary>
    public static DbConnection? GetSqlServerConnection(TestContext? testContext = null)
    {
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION") ??
                               testContext?.Properties["SqlServerConnection"]?.ToString() ??
                               "Server=localhost;Database=sqlx_test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True";

        try
        {
            var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to connect to SQL Server: {ex.Message}");
            Console.WriteLine($"Connection string (masked): Server=localhost;Database=sqlx_test;User Id=***;Password=***;TrustServerCertificate=True");
            return null;
        }
    }

    /// <summary>
    /// 获取Oracle数据库连接
    /// </summary>
    public static DbConnection? GetOracleConnection(TestContext? testContext = null)
    {
        var connectionString = Environment.GetEnvironmentVariable("ORACLE_CONNECTION") ??
                               testContext?.Properties["OracleConnection"]?.ToString() ??
                               "Data Source=localhost:1521/XEPDB1;User Id=system;Password=oracle";

        try
        {
            // 需要 Oracle.ManagedDataAccess.Core 包
            // var connection = new OracleConnection(connectionString);
            // connection.Open();
            // return connection;

            // 暂时返回null，等待实现
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to Oracle: {ex.Message}", ex);
        }
    }
}

