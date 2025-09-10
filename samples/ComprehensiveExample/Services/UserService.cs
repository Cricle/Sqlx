// -----------------------------------------------------------------------
// <copyright file="UserService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using Sqlx.Annotations;

namespace ComprehensiveExample.Services;

/// <summary>
/// 用户服务实现类，使用 Sqlx 自动生成 SQL 代码
/// </summary>
[RepositoryFor(typeof(IUserService))]
public partial class UserService : IUserService
{
    protected readonly DbConnection connection;

    public UserService(DbConnection connection)
    {
        this.connection = connection;
    }
}

/// <summary>
/// 部门服务实现类
/// </summary>
[RepositoryFor(typeof(IDepartmentService))]
public partial class DepartmentService : IDepartmentService
{
    protected readonly DbConnection connection;

    public DepartmentService(DbConnection connection)
    {
        this.connection = connection;
    }
}

