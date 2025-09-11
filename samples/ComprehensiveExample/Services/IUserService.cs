// -----------------------------------------------------------------------
// <copyright file="IUserService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ComprehensiveExample.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace ComprehensiveExample.Services;

/// <summary>
/// 用户服务接口，演示 Sqlx 的各种功能
/// </summary>
public interface IUserService
{
    // === 基本 CRUD 操作 ===

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, created_at AS CreatedAt, is_active AS IsActive, department_id AS DepartmentId FROM users WHERE id = @id")]
    Task<User> GetUserByIdAsync(int id);

    /// <summary>
    /// 获取所有用户
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, created_at AS CreatedAt, is_active AS IsActive, department_id AS DepartmentId FROM users ORDER BY id")]
    Task<IList<User>> GetAllUsersAsync();

    /// <summary>
    /// 创建新用户 (自动推断为 INSERT)
    /// </summary>
    Task<int> CreateUserAsync(User user);

    /// <summary>
    /// 更新用户信息 (自动推断为 UPDATE)
    /// </summary>
    Task<int> UpdateUserAsync(User user);

    /// <summary>
    /// 删除用户 (自动推断为 DELETE)
    /// </summary>
    Task<int> DeleteUserAsync(int id);

    // === 自定义查询 ===

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, created_at AS CreatedAt, is_active AS IsActive, department_id AS DepartmentId FROM users WHERE email = @email")]
    Task<User> GetUserByEmailAsync(string email);

    /// <summary>
    /// 获取活跃用户数量 (标量查询)
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM users WHERE is_active = 1")]
    Task<int> CountActiveUsersAsync();

    /// <summary>
    /// 获取最近创建的用户
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, created_at AS CreatedAt, is_active AS IsActive, department_id AS DepartmentId FROM users WHERE created_at > @since ORDER BY created_at DESC")]
    Task<IList<User>> GetRecentUsersAsync(DateTime since);

    /// <summary>
    /// 搜索用户（按名称或邮箱）
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, created_at AS CreatedAt, is_active AS IsActive, department_id AS DepartmentId FROM users WHERE name LIKE @pattern OR email LIKE @pattern")]
    Task<IList<User>> SearchUsersAsync(string pattern);

    /// <summary>
    /// 获取指定部门的用户
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, created_at AS CreatedAt, is_active AS IsActive, department_id AS DepartmentId FROM users WHERE department_id = @departmentId")]
    Task<IList<User>> GetUsersByDepartmentAsync(int departmentId);

    /// <summary>
    /// 批量停用用户
    /// </summary>
    [Sqlx("UPDATE users SET is_active = 0 WHERE id IN (@userIds)")]
    Task<int> BatchDeactivateUsersAsync(string userIds);
}

/// <summary>
/// 部门服务接口
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 获取所有部门
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, description AS Description, created_at AS CreatedAt FROM departments ORDER BY id")]
    Task<IList<Department>> GetAllDepartmentsAsync();

    /// <summary>
    /// 创建新部门 (自动推断为 INSERT)
    /// </summary>
    Task<int> CreateDepartmentAsync(Department department);

    /// <summary>
    /// 根据ID获取部门
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, description AS Description, created_at AS CreatedAt FROM departments WHERE id = @id")]
    Task<Department> GetDepartmentByIdAsync(int id);

    /// <summary>
    /// 统计部门用户数量
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM users WHERE department_id = @departmentId")]
    Task<int> CountUsersByDepartmentAsync(int departmentId);
}

/// <summary>
/// 现代语法服务接口，演示 Record 支持
/// </summary>
public interface IModernSyntaxService
{
    // === Record 类型支持 ===

    /// <summary>
    /// 获取所有产品 (Record 类型)
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, price AS Price, category_id AS CategoryId, created_at AS CreatedAt, is_active AS IsActive FROM products ORDER BY id")]
    Task<IList<Product>> GetAllProductsAsync();

    /// <summary>
    /// 添加产品 (Record 类型自动推断为 INSERT)
    /// </summary>
    Task<int> AddProductAsync(Product product);

    /// <summary>
    /// 获取所有订单
    /// </summary>
    [Sqlx("SELECT id AS Id, customer_name AS CustomerName, order_date AS OrderDate, total_amount AS TotalAmount FROM orders ORDER BY id")]
    Task<IList<Order>> GetAllOrdersAsync();

    /// <summary>
    /// 添加订单
    /// </summary>
    Task<int> AddOrderAsync(Order order);

    /// <summary>
    /// 根据客户名称查询订单
    /// </summary>
    [Sqlx("SELECT id AS Id, customer_name AS CustomerName, order_date AS OrderDate, total_amount AS TotalAmount FROM orders WHERE customer_name LIKE @pattern")]
    Task<IList<Order>> GetOrdersByCustomerAsync(string pattern);
}

