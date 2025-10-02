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
/// TODO数据访问接口 - 充分展示Sqlx模板占位符的强大能力
/// 使用{{columns}}、{{where}}、{{set}}等占位符，无需手写SQL列名
/// </summary>
public interface ITodoService
{
    /// <summary>获取所有TODO - 使用{{columns:auto}}和{{orderby}}</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} {{orderby:created_at_desc}}")]
    Task<List<Todo>> GetAllAsync();

    /// <summary>根据ID获取TODO - 使用{{where:id}}自动生成WHERE条件</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
    Task<Todo?> GetByIdAsync(long id);

    /// <summary>创建新TODO - 使用{{insert}}占位符简化INSERT语句</summary>
    [Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}}); SELECT last_insert_rowid()")]
    Task<long> CreateAsync(Todo todo);

    /// <summary>更新TODO - 使用{{set:auto}}自动生成SET子句</summary>
    [Sqlx("UPDATE {{table}} SET {{set:auto|exclude=Id,CreatedAt}} WHERE {{where:id}}")]
    Task<int> UpdateAsync(Todo todo);

    /// <summary>删除TODO - 使用{{where:id}}</summary>
    [Sqlx("DELETE FROM {{table}} WHERE {{where:id}}")]
    Task<int> DeleteAsync(long id);

    /// <summary>搜索TODO - 使用{{contains}}占位符进行LIKE查询（title OR description）</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{contains:title|text=@query}} OR {{contains:description|text=@query}} {{orderby:updated_at_desc}}")]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>获取已完成的TODO - 使用{{where:auto}}自动推断条件</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:auto}} {{orderby:completed_at_desc}}")]
    Task<List<Todo>> GetCompletedAsync(bool isCompleted = true);

    /// <summary>获取高优先级TODO - 混合使用占位符和SQL表达式</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE priority >= @minPriority AND is_completed = @isCompleted {{orderby:priority_desc,created_at_desc}}")]
    Task<List<Todo>> GetHighPriorityAsync(int minPriority = 3, bool isCompleted = false);

    /// <summary>获取即将到期的TODO - 混合占位符{{notnull}}和SQL表达式</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{notnull:due_date}} AND due_date <= @maxDueDate AND is_completed = @isCompleted {{orderby:due_date_asc}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime maxDueDate, bool isCompleted = false);

    /// <summary>获取任务总数 - 使用{{count:all}}聚合占位符</summary>
    [Sqlx("SELECT {{count:all}} FROM {{table}}")]
    Task<int> GetTotalCountAsync();

    /// <summary>批量更新优先级 - {{update}}+{{set}}占位符，WHERE使用JSON数组</summary>
    [Sqlx("{{update}} SET {{set:priority,updated_at}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> UpdatePriorityBatchAsync(string idsJson, int newPriority, DateTime updatedAt);

    /// <summary>归档过期任务 - {{update}}+{{set}}占位符，WHERE混合SQL表达式</summary>
    [Sqlx("{{update}} SET {{set:is_completed,completed_at,updated_at}} WHERE due_date < @maxDueDate AND is_completed = @isCompleted")]
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
