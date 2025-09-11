// -----------------------------------------------------------------------
// <copyright file="ISmartUpdateService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ComprehensiveExample.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace ComprehensiveExample.Services;

/// <summary>
/// 智能 UPDATE 服务接口 - 展示优化后的灵活、高性能更新操作
/// 🚀 性能提升：只更新需要的字段，减少数据传输和处理开销
/// 🎯 灵活性：多种更新模式适应不同场景
/// ✨ 易用性：类型安全的表达式语法，智能推断
/// </summary>
public interface ISmartUpdateService
{
    // ==================== 🎯 部分更新 - 只更新指定字段 ====================
    
    /// <summary>
    /// 🎯 智能部分更新 - 只更新指定字段 (性能优化)
    /// 适用场景：只需要更新实体的少数几个字段时
    /// 性能优势：减少 SQL 语句大小，降低网络传输和数据库处理开销
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="fields">要更新的字段选择器</param>
    /// <returns>影响的行数</returns>
    /// <example>
    /// // 只更新用户的邮箱和活跃状态
    /// await smartUpdateService.UpdateUserPartialAsync(user, 
    ///     u => u.Email, 
    ///     u => u.IsActive);
    /// </example>
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    Task<int> UpdateUserPartialAsync(User user, params Expression<Func<User, object>>[] fields);
    
