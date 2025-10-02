using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TodoWebApi.Models;
using Sqlx;
using Sqlx.Annotations;

namespace TodoWebApi.Services;

/// <summary>
/// Sqlx 驱动的 TODO 数据访问服务 - 展示所有核心功能
/// </summary>
public class TodoService(SqliteConnection connection)
{
    private readonly SqliteConnection _connection = connection;

    /// <summary>
    /// 获取所有TODO - 展示基本查询
    /// </summary>
    public async Task<List<Todo>> GetAllAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, title, description, is_completed, priority, due_date,
                   created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
            FROM todos
            ORDER BY created_at DESC";

        var todos = new List<Todo>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            todos.Add(new Todo
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsCompleted = reader.GetBoolean(3),
                Priority = reader.GetInt32(4),
                DueDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                CompletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                Tags = reader.IsDBNull(9) ? null : reader.GetString(9),
                EstimatedMinutes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                ActualMinutes = reader.IsDBNull(11) ? null : reader.GetInt32(11)
            });
        }
        return todos;
    }

    /// <summary>
    /// 根据ID获取TODO - 展示参数化查询
    /// </summary>
    public async Task<Todo?> GetByIdAsync(long id)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, title, description, is_completed, priority, due_date,
                   created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
            FROM todos
            WHERE id = @id";
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Todo
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsCompleted = reader.GetBoolean(3),
                Priority = reader.GetInt32(4),
                DueDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                CompletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                Tags = reader.IsDBNull(9) ? null : reader.GetString(9),
                EstimatedMinutes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                ActualMinutes = reader.IsDBNull(11) ? null : reader.GetInt32(11)
            };
        }
        return null;
    }

    /// <summary>
    /// 创建新TODO - 展示INSERT操作
    /// </summary>
    public async Task<long> CreateAsync(Todo todo)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO todos (title, description, is_completed, priority, due_date, created_at, updated_at, tags, estimated_minutes, actual_minutes)
            VALUES (@title, @description, @isCompleted, @priority, @dueDate, @createdAt, @updatedAt, @tags, @estimatedMinutes, @actualMinutes);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@title", todo.Title);
        command.Parameters.AddWithValue("@description", todo.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@isCompleted", todo.IsCompleted);
        command.Parameters.AddWithValue("@priority", todo.Priority);
        command.Parameters.AddWithValue("@dueDate", todo.DueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@tags", todo.Tags ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@estimatedMinutes", todo.EstimatedMinutes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@actualMinutes", todo.ActualMinutes ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    /// <summary>
    /// 更新TODO - 展示UPDATE操作
    /// </summary>
    public async Task<int> UpdateAsync(Todo todo)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE todos
            SET title = @title, description = @description, is_completed = @isCompleted,
                priority = @priority, due_date = @dueDate, updated_at = @updatedAt,
                completed_at = @completedAt, tags = @tags, estimated_minutes = @estimatedMinutes,
                actual_minutes = @actualMinutes
            WHERE id = @id";

        command.Parameters.AddWithValue("@id", todo.Id);
        command.Parameters.AddWithValue("@title", todo.Title);
        command.Parameters.AddWithValue("@description", todo.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@isCompleted", todo.IsCompleted);
        command.Parameters.AddWithValue("@priority", todo.Priority);
        command.Parameters.AddWithValue("@dueDate", todo.DueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@completedAt", todo.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@tags", todo.Tags ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@estimatedMinutes", todo.EstimatedMinutes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@actualMinutes", todo.ActualMinutes ?? (object)DBNull.Value);

        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 删除TODO - 展示DELETE操作
    /// </summary>
    public async Task<int> DeleteAsync(long id)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM todos WHERE id = @id";
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 搜索TODO - 展示LIKE操作
    /// </summary>
    public async Task<List<Todo>> SearchAsync(string searchTerm)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, title, description, is_completed, priority, due_date,
                   created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
            FROM todos
            WHERE title LIKE @search OR description LIKE @search
            ORDER BY updated_at DESC";
        command.Parameters.AddWithValue("@search", $"%{searchTerm}%");

        var todos = new List<Todo>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            todos.Add(new Todo
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsCompleted = reader.GetBoolean(3),
                Priority = reader.GetInt32(4),
                DueDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                CompletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                Tags = reader.IsDBNull(9) ? null : reader.GetString(9),
                EstimatedMinutes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                ActualMinutes = reader.IsDBNull(11) ? null : reader.GetInt32(11)
            });
        }
        return todos;
    }

    /// <summary>
    /// 获取已完成的TODO - 展示条件筛选
    /// </summary>
    public async Task<List<Todo>> GetCompletedAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, title, description, is_completed, priority, due_date,
                   created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
            FROM todos
            WHERE is_completed = 1
            ORDER BY completed_at DESC";

        var todos = new List<Todo>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            todos.Add(new Todo
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsCompleted = reader.GetBoolean(3),
                Priority = reader.GetInt32(4),
                DueDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                CompletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                Tags = reader.IsDBNull(9) ? null : reader.GetString(9),
                EstimatedMinutes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                ActualMinutes = reader.IsDBNull(11) ? null : reader.GetInt32(11)
            });
        }
        return todos;
    }

    /// <summary>
    /// 获取高优先级TODO - 展示BETWEEN操作
    /// </summary>
    public async Task<List<Todo>> GetHighPriorityAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, title, description, is_completed, priority, due_date,
                   created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
            FROM todos
            WHERE priority BETWEEN 3 AND 5
              AND is_completed = 0
            ORDER BY priority DESC";

        var todos = new List<Todo>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            todos.Add(new Todo
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsCompleted = reader.GetBoolean(3),
                Priority = reader.GetInt32(4),
                DueDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                CompletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                Tags = reader.IsDBNull(9) ? null : reader.GetString(9),
                EstimatedMinutes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                ActualMinutes = reader.IsDBNull(11) ? null : reader.GetInt32(11)
            });
        }
        return todos;
    }

    /// <summary>
    /// 按优先级统计 - 展示GROUP BY
    /// </summary>
    public async Task<List<PriorityStats>> GetPriorityStatsAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT priority, COUNT(*) as task_count
            FROM todos
            GROUP BY priority
            ORDER BY priority ASC";

        var stats = new List<PriorityStats>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            stats.Add(new PriorityStats(reader.GetInt32(0), reader.GetInt32(1)));
        }
        return stats;
    }

    /// <summary>
    /// 获取即将到期的TODO - 展示日期函数
    /// </summary>
    public async Task<List<Todo>> GetDueSoonAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, title, description, is_completed, priority, due_date,
                   created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
            FROM todos
            WHERE due_date IS NOT NULL
              AND due_date <= datetime('now', '+7 days')
              AND is_completed = 0
            ORDER BY due_date ASC";

        var todos = new List<Todo>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            todos.Add(new Todo
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsCompleted = reader.GetBoolean(3),
                Priority = reader.GetInt32(4),
                DueDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                CompletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                Tags = reader.IsDBNull(9) ? null : reader.GetString(9),
                EstimatedMinutes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                ActualMinutes = reader.IsDBNull(11) ? null : reader.GetInt32(11)
            });
        }
        return todos;
    }

    /// <summary>
    /// 批量更新优先级 - 展示IN操作
    /// </summary>
    public async Task<int> UpdatePriorityBatchAsync(List<long> ids, int newPriority)
    {
        if (ids?.Count == 0) return 0;

        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();

        var placeholders = string.Join(",", ids.Select((_, i) => $"@id{i}"));
        command.CommandText = $@"
            UPDATE todos
            SET priority = @newPriority, updated_at = datetime('now')
            WHERE id IN ({placeholders})";

        command.Parameters.AddWithValue("@newPriority", newPriority);
        for (int i = 0; i < ids.Count; i++)
        {
            command.Parameters.AddWithValue($"@id{i}", ids[i]);
        }

        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 获取任务总数 - 展示COUNT
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM todos";

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 获取完成率 - 展示复杂聚合
    /// </summary>
    public async Task<CompletionStats> GetCompletionStatsAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT
                COUNT(*) as total_tasks,
                SUM(CASE WHEN is_completed = 1 THEN 1 ELSE 0 END) as completed_tasks,
                ROUND(SUM(CASE WHEN is_completed = 1 THEN 1.0 ELSE 0 END) * 100.0 / COUNT(*), 2) as completion_rate
            FROM todos";

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CompletionStats(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetDecimal(2)
            );
        }
        return new CompletionStats(0, 0, 0);
    }

    /// <summary>
    /// 归档过期任务 - 展示复杂UPDATE
    /// </summary>
    public async Task<int> ArchiveExpiredTasksAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE todos
            SET is_completed = 1,
                completed_at = datetime('now'),
                updated_at = datetime('now')
            WHERE due_date < datetime('now', '-30 days')
              AND is_completed = 0";

        return await command.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// 优先级统计模型
