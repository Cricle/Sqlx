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