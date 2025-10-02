using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TodoWebApi.Services;

/// <summary>
/// 数据库初始化服务 - 使用主构造函数简化
/// </summary>
public class DatabaseService(SqliteConnection connection)
{
    /// <summary>
    /// 初始化数据库表结构
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS todos (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT,
                is_completed INTEGER NOT NULL DEFAULT 0,
                priority INTEGER NOT NULL DEFAULT 1,
                due_date TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                completed_at TEXT,
                tags TEXT,
                estimated_minutes INTEGER,
                actual_minutes INTEGER
            );

            CREATE INDEX IF NOT EXISTS idx_todos_is_completed ON todos(is_completed);
            CREATE INDEX IF NOT EXISTS idx_todos_priority ON todos(priority);
            CREATE INDEX IF NOT EXISTS idx_todos_due_date ON todos(due_date);
        ";

        await command.ExecuteNonQueryAsync();
        Console.WriteLine("✅ 数据库初始化完成");
    }
}