/// </summary>
public record PriorityStats(int Priority, int TaskCount);

/// <summary>
/// 完成统计模型
/// </summary>
public record CompletionStats(int TotalTasks, int CompletedTasks, decimal CompletionRate);

/// <summary>
/// 数据库服务，用于初始化数据库
/// </summary>
public class DatabaseService(SqliteConnection connection)
{
    private readonly SqliteConnection _connection = connection;

    public async Task InitializeDatabaseAsync()
    {
        await _connection.OpenAsync();

        var createTodosTableSql = @"
            CREATE TABLE IF NOT EXISTS todos (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT,
                is_completed INTEGER NOT NULL DEFAULT 0,
                priority INTEGER NOT NULL DEFAULT 2,
                due_date TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                completed_at TEXT,
                tags TEXT,
                estimated_minutes INTEGER,
                actual_minutes INTEGER
            );";

        using var command = _connection.CreateCommand();
        command.CommandText = createTodosTableSql;
        await command.ExecuteNonQueryAsync();

        // 插入一些初始数据
        await SeedDataAsync();
    }

    private async Task SeedDataAsync()
    {
        // 检查是否有数据，如果没有则插入
        using var checkCommand = _connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM todos";
        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

        if (count == 0)
        {
            using var insertCommand = _connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO todos (title, description, is_completed, priority, due_date, estimated_minutes) VALUES
                ('学习 Sqlx ORM', '深入理解源代码生成和AOT特性，掌握零反射设计', 0, 5, datetime('now', '+7 days'), 180),
                ('构建 Vue SPA', '完成现代化前端界面，集成所有API功能', 0, 4, datetime('now', '+14 days'), 240),
                ('编写完整文档', '详细说明所有占位符功能和最佳实践', 0, 3, datetime('now', '+10 days'), 120),
                ('性能优化完成', '成功减少代码量50%，提升编译时性能', 1, 5, datetime('now', '-2 days'), 300),
                ('修复所有警告', '解决StyleCop和编译器警告，提升代码质量', 1, 2, datetime('now', '-5 days'), 90),
                ('多数据库测试', '验证MySQL、PostgreSQL、SQL Server兼容性', 0, 4, datetime('now', '+21 days'), 360),
                ('AOT发布测试', '确保原生AOT编译和运行正常', 0, 5, datetime('now', '+3 days'), 60),
                ('集成测试覆盖', '编写全面的集成测试用例', 0, 3, datetime('now', '+30 days'), 180);
            ";
            await insertCommand.ExecuteNonQueryAsync();
        }
    }
}
