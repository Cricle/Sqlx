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

