// -----------------------------------------------------------------------
// <copyright file="DemoUserRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx;
using Sqlx.Annotations;
using SqlxDemo.Models;
using System.Data;

namespace SqlxDemo.Services;

/// <summary>
/// 演示用户仓储接口 - 展示真正的模板引擎功能
/// 这里的SQL模板会在编译时被源代码生成器处理
/// </summary>
public interface IDemoUserRepository
{
    /// <summary>
    /// 使用模板占位符获取用户 - 编译时会替换{{table}}、{{columns:auto}}等占位符
    /// </summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
    Task<User?> GetUserByIdAsync(int id);

    /// <summary>
    /// 使用模板占位符获取活跃用户列表
    /// </summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE [IsActive] = 1")]
    Task<List<User>> GetActiveUsersAsync();

    /// <summary>
    /// 使用模板占位符进行范围查询
    /// </summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE [Age] BETWEEN @minAge AND @maxAge ORDER BY {{orderby:age}}")]
    Task<List<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);

    /// <summary>
    /// 使用SqlTemplateAttribute演示编译时SQL模板 - 支持@{参数名}占位符语法
    /// </summary>
    [SqlTemplate("SELECT * FROM [user] WHERE [Name] LIKE @{namePattern} AND [Age] > @{minAge}",
                 Dialect = SqlDefineTypes.SQLite, SafeMode = true)]
    Task<List<User>> SearchUsersByNameAndAgeAsync(string namePattern, int minAge);

    /// <summary>
    /// 使用SqlTemplateAttribute进行复杂条件查询
    /// </summary>
    [SqlTemplate("SELECT [Id], [Name], [Email], [Salary] FROM [user] WHERE [DepartmentId] = @{deptId} AND [Salary] >= @{minSalary} ORDER BY [Salary] DESC",
                 Dialect = SqlDefineTypes.SQLite, Operation = SqlOperation.Select)]
    Task<List<User>> GetUsersByDepartmentAndSalaryAsync(int deptId, decimal minSalary);

    /// <summary>
    /// 使用SqlTemplateAttribute进行更新操作
    /// </summary>
    [SqlTemplate("UPDATE [user] SET [Salary] = @{newSalary}, [Bonus] = @{bonusAmount} WHERE [Id] = @{userId}",
                 Dialect = SqlDefineTypes.SQLite, Operation = SqlOperation.Update)]
    Task<int> UpdateUserSalaryAndBonusAsync(int userId, decimal newSalary, decimal bonusAmount);
}

/// <summary>
/// 用户仓储实现 - 使用RepositoryFor特性让源代码生成器自动生成实现
/// 生成器会处理SQL模板并生成对应的方法实现
/// 使用主构造函数提供依赖注入
/// </summary>
[TableName("user")]
[RepositoryFor(typeof(IDemoUserRepository))]
public partial class DemoUserRepository(IDbConnection connection) : IDemoUserRepository
{
    // 源代码生成器会在这里生成实际的方法实现
    // 生成的代码会包含处理好的SQL（占位符已替换）

    /// <summary>
    /// 实现执行前拦截器 - 可以用于日志记录、权限检查等
    /// </summary>
    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        Console.WriteLine($"🔄 [执行前] 操作: {operationName}");
        Console.WriteLine($"   SQL: {command.CommandText}");
        Console.WriteLine($"   参数数量: {command.Parameters.Count}");
    }

    /// <summary>
    /// 实现执行成功拦截器 - 可以用于性能监控、缓存更新等
    /// </summary>
    partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks / (decimal)TimeSpan.TicksPerMillisecond;
        Console.WriteLine($"✅ [执行成功] 操作: {operationName}");
        Console.WriteLine($"   执行时间: {elapsedMs:F2}ms");
        Console.WriteLine($"   结果类型: {result?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// 实现执行失败拦截器 - 可以用于错误日志记录、回滚操作等
    /// </summary>
    partial void OnExecuteFail(string operationName, global::System.Data.IDbCommand command, global::System.Exception exception, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks / (decimal)TimeSpan.TicksPerMillisecond;
        Console.WriteLine($"❌ [执行失败] 操作: {operationName}");
        Console.WriteLine($"   执行时间: {elapsedMs:F2}ms");
        Console.WriteLine($"   错误信息: {exception.Message}");
        Console.WriteLine($"   错误类型: {exception.GetType().Name}");

        // 这里可以添加错误日志记录、监控告警等逻辑
        // 注意：不要在这里处理异常，因为异常会重新抛出
    }
}
