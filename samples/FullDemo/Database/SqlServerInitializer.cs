using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace FullDemo.Database;

/// <summary>
/// SQL Server 数据库初始化
/// </summary>
public static class SqlServerInitializer
{
    public static async Task InitializeAsync(DbConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var commands = new[]
        {
            // 用户表
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
            BEGIN
                CREATE TABLE users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(200) NOT NULL UNIQUE,
                    age INT NOT NULL DEFAULT 0,
                    balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
                    is_active BIT NOT NULL DEFAULT 1
                )
                CREATE INDEX idx_users_email ON users(email)
                CREATE INDEX idx_users_is_active ON users(is_active)
            END",

            // 产品表（带软删除）
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'products')
            BEGIN
                CREATE TABLE products (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(200) NOT NULL,
                    category NVARCHAR(50) NOT NULL,
                    price DECIMAL(18,2) NOT NULL DEFAULT 0,
                    stock INT NOT NULL DEFAULT 0,
                    is_deleted BIT NOT NULL DEFAULT 0,
                    created_at DATETIME2 NOT NULL DEFAULT GETDATE()
                )
                CREATE INDEX idx_products_category ON products(category)
                CREATE INDEX idx_products_is_deleted ON products(is_deleted)
            END",

            // 订单表（带审计字段）
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'orders')
            BEGIN
                CREATE TABLE orders (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
                    created_by NVARCHAR(100) NOT NULL DEFAULT '',
                    updated_at DATETIME2,
                    updated_by NVARCHAR(100),
                    FOREIGN KEY (user_id) REFERENCES users(id)
                )
                CREATE INDEX idx_orders_user_id ON orders(user_id)
                CREATE INDEX idx_orders_status ON orders(status)
            END",

            // 账户表（带乐观锁）
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'accounts')
            BEGIN
                CREATE TABLE accounts (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    account_no NVARCHAR(50) NOT NULL UNIQUE,
                    balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                    version BIGINT NOT NULL DEFAULT 0
                )
            END",

            // 日志表（批量操作）
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'logs')
            BEGIN
                CREATE TABLE logs (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    level NVARCHAR(20) NOT NULL DEFAULT 'INFO',
                    message NVARCHAR(MAX) NOT NULL,
                    timestamp DATETIME2 NOT NULL DEFAULT GETDATE()
                )
                CREATE INDEX idx_logs_level ON logs(level)
                CREATE INDEX idx_logs_timestamp ON logs(timestamp)
            END",

            // 分类表
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'categories')
            BEGIN
                CREATE TABLE categories (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL UNIQUE,
                    parent_id BIGINT NULL,
                    FOREIGN KEY (parent_id) REFERENCES categories(id)
                )
            END"
        };

        foreach (var commandText in commands)
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        Console.WriteLine("[SqlServer] Database initialized successfully!");
    }

    public static SqlConnection CreateConnection(string host = "localhost", int port = 1433, string database = "fulldemo", string user = "sa", string password = "YourStrong@Passw0rd")
    {
        var connectionString = $"Server={host},{port};Database={database};User Id={user};Password={password};TrustServerCertificate=true;Encrypt=true";
        return new SqlConnection(connectionString);
    }

    public static async Task CreateDatabaseIfNotExistsAsync(string host = "localhost", int port = 1433, string database = "fulldemo", string user = "sa", string password = "YourStrong@Passw0rd")
    {
        var connectionString = $"Server={host},{port};Database=master;User Id={user};Password={password};TrustServerCertificate=true;Encrypt=true";
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
            IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{database}')
            BEGIN
                CREATE DATABASE [{database}]
            END";
        await command.ExecuteNonQueryAsync();

        Console.WriteLine($"[SqlServer] Database '{database}' ready!");
    }
}
