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
/// TODO数据访问接口 - 展示Sqlx统一友好语法
/// 特点：清晰命名、默认简化、空格分隔、命令行选项、灵活混用
/// </summary>
public interface ITodoService
{
    /// <summary>获取所有TODO - 自动生成列名和排序</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
    Task<List<Todo>> GetAllAsync();

    /// <summary>根据ID获取TODO - WHERE 表达式</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where id=@id}}")]
    Task<Todo?> GetByIdAsync(long id);

    /// <summary>创建新TODO - 使用 {{insert into}} 和 --exclude 选项</summary>
    [Sqlx("{{insert into}} ({{columns --exclude Id}}) VALUES ({{values}}); SELECT last_insert_rowid()")]
    Task<long> CreateAsync(Todo todo);

    /// <summary>更新TODO - 使用 {{update}} 和 {{set}} 占位符</summary>
    [Sqlx("{{update}} SET {{set --exclude Id CreatedAt}} WHERE {{where id=@id}}")]
    Task<int> UpdateAsync(Todo todo);

    /// <summary>删除TODO - 使用 {{delete from}} 占位符</summary>
    [Sqlx("{{delete from}} WHERE {{where id=@id}}")]
    Task<int> DeleteAsync(long id);

    /// <summary>搜索TODO - WHERE 表达式组合（OR）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where title LIKE @query}} OR {{where description LIKE @query}} {{orderby updated_at --desc}}")]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>获取已完成的TODO - WHERE 表达式（等值查询）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_completed=@isCompleted}} {{orderby completed_at --desc}}")]
    Task<List<Todo>> GetCompletedAsync(bool isCompleted = true);

    /// <summary>获取高优先级TODO - WHERE 表达式（多条件 AND）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where priority>=@minPriority}} AND {{where is_completed=@isCompleted}} {{orderby priority --desc}}")]
    Task<List<Todo>> GetHighPriorityAsync(int minPriority = 3, bool isCompleted = false);

    /// <summary>获取即将到期的TODO - WHERE 表达式（NULL 检查 + 比较）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where due_date IS NOT NULL}} AND {{where due_date<=@maxDueDate}} AND {{where is_completed=@isCompleted}} {{orderby due_date}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime maxDueDate, bool isCompleted = false);

    /// <summary>获取任务总数 - 使用 {{count}} 聚合函数</summary>
    [Sqlx("SELECT {{count}} FROM {{table}}")]
    Task<int> GetTotalCountAsync();

    /// <summary>批量更新优先级 - {{set}} 指定列 + 复杂 WHERE</summary>
    [Sqlx("{{update}} SET {{set --only priority updated_at}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> UpdatePriorityBatchAsync(string idsJson, int newPriority, DateTime updatedAt);

    /// <summary>归档过期任务 - {{set}} 指定列 + WHERE 表达式</summary>
    [Sqlx("{{update}} SET {{set --only is_completed completed_at updated_at}} WHERE {{where due_date<@maxDueDate}} AND {{where is_completed=@isCompleted}}")]
    Task<int> ArchiveExpiredTasksAsync(DateTime maxDueDate, bool isCompleted, DateTime completedAt, DateTime updatedAt);
}

/// <summary>
/// TODO数据访问服务实现
/// 通过[RepositoryFor]特性，Sqlx源代码生成器会自动生成所有方法实现
/// 生成的代码位于编译输出的 TodoService.Repository.g.cs 文件中
/// </summary>
[TableName("todos")]  // 指定表名
[RepositoryFor(typeof(ITodoService))]  // 指定要实现的接口
public partial class TodoService(SqliteConnection connection) : ITodoService
{
    // ✨ 所有方法实现由Sqlx源代码生成器在编译时自动生成
    // 🔥 占位符在编译时被替换为实际的SQL语句
    // 📝 生成的代码包含参数绑定、结果映射等所有ADO.NET操作

    // 可选：添加执行监控
    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        // 调试时可以查看生成的SQL
        System.Diagnostics.Debug.WriteLine($"🔄 [{operationName}] {command.CommandText}");
    }
}
