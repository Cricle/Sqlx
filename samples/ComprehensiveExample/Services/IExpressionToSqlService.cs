// -----------------------------------------------------------------------
// <copyright file="IExpressionToSqlService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ComprehensiveExample.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace ComprehensiveExample.Services;

/// <summary>
/// Expression to SQL 演示服务接口 - 展示动态查询构建
/// </summary>
public interface IExpressionToSqlService
{
    /// <summary>
    /// 动态查询活跃用户
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE is_active = @isActive")]
    Task<IList<User>> QueryActiveUsersAsync(bool isActive);

    /// <summary>
    /// 按部门查询用户
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE department_id = @departmentId")]
    Task<IList<User>> QueryUsersByDepartmentAsync(int departmentId);

    /// <summary>
    /// 模糊搜索用户
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE name LIKE @pattern")]
    Task<IList<User>> SearchUsersByNameAsync(string pattern);

    /// <summary>
    /// 按日期范围查询用户
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE created_at >= @startDate AND created_at <= @endDate")]
    Task<IList<User>> QueryUsersByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 复杂条件查询用户
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE is_active = @isActive AND department_id IS NOT NULL AND created_at > @minDate ORDER BY name LIMIT @limit")]
    Task<IList<User>> QueryUsersWithComplexConditionsAsync(bool isActive, DateTime minDate, int limit);

    /// <summary>
    /// 查询VIP客户
    /// </summary>
    [Sqlx("SELECT * FROM customers WHERE is_vip = 1 ORDER BY total_spent DESC")]
    Task<IList<Customer>> QueryVipCustomersAsync();

    /// <summary>
    /// 按价格范围查询产品
    /// </summary>
    [Sqlx("SELECT * FROM products WHERE price >= @minPrice AND price <= @maxPrice")]
    Task<IList<Product>> QueryProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    /// <summary>
    /// 查询低库存商品
    /// </summary>
    [Sqlx("SELECT * FROM inventory WHERE quantity <= reorder_level")]
    Task<IList<InventoryItem>> QueryLowStockItemsAsync();

    /// <summary>
    /// 使用表达式查询用户 - 支持 ExpressionToSql
    /// </summary>
    [Sqlx("SELECT * FROM users {0}")]
    Task<IList<User>> QueryUsersAsync([ExpressionToSql] string whereClause);

    /// <summary>
    /// 分页查询用户
    /// </summary>
    [Sqlx("SELECT * FROM users {0} ORDER BY id LIMIT @limit OFFSET @offset")]
    Task<IList<User>> GetPagedUsersAsync([ExpressionToSql] string whereClause, int limit, int offset);

    /// <summary>
    /// 更新用户 - 支持 ExpressionToSql
    /// </summary>
    [Sqlx("UPDATE users SET {0} {1}")]
    Task<int> UpdateUsersAsync([ExpressionToSql] string setClause, [ExpressionToSql] string whereClause);

    /// <summary>
    /// 查询客户 - 支持 ExpressionToSql
    /// </summary>
    [Sqlx("SELECT * FROM customers {0}")]
    Task<IList<Customer>> QueryCustomersAsync([ExpressionToSql] string whereClause);

    /// <summary>
    /// 查询产品 - 支持 ExpressionToSql
    /// </summary>
    [Sqlx("SELECT * FROM products {0}")]
    Task<IList<Product>> QueryProductsAsync([ExpressionToSql] string whereClause);

    /// <summary>
    /// 查询库存 - 支持 ExpressionToSql
    /// </summary>
    [Sqlx("SELECT * FROM inventory {0}")]
    Task<IList<InventoryItem>> QueryInventoryAsync([ExpressionToSql] string whereClause);

    /// <summary>
    /// 搜索客户 - 支持 ExpressionToSql
    /// </summary>
    [Sqlx("SELECT * FROM customers {0}")]
    Task<IList<Customer>> SearchCustomersAsync([ExpressionToSql] string whereClause);

