// -----------------------------------------------------------------------
// <copyright file="IUserRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using SqlxDemo.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SqlxDemo.Services;

/// <summary>
/// 用户仓储接口，定义标准的 CRUD 操作
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户对象，如果不存在则返回null</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有活跃用户
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>活跃用户列表</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据部门ID获取用户
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>该部门的用户列表</returns>
    Task<IEnumerable<User>> GetByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据年龄范围获取用户
    /// </summary>
    /// <param name="minAge">最小年龄</param>
    /// <param name="maxAge">最大年龄</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合年龄范围的用户列表</returns>
    Task<IEnumerable<User>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建新用户
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的用户ID</returns>
    Task<int> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户总数
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户总数</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱查找用户
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户对象，如果不存在则返回null</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}


