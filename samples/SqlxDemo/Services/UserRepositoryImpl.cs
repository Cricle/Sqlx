// -----------------------------------------------------------------------
// <copyright file="UserRepositoryImpl.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using Sqlx.Annotations;
using SqlxDemo.Models;

namespace SqlxDemo.Services;

/// <summary>
/// 用户仓储实现 - 演示 RepositoryFor 属性的正确用法
/// </summary>
[RepositoryFor(typeof(IRepositoryService))]
public partial class UserRepositoryImpl
{
    private readonly IDbConnection _connection;

    public UserRepositoryImpl(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    // 源生成器会自动实现 IRepositoryService 接口的所有方法
    // 不需要手动实现，编译器会根据接口中的 [Sqlx] 和 [SqlExecuteType] 属性生成实现
}

