// -----------------------------------------------------------------------
// <copyright file="PostgreSQLProductRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using System.Data.Common;
using UnifiedDialectDemo.Models;

namespace UnifiedDialectDemo.Repositories;

/// <summary>
/// PostgreSQL implementation - only needs to specify dialect and table name.
/// The source generator will adapt all SQL templates from IProductRepositoryBase.
/// </summary>
[RepositoryFor(typeof(IProductRepositoryBase), 
    Dialect = SqlDefineTypes.PostgreSql, 
    TableName = "products")]
public partial class PostgreSQLProductRepository : IProductRepositoryBase
{
    private readonly DbConnection _connection;

    public PostgreSQLProductRepository(DbConnection connection)
    {
        _connection = connection;
    }

    // PostgreSQL-specific LIMIT/OFFSET
    // Note: GetPagedAsync is defined in base interface without [SqlTemplate]
    // so we provide dialect-specific implementation here
}