    /// <summary>
    /// 🎯 部分更新客户信息 - Primary Constructor 支持
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "customers")]
    Task<int> UpdateCustomerPartialAsync(Customer customer, params Expression<Func<Customer, object>>[] fields);
    
    // ==================== 📦 批量条件更新 - 根据条件批量更新 ====================
    
    /// <summary>
    /// 📦 智能批量更新 - 根据条件批量更新字段
    /// 适用场景：需要根据条件批量更新多条记录的相同字段
    /// 性能优势：一条 SQL 语句完成批量更新，避免 N+1 问题
    /// </summary>
    /// <param name="setValues">要设置的字段和值</param>
    /// <param name="whereClause">WHERE 条件子句 (可选)</param>
    /// <returns>影响的行数</returns>
    /// <example>
    /// // 批量将指定部门的用户设为非活跃状态
    /// var updates = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["IsActive"] = false,
    ///     ["LastUpdated"] = DateTime.Now
    /// };
    /// await smartUpdateService.UpdateUsersBatchAsync(updates, "department_id = 1");
    /// </example>
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    Task<int> UpdateUsersBatchAsync(Dictionary<string, object> setValues, string whereClause = null);
    
    /// <summary>
    /// 📦 批量更新客户状态
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "customers")]
    Task<int> UpdateCustomersBatchAsync(Dictionary<string, object> setValues, string whereClause = null);
    
    // ==================== ➕➖ 增量更新 - 数值字段增减 ====================
    
    /// <summary>
    /// ➕➖ 智能增量更新 - 数值字段增减操作
    /// 适用场景：计数器、金额、库存等数值字段的增减操作
    /// 性能优势：原子操作，避免读取-修改-写入的竞态条件
    /// </summary>
    /// <param name="customerId">客户ID</param>
    /// <param name="increments">字段增量值 (正数为增加，负数为减少)</param>
    /// <returns>影响的行数</returns>
    /// <example>
    /// // 增加客户的总消费金额，减少积分
    /// var increments = new Dictionary&lt;string, decimal&gt;
    /// {
    ///     ["TotalSpent"] = 199.99m,    // 增加消费金额
    ///     ["Points"] = -100m           // 减少积分
    /// };
    /// await smartUpdateService.UpdateCustomerIncrementAsync(customerId, increments);
    /// </example>
    [SqlExecuteType(SqlExecuteTypes.Update, "customers")]
    Task<int> UpdateCustomerIncrementAsync(int customerId, Dictionary<string, decimal> increments);
    
    /// <summary>
    /// ➕➖ 库存增量更新 - 原子操作
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "inventory")]
    Task<int> UpdateInventoryIncrementAsync(int productId, Dictionary<string, decimal> increments);
    
    // ==================== 🔒 乐观锁更新 - 并发安全 ====================
    
    /// <summary>
    /// 🔒 乐观锁更新用户 - 并发安全的更新操作
    /// 适用场景：多用户并发修改同一条记录时，避免数据覆盖
    /// 性能优势：无需悲观锁，提高并发性能
    /// </summary>
    /// <param name="user">要更新的用户（需包含版本信息）</param>
    /// <returns>更新结果：true=成功，false=版本冲突</returns>
    /// <example>
    /// // 带版本控制的安全更新
    /// user.Name = "新名称";
    /// bool success = await smartUpdateService.UpdateUserOptimisticAsync(user);
    /// if (!success) {
    ///     // 处理版本冲突 - 数据已被其他用户修改
    ///     Console.WriteLine("数据已被其他用户修改，请刷新后重试");
    /// }
    /// </example>
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    Task<bool> UpdateUserOptimisticAsync(User user);
    
    // ==================== ⚡ 高性能批量字段更新 ====================
    
    /// <summary>
    /// ⚡ 高性能批量字段更新 - 不同记录更新不同字段
    /// 适用场景：需要批量更新多条记录，每条记录更新的字段可能不同
    /// 性能优势：使用 DbBatch 实现真正的批量操作，性能提升 10-100 倍
    /// </summary>
    /// <param name="updates">批量更新数据：Key=用户ID, Value=字段更新字典</param>
    /// <returns>影响的行数</returns>
    /// <example>
    /// // 批量更新不同用户的不同字段
    /// var updates = new Dictionary&lt;int, Dictionary&lt;string, object&gt;&gt;
    /// {
    ///     [1] = new() { ["Email"] = "user1@new.com", ["IsActive"] = true },
    ///     [2] = new() { ["Name"] = "User2 New Name" },
    ///     [3] = new() { ["IsActive"] = false, ["LastLogin"] = DateTime.Now }
    /// };
    /// await smartUpdateService.UpdateUsersBulkFieldsAsync(updates);
    /// </example>
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    Task<int> UpdateUsersBulkFieldsAsync(Dictionary<int, Dictionary<string, object>> updates);
    
    /// <summary>
    /// ⚡ 批量更新客户字段 - 高性能模式
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "customers")]
    Task<int> UpdateCustomersBulkFieldsAsync(Dictionary<int, Dictionary<string, object>> updates);
    
    // ==================== 🎨 智能条件更新 - 类型安全 ====================
    
    /// <summary>
    /// 🎨 智能条件更新 - 类型安全的条件构建
    /// 结合 ExpressionToSql 实现类型安全的条件更新
    /// </summary>
    /// <param name="setFields">要设置的字段</param>
    /// <param name="whereExpression">WHERE 条件表达式</param>
    /// <returns>影响的行数</returns>
    /// <example>
    /// // 类型安全的条件更新
    /// await smartUpdateService.UpdateUsersConditionalAsync(
    ///     new { IsActive = false, LastUpdated = DateTime.Now },
    ///     u => u.DepartmentId == 1 && u.CreatedAt &lt; DateTime.Now.AddYears(-1)
    /// );
    /// </example>
    [Sqlx("UPDATE users SET {0} WHERE {1}")]
    Task<int> UpdateUsersConditionalAsync(object setFields, [ExpressionToSql] string whereExpression);
}

/// <summary>
/// 智能更新服务实现 - 自动生成所有方法
/// </summary>
[RepositoryFor(typeof(ISmartUpdateService))]
public partial class SmartUpdateService : ISmartUpdateService
{
    private readonly System.Data.Common.DbConnection connection;
    
    public SmartUpdateService(System.Data.Common.DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // 🎉 所有智能更新方法都会被自动生成！
    // ✨ 包括：
    // - 部分字段更新 (性能优化)
    // - 批量条件更新 (避免 N+1)
    // - 增量数值更新 (原子操作)
    // - 乐观锁更新 (并发安全)
    // - 高性能批量更新 (DbBatch)
    // - 类型安全条件更新 (ExpressionToSql)
}
