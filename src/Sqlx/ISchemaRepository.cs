// -----------------------------------------------------------------------
// <copyright file="ISchemaRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx
{
    /// <summary>
    /// Schema repository interface for database table structure operations.
    /// Used for migrations, schema inspection, and table management.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <remarks>
    /// This interface is separate from other repositories to avoid accidental
    /// schema modifications during normal data operations.
    /// Typically used during application startup or migration scripts.
    /// </remarks>
    public interface ISchemaRepository<TEntity>
        where TEntity : class
    {
        /// <summary>Checks if table exists in database.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if table exists, false otherwise</returns>
        /// <remarks>
        /// Uses database-specific information schema or system catalogs:
        /// - SQL Server: INFORMATION_SCHEMA.TABLES
        /// - MySQL: INFORMATION_SCHEMA.TABLES
        /// - PostgreSQL: pg_tables
        /// - SQLite: sqlite_master
        /// </remarks>
        Task<bool> TableExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>Generates CREATE TABLE DDL statement for entity.</summary>
        /// <returns>CREATE TABLE SQL statement</returns>
        /// <example>
        /// string ddl = await repo.GenerateCreateTableSqlAsync();
        /// // CREATE TABLE users (
        /// //   id INTEGER PRIMARY KEY AUTOINCREMENT,
        /// //   name TEXT NOT NULL,
        /// //   email TEXT UNIQUE,
        /// //   created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        /// // );
        /// </example>
        /// <remarks>
        /// Generates database-specific DDL based on entity properties and attributes.
        /// Supports:
        /// - Primary keys (auto-increment)
        /// - Nullable columns
        /// - String length constraints
        /// - Unique constraints
        /// - Default values
        /// - Indexes
        /// </remarks>
        Task<string> GenerateCreateTableSqlAsync();

        /// <summary>Creates table if it doesn't exist.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Idempotent operation - safe to call multiple times.
        /// Commonly used during application startup for code-first scenarios.
        /// </remarks>
        Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets column names of the table.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of column names in database order</returns>
        Task<IList<string>> GetColumnNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets table row count estimate (fast, approximate).</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Approximate row count</returns>
        /// <remarks>
        /// Much faster than COUNT(*) for large tables, but less accurate.
        /// Uses database statistics instead of scanning the table:
        /// - SQL Server: sys.dm_db_partition_stats
        /// - PostgreSQL: pg_class.reltuples
        /// - MySQL: INFORMATION_SCHEMA.TABLES.TABLE_ROWS
        /// - SQLite: Falls back to COUNT(*)
        /// </remarks>
        Task<long> GetApproximateRowCountAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets table size in bytes.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Table size in bytes (data + indexes)</returns>
        Task<long> GetTableSizeBytesAsync(CancellationToken cancellationToken = default);
    }
}