    /// <summary>
    /// 统计用户数量 - 支持 ExpressionToSql
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM users {0}")]
    Task<int> CountUsersAsync([ExpressionToSql] string whereClause);
}

/// <summary>
/// 批量操作服务接口 - 演示 DbBatch 功能
/// </summary>
public interface IBatchOperationService
{
    /// <summary>
    /// 批量创建用户 - DbBatch 演示
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "users")]
    Task<int> BatchCreateUsersAsync(IList<User> users);

    /// <summary>
    /// 批量更新用户 - DbBatch 演示
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "users")]
    Task<int> BatchUpdateUsersAsync(IList<User> users);

    /// <summary>
    /// 批量删除用户 - DbBatch 演示
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchDelete, "users")]
    Task<int> BatchDeleteUsersAsync(IList<int> userIds);

    /// <summary>
    /// 批量创建客户 - Primary Constructor + DbBatch
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "customers")]
    Task<int> BatchCreateCustomersAsync(IList<Customer> customers);

    /// <summary>
    /// 批量创建产品 - Record + DbBatch
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
    Task<int> BatchCreateProductsAsync(IList<Product> products);

    /// <summary>
    /// 批量创建库存项 - Record + DbBatch
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "inventory")]
    Task<int> BatchCreateInventoryAsync(IList<InventoryItem> items);

    /// <summary>
    /// 执行批量命令 - 通用批量操作
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, "users")]
    Task<int> ExecuteBatchCommandAsync(IList<string> commands);
}

/// <summary>
/// 多数据库方言支持演示服务接口
/// </summary>
public interface IMultiDatabaseService
{
    /// <summary>
    /// 通用用户查询 - 适配不同数据库
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE is_active = @isActive ORDER BY name")]
    Task<IList<User>> QueryActiveUsersAsync(bool isActive);
}

/// <summary>
/// 异步操作和取消支持演示服务接口
/// </summary>
public interface IAsyncOperationService
{
    /// <summary>
    /// 可取消的长时间查询
    /// </summary>
    [Sqlx("SELECT * FROM users ORDER BY id")]
    Task<IList<User>> GetAllUsersWithCancellationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 可取消的复杂查询
    /// </summary>
    [Sqlx(@"SELECT 
                u.*, 
                d.name as DepartmentName,
                COUNT(al.id) as AuditLogCount
            FROM users u
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN audit_logs al ON al.entity_type = 'User' AND al.entity_id = CAST(u.id AS TEXT)
            GROUP BY u.id, u.name, u.email, u.created_at, u.is_active, u.department_id, d.name
            ORDER BY u.id")]
    Task<IList<User>> GetUsersWithDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 带超时的批量操作
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "users")]
    Task<int> BatchInsertWithTimeoutAsync(IList<User> users, int timeoutSeconds = 30);

    /// <summary>
    /// 事务中的异步操作
    /// </summary>
    [Sqlx("INSERT INTO users (name, email, created_at, is_active, department_id) VALUES (@name, @email, @createdAt, @isActive, @departmentId)")]
    Task<int> CreateUserInTransactionAsync(User user, DbTransaction transaction, CancellationToken cancellationToken = default);
}

/// <summary>
/// 高级功能演示服务接口
/// </summary>
public interface IAdvancedFeatureService
{
    /// <summary>
    /// 复杂统计查询
    /// </summary>
    [Sqlx(@"SELECT 
                d.name as DepartmentName,
                COUNT(u.id) as TotalUsers,
                COUNT(CASE WHEN u.is_active = 1 THEN 1 END) as ActiveUsers
            FROM departments d
            LEFT JOIN users u ON d.id = u.department_id
            GROUP BY d.id, d.name
            ORDER BY d.name")]
    Task<IList<DepartmentUserCount>> GetDepartmentUserCountsAsync();

    /// <summary>
    /// 统计查询演示
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM users WHERE department_id = @departmentId")]
    Task<int> GetUserCountAsync(int departmentId);
}

/// <summary>
/// 部门用户统计数据传输对象
/// </summary>
public class DepartmentUserCount
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
}
