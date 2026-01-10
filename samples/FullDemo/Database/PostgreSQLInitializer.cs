using System.Data.Common;
using Npgsql;

namespace FullDemo.Database;

/// <summary>
/// PostgreSQL 数据库初始化
/// </summary>
public static class PostgreSQLInitializer
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
            @"CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                email VARCHAR(200) NOT NULL UNIQUE,
                age INTEGER NOT NULL DEFAULT 0,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN NOT NULL DEFAULT TRUE
            )",

            // 产品表（带软删除）
            @"CREATE TABLE IF NOT EXISTS products (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                category VARCHAR(50) NOT NULL,
                price DECIMAL(18,2) NOT NULL DEFAULT 0,
                stock INTEGER NOT NULL DEFAULT 0,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            )",

            // 订单表（带审计字段）
            @"CREATE TABLE IF NOT EXISTS orders (
                id BIGSERIAL PRIMARY KEY,
                user_id BIGINT NOT NULL REFERENCES users(id),
                total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_by VARCHAR(100) NOT NULL DEFAULT '',
                updated_at TIMESTAMP,
                updated_by VARCHAR(100)
            )",

            // 账户表（带乐观锁）
            @"CREATE TABLE IF NOT EXISTS accounts (
                id BIGSERIAL PRIMARY KEY,
                account_no VARCHAR(50) NOT NULL UNIQUE,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                version BIGINT NOT NULL DEFAULT 0
            )",

            // 日志表（批量操作）
            @"CREATE TABLE IF NOT EXISTS logs (
                id BIGSERIAL PRIMARY KEY,
                level VARCHAR(20) NOT NULL DEFAULT 'INFO',
                message TEXT NOT NULL,
                timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            )",

            // 分类表
            @"CREATE TABLE IF NOT EXISTS categories (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                code VARCHAR(10) NOT NULL UNIQUE,
                description TEXT,
                is_active BOOLEAN NOT NULL DEFAULT TRUE
            )"
        };

        // PostgreSQL 需要单独创建索引（如果不存在）
        var indexCommands = new[]
        {
            @"CREATE INDEX IF NOT EXISTS idx_users_email ON users(email)",
            @"CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active)",
            @"CREATE INDEX IF NOT EXISTS idx_products_category ON products(category)",
            @"CREATE INDEX IF NOT EXISTS idx_products_is_deleted ON products(is_deleted)",
            @"CREATE INDEX IF NOT EXISTS idx_orders_user_id ON orders(user_id)",
            @"CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status)",
            @"CREATE INDEX IF NOT EXISTS idx_logs_level ON logs(level)",
            @"CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON logs(timestamp)"
        };

        foreach (var commandText in commands)
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        foreach (var commandText in indexCommands)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = commandText;
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // 索引可能已存在，忽略错误
            }
        }

        Console.WriteLine("[PostgreSQL] Database initialized successfully!");
    }

    public static NpgsqlConnection CreateConnection(string host = "localhost", int port = 5432, string database = "fulldemo", string user = "postgres", string password = "postgres")
    {
        var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password}";
        return new NpgsqlConnection(connectionString);
    }
}
