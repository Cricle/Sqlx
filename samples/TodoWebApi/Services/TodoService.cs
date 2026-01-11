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
/// Todo repository implementation using ICrudRepository generic interface.
/// Demonstrates using predefined CRUD operations + custom business methods.
/// </summary>
/// <remarks>
/// Inherits 8 standard methods from ICrudRepository:
/// - GetByIdAsync, GetAllAsync, InsertAsync, UpdateAsync, DeleteAsync
/// - CountAsync, ExistsAsync, BatchInsertAsync
/// Plus 6 custom business-specific methods.
/// </remarks>
[TableName("todos")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITodoRepository))]
public partial class TodoRepository(SqliteConnection connection) : ITodoRepository
{
    // Standard CRUD methods auto-generated from ICrudRepository<Todo, long>
    // Custom business methods auto-generated from interface definitions below

    // Optional: execution monitoring
    partial void OnExecuting(string operationName, global::System.Data.Common.DbCommand command)
    {
        System.Diagnostics.Debug.WriteLine($"[{operationName}] SQL: {command.CommandText}");
    }
}

/// <summary>
/// Todo repository interface - combines ICrudRepository with custom business methods.
/// </summary>
public interface ITodoRepository : ICrudRepository<Todo, long>
{
    // Inherited from ICrudRepository<Todo, long>:
    // - GetByIdAsync(id)
    // - GetAllAsync(limit, offset)
    // - InsertAsync(entity)
    // - UpdateAsync(entity)
    // - DeleteAsync(id)
    // - CountAsync()
    // - ExistsAsync(id)
    // - BatchInsertAsync(entities)

    // Custom business methods:

    /// <summary>Searches todos by title or description.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query {{orderby updated_at --desc}}")]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>Gets todos by completion status.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted {{orderby completed_at --desc}}")]
    Task<List<Todo>> GetByCompletionStatusAsync(bool isCompleted = true);

    /// <summary>Gets high priority todos.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE priority >= @minPriority AND is_completed = @isCompleted {{orderby priority --desc}}")]
    Task<List<Todo>> GetByPriorityAsync(int minPriority = 3, bool isCompleted = false);

    /// <summary>Gets todos due soon.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE due_date IS NOT NULL AND due_date <= @dueDate AND is_completed = @isCompleted {{orderby due_date}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime dueDate, bool isCompleted = false);

    /// <summary>Marks todo as completed.</summary>
    [SqlTemplate("UPDATE {{table}} SET is_completed = 1, completed_at = @completedAt, updated_at = @updatedAt WHERE id = @id")]
    Task<int> MarkAsCompletedAsync(long id, DateTime completedAt, DateTime updatedAt);

    /// <summary>Batch updates priority for multiple todos.</summary>
    [SqlTemplate("UPDATE {{table}} SET priority = @priority, updated_at = @updatedAt WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> BatchUpdatePriorityAsync(string idsJson, int priority, DateTime updatedAt);

    // SqlTemplate return type methods - for debugging and testing
    // These methods return the generated SQL and parameters without executing the query

    /// <summary>Gets the SQL for searching todos (debug mode).</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query {{orderby updated_at --desc}}")]
    SqlTemplate SearchSql(string query);

    /// <summary>Gets the SQL for fetching todos by completion status (debug mode).</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted {{orderby completed_at --desc}}")]
    SqlTemplate GetByCompletionStatusSql(bool isCompleted = true);
}
