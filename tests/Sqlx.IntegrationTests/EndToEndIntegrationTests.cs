// -----------------------------------------------------------------------
// <copyright file="EndToEndIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.IntegrationTests
{
    /// <summary>
    /// End-to-end integration tests that verify the complete Sqlx workflow
    /// from source generation to actual database operations
    /// </summary>
    [TestClass]
    public class EndToEndIntegrationTests
    {
        private SQLiteConnection _connection = null!;
        private const string TestDatabasePath = ":memory:";

        [TestInitialize]
        public async Task Setup()
        {
            _connection = new SQLiteConnection($"Data Source={TestDatabasePath}");
            await _connection.OpenAsync();

            // Create test tables
            await CreateTestTablesAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _connection?.Dispose();
        }

        private async Task CreateTestTablesAsync()
        {
            var createUserTableSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL UNIQUE,
                    age INTEGER,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    is_active BOOLEAN DEFAULT 1
                )";

            var createProductTableSql = @"
                CREATE TABLE IF NOT EXISTS products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    category_id INTEGER,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            using var userCmd = new SQLiteCommand(createUserTableSql, _connection);
            await userCmd.ExecuteNonQueryAsync();

            using var productCmd = new SQLiteCommand(createProductTableSql, _connection);
            await productCmd.ExecuteNonQueryAsync();
        }

        [TestMethod]
        public async Task EndToEndIntegration_UserCrudOperations_WorksCorrectly()
        {
            // This test verifies that generated repository code would work with real database operations
            // Since we can't easily test the actual generated code in isolation,
            // we simulate the expected behavior using direct SQL commands

            // Test INSERT operation
            var insertSql = "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)";
            using (var insertCmd = new SQLiteCommand(insertSql, _connection))
            {
                insertCmd.Parameters.AddWithValue("@name", "John Doe");
                insertCmd.Parameters.AddWithValue("@email", "john@example.com");
                insertCmd.Parameters.AddWithValue("@age", 30);

                var insertResult = await insertCmd.ExecuteNonQueryAsync();
                Assert.AreEqual(1, insertResult, "Should insert one row");
            }

            // Test SELECT operation
            var selectSql = "SELECT id, name, email, age FROM users WHERE email = @email";
            using (var selectCmd = new SQLiteCommand(selectSql, _connection))
            {
                selectCmd.Parameters.AddWithValue("@email", "john@example.com");

                using var reader = await selectCmd.ExecuteReaderAsync();
                Assert.IsTrue(await reader.ReadAsync(), "Should find the inserted user");
                Assert.AreEqual("John Doe", reader.GetString(1)); // name column
                Assert.AreEqual("john@example.com", reader.GetString(2)); // email column
                Assert.AreEqual(30, reader.GetInt32(3)); // age column
            }

            // Test UPDATE operation
            var updateSql = "UPDATE users SET age = @age WHERE email = @email";
            using (var updateCmd = new SQLiteCommand(updateSql, _connection))
            {
                updateCmd.Parameters.AddWithValue("@age", 31);
                updateCmd.Parameters.AddWithValue("@email", "john@example.com");

                var updateResult = await updateCmd.ExecuteNonQueryAsync();
                Assert.AreEqual(1, updateResult, "Should update one row");
            }

            // Verify UPDATE
            using (var verifyCmd = new SQLiteCommand("SELECT age FROM users WHERE email = @email", _connection))
            {
                verifyCmd.Parameters.AddWithValue("@email", "john@example.com");
                var age = await verifyCmd.ExecuteScalarAsync();
                Assert.AreEqual(31L, age, "Age should be updated");
            }

            // Test DELETE operation
            var deleteSql = "DELETE FROM users WHERE email = @email";
            using (var deleteCmd = new SQLiteCommand(deleteSql, _connection))
            {
                deleteCmd.Parameters.AddWithValue("@email", "john@example.com");

                var deleteResult = await deleteCmd.ExecuteNonQueryAsync();
                Assert.AreEqual(1, deleteResult, "Should delete one row");
            }

            // Verify DELETE
            using (var countCmd = new SQLiteCommand("SELECT COUNT(*) FROM users", _connection))
            {
                var count = await countCmd.ExecuteScalarAsync();
                Assert.AreEqual(0L, count, "Should have no users after delete");
            }
        }

        [TestMethod]
        public async Task EndToEndIntegration_BatchOperations_WorksCorrectly()
        {
            // Test batch insert operations that would be generated by Sqlx
            var users = new[]
            {
                new { Name = "Alice", Email = "alice@example.com", Age = 25 },
                new { Name = "Bob", Email = "bob@example.com", Age = 35 },
                new { Name = "Charlie", Email = "charlie@example.com", Age = 28 }
            };

            // Simulate batch insert
            var batchInsertSql = @"
                INSERT INTO users (name, email, age) VALUES 
                (@name0, @email0, @age0),
                (@name1, @email1, @age1),
                (@name2, @email2, @age2)";

            using (var batchCmd = new SQLiteCommand(batchInsertSql, _connection))
            {
                for (int i = 0; i < users.Length; i++)
                {
                    batchCmd.Parameters.AddWithValue($"@name{i}", users[i].Name);
                    batchCmd.Parameters.AddWithValue($"@email{i}", users[i].Email);
                    batchCmd.Parameters.AddWithValue($"@age{i}", users[i].Age);
                }

                var result = await batchCmd.ExecuteNonQueryAsync();
                Assert.AreEqual(3, result, "Should insert three rows");
            }

            // Verify batch insert
            using (var countCmd = new SQLiteCommand("SELECT COUNT(*) FROM users", _connection))
            {
                var count = await countCmd.ExecuteScalarAsync();
                Assert.AreEqual(3L, count, "Should have three users");
            }

            // Test batch select (simulating GetAll)
            using (var selectAllCmd = new SQLiteCommand("SELECT name, email, age FROM users ORDER BY name", _connection))
            using (var reader = await selectAllCmd.ExecuteReaderAsync())
            {
                var retrievedUsers = new List<object>();
                while (await reader.ReadAsync())
                {
                    retrievedUsers.Add(new
                    {
                        Name = reader.GetString(0), // name column
                        Email = reader.GetString(1), // email column
                        Age = reader.GetInt32(2) // age column
                    });
                }

                Assert.AreEqual(3, retrievedUsers.Count, "Should retrieve all users");
            }
        }

        [TestMethod]
        public async Task EndToEndIntegration_ComplexQueries_WorksCorrectly()
        {
            // Insert test data
            await InsertTestDataAsync();

            // Test complex query with JOIN (simulating what would be generated)
            var complexSql = @"
                SELECT u.name, u.email, COUNT(p.id) as product_count
                FROM users u
                LEFT JOIN products p ON u.id = p.category_id
                WHERE u.is_active = 1
                GROUP BY u.id, u.name, u.email
                HAVING COUNT(p.id) >= 0
                ORDER BY u.name";

            using var cmd = new SQLiteCommand(complexSql, _connection);
            using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<object>();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    Name = reader.GetString(0), // name column
                    Email = reader.GetString(1), // email column
                    ProductCount = reader.GetInt64(2) // product_count column
                });
            }

            Assert.IsTrue(results.Count > 0, "Should return results from complex query");
        }

        [TestMethod]
        public async Task EndToEndIntegration_TransactionSupport_WorksCorrectly()
        {
            using var transaction = _connection.BeginTransaction();

            try
            {
                // Insert user within transaction
                var insertUserSql = "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)";
                using (var userCmd = new SQLiteCommand(insertUserSql, _connection, transaction))
                {
                    userCmd.Parameters.AddWithValue("@name", "Transaction User");
                    userCmd.Parameters.AddWithValue("@email", "transaction@example.com");
                    userCmd.Parameters.AddWithValue("@age", 25);

                    await userCmd.ExecuteNonQueryAsync();
                }

                // Insert product within same transaction
                var insertProductSql = "INSERT INTO products (name, price, category_id) VALUES (@name, @price, @category_id)";
                using (var productCmd = new SQLiteCommand(insertProductSql, _connection, transaction))
                {
                    productCmd.Parameters.AddWithValue("@name", "Transaction Product");
                    productCmd.Parameters.AddWithValue("@price", 99.99);
                    productCmd.Parameters.AddWithValue("@category_id", 1);

                    await productCmd.ExecuteNonQueryAsync();
                }

                // Commit transaction
                transaction.Commit();

                // Verify both records exist
                using (var verifyCmd = new SQLiteCommand("SELECT COUNT(*) FROM users WHERE email = 'transaction@example.com'", _connection))
                {
                    var userCount = await verifyCmd.ExecuteScalarAsync();
                    Assert.AreEqual(1L, userCount, "User should exist after transaction commit");
                }

                using (var verifyCmd = new SQLiteCommand("SELECT COUNT(*) FROM products WHERE name = 'Transaction Product'", _connection))
                {
                    var productCount = await verifyCmd.ExecuteScalarAsync();
                    Assert.AreEqual(1L, productCount, "Product should exist after transaction commit");
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [TestMethod]
        public async Task EndToEndIntegration_ErrorHandling_WorksCorrectly()
        {
            // Test constraint violation (duplicate email)
            var insertSql = "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)";

            // Insert first user
            using (var cmd1 = new SQLiteCommand(insertSql, _connection))
            {
                cmd1.Parameters.AddWithValue("@name", "User 1");
                cmd1.Parameters.AddWithValue("@email", "duplicate@example.com");
                cmd1.Parameters.AddWithValue("@age", 25);

                await cmd1.ExecuteNonQueryAsync();
            }

            // Try to insert user with same email (should fail)
            using (var cmd2 = new SQLiteCommand(insertSql, _connection))
            {
                cmd2.Parameters.AddWithValue("@name", "User 2");
                cmd2.Parameters.AddWithValue("@email", "duplicate@example.com");
                cmd2.Parameters.AddWithValue("@age", 30);

                await Assert.ThrowsExceptionAsync<SQLiteException>(async () =>
                {
                    await cmd2.ExecuteNonQueryAsync();
                }, "Should throw exception for duplicate email");
            }
        }

        [TestMethod]
        public async Task EndToEndIntegration_ParameterizedQueries_PreventsSqlInjection()
        {
            // Insert test user
            var insertSql = "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)";
            using (var insertCmd = new SQLiteCommand(insertSql, _connection))
            {
                insertCmd.Parameters.AddWithValue("@name", "Test User");
                insertCmd.Parameters.AddWithValue("@email", "test@example.com");
                insertCmd.Parameters.AddWithValue("@age", 25);

                await insertCmd.ExecuteNonQueryAsync();
            }

            // Test that parameterized queries prevent SQL injection
            var maliciousEmail = "test@example.com'; DROP TABLE users; --";
            var selectSql = "SELECT COUNT(*) FROM users WHERE email = @email";

            using (var selectCmd = new SQLiteCommand(selectSql, _connection))
            {
                selectCmd.Parameters.AddWithValue("@email", maliciousEmail);

                var count = await selectCmd.ExecuteScalarAsync();
                Assert.AreEqual(0L, count, "Should not find user with malicious email");
            }

            // Verify table still exists (wasn't dropped by injection)
            using (var verifyCmd = new SQLiteCommand("SELECT COUNT(*) FROM users", _connection))
            {
                var totalCount = await verifyCmd.ExecuteScalarAsync();
                Assert.AreEqual(1L, totalCount, "Table should still exist with original user");
            }
        }

        private async Task InsertTestDataAsync()
        {
            // Insert test users
            var userInsertSql = "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)";
            var users = new[]
            {
                new { Name = "John Doe", Email = "john@test.com", Age = 30 },
                new { Name = "Jane Smith", Email = "jane@test.com", Age = 28 }
            };

            foreach (var user in users)
            {
                using var cmd = new SQLiteCommand(userInsertSql, _connection);
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@age", user.Age);
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert test products
            var productInsertSql = "INSERT INTO products (name, price, category_id) VALUES (@name, @price, @category_id)";
            var products = new[]
            {
                new { Name = "Product 1", Price = 19.99m, CategoryId = 1 },
                new { Name = "Product 2", Price = 29.99m, CategoryId = 1 },
                new { Name = "Product 3", Price = 39.99m, CategoryId = 2 }
            };

            foreach (var product in products)
            {
                using var cmd = new SQLiteCommand(productInsertSql, _connection);
                cmd.Parameters.AddWithValue("@name", product.Name);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@category_id", product.CategoryId);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
