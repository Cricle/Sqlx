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
/// TODO数据访问仓储接口 - 展示Sqlx的标准CRUD模式
/// </summary>
/// <remarks>
/// 💡 **设计模式**：
/// - 基础CRUD操作（GetById, GetAll, Insert, Update, Delete）
/// - 业务特定查询方法
/// - 所有SQL明确列出列名（不使用SELECT *）
/// - 参数化查询防止SQL注入
/// 
/// 🔍 **提示**：
/// - 可以参考 ICrudRepository&lt;TEntity, TKey&gt; 作为标准模板
/// - Sqlx支持接口继承（实验性功能）
/// </remarks>
public interface ITodoRepository
{
    // ✅ 标准CRUD操作
    
    /// <summary>根据ID获取TODO</summary>
    [SqlxAttribute("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Todo?> GetByIdAsync(long id);

    /// <summary>获取所有TODO（分页）</summary>
    [SqlxAttribute("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}} {{limit --param limit}} {{offset --param offset}}")]
    Task<List<Todo>> GetAllAsync(int limit = 100, int offset = 0);

    /// <summary>插入新TODO</summary>
    [SqlxAttribute("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    Task<int> InsertAsync(Todo todo);

    /// <summary>更新TODO</summary>
    [SqlxAttribute("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(Todo todo);

    /// <summary>删除TODO</summary>
    [SqlxAttribute("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    /// <summary>获取TODO总数</summary>
    [SqlxAttribute("SELECT COUNT(*) FROM {{table}}")]
    Task<int> CountAsync();

    /// <summary>检查TODO是否存在</summary>
    [SqlxAttribute("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE id = @id) THEN 1 ELSE 0 END")]
    Task<bool> ExistsAsync(long id);

    // ✅ 业务特定查询

    /// <summary>搜索TODO - 按标题或描述搜索</summary>
    /// <remarks>
    /// 📝 **生成的SQL**：
    /// <code>
    /// SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at
    /// FROM todos 
    /// WHERE title LIKE @query OR description LIKE @query 
    /// ORDER BY updated_at DESC
    /// </code>
    /// </remarks>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query {{orderby updated_at --desc}}")]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>获取已完成的TODO</summary>
    /// <remarks>
    /// 📝 **生成的SQL**：
    /// <code>
    /// SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at
    /// FROM todos 
    /// WHERE is_completed = @isCompleted 
    /// ORDER BY completed_at DESC
    /// </code>
    /// </remarks>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted {{orderby completed_at --desc}}")]
    Task<List<Todo>> GetByCompletionStatusAsync(bool isCompleted = true);

    /// <summary>获取高优先级TODO</summary>
    /// <remarks>
    /// 📝 **生成的SQL**：
    /// <code>
    /// SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at
    /// FROM todos 
    /// WHERE priority >= @minPriority AND is_completed = @isCompleted 
    /// ORDER BY priority DESC, created_at DESC
    /// </code>
    /// </remarks>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE priority >= @minPriority AND is_completed = @isCompleted {{orderby priority --desc}} {{orderby created_at --desc}}")]
    Task<List<Todo>> GetByPriorityAsync(int minPriority = 3, bool isCompleted = false);

    /// <summary>获取即将到期的TODO</summary>
    /// <remarks>
    /// 📝 **生成的SQL**：
    /// <code>
    /// SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at
    /// FROM todos 
    /// WHERE due_date IS NOT NULL AND due_date <= @dueDate AND is_completed = @isCompleted 
    /// ORDER BY due_date ASC
    /// </code>
    /// </remarks>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE due_date IS NOT NULL AND due_date <= @dueDate AND is_completed = @isCompleted {{orderby due_date}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime dueDate, bool isCompleted = false);

    /// <summary>标记任务为已完成</summary>
    /// <remarks>
    /// 📝 **生成的SQL**：
    /// <code>
    /// UPDATE todos 
    /// SET is_completed = 1, completed_at = @completedAt, updated_at = @updatedAt 
    /// WHERE id = @id
    /// </code>
    /// </remarks>
    [Sqlx("UPDATE {{table}} SET is_completed = 1, completed_at = @completedAt, updated_at = @updatedAt WHERE id = @id")]
    Task<int> MarkAsCompletedAsync(long id, DateTime completedAt, DateTime updatedAt);

    /// <summary>批量更新优先级</summary>
    /// <remarks>
    /// 📝 **生成的SQL（SQLite）**：
    /// <code>
    /// UPDATE todos 
    /// SET priority = @priority, updated_at = @updatedAt 
    /// WHERE id IN (SELECT value FROM json_each(@idsJson))
    /// </code>
    /// </remarks>
    [Sqlx("UPDATE {{table}} SET priority = @priority, updated_at = @updatedAt WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> BatchUpdatePriorityAsync(string idsJson, int priority, DateTime updatedAt);
}

/// <summary>
/// TODO数据访问仓储实现
/// 展示如何使用通用CRUD接口 + 自定义方法
/// </summary>
/// <remarks>
/// 🎯 **实现方式**：
/// - 通过 [RepositoryFor] 特性，Sqlx源代码生成器会自动生成所有方法实现
/// - 生成的代码位于编译输出的 TodoRepository.Repository.g.cs 文件中
/// - 包括从 ICrudRepository 继承的8个标准方法
/// - 包括自定义的6个业务方法
/// 
/// ✨ **生成的代码特点**：
/// - 编译时生成（零反射）
/// - 类型安全
/// - 接近原生ADO.NET性能
/// - 完整的参数化查询
/// - 明确的列名（不使用SELECT *）
/// </remarks>
[TableName("todos")]  // 指定表名
[SqlDefine(SqlDefineTypes.SQLite)]  // 指定 SQL 方言
[RepositoryFor(typeof(ITodoRepository))]  // 指定要实现的接口
public partial class TodoRepository(SqliteConnection connection) : ITodoRepository
{
    // ✅ 以下方法由Sqlx自动生成（从 ICrudRepository 继承）：
    // - GetByIdAsync          : SELECT id, title, ... FROM todos WHERE id = @id
    // - GetAllAsync           : SELECT id, title, ... FROM todos ORDER BY id LIMIT @limit OFFSET @offset
    // - InsertAsync           : INSERT INTO todos (title, description, ...) VALUES (@title, @description, ...)
    // - UpdateAsync           : UPDATE todos SET title = @title, ... WHERE id = @id
    // - DeleteAsync           : DELETE FROM todos WHERE id = @id
    // - CountAsync            : SELECT COUNT(*) FROM todos
    // - ExistsAsync           : SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END
    // - BatchInsertAsync      : INSERT INTO todos (...) VALUES (...), (...), (...)
    
    // ✅ 以下自定义方法也由Sqlx自动生成：
    // - SearchAsync
    // - GetByCompletionStatusAsync
    // - GetByPriorityAsync
    // - GetDueSoonAsync
    // - MarkAsCompletedAsync
    // - BatchUpdatePriorityAsync

    // 📝 所有方法实现在编译时自动生成，无需手写ADO.NET代码

    // 可选：添加执行监控
    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        // 调试时可以查看生成的SQL和参数
        System.Diagnostics.Debug.WriteLine($"🔄 [{operationName}] SQL: {command.CommandText}");
    }
}
