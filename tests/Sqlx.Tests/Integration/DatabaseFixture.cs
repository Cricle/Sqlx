// -----------------------------------------------------------------------
// <copyright file="DatabaseFixture.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 数据库连接管理和测试基础设施
/// 支持 SQLite, MySQL, PostgreSQL, SQL Server, Oracle
/// </summary>
public class DatabaseFixture : IDisposable
{
    private readonly Dictionary<SqlDefineTypes, DbConnection> _connections = new();
    private readonly Dictionary<SqlDefineTypes, bool> _initialized = new();

    /// <summary>
    /// 获取指定方言的数据库连接
    /// </summary>
    public DbConnection GetConnection(SqlDefineTypes dialect)
    {
        if (_connections.TryGetValue(dialect, out var connection))
        {
            return connection;
        }

        connection = CreateConnection(dialect);
        connection.Open();
        _connections[dialect] = connection;

        // 初始化数据库架构
        if (!_initialized.ContainsKey(dialect))
        {
            InitializeSchema(dialect, connection);
            _initialized[dialect] = true;
        }

        return connection;
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    private DbConnection CreateConnection(SqlDefineTypes dialect)
    {
        return dialect switch
        {
            SqlDefineTypes.SQLite => new SqliteConnection("Data Source=:memory:"),
            SqlDefineTypes.MySql => throw new NotImplementedException("MySQL connection requires Docker"),
            SqlDefineTypes.PostgreSql => throw new NotImplementedException("PostgreSQL connection requires Docker"),
            SqlDefineTypes.SqlServer => throw new NotImplementedException("SQL Server connection requires Docker"),
            SqlDefineTypes.Oracle => throw new NotImplementedException("Oracle connection requires Docker"),
            _ => throw new ArgumentException($"Unsupported dialect: {dialect}")
        };
    }

    /// <summary>
    /// 初始化数据库架构
    /// </summary>
    private void InitializeSchema(SqlDefineTypes dialect, DbConnection connection)
    {
        using var cmd = connection.CreateCommand();

        // 创建所有测试表
        cmd.CommandText = GetSchemaScript(dialect);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 获取数据库架构脚本
    /// </summary>
    private string GetSchemaScript(SqlDefineTypes dialect)
    {
        // SQLite schema
        if (dialect == SqlDefineTypes.SQLite)
        {
            return @"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    balance REAL NOT NULL,
                    created_at TEXT NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    category TEXT NOT NULL,
                    price REAL NOT NULL,
                    stock INTEGER NOT NULL,
                    is_deleted INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE categories (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    code TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL
                );

                INSERT INTO categories (code, name) VALUES ('Electronics', '电子产品');
                INSERT INTO categories (code, name) VALUES ('Books', '图书');
                INSERT INTO categories (code, name) VALUES ('Clothing', '服装');

                CREATE TABLE orders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    total_amount REAL NOT NULL,
                    status TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    created_by TEXT NOT NULL,
                    updated_at TEXT,
                    updated_by TEXT
                );

                CREATE TABLE accounts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    account_no TEXT NOT NULL,
                    balance REAL NOT NULL,
                    version INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    timestamp TEXT NOT NULL
                );
            ";
        }

        throw new NotImplementedException($"Schema script not implemented for {dialect}");
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    public void CleanupData(SqlDefineTypes dialect)
    {
        var connection = GetConnection(dialect);
        using var cmd = connection.CreateCommand();

        cmd.CommandText = @"
            DELETE FROM logs;
            DELETE FROM accounts;
            DELETE FROM orders;
            DELETE FROM products;
            DELETE FROM users;
        ";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 释放所有数据库连接
    /// </summary>
    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            connection?.Dispose();
        }
        _connections.Clear();
    }
}
