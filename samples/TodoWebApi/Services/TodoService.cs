using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TodoWebApi.Models;
using Sqlx;
using Sqlx.Annotations;

namespace TodoWebApi.Services;

/// <summary>
/// Todo repository implementation using ICrudRepository generic interface.
/// Demonstrates three query approaches: SqlTemplate, LINQ expressions, and IQueryable.
/// </summary>
/// <remarks>
/// Inherits 42 standard methods from ICrudRepository (24 query + 18 command):
/// Query: GetByIdAsync/GetById, GetByIdsAsync/GetByIds, GetAllAsync/GetAll, 
///        GetWhereAsync/GetWhere, GetFirstWhereAsync/GetFirstWhere,
///        GetPagedAsync/GetPaged, GetPagedWhereAsync/GetPagedWhere,
///        ExistsByIdAsync/ExistsById, ExistsAsync/Exists,
///        CountAsync/Count, CountWhereAsync/CountWhere
/// Command: InsertAndGetIdAsync/InsertAndGetId, InsertAsync/Insert,
///          BatchInsertAsync/BatchInsert, UpdateAsync/Update,
///          UpdateWhereAsync/UpdateWhere, BatchUpdateAsync/BatchUpdate,
///          DeleteAsync/Delete, DeleteByIdsAsync/DeleteByIds,
///          DeleteWhereAsync/DeleteWhere, DeleteAllAsync/DeleteAll
/// Plus 12 custom business-specific methods demonstrating different query approaches.
/// </remarks>
[TableName("todos")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITodoRepository))]
public partial class TodoRepository(SqliteConnection connection) : ITodoRepository
{
    private readonly SqliteConnection _connection = connection;
    public System.Data.Common.DbTransaction? Transaction { get; set; }

    /// <summary>
    /// Returns an IQueryable for building complex LINQ queries.
    /// </summary>
    public SqlxQueryable<Todo> AsQueryable()
    {
        var query = SqlQuery<Todo>.ForSqlite();
        return (SqlxQueryable<Todo>)query.WithConnection(_connection);
    }

    // Standard CRUD methods auto-generated from ICrudRepository<Todo, long>
    // Custom business methods auto-generated from interface definitions below

#if !SQLX_DISABLE_INTERCEPTOR
    // Optional: execution monitoring
    partial void OnExecuting(string operationName, global::System.Data.Common.DbCommand command, global::Sqlx.SqlTemplate template)
    {
        System.Diagnostics.Debug.WriteLine($"[{operationName}] SQL: {command.CommandText}");
    }

    partial void OnExecuted(string operationName, global::System.Data.Common.DbCommand command, global::Sqlx.SqlTemplate template, object? result, long elapsedTicks)
    {
        var ms = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        System.Diagnostics.Debug.WriteLine($"[{operationName}] Completed in {ms:F2}ms");
    }

    partial void OnExecuteFail(string operationName, global::System.Data.Common.DbCommand command, global::Sqlx.SqlTemplate template, Exception exception, long elapsedTicks)
    {
        System.Diagnostics.Debug.WriteLine($"[{operationName}] Failed: {exception.Message}");
    }
#endif
}

/// <summary>
/// Todo repository interface - combines ICrudRepository with custom business methods.
/// Demonstrates three query approaches: SqlTemplate, LINQ expressions, and IQueryable.
/// </summary>
public interface ITodoRepository : ICrudRepository<Todo, long>
{
    // Inherited from ICrudRepository<Todo, long> (42 methods):
    // Query (24): GetByIdAsync/GetById, GetByIdsAsync/GetByIds, GetAllAsync/GetAll,
    //             GetWhereAsync/GetWhere, GetFirstWhereAsync/GetFirstWhere,
    //             GetPagedAsync/GetPaged, GetPagedWhereAsync/GetPagedWhere,
    //             ExistsByIdAsync/ExistsById, ExistsAsync/Exists,
    //             CountAsync/Count, CountWhereAsync/CountWhere
    // Command (18): InsertAndGetIdAsync/InsertAndGetId, InsertAsync/Insert,
    //               BatchInsertAsync/BatchInsert, UpdateAsync/Update,
    //               UpdateWhereAsync/UpdateWhere, BatchUpdateAsync/BatchUpdate,
    //               DeleteAsync/Delete, DeleteByIdsAsync/DeleteByIds,
    //               DeleteWhereAsync/DeleteWhere, DeleteAllAsync/DeleteAll