/// <summary>
/// 客户服务接口 - Primary Constructor 演示
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// 获取所有客户 (Primary Constructor 演示)
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, birth_date AS BirthDate, status AS Status, total_spent AS TotalSpent, referred_by AS ReferredBy, address AS Address, phone AS Phone, created_at AS CreatedAt, last_login_at AS LastLoginAt, is_vip AS IsVip FROM customers ORDER BY id")]
    Task<IList<Customer>> GetAllCustomersAsync();

    /// <summary>
    /// 创建客户 (Primary Constructor 自动推断为 INSERT)
    /// </summary>
    Task<int> CreateCustomerAsync(Customer customer);

    /// <summary>
    /// 根据ID获取客户
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, birth_date AS BirthDate, status AS Status, total_spent AS TotalSpent, referred_by AS ReferredBy, address AS Address, phone AS Phone, created_at AS CreatedAt, last_login_at AS LastLoginAt, is_vip AS IsVip FROM customers WHERE id = @id")]
    Task<Customer> GetCustomerByIdAsync(int id);

    /// <summary>
    /// 更新客户信息 (Primary Constructor 自动推断为 UPDATE)
    /// </summary>
    Task<int> UpdateCustomerAsync(Customer customer);

    /// <summary>
    /// 获取VIP客户
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, email AS Email, birth_date AS BirthDate, status AS Status, total_spent AS TotalSpent, referred_by AS ReferredBy, address AS Address, phone AS Phone, created_at AS CreatedAt, last_login_at AS LastLoginAt, is_vip AS IsVip FROM customers WHERE is_vip = 1 ORDER BY total_spent DESC")]
    Task<IList<Customer>> GetVipCustomersAsync();

    /// <summary>
    /// 按状态获取客户数量
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM customers WHERE status = @status")]
    Task<int> CountCustomersByStatusAsync(CustomerStatus status);
}

/// <summary>
/// 分类管理服务接口
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// 获取所有分类
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, description AS Description, parent_id AS ParentId, sort_order AS SortOrder, is_active AS IsActive, created_at AS CreatedAt FROM categories ORDER BY sort_order, id")]
    Task<IList<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// 创建分类
    /// </summary>
    Task<int> CreateCategoryAsync(Category category);

    /// <summary>
    /// 获取子分类
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, description AS Description, parent_id AS ParentId, sort_order AS SortOrder, is_active AS IsActive, created_at AS CreatedAt FROM categories WHERE parent_id = @parentId ORDER BY sort_order")]
    Task<IList<Category>> GetSubCategoriesAsync(int parentId);

    /// <summary>
    /// 获取顶级分类
    /// </summary>
    [Sqlx("SELECT id AS Id, name AS Name, description AS Description, parent_id AS ParentId, sort_order AS SortOrder, is_active AS IsActive, created_at AS CreatedAt FROM categories WHERE parent_id IS NULL ORDER BY sort_order")]
    Task<IList<Category>> GetTopLevelCategoriesAsync();
}

/// <summary>
/// 库存管理服务接口 - Record 演示
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// 获取所有库存项 (Record 类型)
    /// </summary>
    [Sqlx("SELECT id AS Id, product_id AS ProductId, quantity AS Quantity, reorder_level AS ReorderLevel, last_updated AS LastUpdated, warehouse_location AS WarehouseLocation, reserved_quantity AS ReservedQuantity FROM inventory ORDER BY id")]
    Task<IList<InventoryItem>> GetAllInventoryAsync();

    /// <summary>
    /// 创建库存项 (Record 自动推断为 INSERT)
    /// </summary>
    Task<int> CreateInventoryItemAsync(InventoryItem item);

    /// <summary>
    /// 更新库存数量
    /// </summary>
    [Sqlx("UPDATE inventory SET quantity = @quantity, last_updated = @lastUpdated WHERE product_id = @productId")]
    Task<int> UpdateInventoryQuantityAsync(int productId, int quantity, DateTime lastUpdated);

    /// <summary>
    /// 获取低库存商品
    /// </summary>
    [Sqlx("SELECT id AS Id, product_id AS ProductId, quantity AS Quantity, reorder_level AS ReorderLevel, last_updated AS LastUpdated, warehouse_location AS WarehouseLocation, reserved_quantity AS ReservedQuantity FROM inventory WHERE quantity <= reorder_level")]
    Task<IList<InventoryItem>> GetLowStockItemsAsync();
}

/// <summary>
/// 审计日志服务接口 - Primary Constructor + Record 演示
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// 创建审计日志 (Primary Constructor + Record 自动推断为 INSERT)
    /// </summary>
    Task<int> CreateAuditLogAsync(AuditLog log);

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    [Sqlx("SELECT id AS Id, action AS Action, entity_type AS EntityType, entity_id AS EntityId, user_id AS UserId, created_at AS CreatedAt, old_values AS OldValues, new_values AS NewValues, ip_address AS IpAddress, user_agent AS UserAgent FROM audit_logs WHERE user_id = @userId ORDER BY created_at DESC")]
    Task<IList<AuditLog>> GetUserAuditLogsAsync(string userId);

    /// <summary>
    /// 获取实体操作历史
    /// </summary>
    [Sqlx("SELECT id AS Id, action AS Action, entity_type AS EntityType, entity_id AS EntityId, user_id AS UserId, created_at AS CreatedAt, old_values AS OldValues, new_values AS NewValues, ip_address AS IpAddress, user_agent AS UserAgent FROM audit_logs WHERE entity_type = @entityType AND entity_id = @entityId ORDER BY created_at DESC")]
    Task<IList<AuditLog>> GetEntityAuditHistoryAsync(string entityType, string entityId);
}
