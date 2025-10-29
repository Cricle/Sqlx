// -----------------------------------------------------------------------
// <copyright file="IMaintenanceRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// Maintenance repository interface for dangerous database operations.
    /// ⚠️ WARNING: Methods in this interface can cause PERMANENT DATA LOSS.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <remarks>
    /// This interface is intentionally separated to prevent accidental usage.
    /// Requires explicit implementation and should be restricted to admin users.
    /// Always backup data before using these methods.
    /// 
    /// Recommended usage:
    /// 1. Separate from normal repositories
    /// 2. Require special permissions
    /// 3. Log all operations
    /// 4. Confirm operations (require user confirmation)
    /// 5. Backup before executing
    /// </remarks>
    public interface IMaintenanceRepository<TEntity>
        where TEntity : class
    {
        // ===== Destructive Operations =====

        /// <summary>
        /// Truncates table (deletes ALL rows and resets auto-increment counter).
        /// ⚠️ DANGER: This is a PERMANENT operation that CANNOT be undone!
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        /// <remarks>
        /// Differences from DELETE:
        /// - TRUNCATE is faster (no row-by-row deletion)
        /// - TRUNCATE resets auto-increment counter
        /// - TRUNCATE cannot be rolled back in some databases
        /// - TRUNCATE bypasses triggers
        /// - TRUNCATE requires TRUNCATE privilege
        /// 
        /// Use cases:
        /// - Clear test data
        /// - Reset staging environment
        /// - Remove all temporary/cache data
        /// 
        /// ⚠️ Always backup before truncating!
        /// </remarks>
        [SqlTemplate("TRUNCATE TABLE {{table}}")]
        Task TruncateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops (deletes) table from database permanently.
        /// ⚠️ DANGER: This will DELETE the TABLE and ALL DATA FOREVER!
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        /// <remarks>
        /// This operation:
        /// - Removes table structure
        /// - Deletes all table data
        /// - Removes all indexes
        /// - Removes all triggers
        /// - CANNOT be undone
        /// 
        /// Use cases:
        /// - Remove obsolete tables
        /// - Clean up migration tables
        /// - Delete test tables
        /// 
        /// ⚠️ ALWAYS backup before dropping!
        /// ⚠️ Verify table name before execution!
        /// </remarks>
        [SqlTemplate("DROP TABLE IF EXISTS {{table}}")]
        Task DropTableAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes ALL rows from table (similar to TRUNCATE but can be rolled back).
        /// ⚠️ WARNING: This deletes ALL data in the table!
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of rows deleted</returns>
        /// <remarks>
        /// Differences from TRUNCATE:
        /// - DELETE is slower (row-by-row deletion)
        /// - DELETE does NOT reset auto-increment
        /// - DELETE CAN be rolled back
        /// - DELETE triggers fire
        /// - DELETE can be in transaction
        /// 
        /// Use when:
        /// - Need to rollback if something goes wrong
        /// - Need triggers to fire
        /// - TRUNCATE is not available
        /// </remarks>
        [SqlTemplate("DELETE FROM {{table}}")]
        Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);

        // ===== Maintenance Operations =====

        /// <summary>Rebuilds table indexes for performance optimization.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Database-specific implementation:
        /// - SQL Server: ALTER INDEX ALL ON table REBUILD
        /// - PostgreSQL: REINDEX TABLE table
        /// - MySQL: OPTIMIZE TABLE table
        /// - SQLite: VACUUM
        /// 
        /// When to use:
        /// - After bulk inserts/updates
        /// - Index fragmentation detected
        /// - Query performance degradation
        /// - Regular maintenance schedule
        /// </remarks>
        Task RebuildIndexesAsync(CancellationToken cancellationToken = default);

        /// <summary>Updates table statistics for query optimizer.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Database-specific implementation:
        /// - SQL Server: UPDATE STATISTICS table
        /// - PostgreSQL: ANALYZE table
        /// - MySQL: ANALYZE TABLE table
        /// - SQLite: ANALYZE table
        /// 
        /// When to use:
        /// - After large data changes
        /// - Query plans seem suboptimal
        /// - Regular maintenance schedule
        /// </remarks>
        Task UpdateStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>Shrinks table to reclaim unused space.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of bytes reclaimed</returns>
        /// <remarks>
        /// Database-specific implementation:
        /// - SQL Server: DBCC SHRINKDATABASE
        /// - PostgreSQL: VACUUM FULL
        /// - MySQL: OPTIMIZE TABLE
        /// - SQLite: VACUUM
        /// 
        /// When to use:
        /// - After bulk deletes
        /// - Disk space is critical
        /// - File size is much larger than data size
        /// 
        /// Note: Can be slow on large tables
        /// </remarks>
        Task<long> ShrinkTableAsync(CancellationToken cancellationToken = default);
    }
}

