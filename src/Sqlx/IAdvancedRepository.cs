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

        // ===== Transaction Operations =====

        /// <summary>Begins a new database transaction.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Call CommitTransactionAsync() to commit or RollbackTransactionAsync() to rollback.
        /// Ensure you dispose the transaction properly.
        /// </remarks>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>Commits the current transaction.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>Rolls back the current transaction.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}

