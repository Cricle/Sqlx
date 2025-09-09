// -----------------------------------------------------------------------
// <copyright file="GenericUserRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.RepositoryExample;

/// <summary>
/// Generic repository implementation using RepositoryFor with generic interfaces.
/// This demonstrates how RepositoryFor can work with generic type parameters.
/// </summary>
[RepositoryFor(typeof(IRepository<User>))]
public partial class GenericUserRepository : IRepository<User>
{
    private readonly DbConnection connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericUserRepository"/> class.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    public GenericUserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    // ========================================
    // RepositoryFor 泛型接口演示
    // RepositoryFor Generic Interface Demo
    // 
    // 这个类演示了如何使用 RepositoryFor 与泛型接口
    // This class demonstrates using RepositoryFor with generic interfaces
    // 
    // 接口: IRepository<User>
    // Interface: IRepository<User>
    // 
    // 生成的方法将根据 User 类型自动生成：
    // Generated methods will be automatically created based on User type:
    // - GetAll() -> SELECT * FROM users
    // - GetById(int id) -> SELECT * FROM users WHERE Id = @id
    // - Create(User entity) -> INSERT INTO users (columns...) VALUES (values...)
    // - Update(User entity) -> UPDATE users SET columns... WHERE Id = @id
    // - Delete(int id) -> DELETE FROM users WHERE Id = @id
    // ========================================
}

/// <summary>
/// Another example with explicit generic type parameters.
/// </summary>
[RepositoryFor(typeof(IGenericRepository<User, int>))]
public partial class AdvancedGenericUserRepository : IGenericRepository<User, int>
{
    private readonly DbConnection connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedGenericUserRepository"/> class.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    public AdvancedGenericUserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    // ========================================
    // RepositoryFor 高级泛型接口演示
    // RepositoryFor Advanced Generic Interface Demo
    // 
    // 接口: IGenericRepository<User, int>
    // Interface: IGenericRepository<User, int>
    // 
    // 此示例展示了双泛型参数的支持
    // This example shows support for dual generic parameters
    // ========================================
}
