// -----------------------------------------------------------------------
// <copyright file="TestRepositories.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined
{
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ICrudRepository<User, long>))]
    public partial class UserCrudRepository(IDbConnection connection) 
        : ICrudRepository<User, long>
    {
        // 🔧 IMPORTANT: For partial classes with primary constructor, 
        // define a field to store the connection so generated code can access it
        protected readonly IDbConnection connection = connection;
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IQueryRepository<User, long>))]
    public partial class UserQueryRepository(IDbConnection connection) 
        : IQueryRepository<User, long>
    {
        protected readonly IDbConnection connection = connection;
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ICommandRepository<User, long>))]
    public partial class UserCommandRepository(IDbConnection connection) 
        : ICommandRepository<User, long>
    {
        protected readonly IDbConnection connection = connection;
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IAggregateRepository<User, long>))]
    public partial class UserAggregateRepository(IDbConnection connection) 
        : IAggregateRepository<User, long>
    {
        protected readonly IDbConnection connection = connection;
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IBatchRepository<User, long>))]
    public partial class UserBatchRepository(IDbConnection connection) 
        : IBatchRepository<User, long>
    {
        protected readonly IDbConnection connection = connection;
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ICrudRepository<Product, long>))]
    public partial class ProductRepository(IDbConnection connection) 
        : ICrudRepository<Product, long>
    {
        protected readonly IDbConnection connection = connection;
    }

    // ===== AOT-Compatible Partial Update Repositories =====
    
    /// <summary>
    /// Repository using IPartialUpdateRepository with interface-level generics.
    /// TUpdates is resolved at compile time, enabling AOT-compatible code generation.
    /// </summary>
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IPartialUpdateRepository<User, long, UserNameUpdate>))]
    public partial class UserNameUpdateRepository(IDbConnection connection) 
        : IPartialUpdateRepository<User, long, UserNameUpdate>
    {
        protected readonly IDbConnection connection = connection;
    }

    /// <summary>
    /// Repository using IPartialUpdateRepository for status updates.
    /// </summary>
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IPartialUpdateRepository<User, long, UserStatusUpdate>))]
    public partial class UserStatusUpdateRepository(IDbConnection connection) 
        : IPartialUpdateRepository<User, long, UserStatusUpdate>
    {
        protected readonly IDbConnection connection = connection;
    }

    /// <summary>
    /// Repository using IExpressionUpdateRepository for expression-based updates.
    /// Uses Expression&lt;Func&lt;TEntity, TEntity&gt;&gt; to specify updates.
    /// </summary>
    /// <remarks>
    /// TEMPORARILY DISABLED: The source generator has a bug where it generates
    /// GetParameters() calls for Expression&lt;Func&lt;T, T&gt;&gt; types, but this
    /// extension method only supports Expression&lt;Func&lt;T, bool&gt;&gt;.
    /// This needs to be fixed in the source generator.
    /// </remarks>
    // [SqlDefine(SqlDefineTypes.SQLite)]
    // [RepositoryFor(typeof(IExpressionUpdateRepository<User, long>))]
    // public partial class UserExpressionUpdateRepository(IDbConnection connection) 
    //     : IExpressionUpdateRepository<User, long>
    // {
    //     protected readonly IDbConnection connection = connection;
    // }
}

