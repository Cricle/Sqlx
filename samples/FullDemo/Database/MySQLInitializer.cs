using System.Data.Common;
using MySqlConnector;

namespace FullDemo.Database;

/// <summary>
/// MySQL 数据库初始化
/// </summary>
public static class MySQLInitializer
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
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                email VARCHAR(200) NOT NULL UNIQUE,
                age INT NOT NULL DEFAULT 0,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                INDEX idx_users_email (email),
                INDEX idx_users_is_active (is_active)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",

            // 产品表（带软删除）
            @"CREATE TABLE IF NOT EXISTS products (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                category VARCHAR(50) NOT NULL,
                price DECIMAL(18,2) NOT NULL DEFAULT 0,
                stock INT NOT NULL DEFAULT 0,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_products_category (category),
                INDEX idx_products_is_deleted (is_deleted)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",

            // 订单表（带审计字段）
            @"CREATE TABLE IF NOT EXISTS orders (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                user_id BIGINT NOT NULL,
                total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_by VARCHAR(100) NOT NULL DEFAULT '',
                updated_at DATETIME,
                updated_by VARCHAR(100),
                INDEX idx_orders_user_id (user_id),
                INDEX idx_orders_status (status),
                FOREIGN KEY (user_id) REFERENCES users(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",

            // 账户表（带乐观锁）
            @"CREATE TABLE IF NOT EXISTS accounts (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                account_no VARCHAR(50) NOT NULL UNIQUE,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                version BIGINT NOT NULL DEFAULT 0
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",

            // 日志表（批量操作）
            @"CREATE TABLE IF NOT EXISTS logs (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                level VARCHAR(20) NOT NULL DEFAULT 'INFO',
                message TEXT NOT NULL,
                timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_logs_level (level),
                INDEX idx_logs_timestamp (timestamp)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",

            // 分类表
            @"CREATE TABLE IF NOT EXISTS categories (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                code VARCHAR(10) NOT NULL UNIQUE,
                description TEXT,
                is_active BOOLEAN NOT NULL DEFAULT TRUE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
        };

        foreach (var commandText in commands)
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        Console.WriteLine("[MySQL] Database initialized successfully!");
    }

    public static MySqlConnection CreateConnection(string host = "localhost", int port = 3306, string database = "fulldemo", string user = "root", string password = "root")
    {
        var connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};AllowUserVariables=true;AllowPublicKeyRetrieval=true;SslMode=None;ConnectionReset=true";
        return new MySqlConnection(connectionString);
    }

    public static async Task CreateDatabaseIfNotExistsAsync(string host = "localhost", int port = 3306, string database = "fulldemo", string user = "root", string password = "root")
    {
        var connectionString = $"Server={host};Port={port};User={user};Password={password};AllowPublicKeyRetrieval=true;SslMode=None;ConnectionReset=true";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
        await command.ExecuteNonQueryAsync();

        Console.WriteLine($"[MySQL] Database '{database}' ready!");
    }
}

