using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TodoWebApi.Models;
using Sqlx;
using Sqlx.Annotations;

namespace TodoWebApi.Services;

/// <summary>
/// TODO数据访问接口 - 完全使用Sqlx功能，展示真实的模板引擎能力
/// </summary>
public interface ITodoService
{
    /// <summary>获取所有TODO - 使用SqlTemplate</summary>
    [SqlTemplate("SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todos ORDER BY created_at DESC", Dialect = SqlDefineTypes.SQLite)]
    Task<List<Todo>> GetAllAsync();

    /// <summary>根据ID获取TODO - 使用SqlTemplate参数化查询</summary>
    [SqlTemplate("SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todos WHERE id = @id", Dialect = SqlDefineTypes.SQLite)]
    Task<Todo?> GetByIdAsync(long id);

    /// <summary>创建新TODO - 使用SqlTemplate INSERT</summary>
    [SqlTemplate("INSERT INTO todos (title, description, is_completed, priority, due_date, created_at, updated_at, tags, estimated_minutes, actual_minutes) VALUES (@Title, @Description, @IsCompleted, @Priority, @DueDate, @CreatedAt, @UpdatedAt, @Tags, @EstimatedMinutes, @ActualMinutes); SELECT last_insert_rowid()", Dialect = SqlDefineTypes.SQLite, Operation = SqlOperation.Insert)]
    Task<long> CreateAsync(Todo todo);

    /// <summary>更新TODO - 使用SqlTemplate UPDATE</summary>
    [SqlTemplate("UPDATE todos SET title = @Title, description = @Description, is_completed = @IsCompleted, priority = @Priority, due_date = @DueDate, updated_at = @UpdatedAt, completed_at = @CompletedAt, tags = @Tags, estimated_minutes = @EstimatedMinutes, actual_minutes = @ActualMinutes WHERE id = @Id", Dialect = SqlDefineTypes.SQLite, Operation = SqlOperation.Update)]
    Task<int> UpdateAsync(Todo todo);

    /// <summary>删除TODO - 使用SqlTemplate DELETE</summary>
    [SqlTemplate("DELETE FROM todos WHERE id = @id", Dialect = SqlDefineTypes.SQLite, Operation = SqlOperation.Delete)]
    Task<int> DeleteAsync(long id);

    /// <summary>搜索TODO - 使用SqlTemplate LIKE查询</summary>
    [SqlTemplate("SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todos WHERE title LIKE '%' || @query || '%' OR description LIKE '%' || @query || '%' ORDER BY updated_at DESC", Dialect = SqlDefineTypes.SQLite)]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>获取已完成的TODO - 使用SqlTemplate WHERE过滤</summary>
    [SqlTemplate("SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todos WHERE is_completed = 1 ORDER BY completed_at DESC", Dialect = SqlDefineTypes.SQLite)]
    Task<List<Todo>> GetCompletedAsync();

    /// <summary>获取高优先级TODO - 使用SqlTemplate条件查询</summary>
    [SqlTemplate("SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todos WHERE priority >= 3 AND is_completed = 0 ORDER BY priority DESC, created_at DESC", Dialect = SqlDefineTypes.SQLite)]
    Task<List<Todo>> GetHighPriorityAsync();

    /// <summary>获取即将到期的TODO - 使用SqlTemplate日期比较</summary>
    [SqlTemplate("SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todos WHERE due_date IS NOT NULL AND due_date <= datetime('now', '+7 days') AND is_completed = 0 ORDER BY due_date ASC", Dialect = SqlDefineTypes.SQLite)]
    Task<List<Todo>> GetDueSoonAsync();

    /// <summary>获取任务总数 - 使用SqlTemplate聚合函数</summary>
    [SqlTemplate("SELECT COUNT(*) FROM todos", Dialect = SqlDefineTypes.SQLite)]
    Task<int> GetTotalCountAsync();

    /// <summary>批量更新优先级 - 使用SqlTemplate复杂UPDATE和JSON</summary>
    [SqlTemplate("UPDATE todos SET priority = @newPriority, updated_at = datetime('now') WHERE id IN (SELECT value FROM json_each(@ids))", Dialect = SqlDefineTypes.SQLite, Operation = SqlOperation.Update)]
    Task<int> UpdatePriorityBatchAsync(string ids, int newPriority);

    /// <summary>归档过期任务 - 使用SqlTemplate批量UPDATE</summary>
    [SqlTemplate("UPDATE todos SET is_completed = 1, completed_at = datetime('now'), updated_at = datetime('now') WHERE due_date < datetime('now') AND is_completed = 0", Dialect = SqlDefineTypes.SQLite, Operation = SqlOperation.Update)]
    Task<int> ArchiveExpiredTasksAsync();
}

/// <summary>
/// TODO数据访问服务实现 - 由Sqlx源代码生成器自动生成方法实现
/// 无需手写任何ADO.NET代码！
/// </summary>
[TableName("todos")]
[RepositoryFor(typeof(ITodoService))]
public partial class TodoService(SqliteConnection connection) : ITodoService
{
    // 所有方法实现由Sqlx源代码生成器自动生成
    // 编译时生成 TodoService.Repository.g.cs 文件
}
