// -----------------------------------------------------------------------
// <copyright file="BatchInsertAndGetIdsPropertyTests.cs" company="Cricle">
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
    /// Property-based tests for BatchInsertAndGetIdsAsync method.
    /// Validates: Requirements 11.1
    /// Property 6: BatchInsertAndGetIdsAsync 功能正确性
    /// </summary>
    [TestClass]
    public class BatchInsertAndGetIdsPropertyTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        public BatchInsertAndGetIdsPropertyTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        #region Helper Methods

        private async Task CreateUserTableAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    is_active INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT
                )";
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<long> GetTableCountAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM users";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        private async Task<List<long>> GetAllIdsAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM users ORDER BY id";
            var ids = new List<long>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt64(0));
            }
            return ids;
        }

        #endregion

        #region Property 6.1: ID Count Matches Input Count

        /// <summary>
        /// Property: The number of returned IDs must equal the number of input entities.
        /// </summary>
        [TestMethod]
        public async Task Property_ReturnedIdCount_EqualsInputCount()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            // Test with various sizes
            var testSizes = new[] { 1, 2, 5, 10, 50, 100 };

            foreach (var size in testSizes)
            {
                // Create entities
                var users = Enumerable.Range(1, size)
                    .Select(i => new User
                    {
                        Name = $"User{i}",
                        Email = $"user{i}@test.com",
                        Age = 20 + i,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                // Act
                var ids = await repo.BatchInsertAndGetIdsAsync(users);

                // Assert
                Assert.AreEqual(size, ids.Count, $"Failed for size {size}: Expected {size} IDs, got {ids.Count}");
            }
        }

        #endregion

        #region Property 6.2: ID Order Matches Input Order

        /// <summary>
        /// Property: The order of returned IDs must match the order of input entities.
        /// This is critical for users to correlate returned IDs with their input entities.
        /// </summary>
        [TestMethod]
        public async Task Property_IdOrder_MatchesInputOrder()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            var users = new List<User>
            {
                new User { Name = "Alice", Email = "alice@test.com", Age = 25, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Name = "Bob", Email = "bob@test.com", Age = 30, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Name = "Charlie", Email = "charlie@test.com", Age = 35, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert - IDs should be sequential and in order
            Assert.AreEqual(3, ids.Count);
            Assert.IsTrue(ids[0] < ids[1], "First ID should be less than second ID");
            Assert.IsTrue(ids[1] < ids[2], "Second ID should be less than third ID");

            // Verify the IDs are sequential (for auto-increment)
            Assert.AreEqual(ids[0] + 1, ids[1], "IDs should be sequential");
            Assert.AreEqual(ids[1] + 1, ids[2], "IDs should be sequential");
        }

        #endregion

        #region Property 6.3: All IDs Are Unique

        /// <summary>
        /// Property: All returned IDs must be unique (no duplicates).
        /// </summary>
        [TestMethod]
        public async Task Property_AllIds_AreUnique()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            var users = Enumerable.Range(1, 100)
                .Select(i => new User
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    Age = 20 + (i % 50),
                    IsActive = i % 2 == 0,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert
            var uniqueIds = ids.Distinct().ToList();
            Assert.AreEqual(ids.Count, uniqueIds.Count, "All IDs should be unique");
        }

        #endregion

        #region Property 6.4: All IDs Are Valid (Exist in Database)

        /// <summary>
        /// Property: All returned IDs must exist in the database and be retrievable.
        /// </summary>
        [TestMethod]
        public async Task Property_AllIds_ExistInDatabase()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            var users = Enumerable.Range(1, 20)
                .Select(i => new User
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    Age = 20 + i,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert - Verify each ID exists in database
            var dbIds = await GetAllIdsAsync();
            foreach (var id in ids)
            {
                Assert.IsTrue(dbIds.Contains(id), $"ID {id} should exist in database");
            }
        }

        #endregion

        #region Property 6.5: Correct Row Count Inserted

        /// <summary>
        /// Property: The number of rows in the database should increase by the number of input entities.
        /// </summary>
        [TestMethod]
        public async Task Property_RowCount_IncreasesCorrectly()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            var initialCount = await GetTableCountAsync();

            var users = Enumerable.Range(1, 15)
                .Select(i => new User
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    Age = 20 + i,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert
            var finalCount = await GetTableCountAsync();
            Assert.AreEqual(initialCount + users.Count, finalCount, 
                $"Table should have {users.Count} more rows");
        }

        #endregion

        #region Property 6.6: Empty List Returns Empty List

        /// <summary>
        /// Property: Inserting an empty list should return an empty list of IDs.
        /// </summary>
        [TestMethod]
        public async Task Property_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            var users = new List<User>();

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert
            Assert.IsNotNull(ids);
            Assert.AreEqual(0, ids.Count, "Empty input should return empty ID list");
        }

        #endregion

        #region Property 6.7: Single Entity Works Correctly

        /// <summary>
        /// Property: Inserting a single entity should work correctly (edge case).
        /// </summary>
        [TestMethod]
        public async Task Property_SingleEntity_WorksCorrectly()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            var users = new List<User>
            {
                new User 
                { 
                    Name = "SingleUser", 
                    Email = "single@test.com", 
                    Age = 25, 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow 
                }
            };

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert
            Assert.AreEqual(1, ids.Count, "Should return exactly one ID");
            Assert.IsTrue(ids[0] > 0, "ID should be positive");
        }

        #endregion

        #region Property 6.8: IDs Are Positive Integers

        /// <summary>
        /// Property: All returned IDs must be positive integers (> 0).
        /// </summary>
        [TestMethod]
        public async Task Property_AllIds_ArePositive()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            var users = Enumerable.Range(1, 30)
                .Select(i => new User
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    Age = 20 + i,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert
            foreach (var id in ids)
            {
                Assert.IsTrue(id > 0, $"ID {id} should be positive");
            }
        }

        #endregion

        #region Property 6.9: Multiple Batches Maintain Order

        /// <summary>
        /// Property: Multiple sequential batch inserts should maintain ID ordering.
        /// </summary>
        [TestMethod]
        public async Task Property_MultipleBatches_MaintainOrder()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);

            // First batch
            var batch1 = Enumerable.Range(1, 10)
                .Select(i => new User
                {
                    Name = $"Batch1_User{i}",
                    Email = $"batch1_{i}@test.com",
                    Age = 20 + i,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            // Second batch
            var batch2 = Enumerable.Range(1, 10)
                .Select(i => new User
                {
                    Name = $"Batch2_User{i}",
                    Email = $"batch2_{i}@test.com",
                    Age = 30 + i,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            // Act
            var ids1 = await repo.BatchInsertAndGetIdsAsync(batch1);
            var ids2 = await repo.BatchInsertAndGetIdsAsync(batch2);

            // Assert
            Assert.AreEqual(10, ids1.Count);
            Assert.AreEqual(10, ids2.Count);

            // All IDs in batch2 should be greater than all IDs in batch1
            var maxId1 = ids1.Max();
            var minId2 = ids2.Min();
            Assert.IsTrue(minId2 > maxId1, "Second batch IDs should be greater than first batch IDs");
        }

        #endregion

        #region Property 6.10: Data Integrity - Inserted Data Matches

        /// <summary>
        /// Property: The data inserted should match the input entities (data integrity).
        /// </summary>
        [TestMethod]
        public async Task Property_InsertedData_MatchesInput()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);
            var queryRepo = new UserQueryRepository(_connection);

            var users = new List<User>
            {
                new User { Name = "Alice", Email = "alice@test.com", Age = 25, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Name = "Bob", Email = "bob@test.com", Age = 30, IsActive = false, CreatedAt = DateTime.UtcNow },
                new User { Name = "Charlie", Email = "charlie@test.com", Age = 35, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            // Act
            var ids = await repo.BatchInsertAndGetIdsAsync(users);

            // Assert - Verify each entity was inserted correctly
            for (int i = 0; i < users.Count; i++)
            {
                var insertedUser = await queryRepo.GetByIdAsync(ids[i]);
                Assert.IsNotNull(insertedUser, $"User at index {i} should exist");
                Assert.AreEqual(users[i].Name, insertedUser.Name, $"Name mismatch at index {i}");
                Assert.AreEqual(users[i].Email, insertedUser.Email, $"Email mismatch at index {i}");
                Assert.AreEqual(users[i].Age, insertedUser.Age, $"Age mismatch at index {i}");
                Assert.AreEqual(users[i].IsActive, insertedUser.IsActive, $"IsActive mismatch at index {i}");
            }
        }

        #endregion

        // Performance test removed - flaky in CI environments due to timing variations
    }
}
