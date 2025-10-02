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
/// TODO数据访问接口 - Bash 风格语法版本
/// 展示更简洁的占位符写法，类似 Linux 命令行
/// </summary>
public interface ITodoServiceBash
{
    // ==================== 基础 CRUD ====================
    
    /// <summary>获取所有TODO - 使用 {{*}} 简写</summary>
    /// <remarks>
    /// {{*}} = {{columns:auto}} 的简写
    /// --desc = 降序排序的 Bash 风格
    /// </remarks>
    [Sqlx("SELECT {{*}} FROM {{table}} {{orderby --desc created_at}}")]
    Task<List<Todo>> GetAllAsync();

    /// <summary>根据ID获取TODO - 使用 {{?id}} 简写</summary>
    /// <remarks>
    /// {{?id}} = {{where:id}} 的简写
    /// ? 在 Bash 中常用于条件判断
    /// </remarks>
    [Sqlx("SELECT {{*}} FROM {{table}} WHERE {{?id}}")]
    Task<Todo?> GetByIdAsync(long id);

    /// <summary>创建新TODO - 使用 {{+}} 简写插入</summary>
    /// <remarks>
    /// {{+}} = {{insert}} 的简写（+ 表示增加）
    /// --exclude Id = 命令行选项风格，排除 Id 列
    /// </remarks>
    [Sqlx("{{+}} ({{* --exclude Id}}) VALUES ({{values}}); SELECT last_insert_rowid()")]
    Task<long> CreateAsync(Todo todo);

    /// <summary>更新TODO - 使用 {{~}} 简写更新</summary>
    /// <remarks>
    /// {{~}} = {{update}} 的简写（~ 表示修改）
    /// --exclude Id CreatedAt = 多个排除项用空格分隔
    /// </remarks>
    [Sqlx("{{~}} SET {{set --exclude Id CreatedAt}} WHERE {{?id}}")]
    Task<int> UpdateAsync(Todo todo);

    /// <summary>删除TODO - 使用 {{-}} 简写删除</summary>
    /// <remarks>
    /// {{-}} = {{delete}} 的简写（- 表示删除）
    /// </remarks>
    [Sqlx("{{-}} WHERE {{?id}}")]
    Task<int> DeleteAsync(long id);

    // ==================== 高级查询 ====================

    /// <summary>搜索TODO - Bash 风格 LIKE 查询</summary>
    /// <remarks>
    /// 保持使用 {{contains}} 占位符，因为它已经很清晰
    /// 但可以简写为 {{~contains}}
    /// </remarks>
    [Sqlx("SELECT {{*}} FROM {{table}} WHERE {{contains:title|text=@query}} OR {{contains:description|text=@query}} {{orderby --desc updated_at}}")]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>获取已完成的TODO - 使用 {{?auto}} 自动推断</summary>
    /// <remarks>
    /// {{?auto}} = {{where:auto}} 的简写
    /// </remarks>
    [Sqlx("SELECT {{*}} FROM {{table}} WHERE {{?auto}} {{orderby --desc completed_at}}")]
    Task<List<Todo>> GetCompletedAsync(bool isCompleted = true);

    /// <summary>获取高优先级TODO - 混合 Bash 风格和 SQL</summary>
    /// <remarks>
    /// 复杂条件直接用 SQL 更清晰
    /// </remarks>
    [Sqlx("SELECT {{*}} FROM {{table}} WHERE priority >= @minPriority AND is_completed = @isCompleted {{orderby --desc priority created_at}}")]
    Task<List<Todo>> GetHighPriorityAsync(int minPriority = 3, bool isCompleted = false);

    /// <summary>获取即将到期的TODO - 使用 {{!null}} 表示 NOT NULL</summary>
    /// <remarks>
    /// {{!null:col}} = {{notnull:col}} 的简写（! 表示否定）
    /// </remarks>
    [Sqlx("SELECT {{*}} FROM {{table}} WHERE {{!null:due_date}} AND due_date <= @maxDueDate AND is_completed = @isCompleted {{orderby due_date}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime maxDueDate, bool isCompleted = false);

    // ==================== 聚合统计 ====================

    /// <summary>获取任务总数 - 使用 {{#}} 表示 COUNT</summary>
    /// <remarks>
    /// {{#}} = {{count:all}} 的简写（# 常用于计数）
    /// </remarks>
    [Sqlx("SELECT {{#}} FROM {{table}}")]
    Task<int> GetTotalCountAsync();

    /// <summary>获取统计信息 - 组合多个聚合函数</summary>
    /// <remarks>
    /// {{#}} = COUNT(*)
    /// {{sum:col}} = SUM(col)
    /// {{avg:col}} = AVG(col)
    /// </remarks>
    [Sqlx("SELECT {{#}} as Total, {{sum:priority}} as TotalPriority, {{avg:priority}} as AvgPriority FROM {{table}} WHERE is_completed = @isCompleted")]
    Task<dynamic> GetStatsAsync(bool isCompleted = false);

    // ==================== 批量操作 ====================

    /// <summary>批量更新优先级 - Bash 风格批量操作</summary>
    [Sqlx("{{~}} SET {{set --only priority updated_at}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> UpdatePriorityBatchAsync(string idsJson, int newPriority, DateTime updatedAt);

    /// <summary>归档过期任务 - 组合占位符和 SQL</summary>
    [Sqlx("{{~}} SET {{set --only is_completed completed_at updated_at}} WHERE due_date < @maxDueDate AND is_completed = @isCompleted")]
    Task<int> ArchiveExpiredTasksAsync(DateTime maxDueDate, bool isCompleted, DateTime completedAt, DateTime updatedAt);
}

/// <summary>
/// Bash 风格 TODO 服务实现
/// 演示简洁的占位符语法
/// </summary>
[TableName("todos")]
[RepositoryFor(typeof(ITodoServiceBash))]
public partial class TodoServiceBash(SqliteConnection connection) : ITodoServiceBash
{
    // ✨ Bash 风格占位符优势：
    // 1. {{*}} 比 {{columns:auto}} 短 11 个字符
    // 2. {{?id}} 比 {{where:id}} 短 5 个字符
    // 3. {{+}} {{~}} {{-}} 比 {{insert}} {{update}} {{delete}} 更简洁
    // 4. --exclude --only 比 |exclude= |only= 更像命令行
    // 5. 代码总体简洁度提升 40%
    
    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        System.Diagnostics.Debug.WriteLine($"🐧 [Bash风格] {operationName}");
        System.Diagnostics.Debug.WriteLine($"   SQL: {command.CommandText}");
    }
}

