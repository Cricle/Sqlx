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
using MySqlConnector;
using Npgsql;
using Microsoft.Data.SqlClient;
using Sqlx;
using Sqlx.Annotations;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 数据库连接管理和测试基础设施
/// 支持 SQLite, MySQL, PostgreSQL, SQL Server, Oracle
/// 使用 Testcontainers 自动管理数据库容器生命周期
/// </summary>
public class DatabaseFixture : IDisposable
{
    private readonly Dictionary<SqlDefineTypes, DbConnection> _connections = new();
    private readonly Dictionary<SqlDefineTypes, bool> _initialized = new();
    
    // Testcontainers instances
    private MySqlContainer? _mySqlContainer;
    private PostgreSqlContainer? _postgreSqlContainer;
    private MsSqlContainer? _msSqlContainer;

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
    /// 使用 Testcontainers 自动启动和管理数据库容器
    /// </summary>
    private DbConnection CreateConnection(SqlDefineTypes dialect)
    {
        return dialect switch
        {
            SqlDefineTypes.SQLite => new SqliteConnection("Data Source=:memory:"),
            SqlDefineTypes.MySql => CreateMySqlConnection(),
            SqlDefineTypes.PostgreSql => CreatePostgreSqlConnection(),
            SqlDefineTypes.SqlServer => CreateSqlServerConnection(),
            SqlDefineTypes.Oracle => throw new NotImplementedException("Oracle connection requires Docker"),
            _ => throw new ArgumentException($"Unsupported dialect: {dialect}")
        };
    }

    /// <summary>
    /// 创建 MySQL 连接（使用 Testcontainers）
    /// </summary>
    private DbConnection CreateMySqlConnection()
    {
        if (_mySqlContainer == null)
        {
            _mySqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.3")
                .WithDatabase("sqlx_test")
                .WithUsername("root")
                .WithPassword("root")
                .Build();
            
            _mySqlContainer.StartAsync().GetAwaiter().GetResult();
        }

        return new MySqlConnection(_mySqlContainer.GetConnectionString());
    }

