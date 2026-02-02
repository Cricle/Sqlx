// <copyright file="SetExpressionExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Samples;

/// <summary>
/// 演示使用 Expression&lt;Func&lt;T, T&gt;&gt; 进行类型安全的动态更新。
/// 使用 {{set --param}} 占位符配合表达式树，实现灵活且类型安全的部分更新。
/// </summary>
public class SetExpressionExample
{
    public static async Task Main()
    {
        // 创建内存数据库
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 创建表
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE tasks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT NOT NULL,
                    description TEXT,
                    priority INTEGER DEFAULT 0,
                    is_completed INTEGER DEFAULT 0,
                    actual_minutes INTEGER DEFAULT 0,
                    version INTEGER DEFAULT 1,
                    updated_at TEXT
                )";
            await cmd.ExecuteNonQueryAsync();
        }

        // 创建仓储
        var repo = new TaskRepository(connection);

        // 插入测试数据
        var taskId = await repo.InsertAndGetIdAsync(new Task
        {
            Title = "完成项目文档",
            Description = "编写 API 文档和使用指南",
            Priority = 3,
            Version = 1
        });

        Console.WriteLine($"创建任务 ID: {taskId}");
        Console.WriteLine();

        // ========== 示例 1: 更新单个字段（类型安全） ==========
        Console.WriteLine("示例 1: 更新优先级（类型安全）");
        Expression<Func<Task, Task>> update1 = t => new Task { Priority = 5 };
        var setClause1 = update1.ToSetClause();
        Console.WriteLine($"表达式: t => new Task {{ Priority = 5 }}");
        Console.WriteLine($"生成 SET: {setClause1}");
        // 实际使用时会传递给 DynamicUpdateAsync
        Console.WriteLine();

        // ========== 示例 2: 递增表达式 ==========
        Console.WriteLine("示例 2: 版本号递增");
        Expression<Func<Task, Task>> update2 = t => new Task { Version = t.Version + 1 };
        var setClause2 = update2.ToSetClause();
        Console.WriteLine($"表达式: t => new Task {{ Version = t.Version + 1 }}");
        Console.WriteLine($"生成 SET: {setClause2}");
        Console.WriteLine();

        // ========== 示例 3: 多字段更新 ==========
        Console.WriteLine("示例 3: 更新多个字段");
        Expression<Func<Task, Task>> update3 = t => new Task 
        { 
            Title = "更新后的标题",
            Priority = 4,
            Version = t.Version + 1
        };
        var setClause3 = update3.ToSetClause();
        Console.WriteLine($"表达式: t => new Task {{ Title = ..., Priority = 4, Version = t.Version + 1 }}");
        Console.WriteLine($"生成 SET: {setClause3}");
        Console.WriteLine();

        // ========== 示例 4: 条件更新（标记完成） ==========
        Console.WriteLine("示例 4: 标记为已完成");
        Expression<Func<Task, Task>> update4 = t => new Task 
        { 
            IsCompleted = 1,
            Version = t.Version + 1
        };
        var setClause4 = update4.ToSetClause();
        Console.WriteLine($"表达式: t => new Task {{ IsCompleted = 1, Version = t.Version + 1 }}");
        Console.WriteLine($"生成 SET: {setClause4}");
        Console.WriteLine();

        // ========== 示例 5: 复杂表达式（多个递增） ==========
        Console.WriteLine("示例 5: 增加工作时间并递增版本");
        Expression<Func<Task, Task>> update5 = t => new Task 
        { 
            ActualMinutes = t.ActualMinutes + 30,
            Version = t.Version + 1
        };
        var setClause5 = update5.ToSetClause();
        Console.WriteLine($"表达式: t => new Task {{ ActualMinutes = t.ActualMinutes + 30, Version = t.Version + 1 }}");
        Console.WriteLine($"生成 SET: {setClause5}");
        Console.WriteLine();

        // ========== 示例 6: 字符串函数 ==========
        Console.WriteLine("示例 6: 使用字符串函数");
        Expression<Func<Task, Task>> update6 = t => new Task 
        { 
            Title = t.Title.Trim().ToUpper(),
            Description = t.Description + " (已更新)"
        };
        var setClause6 = update6.ToSetClause();
        Console.WriteLine($"表达式: t => new Task {{ Title = t.Title.Trim().ToUpper(), Description = t.Description + \" (已更新)\" }}");
        Console.WriteLine($"生成 SET: {setClause6}");
        Console.WriteLine("支持的字符串函数: ToLower(), ToUpper(), Trim(), Substring(), Replace(), + (连接)");
        Console.WriteLine();

        // ========== 示例 7: 数学函数 ==========
        Console.WriteLine("示例 7: 使用数学函数");
        Expression<Func<Task, Task>> update7 = t => new Task 
        { 
            Priority = Math.Abs(t.Priority),
            ActualMinutes = Math.Max(t.ActualMinutes, 0)
        };
        var setClause7 = update7.ToSetClause();
        Console.WriteLine($"表达式: t => new Task {{ Priority = Math.Abs(t.Priority), ActualMinutes = Math.Max(t.ActualMinutes, 0) }}");
        Console.WriteLine($"生成 SET: {setClause7}");
        Console.WriteLine("支持的数学函数: Abs(), Round(), Ceiling(), Floor(), Pow(), Sqrt(), Max(), Min()");
        Console.WriteLine();

        // ========== 对比：类型安全 vs 字符串拼接 ==========
        Console.WriteLine("========== 类型安全的优势 ==========");
        Console.WriteLine();
        Console.WriteLine("✅ 类型安全（表达式树）:");
        Console.WriteLine("   Expression<Func<Task, Task>> expr = t => new Task { Priority = 5 };");
        Console.WriteLine("   - 编译时检查字段名和类型");
        Console.WriteLine("   - IDE 智能提示和重构支持");
        Console.WriteLine("   - 自动参数化，防止 SQL 注入");
        Console.WriteLine();
        Console.WriteLine("⚠️ 字符串拼接:");
        Console.WriteLine("   var setClause = \"[priority] = @priority\";");
        Console.WriteLine("   - 运行时才能发现错误");
        Console.WriteLine("   - 需要手动管理列名和参数");
        Console.WriteLine("   - 容易出现拼写错误");
        Console.WriteLine();

        // ========== 提取参数 ==========
        Console.WriteLine("========== 参数提取 ==========");
        Expression<Func<Task, Task>> paramExpr = t => new Task 
        { 
            Title = "新标题",
            Priority = 5,
            Version = t.Version + 1
        };
        var parameters = paramExpr.GetSetParameters();
        Console.WriteLine($"表达式: t => new Task {{ Title = \"新标题\", Priority = 5, Version = t.Version + 1 }}");
        Console.WriteLine("提取的参数:");
        foreach (var param in parameters)
        {
            Console.WriteLine($"  {param.Key} = {param.Value}");
        }
    }
}

/// <summary>
/// 任务实体。
/// </summary>
[SqlxEntity]
public class Task
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public int IsCompleted { get; set; }
    public int ActualMinutes { get; set; }
    public int Version { get; set; }
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// 任务仓储接口。
/// </summary>
public interface ITaskRepository : ICrudRepository<Task, long>
{
    /// <summary>
    /// 使用表达式进行类型安全的动态更新。
    /// </summary>
    /// <param name="id">任务 ID。</param>
    /// <param name="updates">SET 子句内容（从表达式生成）。</param>
    /// <returns>受影响的行数。</returns>
    [SqlTemplate("UPDATE {{table}} SET {{set --param updates}} WHERE id = @id")]
    Task<int> DynamicUpdateAsync(long id, string updates);
}

/// <summary>
/// 任务仓储实现。
/// </summary>
[TableName("tasks")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITaskRepository))]
public partial class TaskRepository(SqliteConnection connection) : ITaskRepository
{
}
