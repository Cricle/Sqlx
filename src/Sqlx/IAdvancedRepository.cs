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
    /// AOT-compatible: Uses generic parameters instead of object to avoid reflection.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IAdvancedRepository<TEntity, TKey>
        where TEntity : class
    {
        // ===== Raw SQL Operations (AOT-Compatible) =====

        /// <summary>Executes raw SQL command (INSERT/UPDATE/DELETE) with typed parameters.</summary>
        /// <typeparam name="TParams">Parameter type (use anonymous type or class)</typeparam>
        /// <param name="sql">SQL command string</param>
        /// <param name="parameters">Typed parameters object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        /// <example>
        /// await repo.ExecuteRawAsync("UPDATE users SET status = @status WHERE age > @age", new { status = 1, age = 18 });
        /// </example>
        [SqlTemplate("{{sql}}")]
        Task<int> ExecuteRawAsync<TParams>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
            where TParams : class;

        /// <summary>Executes raw SQL command (INSERT/UPDATE/DELETE) without parameters.</summary>
        /// <param name="sql">SQL command string</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        [SqlTemplate("{{sql}}")]
        Task<int> ExecuteRawAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, CancellationToken cancellationToken = default);

        /// <summary>Executes raw SQL query returning entities with typed parameters.</summary>
        /// <typeparam name="TParams">Parameter type</typeparam>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Typed parameters object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        [SqlTemplate("{{sql}}")]
        Task<List<TEntity>> QueryRawAsync<TParams>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
            where TParams : class;

        /// <summary>Executes raw SQL query returning entities without parameters.</summary>
        /// <param name="sql">SQL query string</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        [SqlTemplate("{{sql}}")]
        Task<List<TEntity>> QueryRawAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, CancellationToken cancellationToken = default);

        /// <summary>Executes raw SQL query returning custom type with typed parameters.</summary>
        /// <typeparam name="TResult">Return type</typeparam>
        /// <typeparam name="TParams">Parameter type</typeparam>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Typed parameters object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of custom type instances</returns>
        /// <example>
        /// var dtos = await repo.QueryRawAsync&lt;UserDto, object&gt;(
        ///     "SELECT u.id, u.name, COUNT(o.id) as OrderCount FROM users u LEFT JOIN orders o ON u.id = o.user_id GROUP BY u.id, u.name"
        /// );
        /// </example>
        [SqlTemplate("{{sql}}")]
        Task<List<TResult>> QueryRawAsync<TResult, TParams>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
            where TResult : class
            where TParams : class;

        /// <summary>Executes raw SQL query returning custom type without parameters.</summary>
        /// <typeparam name="TResult">Return type</typeparam>
        /// <param name="sql">SQL query string</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of custom type instances</returns>
        [SqlTemplate("{{sql}}")]
        Task<List<TResult>> QueryRawAsync<TResult>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, CancellationToken cancellationToken = default)
            where TResult : class;

        /// <summary>Executes raw SQL query returning single scalar value with typed parameters.</summary>
        /// <typeparam name="TResult">Return type</typeparam>
        /// <typeparam name="TParams">Parameter type</typeparam>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Typed parameters object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Scalar value</returns>
        /// <example>
        /// int maxId = await repo.ExecuteScalarAsync&lt;int, object&gt;("SELECT MAX(id) FROM users WHERE status = @status", new { status = 1 });
        /// </example>
        [SqlTemplate("{{sql}}")]
        Task<TResult> ExecuteScalarAsync<TResult, TParams>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
            where TParams : class;

        /// <summary>Executes raw SQL query returning single scalar value without parameters.</summary>
        /// <typeparam name="TResult">Return type</typeparam>
        /// <param name="sql">SQL query string</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Scalar value</returns>
        /// <example>
        /// int maxId = await repo.ExecuteScalarAsync&lt;int&gt;("SELECT MAX(id) FROM users");
        /// </example>
        [SqlTemplate("{{sql}}")]
        Task<TResult> ExecuteScalarAsync<TResult>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, CancellationToken cancellationToken = default);

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

