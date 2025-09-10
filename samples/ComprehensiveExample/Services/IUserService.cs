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
    Task<User?> GetUserByIdAsync(int id);

    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<IList<User>> GetAllUsersAsync();

    /// <summary>
    /// 创建新用户
    /// </summary>
    Task<int> CreateUserAsync(User user);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<int> UpdateUserAsync(User user);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<int> DeleteUserAsync(int id);

    // === 自定义查询 ===

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE email = @email")]
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// 获取活跃用户数量
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM users WHERE is_active = 1")]
    Task<int> CountActiveUsersAsync();

    /// <summary>
    /// 获取最近创建的用户
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE created_at > @since ORDER BY created_at DESC")]
    Task<IList<User>> GetRecentUsersAsync(DateTime since);

    /// <summary>
    /// 搜索用户（按名称或邮箱）
    /// </summary>
    [Sqlx("SELECT * FROM users WHERE name LIKE @pattern OR email LIKE @pattern")]
    Task<IList<User>> SearchUsersAsync(string pattern);
}

/// <summary>
/// 部门服务接口
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 获取所有部门
    /// </summary>
    Task<IList<Department>> GetAllDepartmentsAsync();

    /// <summary>
    /// 创建新部门
    /// </summary>
    Task<int> CreateDepartmentAsync(Department department);
}