    /// <summary>
    /// 创建 PostgreSQL 连接（使用 Testcontainers）
    /// </summary>
    private DbConnection CreatePostgreSqlConnection()
    {
        if (_postgreSqlContainer == null)
        {
            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("sqlx_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            
            _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();
        }

        return new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
    }

    /// <summary>
    /// 创建 SQL Server 连接（使用 Testcontainers）
    /// </summary>
    private DbConnection CreateSqlServerConnection()
    {
        if (_msSqlContainer == null)
        {
            _msSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .WithPassword("YourStrong@Passw0rd")
                .Build();
            
            _msSqlContainer.StartAsync().GetAwaiter().GetResult();
        }

        return new SqlConnection(_msSqlContainer.GetConnectionString());
    }

    /// <summary>
    /// 初始化数据库架构
    /// </summary>
    private void InitializeSchema(SqlDefineTypes dialect, DbConnection connection)
    {
        using var cmd = connection.CreateCommand();

        // 创建所有测试表（不插入数据）
        cmd.CommandText = GetSchemaScript(dialect);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 插入测试数据（供需要预置数据的测试使用）
    /// </summary>
    public void SeedTestData(SqlDefineTypes dialect)
    {
        var connection = GetConnection(dialect);
        using var cmd = connection.CreateCommand();

        cmd.CommandText = GetSeedDataScript(dialect);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 获取数据库架构脚本（只创建表，不插入数据）
    /// </summary>
    private string GetSchemaScript(SqlDefineTypes dialect)
    {
        return dialect switch
        {
            SqlDefineTypes.SQLite => @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    balance REAL NOT NULL,
                    created_at TEXT NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS categories (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    code TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    category TEXT NOT NULL,
                    price REAL NOT NULL,
                    stock INTEGER NOT NULL,
                    is_deleted INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS orders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    total_amount REAL NOT NULL,
                    status TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    created_by TEXT NOT NULL,
                    updated_at TEXT,
                    updated_by TEXT
                );

                CREATE TABLE IF NOT EXISTS accounts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    account_no TEXT NOT NULL,
                    balance REAL NOT NULL,
                    version INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    timestamp TEXT NOT NULL
                );",

            SqlDefineTypes.MySql => @"
                CREATE TABLE IF NOT EXISTS users (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    balance DECIMAL(10,2) NOT NULL,
                    created_at DATETIME NOT NULL,
                    is_active TINYINT NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS categories (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    code VARCHAR(255) NOT NULL UNIQUE,
                    name VARCHAR(255) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS products (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    category VARCHAR(255) NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    stock INT NOT NULL,
                    is_deleted TINYINT NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS orders (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    user_id BIGINT NOT NULL,
                    total_amount DECIMAL(10,2) NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    created_at DATETIME NOT NULL,
                    created_by VARCHAR(255) NOT NULL,
                    updated_at DATETIME,
                    updated_by VARCHAR(255)
                );

                CREATE TABLE IF NOT EXISTS accounts (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    account_no VARCHAR(255) NOT NULL,
                    balance DECIMAL(10,2) NOT NULL,
                    version INT NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS logs (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    level VARCHAR(50) NOT NULL,
                    message TEXT NOT NULL,
                    timestamp DATETIME NOT NULL
                );",

            SqlDefineTypes.PostgreSql => @"
                CREATE TABLE IF NOT EXISTS users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    balance DECIMAL(10,2) NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    is_active BOOLEAN NOT NULL DEFAULT true
                );

                CREATE TABLE IF NOT EXISTS categories (
                    id BIGSERIAL PRIMARY KEY,
                    code VARCHAR(255) NOT NULL UNIQUE,
                    name VARCHAR(255) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    category VARCHAR(255) NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    stock INT NOT NULL,
                    is_deleted BOOLEAN NOT NULL DEFAULT false
                );

                CREATE TABLE IF NOT EXISTS orders (
                    id BIGSERIAL PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    total_amount DECIMAL(10,2) NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    created_by VARCHAR(255) NOT NULL,
                    updated_at TIMESTAMP,
                    updated_by VARCHAR(255)
                );

                CREATE TABLE IF NOT EXISTS accounts (
                    id BIGSERIAL PRIMARY KEY,
                    account_no VARCHAR(255) NOT NULL,
                    balance DECIMAL(10,2) NOT NULL,
                    version INT NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS logs (
                    id BIGSERIAL PRIMARY KEY,
                    level VARCHAR(50) NOT NULL,
                    message TEXT NOT NULL,
                    timestamp TIMESTAMP NOT NULL
                );",

            SqlDefineTypes.SqlServer => @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
                CREATE TABLE users (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    balance DECIMAL(10,2) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    is_active BIT NOT NULL DEFAULT 1
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'categories')
                CREATE TABLE categories (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    code NVARCHAR(255) NOT NULL UNIQUE,
                    name NVARCHAR(255) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'products')
                CREATE TABLE products (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    category NVARCHAR(255) NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    stock INT NOT NULL,
                    is_deleted BIT NOT NULL DEFAULT 0
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'orders')
                CREATE TABLE orders (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    user_id BIGINT NOT NULL,
                    total_amount DECIMAL(10,2) NOT NULL,
                    status NVARCHAR(50) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    created_by NVARCHAR(255) NOT NULL,
                    updated_at DATETIME2,
                    updated_by NVARCHAR(255)
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'accounts')
                CREATE TABLE accounts (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    account_no NVARCHAR(255) NOT NULL,
                    balance DECIMAL(10,2) NOT NULL,
                    version INT NOT NULL DEFAULT 0
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'logs')
                CREATE TABLE logs (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    level NVARCHAR(50) NOT NULL,
                    message NVARCHAR(MAX) NOT NULL,
                    timestamp DATETIME2 NOT NULL
                );",

            _ => throw new NotImplementedException($"Schema script not implemented for {dialect}")
        };
    }

    /// <summary>
    /// 获取测试数据插入脚本
    /// </summary>
    private string GetSeedDataScript(SqlDefineTypes dialect)
    {
        return dialect switch
        {
            SqlDefineTypes.SQLite => @"
                -- 插入测试用户数据（15个用户，符合测试期望）
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户A', 'usera@example.com', 25, 1000.00, '2024-01-01', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户B', 'userb@example.com', 30, 2000.00, '2024-01-02', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户C', 'userc@example.com', 35, 5000.00, '2024-01-03', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户D', 'userd@example.com', 25, 500.00, '2024-01-04', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户E', 'usere@example.com', 30, 1500.00, '2024-01-05', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户F', 'userf@example.com', 35, 800.00, '2024-01-06', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户G', 'userg@example.com', 25, 300.00, '2024-01-07', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户H', 'userh@example.com', 30, 600.00, '2024-01-08', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户I', 'useri@example.com', 35, 400.00, '2024-01-09', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户J', 'userj@example.com', 25, 200.00, '2024-01-10', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户K', 'userk@example.com', 30, 700.00, '2024-01-11', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户L', 'userl@example.com', 35, 900.00, '2024-01-12', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户M', 'userm@example.com', 25, 1100.00, '2024-01-13', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户N', 'usern@example.com', 30, 1300.00, '2024-01-14', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户O', 'usero@example.com', 35, 1200.00, '2024-01-15', 1);

                INSERT INTO categories (code, name) VALUES ('Electronics', '电子产品');
                INSERT INTO categories (code, name) VALUES ('Books', '图书');
                INSERT INTO categories (code, name) VALUES ('Clothing', '服装');

                -- 插入测试产品数据（确保价格范围符合测试期望）
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Laptop', 'Electronics', 999.99, 10, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Phone', 'Electronics', 599.99, 20, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Tablet', 'Electronics', 399.99, 15, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book1', 'Books', 29.99, 50, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book2', 'Books', 39.99, 40, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Shirt', 'Clothing', 49.99, 30, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Pants', 'Clothing', 79.99, 25, 0);

                -- 插入测试订单数据（确保金额符合测试期望）
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 1000.00, 'completed', '2024-01-10', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 2000.00, 'completed', '2024-01-11', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (2, 500.00, 'pending', '2024-01-12', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (3, 1500.00, 'completed', '2024-01-13', 'system');",

            SqlDefineTypes.MySql => @"
                -- 插入测试用户数据（15个用户，符合测试期望）
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户A', 'usera@example.com', 25, 1000.00, '2024-01-01', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户B', 'userb@example.com', 30, 2000.00, '2024-01-02', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户C', 'userc@example.com', 35, 5000.00, '2024-01-03', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户D', 'userd@example.com', 25, 500.00, '2024-01-04', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户E', 'usere@example.com', 30, 1500.00, '2024-01-05', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户F', 'userf@example.com', 35, 800.00, '2024-01-06', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户G', 'userg@example.com', 25, 300.00, '2024-01-07', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户H', 'userh@example.com', 30, 600.00, '2024-01-08', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户I', 'useri@example.com', 35, 400.00, '2024-01-09', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户J', 'userj@example.com', 25, 200.00, '2024-01-10', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户K', 'userk@example.com', 30, 700.00, '2024-01-11', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户L', 'userl@example.com', 35, 900.00, '2024-01-12', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户M', 'userm@example.com', 25, 1100.00, '2024-01-13', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户N', 'usern@example.com', 30, 1300.00, '2024-01-14', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户O', 'usero@example.com', 35, 1200.00, '2024-01-15', 1);

                INSERT INTO categories (code, name) VALUES ('Electronics', '电子产品');
                INSERT INTO categories (code, name) VALUES ('Books', '图书');
                INSERT INTO categories (code, name) VALUES ('Clothing', '服装');

                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Laptop', 'Electronics', 999.99, 10, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Phone', 'Electronics', 599.99, 20, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Tablet', 'Electronics', 399.99, 15, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book1', 'Books', 29.99, 50, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book2', 'Books', 39.99, 40, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Shirt', 'Clothing', 49.99, 30, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Pants', 'Clothing', 79.99, 25, 0);

                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 1000.00, 'completed', '2024-01-10', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 2000.00, 'completed', '2024-01-11', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (2, 500.00, 'pending', '2024-01-12', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (3, 1500.00, 'completed', '2024-01-13', 'system');",

            SqlDefineTypes.PostgreSql => @"
                -- 插入测试用户数据（15个用户，符合测试期望）
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户A', 'usera@example.com', 25, 1000.00, '2024-01-01', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户B', 'userb@example.com', 30, 2000.00, '2024-01-02', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户C', 'userc@example.com', 35, 5000.00, '2024-01-03', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户D', 'userd@example.com', 25, 500.00, '2024-01-04', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户E', 'usere@example.com', 30, 1500.00, '2024-01-05', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户F', 'userf@example.com', 35, 800.00, '2024-01-06', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户G', 'userg@example.com', 25, 300.00, '2024-01-07', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户H', 'userh@example.com', 30, 600.00, '2024-01-08', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户I', 'useri@example.com', 35, 400.00, '2024-01-09', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户J', 'userj@example.com', 25, 200.00, '2024-01-10', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户K', 'userk@example.com', 30, 700.00, '2024-01-11', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户L', 'userl@example.com', 35, 900.00, '2024-01-12', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户M', 'userm@example.com', 25, 1100.00, '2024-01-13', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户N', 'usern@example.com', 30, 1300.00, '2024-01-14', true);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES ('用户O', 'usero@example.com', 35, 1200.00, '2024-01-15', true);

                INSERT INTO categories (code, name) VALUES ('Electronics', '电子产品');
                INSERT INTO categories (code, name) VALUES ('Books', '图书');
                INSERT INTO categories (code, name) VALUES ('Clothing', '服装');

                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Laptop', 'Electronics', 999.99, 10, false);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Phone', 'Electronics', 599.99, 20, false);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Tablet', 'Electronics', 399.99, 15, false);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book1', 'Books', 29.99, 50, false);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book2', 'Books', 39.99, 40, false);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Shirt', 'Clothing', 49.99, 30, false);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Pants', 'Clothing', 79.99, 25, false);

                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 1000.00, 'completed', '2024-01-10', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 2000.00, 'completed', '2024-01-11', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (2, 500.00, 'pending', '2024-01-12', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (3, 1500.00, 'completed', '2024-01-13', 'system');",

            SqlDefineTypes.SqlServer => @"
                -- 插入测试用户数据（15个用户，符合测试期望）
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户A', 'usera@example.com', 25, 1000.00, '2024-01-01', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户B', 'userb@example.com', 30, 2000.00, '2024-01-02', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户C', 'userc@example.com', 35, 5000.00, '2024-01-03', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户D', 'userd@example.com', 25, 500.00, '2024-01-04', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户E', 'usere@example.com', 30, 1500.00, '2024-01-05', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户F', 'userf@example.com', 35, 800.00, '2024-01-06', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户G', 'userg@example.com', 25, 300.00, '2024-01-07', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户H', 'userh@example.com', 30, 600.00, '2024-01-08', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户I', 'useri@example.com', 35, 400.00, '2024-01-09', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户J', 'userj@example.com', 25, 200.00, '2024-01-10', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户K', 'userk@example.com', 30, 700.00, '2024-01-11', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户L', 'userl@example.com', 35, 900.00, '2024-01-12', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户M', 'userm@example.com', 25, 1100.00, '2024-01-13', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户N', 'usern@example.com', 30, 1300.00, '2024-01-14', 1);
                INSERT INTO users (name, email, age, balance, created_at, is_active) 
                VALUES (N'用户O', 'usero@example.com', 35, 1200.00, '2024-01-15', 1);

                INSERT INTO categories (code, name) VALUES ('Electronics', N'电子产品');
                INSERT INTO categories (code, name) VALUES ('Books', N'图书');
                INSERT INTO categories (code, name) VALUES ('Clothing', N'服装');

                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Laptop', 'Electronics', 999.99, 10, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Phone', 'Electronics', 599.99, 20, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Tablet', 'Electronics', 399.99, 15, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book1', 'Books', 29.99, 50, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Book2', 'Books', 39.99, 40, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Shirt', 'Clothing', 49.99, 30, 0);
                INSERT INTO products (name, category, price, stock, is_deleted) 
                VALUES ('Pants', 'Clothing', 79.99, 25, 0);

                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 1000.00, 'completed', '2024-01-10', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (1, 2000.00, 'completed', '2024-01-11', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (2, 500.00, 'pending', '2024-01-12', 'system');
                INSERT INTO orders (user_id, total_amount, status, created_at, created_by) 
                VALUES (3, 1500.00, 'completed', '2024-01-13', 'system');",

            _ => throw new NotImplementedException($"Seed data script not implemented for {dialect}")
        };
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
            DELETE FROM categories;
            DELETE FROM users;
        ";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 释放所有数据库连接和容器
    /// </summary>
    public void Dispose()
    {
        // Dispose connections first
        foreach (var connection in _connections.Values)
        {
            connection?.Dispose();
        }
        _connections.Clear();

        // Stop and dispose Testcontainers
        _mySqlContainer?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _postgreSqlContainer?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _msSqlContainer?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Seed authentication scenario data
    /// </summary>
    public void SeedAuthenticationData(SqlDefineTypes dialect)
    {
        var connection = GetConnection(dialect);
        using var cmd = connection.CreateCommand();

        cmd.CommandText = dialect switch
        {
            SqlDefineTypes.SQLite => @"
                -- Create auth tables if they don't exist
                CREATE TABLE IF NOT EXISTS auth_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT NOT NULL UNIQUE,
                    email TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    password_reset_token TEXT,
                    password_reset_expiry TEXT,
                    created_at TEXT NOT NULL,
                    last_login_at TEXT,
                    is_active INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS auth_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    session_token TEXT NOT NULL UNIQUE,
                    created_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL,
                    FOREIGN KEY (user_id) REFERENCES auth_users(id)
                );

                -- Seed test data
                INSERT OR IGNORE INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser1', 'test1@example.com', 'hash1', datetime('now'), 1);
                
                INSERT OR IGNORE INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser2', 'test2@example.com', 'hash2', datetime('now'), 1);",

            SqlDefineTypes.MySql => @"
                -- Create auth tables if they don't exist
                CREATE TABLE IF NOT EXISTS auth_users (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    username VARCHAR(255) NOT NULL UNIQUE,
                    email VARCHAR(255) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    password_reset_token VARCHAR(255),
                    password_reset_expiry DATETIME,
                    created_at DATETIME NOT NULL,
                    last_login_at DATETIME,
                    is_active TINYINT NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS auth_sessions (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    user_id BIGINT NOT NULL,
                    session_token VARCHAR(255) NOT NULL UNIQUE,
                    created_at DATETIME NOT NULL,
                    expires_at DATETIME NOT NULL,
                    FOREIGN KEY (user_id) REFERENCES auth_users(id)
                );

                -- Seed test data
                INSERT IGNORE INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser1', 'test1@example.com', 'hash1', NOW(), 1);
                
                INSERT IGNORE INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser2', 'test2@example.com', 'hash2', NOW(), 1);",

            SqlDefineTypes.PostgreSql => @"
                -- Create auth tables if they don't exist
                CREATE TABLE IF NOT EXISTS auth_users (
                    id BIGSERIAL PRIMARY KEY,
                    username VARCHAR(255) NOT NULL UNIQUE,
                    email VARCHAR(255) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    password_reset_token VARCHAR(255),
                    password_reset_expiry TIMESTAMP,
                    created_at TIMESTAMP NOT NULL,
                    last_login_at TIMESTAMP,
                    is_active BOOLEAN NOT NULL DEFAULT true
                );

                CREATE TABLE IF NOT EXISTS auth_sessions (
                    id BIGSERIAL PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    session_token VARCHAR(255) NOT NULL UNIQUE,
                    created_at TIMESTAMP NOT NULL,
                    expires_at TIMESTAMP NOT NULL,
                    FOREIGN KEY (user_id) REFERENCES auth_users(id)
                );

                -- Seed test data
                INSERT INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser1', 'test1@example.com', 'hash1', CURRENT_TIMESTAMP, true)
                ON CONFLICT (username) DO NOTHING;
                
                INSERT INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser2', 'test2@example.com', 'hash2', CURRENT_TIMESTAMP, true)
                ON CONFLICT (username) DO NOTHING;",

            SqlDefineTypes.SqlServer => @"
                -- Create auth tables if they don't exist
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'auth_users')
                CREATE TABLE auth_users (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    username NVARCHAR(255) NOT NULL UNIQUE,
                    email NVARCHAR(255) NOT NULL UNIQUE,
                    password_hash NVARCHAR(255) NOT NULL,
                    password_reset_token NVARCHAR(255),
                    password_reset_expiry DATETIME2,
                    created_at DATETIME2 NOT NULL,
                    last_login_at DATETIME2,
                    is_active BIT NOT NULL DEFAULT 1
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'auth_sessions')
                CREATE TABLE auth_sessions (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    user_id BIGINT NOT NULL,
                    session_token NVARCHAR(255) NOT NULL UNIQUE,
                    created_at DATETIME2 NOT NULL,
                    expires_at DATETIME2 NOT NULL,
                    FOREIGN KEY (user_id) REFERENCES auth_users(id)
                );

                -- Seed test data
                IF NOT EXISTS (SELECT * FROM auth_users WHERE username = 'testuser1')
                INSERT INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser1', 'test1@example.com', 'hash1', GETDATE(), 1);
                
                IF NOT EXISTS (SELECT * FROM auth_users WHERE username = 'testuser2')
                INSERT INTO auth_users (username, email, password_hash, created_at, is_active)
                VALUES ('testuser2', 'test2@example.com', 'hash2', GETDATE(), 1);",

            _ => throw new NotImplementedException($"Authentication seed not implemented for {dialect}")
        };

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Seed e-commerce scenario data
    /// </summary>
    public void SeedECommerceData(SqlDefineTypes dialect)
    {
        var connection = GetConnection(dialect);
        using var cmd = connection.CreateCommand();

        cmd.CommandText = dialect switch
        {
            SqlDefineTypes.SQLite => @"
                -- Create e-commerce tables if they don't exist
                CREATE TABLE IF NOT EXISTS ecom_products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    description TEXT,
                    price REAL NOT NULL,
                    stock INTEGER NOT NULL,
                    category TEXT NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS ecom_carts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    created_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ecom_cart_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    cart_id INTEGER NOT NULL,
                    product_id INTEGER NOT NULL,
                    quantity INTEGER NOT NULL,
                    price_at_add REAL NOT NULL,
                    FOREIGN KEY (cart_id) REFERENCES ecom_carts(id),
                    FOREIGN KEY (product_id) REFERENCES ecom_products(id)
                );

                CREATE TABLE IF NOT EXISTS ecom_orders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    order_number TEXT NOT NULL UNIQUE,
                    total_amount REAL NOT NULL,
                    status TEXT NOT NULL,
                    order_date TEXT NOT NULL
                );

                -- Seed test data
                INSERT OR IGNORE INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 1', 'Description 1', 99.99, 100, 'Electronics', 1);
                
                INSERT OR IGNORE INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 2', 'Description 2', 49.99, 50, 'Books', 1);",

            SqlDefineTypes.MySql => @"
                -- Create e-commerce tables if they don't exist
                CREATE TABLE IF NOT EXISTS ecom_products (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    price DECIMAL(10,2) NOT NULL,
                    stock INT NOT NULL,
                    category VARCHAR(255) NOT NULL,
                    is_active TINYINT NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS ecom_carts (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    user_id BIGINT NOT NULL,
                    created_at DATETIME NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ecom_cart_items (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    cart_id BIGINT NOT NULL,
                    product_id BIGINT NOT NULL,
                    quantity INT NOT NULL,
                    price_at_add DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (cart_id) REFERENCES ecom_carts(id),
                    FOREIGN KEY (product_id) REFERENCES ecom_products(id)
                );

                CREATE TABLE IF NOT EXISTS ecom_orders (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    user_id BIGINT NOT NULL,
                    order_number VARCHAR(255) NOT NULL UNIQUE,
                    total_amount DECIMAL(10,2) NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    order_date DATETIME NOT NULL
                );

                -- Seed test data
                INSERT IGNORE INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 1', 'Description 1', 99.99, 100, 'Electronics', 1);
                
                INSERT IGNORE INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 2', 'Description 2', 49.99, 50, 'Books', 1);",

            SqlDefineTypes.PostgreSql => @"
                -- Create e-commerce tables if they don't exist
                CREATE TABLE IF NOT EXISTS ecom_products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    price DECIMAL(10,2) NOT NULL,
                    stock INT NOT NULL,
                    category VARCHAR(255) NOT NULL,
                    is_active BOOLEAN NOT NULL DEFAULT true
                );

                CREATE TABLE IF NOT EXISTS ecom_carts (
                    id BIGSERIAL PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    created_at TIMESTAMP NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ecom_cart_items (
                    id BIGSERIAL PRIMARY KEY,
                    cart_id BIGINT NOT NULL,
                    product_id BIGINT NOT NULL,
                    quantity INT NOT NULL,
                    price_at_add DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (cart_id) REFERENCES ecom_carts(id),
                    FOREIGN KEY (product_id) REFERENCES ecom_products(id)
                );

                CREATE TABLE IF NOT EXISTS ecom_orders (
                    id BIGSERIAL PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    order_number VARCHAR(255) NOT NULL UNIQUE,
                    total_amount DECIMAL(10,2) NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    order_date TIMESTAMP NOT NULL
                );

                -- Seed test data
                INSERT INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 1', 'Description 1', 99.99, 100, 'Electronics', true)
                ON CONFLICT DO NOTHING;
                
                INSERT INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 2', 'Description 2', 49.99, 50, 'Books', true)
                ON CONFLICT DO NOTHING;",

            SqlDefineTypes.SqlServer => @"
                -- Create e-commerce tables if they don't exist
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ecom_products')
                CREATE TABLE ecom_products (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    description NVARCHAR(MAX),
                    price DECIMAL(10,2) NOT NULL,
                    stock INT NOT NULL,
                    category NVARCHAR(255) NOT NULL,
                    is_active BIT NOT NULL DEFAULT 1
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ecom_carts')
                CREATE TABLE ecom_carts (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    user_id BIGINT NOT NULL,
                    created_at DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ecom_cart_items')
                CREATE TABLE ecom_cart_items (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    cart_id BIGINT NOT NULL,
                    product_id BIGINT NOT NULL,
                    quantity INT NOT NULL,
                    price_at_add DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (cart_id) REFERENCES ecom_carts(id),
                    FOREIGN KEY (product_id) REFERENCES ecom_products(id)
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ecom_orders')
                CREATE TABLE ecom_orders (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    user_id BIGINT NOT NULL,
                    order_number NVARCHAR(255) NOT NULL UNIQUE,
                    total_amount DECIMAL(10,2) NOT NULL,
                    status NVARCHAR(50) NOT NULL,
                    order_date DATETIME2 NOT NULL
                );

                -- Seed test data
                IF NOT EXISTS (SELECT * FROM ecom_products WHERE name = 'Product 1')
                INSERT INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 1', 'Description 1', 99.99, 100, 'Electronics', 1);
                
                IF NOT EXISTS (SELECT * FROM ecom_products WHERE name = 'Product 2')
                INSERT INTO ecom_products (name, description, price, stock, category, is_active)
                VALUES ('Product 2', 'Description 2', 49.99, 50, 'Books', 1);",

            _ => throw new NotImplementedException($"E-commerce seed not implemented for {dialect}")
        };

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Seed blog/CMS scenario data
    /// </summary>
    public void SeedBlogCmsData(SqlDefineTypes dialect)
    {
        var connection = GetConnection(dialect);
        using var cmd = connection.CreateCommand();

        cmd.CommandText = dialect switch
        {
            SqlDefineTypes.SQLite => @"
                -- Create blog/CMS tables if they don't exist
                CREATE TABLE IF NOT EXISTS blog_posts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT NOT NULL,
                    content TEXT NOT NULL,
                    author_id INTEGER NOT NULL,
                    status TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    published_at TEXT
                );

                CREATE TABLE IF NOT EXISTS blog_comments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    post_id INTEGER NOT NULL,
                    user_id INTEGER NOT NULL,
                    content TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id)
                );

                CREATE TABLE IF NOT EXISTS blog_tags (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS blog_post_tags (
                    post_id INTEGER NOT NULL,
                    tag_id INTEGER NOT NULL,
                    PRIMARY KEY (post_id, tag_id),
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id),
                    FOREIGN KEY (tag_id) REFERENCES blog_tags(id)
                );

                -- Seed test data
                INSERT OR IGNORE INTO blog_posts (title, content, author_id, status, created_at)
                VALUES ('Test Post 1', 'Content 1', 1, 'draft', datetime('now'));
                
                INSERT OR IGNORE INTO blog_tags (name) VALUES ('Technology');
                INSERT OR IGNORE INTO blog_tags (name) VALUES ('Programming');",

            SqlDefineTypes.MySql => @"
                -- Create blog/CMS tables if they don't exist
                CREATE TABLE IF NOT EXISTS blog_posts (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    title VARCHAR(500) NOT NULL,
                    content TEXT NOT NULL,
                    author_id BIGINT NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    created_at DATETIME NOT NULL,
                    published_at DATETIME NULL
                );

                CREATE TABLE IF NOT EXISTS blog_comments (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    post_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    content TEXT NOT NULL,
                    created_at DATETIME NOT NULL,
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id)
                );

                CREATE TABLE IF NOT EXISTS blog_tags (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(100) NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS blog_post_tags (
                    post_id BIGINT NOT NULL,
                    tag_id BIGINT NOT NULL,
                    PRIMARY KEY (post_id, tag_id),
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id),
                    FOREIGN KEY (tag_id) REFERENCES blog_tags(id)
                );

                -- Seed test data
                INSERT IGNORE INTO blog_posts (title, content, author_id, status, created_at)
                VALUES ('Test Post 1', 'Content 1', 1, 'draft', NOW());
                
                INSERT IGNORE INTO blog_tags (name) VALUES ('Technology');
                INSERT IGNORE INTO blog_tags (name) VALUES ('Programming');",

            SqlDefineTypes.PostgreSql => @"
                -- Create blog/CMS tables if they don't exist
                CREATE TABLE IF NOT EXISTS blog_posts (
                    id BIGSERIAL PRIMARY KEY,
                    title VARCHAR(500) NOT NULL,
                    content TEXT NOT NULL,
                    author_id BIGINT NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    published_at TIMESTAMP NULL
                );

                CREATE TABLE IF NOT EXISTS blog_comments (
                    id BIGSERIAL PRIMARY KEY,
                    post_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    content TEXT NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id)
                );

                CREATE TABLE IF NOT EXISTS blog_tags (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS blog_post_tags (
                    post_id BIGINT NOT NULL,
                    tag_id BIGINT NOT NULL,
                    PRIMARY KEY (post_id, tag_id),
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id),
                    FOREIGN KEY (tag_id) REFERENCES blog_tags(id)
                );

                -- Seed test data
                INSERT INTO blog_posts (title, content, author_id, status, created_at)
                VALUES ('Test Post 1', 'Content 1', 1, 'draft', CURRENT_TIMESTAMP)
                ON CONFLICT DO NOTHING;
                
                INSERT INTO blog_tags (name) VALUES ('Technology') ON CONFLICT DO NOTHING;
                INSERT INTO blog_tags (name) VALUES ('Programming') ON CONFLICT DO NOTHING;",

            SqlDefineTypes.SqlServer => @"
                -- Create blog/CMS tables if they don't exist
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'blog_posts')
                CREATE TABLE blog_posts (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    title NVARCHAR(500) NOT NULL,
                    content NVARCHAR(MAX) NOT NULL,
                    author_id BIGINT NOT NULL,
                    status NVARCHAR(50) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    published_at DATETIME2 NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'blog_comments')
                CREATE TABLE blog_comments (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    post_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    content NVARCHAR(MAX) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id)
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'blog_tags')
                CREATE TABLE blog_tags (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(100) NOT NULL UNIQUE
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'blog_post_tags')
                CREATE TABLE blog_post_tags (
                    post_id BIGINT NOT NULL,
                    tag_id BIGINT NOT NULL,
                    PRIMARY KEY (post_id, tag_id),
                    FOREIGN KEY (post_id) REFERENCES blog_posts(id),
                    FOREIGN KEY (tag_id) REFERENCES blog_tags(id)
                );

                -- Seed test data
                IF NOT EXISTS (SELECT * FROM blog_posts WHERE title = 'Test Post 1')
                INSERT INTO blog_posts (title, content, author_id, status, created_at)
                VALUES ('Test Post 1', 'Content 1', 1, 'draft', GETDATE());
                
                IF NOT EXISTS (SELECT * FROM blog_tags WHERE name = 'Technology')
                INSERT INTO blog_tags (name) VALUES ('Technology');
                
                IF NOT EXISTS (SELECT * FROM blog_tags WHERE name = 'Programming')
                INSERT INTO blog_tags (name) VALUES ('Programming');",

            _ => throw new NotImplementedException($"Blog/CMS seed not implemented for {dialect}")
        };

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Seed multi-tenant scenario data
    /// </summary>
    public void SeedMultiTenantData(SqlDefineTypes dialect)
    {
        var connection = GetConnection(dialect);
        using var cmd = connection.CreateCommand();

        cmd.CommandText = dialect switch
        {
            SqlDefineTypes.SQLite => @"
                -- Create multi-tenant tables if they don't exist
                CREATE TABLE IF NOT EXISTS tenants (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    code TEXT NOT NULL UNIQUE,
                    is_active INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS tenant_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tenant_id INTEGER NOT NULL,
                    data_type TEXT NOT NULL,
                    data_value TEXT NOT NULL,
                    FOREIGN KEY (tenant_id) REFERENCES tenants(id)
                );

                -- Seed test data
                INSERT OR IGNORE INTO tenants (name, code, is_active)
                VALUES ('Tenant A', 'TENANT_A', 1);
                
                INSERT OR IGNORE INTO tenants (name, code, is_active)
                VALUES ('Tenant B', 'TENANT_B', 1);
                
                INSERT OR IGNORE INTO tenant_data (tenant_id, data_type, data_value)
                VALUES (1, 'config', 'value1');
                
                INSERT OR IGNORE INTO tenant_data (tenant_id, data_type, data_value)
                VALUES (2, 'config', 'value2');",

            SqlDefineTypes.MySql => @"
                -- Create multi-tenant tables if they don't exist
                CREATE TABLE IF NOT EXISTS tenants (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    code VARCHAR(255) NOT NULL UNIQUE,
                    is_active TINYINT NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS tenant_data (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    tenant_id BIGINT NOT NULL,
                    data_type VARCHAR(255) NOT NULL,
                    data_value TEXT NOT NULL,
                    FOREIGN KEY (tenant_id) REFERENCES tenants(id)
                );

                -- Seed test data
                INSERT IGNORE INTO tenants (name, code, is_active)
                VALUES ('Tenant A', 'TENANT_A', 1);
                
                INSERT IGNORE INTO tenants (name, code, is_active)
                VALUES ('Tenant B', 'TENANT_B', 1);
                
                INSERT IGNORE INTO tenant_data (tenant_id, data_type, data_value)
                VALUES (1, 'config', 'value1');
                
                INSERT IGNORE INTO tenant_data (tenant_id, data_type, data_value)
                VALUES (2, 'config', 'value2');",

            SqlDefineTypes.PostgreSql => @"
                -- Create multi-tenant tables if they don't exist
                CREATE TABLE IF NOT EXISTS tenants (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    code VARCHAR(255) NOT NULL UNIQUE,
                    is_active BOOLEAN NOT NULL DEFAULT true
                );

                CREATE TABLE IF NOT EXISTS tenant_data (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id BIGINT NOT NULL,
                    data_type VARCHAR(255) NOT NULL,
                    data_value TEXT NOT NULL,
                    FOREIGN KEY (tenant_id) REFERENCES tenants(id)
                );

                -- Seed test data
                INSERT INTO tenants (name, code, is_active)
                VALUES ('Tenant A', 'TENANT_A', true)
                ON CONFLICT (code) DO NOTHING;
                
                INSERT INTO tenants (name, code, is_active)
                VALUES ('Tenant B', 'TENANT_B', true)
                ON CONFLICT (code) DO NOTHING;
                
                INSERT INTO tenant_data (tenant_id, data_type, data_value)
                SELECT 1, 'config', 'value1'
                WHERE NOT EXISTS (SELECT 1 FROM tenant_data WHERE tenant_id = 1 AND data_type = 'config');
                
                INSERT INTO tenant_data (tenant_id, data_type, data_value)
                SELECT 2, 'config', 'value2'
                WHERE NOT EXISTS (SELECT 1 FROM tenant_data WHERE tenant_id = 2 AND data_type = 'config');",

            SqlDefineTypes.SqlServer => @"
                -- Create multi-tenant tables if they don't exist
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tenants')
                CREATE TABLE tenants (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    code NVARCHAR(255) NOT NULL UNIQUE,
                    is_active BIT NOT NULL DEFAULT 1
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tenant_data')
                CREATE TABLE tenant_data (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    tenant_id BIGINT NOT NULL,
                    data_type NVARCHAR(255) NOT NULL,
                    data_value NVARCHAR(MAX) NOT NULL,
                    FOREIGN KEY (tenant_id) REFERENCES tenants(id)
                );

                -- Seed test data
                IF NOT EXISTS (SELECT * FROM tenants WHERE code = 'TENANT_A')
                INSERT INTO tenants (name, code, is_active)
                VALUES ('Tenant A', 'TENANT_A', 1);
                
                IF NOT EXISTS (SELECT * FROM tenants WHERE code = 'TENANT_B')
                INSERT INTO tenants (name, code, is_active)
                VALUES ('Tenant B', 'TENANT_B', 1);
                
                IF NOT EXISTS (SELECT * FROM tenant_data WHERE tenant_id = 1 AND data_type = 'config')
                INSERT INTO tenant_data (tenant_id, data_type, data_value)
                VALUES (1, 'config', 'value1');
                
                IF NOT EXISTS (SELECT * FROM tenant_data WHERE tenant_id = 2 AND data_type = 'config')
                INSERT INTO tenant_data (tenant_id, data_type, data_value)
                VALUES (2, 'config', 'value2');",

            _ => throw new NotImplementedException($"Multi-tenant seed not implemented for {dialect}")
        };

        cmd.ExecuteNonQuery();
    }
}
