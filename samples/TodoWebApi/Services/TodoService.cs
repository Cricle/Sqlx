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
/// Demonstrates the power of ICrudRepository - no custom methods needed!
/// </summary>
/// <remarks>
/// <para>
/// Inherits 46 standard methods from ICrudRepository (24 query + 22 command):
/// </para>
/// <para>
/// Query Methods (24):
/// </para>
/// <list type="bullet">
/// <item><description>GetByIdAsync/GetById - Get single entity by ID</description></item>
/// <item><description>GetByIdsAsync/GetByIds - Get multiple entities by IDs</description></item>
/// <item><description>GetAllAsync/GetAll - Get all entities with limit</description></item>
/// <item><description>GetWhereAsync/GetWhere - Get entities matching expression</description></item>
/// <item><description>GetFirstWhereAsync/GetFirstWhere - Get first entity matching expression</description></item>
/// <item><description>GetPagedAsync/GetPaged - Get paginated entities</description></item>
/// <item><description>GetPagedWhereAsync/GetPagedWhere - Get paginated entities matching expression</description></item>
/// <item><description>ExistsByIdAsync/ExistsById - Check if entity exists by ID</description></item>
/// <item><description>ExistsAsync/Exists - Check if any entity matches expression</description></item>
/// <item><description>CountAsync/Count - Count all entities</description></item>
/// <item><description>CountWhereAsync/CountWhere - Count entities matching expression</description></item>
/// <item><description>AsQueryable - Get IQueryable for complex LINQ queries</description></item>
/// </list>
/// <para>
/// Command Methods (22):
/// </para>
/// <list type="bullet">
/// <item><description>InsertAndGetIdAsync/InsertAndGetId - Insert and return generated ID</description></item>
/// <item><description>InsertAsync/Insert - Insert entity</description></item>
/// <item><description>BatchInsertAsync/BatchInsert - Batch insert entities</description></item>
/// <item><description>UpdateAsync/Update - Update entity by ID</description></item>
/// <item><description>UpdateWhereAsync/UpdateWhere - Update entities matching expression</description></item>
/// <item><description>BatchUpdateAsync/BatchUpdate - Batch update entities</description></item>
/// <item><description>DynamicUpdateAsync/DynamicUpdate - Update specific fields by expression</description></item>
/// <item><description>DynamicUpdateWhereAsync/DynamicUpdateWhere - Batch update specific fields by expression</description></item>
/// <item><description>DeleteAsync/Delete - Delete entity by ID</description></item>
/// <item><description>DeleteByIdsAsync/DeleteByIds - Delete multiple entities by IDs</description></item>
/// <item><description>DeleteWhereAsync/DeleteWhere - Delete entities matching expression</description></item>
/// <item><description>DeleteAllAsync/DeleteAll - Delete all entities</description></item>
/// </list>
/// </remarks>
[TableName("todos")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITodoRepository))]
public partial class TodoRepository(SqliteConnection connection) : ITodoRepository
{
    // Generator auto-generates:
    // - private readonly SqliteConnection _connection = connection;
    // - public DbTransaction? Transaction { get; set; }
    // - All 46 interface method implementations

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
/// Todo repository interface - demonstrates ICrudRepository usage.
/// All operations use inherited methods from ICrudRepository - no custom methods needed!
/// </summary>
/// <remarks>
/// <para>
/// Inherited from ICrudRepository (46 methods total):
/// </para>
/// <list type="bullet">
/// <item><description>Query (24): GetByIdAsync/GetById, GetByIdsAsync/GetByIds, GetAllAsync/GetAll, GetWhereAsync/GetWhere, GetFirstWhereAsync/GetFirstWhere, GetPagedAsync/GetPaged, GetPagedWhereAsync/GetPagedWhere, ExistsByIdAsync/ExistsById, ExistsAsync/Exists, CountAsync/Count, CountWhereAsync/CountWhere, AsQueryable</description></item>
/// <item><description>Command (22): InsertAndGetIdAsync/InsertAndGetId, InsertAsync/Insert, BatchInsertAsync/BatchInsert, UpdateAsync/Update, UpdateWhereAsync/UpdateWhere, BatchUpdateAsync/BatchUpdate, DynamicUpdateAsync/DynamicUpdate, DynamicUpdateWhereAsync/DynamicUpdateWhere, DeleteAsync/Delete, DeleteByIdsAsync/DeleteByIds, DeleteWhereAsync/DeleteWhere, DeleteAllAsync/DeleteAll</description></item>
/// </list>
/// <para>
/// Examples of using inherited methods:
/// </para>
/// <code>
/// // ========== Query Examples ==========
/// 
/// // Get by ID
/// var todo = await repo.GetByIdAsync(1);
/// 
/// // Get multiple by IDs
/// var todos = await repo.GetByIdsAsync(new List&lt;long&gt; { 1, 2, 3 });
/// 
/// // Query by expression - completed todos
/// var completed = await repo.GetWhereAsync(t => t.IsCompleted);
/// 
/// // Query by expression - high priority pending todos
/// var highPriority = await repo.GetWhereAsync(t => t.Priority >= 3 &amp;&amp; !t.IsCompleted);
/// 
/// // Query by expression - due soon
/// var dueSoon = await repo.GetWhereAsync(t => 
///     t.DueDate != null &amp;&amp; 
///     t.DueDate <= DateTime.UtcNow.AddDays(7) &amp;&amp; 
///     !t.IsCompleted);
/// 
/// // Count by expression
/// var pendingCount = await repo.CountWhereAsync(t => !t.IsCompleted);
/// 
/// // Check existence
/// var exists = await repo.ExistsAsync(t => t.Title == "Important Task");
/// 
/// // Pagination
/// var page1 = await repo.GetPagedAsync(pageSize: 20, offset: 0);
/// var page2 = await repo.GetPagedWhereAsync(
///     t => !t.IsCompleted, 
///     pageSize: 20, 
///     offset: 20);
/// 
/// // Complex LINQ queries with AsQueryable
/// var query = repo.AsQueryable()
///     .Where(t => !t.IsCompleted)
///     .Where(t => t.Priority >= 3)
///     .OrderByDescending(t => t.Priority)
///     .ThenBy(t => t.DueDate);
/// var results = await query.ToListAsync();
/// 
/// // ========== Update Examples ==========
/// 
/// // Update entire entity
/// todo.Title = "Updated Title";
/// await repo.UpdateAsync(todo);
/// 
/// // Dynamic update - single field by expression
/// await repo.DynamicUpdateAsync(id, t => new Todo 
/// { 
///     IsCompleted = true, 
///     CompletedAt = DateTime.UtcNow,
///     UpdatedAt = DateTime.UtcNow
/// });
/// 
/// // Dynamic update - multiple fields by expression
/// await repo.DynamicUpdateAsync(id, t => new Todo 
/// { 
///     Title = "New Title",
///     Priority = 5,
///     UpdatedAt = DateTime.UtcNow
/// });
/// 
/// // Dynamic update - with expressions (increment, calculate)
/// await repo.DynamicUpdateAsync(id, t => new Todo 
/// { 
///     ActualMinutes = t.ActualMinutes + 30,
///     UpdatedAt = DateTime.UtcNow
/// });
/// 
/// // Batch update - update multiple todos by expression
/// await repo.DynamicUpdateWhereAsync(
///     t => new Todo { Priority = 5, UpdatedAt = DateTime.UtcNow },
///     t => t.Id == 1 || t.Id == 2 || t.Id == 3);
/// 
/// // Batch update - mark all overdue as high priority
/// await repo.DynamicUpdateWhereAsync(
///     t => new Todo { Priority = 5 },
///     t => t.DueDate != null &amp;&amp; t.DueDate &lt; DateTime.UtcNow &amp;&amp; !t.IsCompleted);
/// 
/// // ========== Delete Examples ==========
/// 
/// // Delete by ID
/// await repo.DeleteAsync(1);
/// 
/// // Delete multiple by IDs
/// await repo.DeleteByIdsAsync(new List&lt;long&gt; { 1, 2, 3 });
/// 
/// // Delete by expression
/// await repo.DeleteWhereAsync(t => t.IsCompleted &amp;&amp; t.CompletedAt &lt; DateTime.UtcNow.AddMonths(-6));
/// </code>
/// </remarks>
public interface ITodoRepository : ICrudRepository<Todo, long>
{
    // All standard CRUD operations are inherited from ICrudRepository
    // No custom methods needed for most scenarios!

    // Only add custom methods for complex business logic that cannot be expressed with expressions:
    // - Full-text search across multiple fields
    // - Complex joins with other tables
    // - Stored procedures
    // - Database-specific functions (e.g., PostGIS, JSON operations)
}