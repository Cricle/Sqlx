// -----------------------------------------------------------------------
// <copyright file="SQLiteProductRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using System.Data.Common;
using UnifiedDialectDemo.Models;

namespace UnifiedDialectDemo.Repositories;

/// <summary>
/// SQLite implementation - only needs to specify dialect and table name.
/// The source generator will adapt all SQL templates from IProductRepositoryBase.
/// </summary>
[RepositoryFor(typeof(IProductRepositoryBase),
    Dialect = SqlDefineTypes.SQLite,
    TableName = "products")]
public partial class SQLiteProductRepository : IProductRepositoryBase
{
    private readonly DbConnection _connection;

    public SQLiteProductRepository(DbConnection connection)
    {
        _connection = connection;
    }

    // SQLite-specific LIMIT/OFFSET
    // Note: GetPagedAsync is defined in base interface without [SqlTemplate]
    // so we provide dialect-specific implementation here
}

