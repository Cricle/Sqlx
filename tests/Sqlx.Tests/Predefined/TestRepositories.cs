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
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IQueryRepository<User, long>))]
    public partial class UserQueryRepository(IDbConnection connection) 
        : IQueryRepository<User, long>
    {
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ICommandRepository<User, long>))]
    public partial class UserCommandRepository(IDbConnection connection) 
        : ICommandRepository<User, long>
    {
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IAggregateRepository<User, long>))]
    public partial class UserAggregateRepository(IDbConnection connection) 
        : IAggregateRepository<User, long>
    {
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IBatchRepository<User, long>))]
    public partial class UserBatchRepository(IDbConnection connection) 
        : IBatchRepository<User, long>
    {
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ICrudRepository<Product, long>))]
    public partial class ProductRepository(IDbConnection connection) 
        : ICrudRepository<Product, long>
    {
    }
}

