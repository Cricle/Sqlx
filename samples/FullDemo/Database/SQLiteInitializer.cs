using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace FullDemo.Database;

/// <summary>
/// SQLite 数据库初始化
/// </summary>
public static class SQLiteInitializer
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
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                age INTEGER NOT NULL DEFAULT 0,
                balance REAL NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                is_active INTEGER NOT NULL DEFAULT 1
            )",

            // 产品表（带软删除）
            @"CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                category TEXT NOT NULL,
                price REAL NOT NULL DEFAULT 0,
                stock INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            )",

            // 订单表（带审计字段）
            @"CREATE TABLE IF NOT EXISTS orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                total_amount REAL NOT NULL DEFAULT 0,
                status TEXT NOT NULL DEFAULT 'Pending',
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                created_by TEXT NOT NULL DEFAULT '',
                updated_at TEXT,
                updated_by TEXT,
                FOREIGN KEY (user_id) REFERENCES users(id)
            )",

            // 账户表（带乐观锁）
            @"CREATE TABLE IF NOT EXISTS accounts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_no TEXT NOT NULL UNIQUE,
                balance REAL NOT NULL DEFAULT 0,
                version INTEGER NOT NULL DEFAULT 0
            )",

            // 日志表（批量操作）
            @"CREATE TABLE IF NOT EXISTS logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                level TEXT NOT NULL DEFAULT 'INFO',
                message TEXT NOT NULL,
                timestamp TEXT NOT NULL DEFAULT (datetime('now'))
            )",

            // 分类表
            @"CREATE TABLE IF NOT EXISTS categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                code TEXT NOT NULL UNIQUE,
                description TEXT,
                is_active INTEGER NOT NULL DEFAULT 1
            )",

            // 创建索引
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

        Console.WriteLine("[SQLite] Database initialized successfully!");
    }

    public static SqliteConnection CreateConnection(string dbPath = "fulldemo.db")
    {
        var connectionString = $"Data Source={dbPath};Cache=Shared;Foreign Keys=true";
        return new SqliteConnection(connectionString);
    }
}
