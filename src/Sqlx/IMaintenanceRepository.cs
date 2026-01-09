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
    /// <para>
    /// This interface is intentionally separated to prevent accidental usage.
    /// Requires explicit implementation and should be restricted to admin users.
    /// Always backup data before using these methods.
    /// </para>
    /// <para>
    /// <para>
    /// Recommended usage:
    /// </para>
    /// <list type="bullet">
    /// <item>Separate from normal repositories</item>
    /// <item>Require special permissions</item>
    /// <item>Log all operations</item>
    /// <item>Confirm operations (require user confirmation)</item>
    /// <item>Backup before executing</item>
    /// </list>
    /// </para>
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
        /// <para>
        /// Differences from DELETE:
        /// - TRUNCATE is faster (no row-by-row deletion)
        /// - TRUNCATE resets auto-increment counter
        /// - TRUNCATE cannot be rolled back in some databases
        /// - TRUNCATE bypasses triggers
        /// - TRUNCATE requires TRUNCATE privilege
        /// </para>
        /// <para>
        /// <list type="bullet">
        /// Use cases:
        /// <item>Clear test data</item>
        /// <item>Reset staging environment</item>
        /// <item>Remove all temporary/cache data</item>
        /// </list>
        /// </para>
        /// <para>⚠️ Always backup before truncating!</para>
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
        /// <para>
        /// This operation:
        /// <list type="bullet">
        /// <item>Removes table structure</item>
        /// <item>Deletes all table data</item>
        /// <item>Removes all indexes</item>
        /// <item>Removes all triggers</item>
        /// <item>CANNOT be undone</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use cases:
        /// <list type="bullet">
        /// <item>Remove obsolete tables</item>
        /// <item>Clean up migration tables</item>
        /// <item>Delete test tables</item>
        /// </list>
        /// </para>
        /// <para>
        /// ⚠️ ALWAYS backup before dropping!
        /// ⚠️ Verify table name before execution!
        /// </para>
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
        /// <para>
        /// Differences from TRUNCATE:
        /// <para>
        /// - DELETE is slower (row-by-row deletion)
        /// </para>
        /// - DELETE does NOT reset auto-increment
        /// - DELETE CAN be rolled back
        /// - DELETE triggers fire
        /// - DELETE can be in transaction
        /// </para>
        /// <para>
        /// Use when:
        /// - Need to rollback if something goes wrong
        /// - Need triggers to fire
        /// - TRUNCATE is not available
        /// </para>
        /// </remarks>
        [SqlTemplate("DELETE FROM {{table}}")]
        Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);

        // ===== Maintenance Operations =====

        /// <summary>Rebuilds table indexes for performance optimization.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// <para>
        /// Database-specific implementation:
        /// - SQL Server: ALTER INDEX ALL ON table REBUILD
        /// - PostgreSQL: REINDEX TABLE table
        /// - MySQL: OPTIMIZE TABLE table
        /// - SQLite: VACUUM
        /// </para>
        /// <para>
        /// When to use:
        /// - After bulk inserts/updates
        /// - Index fragmentation detected
        /// - Query performance degradation
        /// - Regular maintenance schedule
        /// </para>
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

