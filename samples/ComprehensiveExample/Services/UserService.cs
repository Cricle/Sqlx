// -----------------------------------------------------------------------
// <copyright file="UserService.cs" company="Cricle">
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
/// 用户服务实现
/// 🚀 使用 [RepositoryFor] 特性自动生成所有方法实现
/// ✨ 零样板代码，编译时生成高性能实现
/// </summary>
[RepositoryFor(typeof(IUserService))]
public partial class UserService : IUserService
{
    private readonly DbConnection connection;
    
    /// <summary>
    /// 构造函数 - 这是您需要写的唯一代码！
    /// </summary>
    /// <param name="connection">数据库连接</param>
    public UserService(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // 🎉 所有 IUserService 接口方法都会被自动生成！
    // ✨ 包括：
    // - SQL 语句生成 (基于方法名推断或自定义 SQL)
    // - 参数绑定 (防止 SQL 注入)
    // - 结果映射 (高性能强类型读取)
    // - 异常处理 (友好的错误信息)
    // - 资源管理 (自动释放资源)
    // - Primary Constructor 和 Record 支持
    // - Expression to SQL 转换
    // - 批量操作优化
}

/// <summary>
/// 部门服务实现
/// </summary>
[RepositoryFor(typeof(IDepartmentService))]
public partial class DepartmentService : IDepartmentService
{
    private readonly DbConnection connection;
    
    public DepartmentService(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}

/// <summary>
/// 现代语法服务实现
/// 演示 Primary Constructor 和 Record 类型的完美支持
/// </summary>
[RepositoryFor(typeof(IModernSyntaxService))]
public partial class ModernSyntaxService : IModernSyntaxService
{
    private readonly DbConnection connection;
    
    public ModernSyntaxService(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}