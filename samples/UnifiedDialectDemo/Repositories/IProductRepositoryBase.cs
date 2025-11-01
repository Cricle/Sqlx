// -----------------------------------------------------------------------
// <copyright file="IProductRepositoryBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using UnifiedDialectDemo.Models;

namespace UnifiedDialectDemo.Repositories;

/// <summary>
/// Base repository interface with unified SQL templates using placeholders.
/// This interface is defined ONCE and works for ALL database dialects.
/// </summary>
public interface IProductRepositoryBase
{
    /// <summary>
    /// Gets a product by ID.
    /// Uses {{table}} placeholder which will be replaced based on dialect.
    /// </summary>
    [SqlTemplate(@"SELECT * FROM {{table}} WHERE id = @id")]
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets all active products.
    /// Uses {{table}} and {{bool_true}} placeholders.
    /// PostgreSQL: true, MySQL/SQLite: 1
    /// </summary>
    [SqlTemplate(@"SELECT * FROM {{table}} WHERE is_active = {{bool_true}} ORDER BY name")]
    Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default);

    /// <summary>
    /// Inserts a new product.
    /// Uses {{table}}, {{current_timestamp}}, {{bool_true}}, and {{returning_id}} placeholders.
    /// PostgreSQL: RETURNING id, MySQL/SQLite: uses LAST_INSERT_ID/last_insert_rowid
    /// </summary>
    [SqlTemplate(@"
        INSERT INTO {{table}} (name, description, price, stock, is_active, created_at) 
        VALUES (@name, @description, @price, @stock, {{bool_true}}, {{current_timestamp}})
        {{returning_id}}")]
    Task<int> InsertAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Updates a product.
    /// Uses {{table}} and {{current_timestamp}} placeholders.
    /// </summary>
    [SqlTemplate(@"
        UPDATE {{table}} 
        SET name = @name, 
            description = @description, 
            price = @price, 
            stock = @stock,
            updated_at = {{current_timestamp}}
        WHERE id = @id")]
    Task<int> UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a product (sets is_active to false).
    /// Uses {{table}} and {{bool_false}} placeholders.
    /// </summary>
    [SqlTemplate(@"
        UPDATE {{table}} 
        SET is_active = {{bool_false}},
            updated_at = {{current_timestamp}}
        WHERE id = @id")]
    Task<int> DeactivateAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets products with pagination.
    /// Uses {{table}} placeholder, but LIMIT/OFFSET syntax varies too much between dialects.
    /// For complex queries, use dialect-specific SQL templates in implementation classes.
    /// </summary>
    [SqlTemplate(@"SELECT * FROM {{table}} ORDER BY name")]
    Task<List<Product>> GetPagedAsync(int limit, int offset, CancellationToken ct = default);

    /// <summary>
    /// Searches products by name (case-insensitive).
    /// Uses {{table}} placeholder.
    /// </summary>
    [SqlTemplate(@"
        SELECT * FROM {{table}} 
        WHERE LOWER(name) LIKE LOWER(@searchTerm) 
          AND is_active = {{bool_true}}
        ORDER BY name")]
    Task<List<Product>> SearchByNameAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Gets products count.
    /// Uses {{table}} placeholder.
    /// </summary>
    [SqlTemplate(@"SELECT COUNT(*) FROM {{table}}")]
    Task<int> GetCountAsync(CancellationToken ct = default);
}