    // ========== Approach 1: SqlTemplate - Direct SQL with placeholders ==========

    /// <summary>Searches todos by title or description using SqlTemplate.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query ORDER BY updated_at DESC")]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>Gets todos by completion status using SqlTemplate.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted ORDER BY completed_at DESC")]
    Task<List<Todo>> GetByCompletionStatusAsync(bool isCompleted = true);

    /// <summary>Gets high priority todos using SqlTemplate.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE priority >= @minPriority AND is_completed = @isCompleted ORDER BY priority DESC")]
    Task<List<Todo>> GetByPriorityAsync(int minPriority = 3, bool isCompleted = false);

    /// <summary>Gets todos due soon using SqlTemplate.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE due_date IS NOT NULL AND due_date <= @dueDate AND is_completed = @isCompleted ORDER BY due_date")]
    Task<List<Todo>> GetDueSoonAsync(DateTime dueDate, bool isCompleted = false);

    /// <summary>Marks todo as completed using SqlTemplate.</summary>
    [SqlTemplate("UPDATE {{table}} SET is_completed = 1, completed_at = @completedAt, updated_at = @updatedAt WHERE id = @id")]
    Task<int> MarkAsCompletedAsync(long id, DateTime completedAt, DateTime updatedAt);

    /// <summary>Batch updates priority for multiple todos using SqlTemplate.</summary>
    [SqlTemplate("UPDATE {{table}} SET priority = @priority, updated_at = @updatedAt WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> BatchUpdatePriorityAsync(string idsJson, int priority, DateTime updatedAt);

    /// <summary>Batch completes multiple todos using SqlTemplate.</summary>
    [SqlTemplate("UPDATE {{table}} SET is_completed = 1, completed_at = @completedAt, updated_at = @updatedAt WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> BatchCompleteAsync(string idsJson, DateTime completedAt, DateTime updatedAt);

    /// <summary>Updates actual minutes for a todo using SqlTemplate.</summary>
    [SqlTemplate("UPDATE {{table}} SET actual_minutes = @actualMinutes, updated_at = @updatedAt WHERE id = @id")]
    Task<int> UpdateActualMinutesAsync(long id, int actualMinutes, DateTime updatedAt);

    // ========== Approach 2: LINQ Expression - Type-safe predicates ==========

    /// <summary>Gets todos matching a LINQ expression predicate.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
    Task<List<Todo>> GetWhereAsync(System.Linq.Expressions.Expression<Func<Todo, bool>> predicate);

    /// <summary>Counts todos matching a LINQ expression predicate.</summary>
    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE {{where --param predicate}}")]
    Task<long> CountWhereAsync(System.Linq.Expressions.Expression<Func<Todo, bool>> predicate);

    // ========== Approach 3: IQueryable - Full LINQ query builder ==========

    /// <summary>
    /// Gets an IQueryable for building complex queries with LINQ.
    /// Example usage:
    /// <code>
    /// var query = repo.AsQueryable()
    ///     .Where(t => t.Priority >= 3 && !t.IsCompleted)
    ///     .OrderByDescending(t => t.Priority)
    ///     .ThenBy(t => t.DueDate)
    ///     .Take(10);
    /// var todos = await query.ToListAsync();
    /// </code>
    /// </summary>
    SqlxQueryable<Todo> AsQueryable();

    // ========== Debug/Testing Methods - Return SqlTemplate ==========

    /// <summary>Gets the SQL for searching todos (debug mode).</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query ORDER BY updated_at DESC")]
    SqlTemplate SearchSql(string query);

    /// <summary>Gets the SQL for fetching todos by completion status (debug mode).</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted ORDER BY completed_at DESC")]
    SqlTemplate GetByCompletionStatusSql(bool isCompleted = true);
}
