using Microsoft.Data.Sqlite;
using TodoWebApi.Models;

namespace TodoWebApi.Services;

/// <summary>
/// 简单的TODO数据访问服务
/// </summary>
public class TodoService(SqliteConnection connection)
{
    private readonly SqliteConnection _connection = connection;

    public async Task<List<Todo>> GetAllAsync()
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Title, Description, IsCompleted, Priority, DueDate,
                   CreatedAt, UpdatedAt, CompletedAt, Tags, EstimatedMinutes, ActualMinutes
            FROM todos
            ORDER BY CreatedAt DESC";

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

    public async Task<Todo?> GetByIdAsync(long id)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Title, Description, IsCompleted, Priority, DueDate,
                   CreatedAt, UpdatedAt, CompletedAt, Tags, EstimatedMinutes, ActualMinutes
            FROM todos
            WHERE Id = @id";
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

    public async Task<long> CreateAsync(Todo todo)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO todos (Title, Description, IsCompleted, Priority, DueDate, CreatedAt, UpdatedAt, Tags, EstimatedMinutes, ActualMinutes)
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

    public async Task<int> UpdateAsync(Todo todo)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE todos
            SET Title = @title, Description = @description, IsCompleted = @isCompleted,
                Priority = @priority, DueDate = @dueDate, UpdatedAt = @updatedAt,
                CompletedAt = @completedAt, Tags = @tags, EstimatedMinutes = @estimatedMinutes,
                ActualMinutes = @actualMinutes
            WHERE Id = @id";

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

    public async Task<int> DeleteAsync(long id)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM todos WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Todo>> SearchAsync(string searchTerm)
    {
        await _connection.OpenAsync();
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Title, Description, IsCompleted, Priority, DueDate,
                   CreatedAt, UpdatedAt, CompletedAt, Tags, EstimatedMinutes, ActualMinutes
            FROM todos
            WHERE Title LIKE @search OR Description LIKE @search
            ORDER BY CreatedAt DESC";
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
}

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
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                Priority INTEGER NOT NULL DEFAULT 2,
                DueDate TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                CompletedAt TEXT,
                Tags TEXT,
                EstimatedMinutes INTEGER,
                ActualMinutes INTEGER
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
                INSERT INTO todos (Title, Description, IsCompleted, Priority, DueDate, CreatedAt, UpdatedAt) VALUES
                ('学习 Sqlx ORM', '深入理解源代码生成和AOT', 0, 4, '2025-10-15 00:00:00', datetime('now'), datetime('now')),
                ('构建 Vue SPA', '完成前端界面和API集成', 0, 3, '2025-10-20 00:00:00', datetime('now'), datetime('now')),
                ('编写 README 文档', '详细说明项目功能和使用方法', 0, 2, '2025-10-10 00:00:00', datetime('now'), datetime('now')),
                ('优化性能', '进一步减少代码量，提升运行时效率', 1, 4, '2025-09-28 00:00:00', datetime('now'), datetime('now')),
                ('修复所有警告', '解决StyleCop和其他编译器警告', 0, 1, '2025-10-05 00:00:00', datetime('now'), datetime('now'));
            ";
            await insertCommand.ExecuteNonQueryAsync();
        }
    }
}
