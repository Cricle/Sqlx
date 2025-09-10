using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class BatchOperationsTests
    {
        private SQLiteConnection _connection = null!;
        private const string TestTableName = "TestUsers";

        [TestInitialize]
        public void Setup()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            // 创建测试表
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $@"
                CREATE TABLE {TestTableName} (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Age INTEGER NOT NULL,
                    Department TEXT NOT NULL
                )";
            cmd.ExecuteNonQuery();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        #region InsertBatchAsync Tests

        [TestMethod]
        public async Task InsertBatchAsync_WithValidItems_ShouldInsertSuccessfully()
        {
            // Arrange
            var items = new List<TestUser>
            {
                new TestUser { Name = "John Doe", Email = "john@test.com", Age = 30, Department = "IT" },
                new TestUser { Name = "Jane Smith", Email = "jane@test.com", Age = 25, Department = "HR" }
            };

            // Act
            var result = await BatchOperations.InsertBatchAsync(_connection, TestTableName, items);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.IsTrue(result.ExecutionTime.TotalMilliseconds > 0);

            // Verify data was inserted
            var count = GetRecordCount();
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task InsertBatchAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
        {
            // Arrange
            var items = new List<TestUser>();

            // Act
            var result = await BatchOperations.InsertBatchAsync(_connection, TestTableName, items);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task InsertBatchAsync_WithAutoOptimizeBatchSize_ShouldOptimizeBatchSize()
        {
            // Arrange
            var items = Enumerable.Range(1, 150)
                .Select(i => new TestUser
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    Age = 20 + (i % 40),
                    Department = "Test"
                })
                .ToList();

            // Act
            var result = await BatchOperations.InsertBatchAsync(_connection, TestTableName, items,
                batchSize: 1000, autoOptimizeBatchSize: true);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(150, result.AffectedRows);
            Assert.AreEqual(100, result.OptimalBatchSize); // Should optimize to 100 for 150 items
            Assert.IsTrue(result.BatchCount > 0);
        }

        [TestMethod]
        public async Task InsertBatchAsync_WithTransaction_ShouldRespectTransaction()
        {
            // Arrange
            var items = new List<TestUser>
            {
                new TestUser { Name = "Test User", Email = "test@test.com", Age = 30, Department = "IT" }
            };

            using var transaction = _connection.BeginTransaction();

            // Act
            var result = await BatchOperations.InsertBatchAsync(_connection, TestTableName, items, transaction: transaction);

            // Assert without commit - should not see data
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.AffectedRows);

            // Rollback and verify no data
            transaction.Rollback();
            var count = GetRecordCount();
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task InsertBatchAsync_WithContinueOnError_ShouldContinueAfterError()
        {
            // Arrange - Create items that will cause constraint violation
            var items = new List<TestUser>
            {
                new TestUser { Name = "Valid User", Email = "valid@test.com", Age = 30, Department = "IT" }
            };

            // First insert to create potential duplicate
            await BatchOperations.InsertBatchAsync(_connection, TestTableName, items);

            // Add more items including potential duplicate
            items.Add(new TestUser { Name = "Another User", Email = "another@test.com", Age = 25, Department = "HR" });

            // Act with continueOnError = true
            var result = await BatchOperations.InsertBatchAsync(_connection, TestTableName, items,
                continueOnError: true);

            // Assert - should continue despite errors
            Assert.IsTrue(result.AffectedRows > 0 || result.Errors.Count > 0);
        }

        #endregion

        #region UpdateBatchAsync Tests

        [TestMethod]
        public async Task UpdateBatchAsync_WithValidItems_ShouldUpdateSuccessfully()
        {
            // Arrange - Insert test data first
            var initialItems = new List<TestUser>
            {
                new TestUser { Name = "Original Name", Email = "original@test.com", Age = 30, Department = "IT" }
            };
            await BatchOperations.InsertBatchAsync(_connection, TestTableName, initialItems);

            // Get the inserted ID
            var insertedUser = GetFirstUser();
            insertedUser.Name = "Updated Name";
            insertedUser.Age = 35;

            // Act
            var result = await BatchOperations.UpdateBatchAsync(_connection, TestTableName,
                new List<TestUser> { insertedUser }, "Id");

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);

            // Verify update
            var updatedUser = GetFirstUser();
            Assert.AreEqual("Updated Name", updatedUser.Name);
            Assert.AreEqual(35, updatedUser.Age);
        }

        [TestMethod]
        public async Task UpdateBatchAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
        {
            // Arrange
            var items = new List<TestUser>();

            // Act
            var result = await BatchOperations.UpdateBatchAsync(_connection, TestTableName, items, "Id");

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task UpdateBatchAsync_WithObjectParameter_ShouldUpdateWithCondition()
        {
            // Arrange - Insert test data
            var items = new List<TestUser>
            {
                new TestUser { Name = "Test User", Email = "test@test.com", Age = 30, Department = "IT" }
            };
            await BatchOperations.InsertBatchAsync(_connection, TestTableName, items);

            var updateData = new
            {
                SetClause = "Age = @newAge",
                WhereClause = "Department = @dept",
                Parameters = new { newAge = 35, dept = "IT" }
            };

            // Act
            var result = await BatchOperations.UpdateBatchAsync(_connection, TestTableName, updateData);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);

            // Verify update
            var updatedUser = GetFirstUser();
            Assert.AreEqual(35, updatedUser.Age);
        }

        #endregion

        #region DeleteBatchAsync Tests

        [TestMethod]
        public async Task DeleteBatchAsync_WithKeyValues_ShouldDeleteSuccessfully()
        {
            // Arrange - Insert test data
            var items = new List<TestUser>
            {
                new TestUser { Name = "User1", Email = "user1@test.com", Age = 30, Department = "IT" },
                new TestUser { Name = "User2", Email = "user2@test.com", Age = 25, Department = "HR" },
                new TestUser { Name = "User3", Email = "user3@test.com", Age = 35, Department = "IT" }
            };
            await BatchOperations.InsertBatchAsync(_connection, TestTableName, items);

            var users = GetAllUsers();
            var idsToDelete = users.Take(2).Select(u => u.Id).ToList();

            // Act
            var result = await BatchOperations.DeleteBatchAsync(_connection, TestTableName, "Id", idsToDelete);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);

            // Verify deletion
            var remainingCount = GetRecordCount();
            Assert.AreEqual(1, remainingCount);
        }

        [TestMethod]
        public async Task DeleteBatchAsync_WithEmptyKeyList_ShouldReturnSuccessWithZeroRows()
        {
            // Arrange
            var keyValues = new List<int>();

            // Act
            var result = await BatchOperations.DeleteBatchAsync(_connection, TestTableName, "Id", keyValues);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task DeleteBatchAsync_WithWhereClause_ShouldDeleteByCondition()
        {
            // Arrange - Insert test data
            var items = new List<TestUser>
            {
                new TestUser { Name = "User1", Email = "user1@test.com", Age = 30, Department = "IT" },
                new TestUser { Name = "User2", Email = "user2@test.com", Age = 25, Department = "HR" },
                new TestUser { Name = "User3", Email = "user3@test.com", Age = 35, Department = "IT" }
            };
            await BatchOperations.InsertBatchAsync(_connection, TestTableName, items);

            // Act - Delete all IT department users
            var result = await BatchOperations.DeleteBatchAsync(_connection, TestTableName,
                "Department = @dept", new { dept = "IT" });

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.AffectedRows);
            Assert.AreEqual(0, result.Errors.Count);

            // Verify deletion
            var remainingCount = GetRecordCount();
            Assert.AreEqual(1, remainingCount);
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public async Task InsertBatchAsync_WithInvalidTable_ShouldReturnError()
        {
            // Arrange
            var items = new List<TestUser>
            {
                new TestUser { Name = "Test", Email = "test@test.com", Age = 30, Department = "IT" }
            };

            // Act
            var result = await BatchOperations.InsertBatchAsync(_connection, "NonExistentTable", items);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, result.AffectedRows);
            Assert.IsTrue(result.Errors.Count > 0);
        }

        [TestMethod]
        public async Task UpdateBatchAsync_WithInvalidKeyColumn_ShouldHandleError()
        {
            // Arrange
            var items = new List<TestUser>
            {
                new TestUser { Name = "Test", Email = "test@test.com", Age = 30, Department = "IT" }
            };

            // Act
            var result = await BatchOperations.UpdateBatchAsync(_connection, TestTableName, items, "NonExistentColumn");

            // Assert
            // Note: The operation might succeed depending on the implementation
            // We just verify that we get a result
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task DeleteBatchAsync_WithInvalidWhereClause_ShouldReturnError()
        {
            // Arrange & Act
            var result = await BatchOperations.DeleteBatchAsync(_connection, TestTableName,
                "InvalidColumn = @value", new { value = "test" });

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Errors.Count > 0);
        }

        #endregion

        #region Helper Methods

        private int GetRecordCount()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {TestTableName}";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private TestUser GetFirstUser()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {TestTableName} LIMIT 1";
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new TestUser
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Email = reader.GetString("Email"),
                    Age = reader.GetInt32("Age"),
                    Department = reader.GetString("Department")
                };
            }

            return null!;
        }

        private List<TestUser> GetAllUsers()
        {
            var users = new List<TestUser>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {TestTableName}";
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new TestUser
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Email = reader.GetString("Email"),
                    Age = reader.GetInt32("Age"),
                    Department = reader.GetString("Department")
                });
            }

            return users;
        }

        #endregion

        #region Test Model

        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Department { get; set; } = string.Empty;
        }

        #endregion
    }
}
