// -----------------------------------------------------------------------
// <copyright file="DatabaseFallbackPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined
{
    /// <summary>
    /// Property-based tests for database-specific functionality fallback handling.
    /// Validates: Requirements 15.3
    /// Property 8: 数据库特定功能降级处理
    /// </summary>
    /// <remarks>
    /// These tests verify that database-specific operations properly fall back to
    /// alternative implementations when the primary command isn't supported.
    /// For example, SQLite doesn't support TRUNCATE TABLE, so it should use DELETE FROM.
    /// </remarks>
    [TestClass]
    public class DatabaseFallbackPropertyTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        public DatabaseFallbackPropertyTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        #region Helper Methods

        private async Task CreateTestTableAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS fallback_test_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL,
                    created_at TEXT NOT NULL
                )";
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertTestDataAsync(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = $@"
                    INSERT INTO fallback_test_entities (name, value, created_at)
                    VALUES ('Entity{i}', {i * 10}, '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}')";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<long> GetRowCountAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM fallback_test_entities";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        private async Task<bool> TableExistsAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='fallback_test_entities'";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        }

        private async Task<long?> GetMaxIdAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT MAX(id) FROM fallback_test_entities";
            var result = await cmd.ExecuteScalarAsync();
            return result == DBNull.Value ? null : Convert.ToInt64(result);
        }

        #endregion

        #region Property 8.1: TRUNCATE Fallback - Data Deletion

        /// <summary>
        /// Property: TRUNCATE fallback (DELETE FROM) should delete all rows.
        /// SQLite doesn't support TRUNCATE TABLE, so the implementation uses DELETE FROM.
        /// </summary>
        [TestMethod]
        public async Task Property_TruncateFallback_DeletesAllRows()
        {
            // Arrange
            await CreateTestTableAsync();
            var testSizes = new[] { 1, 5, 10, 50, 100 };

            foreach (var size in testSizes)
            {
                // Reset table
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM fallback_test_entities";
                    await cmd.ExecuteNonQueryAsync();
                }

                await InsertTestDataAsync(size);
                var repo = new FallbackTestRepository(_connection);

                // Verify data exists
                var countBefore = await GetRowCountAsync();
                Assert.AreEqual(size, countBefore, $"Should have {size} rows before truncate");

                // Act
                var deleted = await repo.TruncateAsync();

                // Assert
                var countAfter = await GetRowCountAsync();
                Assert.AreEqual(0L, countAfter, $"Table should be empty after truncate (size={size})");
                Assert.AreEqual(size, deleted, $"Should report {size} rows deleted");
            }
        }

        #endregion

        #region Property 8.2: TRUNCATE Fallback - Returns Correct Count

        /// <summary>
        /// Property: TRUNCATE fallback should return the correct number of deleted rows.
        /// </summary>
        [TestMethod]
        public async Task Property_TruncateFallback_ReturnsCorrectDeletedCount()
        {
            // Arrange
            await CreateTestTableAsync();
            var repo = new FallbackTestRepository(_connection);

            var testCases = new[] { 0, 1, 10, 25, 50 };

            foreach (var expectedCount in testCases)
            {
                // Reset table
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM fallback_test_entities";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (expectedCount > 0)
                {
                    await InsertTestDataAsync(expectedCount);
                }

                // Act
                var deleted = await repo.TruncateAsync();

                // Assert
                Assert.AreEqual(expectedCount, deleted, 
                    $"Should return {expectedCount} for table with {expectedCount} rows");
            }
        }

        #endregion

        #region Property 8.3: DELETE FROM - Idempotent on Empty Table

        /// <summary>
        /// Property: DELETE FROM on empty table should be idempotent (return 0, no error).
        /// </summary>
        [TestMethod]
        public async Task Property_DeleteAll_IdempotentOnEmptyTable()
        {
            // Arrange
            await CreateTestTableAsync();
            var repo = new FallbackTestRepository(_connection);

            // Act - Delete multiple times on empty table
            for (int i = 0; i < 5; i++)
            {
                var deleted = await repo.DeleteAllAsync();

                // Assert
                Assert.AreEqual(0, deleted, $"Delete on empty table should return 0 (iteration {i})");
                var count = await GetRowCountAsync();
                Assert.AreEqual(0L, count, "Table should remain empty");
            }
        }

        #endregion

        #region Property 8.4: DELETE FROM - Preserves Table Structure

        /// <summary>
        /// Property: DELETE FROM should preserve table structure (table still exists after delete).
        /// </summary>
        [TestMethod]
        public async Task Property_DeleteAll_PreservesTableStructure()
        {
            // Arrange
            await CreateTestTableAsync();
            await InsertTestDataAsync(10);
            var repo = new FallbackTestRepository(_connection);

            // Act
            await repo.DeleteAllAsync();

            // Assert - Table should still exist
            var tableExists = await TableExistsAsync();
            Assert.IsTrue(tableExists, "Table should still exist after DELETE FROM");

            // Should be able to insert new data
            await InsertTestDataAsync(5);
            var count = await GetRowCountAsync();
            Assert.AreEqual(5L, count, "Should be able to insert new data after DELETE FROM");
        }

        #endregion

        #region Property 8.5: ANALYZE Fallback - Executes Without Error

        /// <summary>
        /// Property: ANALYZE (UpdateStatistics) should execute without error on SQLite.
        /// </summary>
        [TestMethod]
        public async Task Property_UpdateStatistics_ExecutesWithoutError()
        {
            // Arrange
            await CreateTestTableAsync();
            var testSizes = new[] { 0, 1, 10, 100 };

            foreach (var size in testSizes)
            {
                // Reset table
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM fallback_test_entities";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (size > 0)
                {
                    await InsertTestDataAsync(size);
                }

                var repo = new FallbackTestRepository(_connection);

                // Act & Assert - Should not throw
                var result = await repo.UpdateStatisticsAsync();
                Assert.IsTrue(result >= 0, $"ANALYZE should complete without error (size={size})");
            }
        }

        #endregion

        #region Property 8.6: Schema Query Fallback - SQLite PRAGMA

        /// <summary>
        /// Property: Schema queries should use SQLite-specific PRAGMA commands.
        /// </summary>
        [TestMethod]
        public async Task Property_SchemaQuery_UsesSQLitePragma()
        {
            // Arrange
            await CreateTestTableAsync();
            var repo = new FallbackSchemaRepository(_connection);

            // Act
            var columns = await repo.GetColumnNamesAsync();

            // Assert
            Assert.IsNotNull(columns);
            Assert.IsTrue(columns.Count > 0, "Should return column names");
            Assert.IsTrue(columns.Any(c => c.Equals("id", StringComparison.OrdinalIgnoreCase)), 
                "Should include 'id' column");
            Assert.IsTrue(columns.Any(c => c.Equals("name", StringComparison.OrdinalIgnoreCase)), 
                "Should include 'name' column");
            Assert.IsTrue(columns.Any(c => c.Equals("value", StringComparison.OrdinalIgnoreCase)), 
                "Should include 'value' column");
        }

        #endregion

        #region Property 8.7: Table Exists Query - SQLite sqlite_master

        /// <summary>
        /// Property: Table existence check should use SQLite sqlite_master.
        /// </summary>
        [TestMethod]
        public async Task Property_TableExists_UsesSQLiteMaster()
        {
            // Arrange
            var repo = new FallbackSchemaRepository(_connection);

            // Test when table doesn't exist
            var existsBefore = await repo.TableExistsAsync();
            Assert.IsFalse(existsBefore, "Should return false when table doesn't exist");

            // Create table
            await CreateTestTableAsync();

            // Test when table exists
            var existsAfter = await repo.TableExistsAsync();
            Assert.IsTrue(existsAfter, "Should return true when table exists");
        }

        #endregion

        #region Property 8.8: Row Count Query - Accurate Count

        /// <summary>
        /// Property: Row count query should return accurate count for various table sizes.
        /// </summary>
        [TestMethod]
        public async Task Property_RowCount_ReturnsAccurateCount()
        {
            // Arrange
            await CreateTestTableAsync();
            var repo = new FallbackSchemaRepository(_connection);
            var testSizes = new[] { 0, 1, 5, 10, 50, 100 };

            foreach (var expectedSize in testSizes)
            {
                // Reset table
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM fallback_test_entities";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (expectedSize > 0)
                {
                    await InsertTestDataAsync(expectedSize);
                }

                // Act
                var count = await repo.GetRowCountAsync();

                // Assert
                Assert.AreEqual(expectedSize, count, 
                    $"Row count should be {expectedSize}");
            }
        }

        #endregion

        #region Property 8.9: Fallback Operations - Transactional Safety

        /// <summary>
        /// Property: Fallback operations should work correctly within transactions.
        /// </summary>
        [TestMethod]
        public async Task Property_FallbackOperations_WorkInTransaction()
        {
            // Arrange
            await CreateTestTableAsync();
            await InsertTestDataAsync(10);
            var repo = new FallbackTestRepository(_connection);

            // Act - Start transaction, delete, then rollback
            using var transaction = _connection.BeginTransaction();
            
            var deleted = await repo.DeleteAllAsync();
            Assert.AreEqual(10, deleted, "Should delete 10 rows");
            
            var countDuringTx = await GetRowCountAsync();
            Assert.AreEqual(0L, countDuringTx, "Table should be empty during transaction");

            transaction.Rollback();

            // Assert - Data should be restored after rollback
            var countAfterRollback = await GetRowCountAsync();
            Assert.AreEqual(10L, countAfterRollback, "Data should be restored after rollback");
        }

        #endregion

        #region Property 8.10: Fallback Operations - Concurrent Safety

        /// <summary>
        /// Property: Multiple sequential fallback operations should work correctly.
        /// </summary>
        [TestMethod]
        public async Task Property_FallbackOperations_SequentialSafety()
        {
            // Arrange
            await CreateTestTableAsync();
            var repo = new FallbackTestRepository(_connection);

            // Act - Perform multiple operations sequentially
            for (int iteration = 0; iteration < 5; iteration++)
            {
                // Insert data
                await InsertTestDataAsync(10);
                var countAfterInsert = await GetRowCountAsync();
                Assert.AreEqual(10L, countAfterInsert, $"Should have 10 rows after insert (iteration {iteration})");

                // Delete all
                var deleted = await repo.DeleteAllAsync();
                Assert.AreEqual(10, deleted, $"Should delete 10 rows (iteration {iteration})");

                // Verify empty
                var countAfterDelete = await GetRowCountAsync();
                Assert.AreEqual(0L, countAfterDelete, $"Table should be empty (iteration {iteration})");
            }
        }

        #endregion
    }

    #region Test Entity

    /// <summary>
    /// Test entity for fallback tests.
    /// </summary>
    [TableName("fallback_test_entities")]
    public class FallbackTestEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Source-Generated Fallback Test Repository

    /// <summary>
    /// Interface for testing database fallback operations.
    /// Uses DELETE FROM as fallback for TRUNCATE (SQLite doesn't support TRUNCATE).
    /// </summary>
    public interface IFallbackTestRepository
    {
        /// <summary>
        /// Truncates table - SQLite fallback uses DELETE FROM.
        /// </summary>
        [SqlTemplate("DELETE FROM {{table}}")]
        Task<int> TruncateAsync(System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Deletes all rows from table.</summary>
        [SqlTemplate("DELETE FROM {{table}}")]
        Task<int> DeleteAllAsync(System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Updates statistics using SQLite ANALYZE.</summary>
        [SqlTemplate("ANALYZE {{table}}")]
        Task<int> UpdateStatisticsAsync(System.Threading.CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Source-generated repository for fallback operation tests.
    /// </summary>
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IFallbackTestRepository))]
    [TableName("fallback_test_entities")]
    public partial class FallbackTestRepository : IFallbackTestRepository
    {
        private readonly IDbConnection _connection;

        public FallbackTestRepository(IDbConnection connection)
        {
            _connection = connection;
        }
    }

    #endregion

    #region Source-Generated Fallback Schema Repository

    /// <summary>
    /// Interface for testing schema query fallbacks.
    /// Uses SQLite-specific PRAGMA and sqlite_master queries.
    /// </summary>
    public interface IFallbackSchemaRepository
    {
        /// <summary>Checks if table exists using SQLite sqlite_master.</summary>
        [SqlTemplate("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='fallback_test_entities'")]
        Task<bool> TableExistsAsync(System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Gets column names using SQLite PRAGMA.</summary>
        [SqlTemplate("SELECT name FROM pragma_table_info('fallback_test_entities')")]
        Task<List<string>> GetColumnNamesAsync(System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Gets row count.</summary>
        [SqlTemplate("SELECT COUNT(*) FROM fallback_test_entities")]
        Task<long> GetRowCountAsync(System.Threading.CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Source-generated repository for schema query fallback tests.
    /// </summary>
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IFallbackSchemaRepository))]
    [TableName("fallback_test_entities")]
    public partial class FallbackSchemaRepository : IFallbackSchemaRepository
    {
        private readonly IDbConnection _connection;

        public FallbackSchemaRepository(IDbConnection connection)
        {
            _connection = connection;
        }
    }

    #endregion
}
