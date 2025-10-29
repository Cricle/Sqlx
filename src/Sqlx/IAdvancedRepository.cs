// -----------------------------------------------------------------------
// <copyright file="IAdvancedRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// Advanced repository interface for special operations and raw SQL execution.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IAdvancedRepository<TEntity, TKey>
        where TEntity : class
    {
        // ===== Raw SQL Operations =====

        /// <summary>Executes raw SQL command (INSERT/UPDATE/DELETE).</summary>
        /// <param name="sql">SQL command string</param>
        /// <param name="parameters">Anonymous object with parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        /// <example>
        /// await repo.ExecuteRawAsync("UPDATE users SET status = @status WHERE age > @age", new { status = 1, age = 18 });
        /// </example>
        Task<int> ExecuteRawAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>Executes raw SQL query returning entities.</summary>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Anonymous object with parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        Task<List<TEntity>> QueryRawAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>Executes raw SQL query returning custom type.</summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Anonymous object with parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of custom type instances</returns>
        /// <example>
        /// var dtos = await repo.QueryRawAsync&lt;UserDto&gt;(
        ///     "SELECT u.id, u.name, COUNT(o.id) as OrderCount FROM users u LEFT JOIN orders o ON u.id = o.user_id GROUP BY u.id, u.name"
        /// );
        /// </example>
        Task<List<T>> QueryRawAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>Executes raw SQL query returning single scalar value.</summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Anonymous object with parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Scalar value</returns>
        /// <example>
        /// int maxId = await repo.ExecuteScalarAsync&lt;int&gt;("SELECT MAX(id) FROM users");
        /// </example>
        Task<T> ExecuteScalarAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        // ===== Bulk Operations =====

        /// <summary>Truncates table (deletes all rows and resets auto-increment).</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// ⚠️ WARNING: This is a permanent operation that cannot be rolled back in most databases.
        /// Extremely fast but destructive. Use with caution.
        /// </remarks>
        [SqlTemplate("TRUNCATE TABLE {{table}}")]
        Task TruncateAsync(CancellationToken cancellationToken = default);

        /// <summary>Bulk copy/import large amount of data using database-specific bulk insert.</summary>
        /// <param name="entities">Entities to import</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows inserted</returns>
        /// <remarks>
        /// Uses database-specific bulk insert for maximum performance:
        /// - SQL Server: SqlBulkCopy
        /// - PostgreSQL: COPY
        /// - MySQL: LOAD DATA INFILE
        /// - SQLite: Multiple INSERTs in transaction
        /// Typically 100-1000x faster than batch insert for millions of rows.
        /// </remarks>
        Task<int> BulkCopyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        // ===== Table Operations =====

        /// <summary>Generates CREATE TABLE DDL statement for entity.</summary>
        /// <returns>CREATE TABLE SQL statement</returns>
        /// <example>
        /// string ddl = await repo.GenerateCreateTableSqlAsync();
        /// // CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL, ...);
        /// </example>
        Task<string> GenerateCreateTableSqlAsync();

        /// <summary>Checks if table exists in database.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if table exists, false otherwise</returns>
        Task<bool> TableExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>Creates table if it doesn't exist.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>Drops table from database.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>⚠️ WARNING: This is irreversible! All data will be lost.</remarks>
        Task DropTableAsync(CancellationToken cancellationToken = default);
    }
}

