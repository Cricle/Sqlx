// -----------------------------------------------------------------------
// <copyright file="TDD_MySqlPostgreSql_Integration_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.Integration;

/// <summary>
/// MySQL 和 PostgreSQL 实际数据库集成测试
/// 使用 Testcontainers 自动管理容器生命周期
/// 验证生成的代码能够正确执行并操作真实数据库
/// </summary>
[TestClass]
[TestCategory(TestCategories.Integration)]
public class TDD_MySqlPostgreSql_Integration_Tests
{
    private static MySqlConnection? _mysqlConnection;
    private static NpgsqlConnection? _postgresConnection;
    
    private static MySqlComprehensiveRepository? _mysqlRepo;
    private static PostgreSqlComprehensiveRepository? _postgresRepo;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // 使用共享容器
        var mysqlTask = Task.Run(async () =>
        {
            try
            {
                var container = AssemblyTestFixture.MySqlContainer;
                if (container == null)
                {
                    context.WriteLine("⚠️ MySQL container not available");
                    return;
                }
                
                // 创建独立数据库
                var dbName = "sqlx_test_mysqlpostgresql";
                var adminConnectionString = container.GetConnectionString();
                using (var adminConn = new MySqlConnection(adminConnectionString))
                {
                    await adminConn.OpenAsync();
                    using (var cmd = adminConn.CreateCommand())
                    {
                        cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}`";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                var builder = new MySqlConnector.MySqlConnectionStringBuilder(adminConnectionString)
                {
                    Database = dbName
                };
                
                _mysqlConnection = new MySqlConnection(builder.ConnectionString);
                await _mysqlConnection.OpenAsync();
                _mysqlRepo = new MySqlComprehensiveRepository(_mysqlConnection);
                
                await CreateMySqlTableAsync(_mysqlConnection);
                context.WriteLine("✅ MySQL ready");
            }
            catch (Exception ex)
            {
                context.WriteLine($"⚠️ MySQL failed: {ex.Message}");
            }
        });

        var postgresTask = Task.Run(async () =>
        {
            try
            {
                var container = AssemblyTestFixture.PostgreSqlContainer;
                if (container == null)
                {
                    context.WriteLine("⚠️ PostgreSQL container not available");
                    return;
                }
                
                // 创建独立数据库
                var dbName = "sqlx_test_mysqlpostgresql_pg";
                var adminConnectionString = container.GetConnectionString();
                using (var adminConn = new NpgsqlConnection(adminConnectionString))
                {
                    await adminConn.OpenAsync();
                    using (var cmd = adminConn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
                        var exists = await cmd.ExecuteScalarAsync() != null;
                        
                        if (!exists)
                        {
                            cmd.CommandText = $"CREATE DATABASE {dbName}";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                var builder = new Npgsql.NpgsqlConnectionStringBuilder(adminConnectionString)
                {
                    Database = dbName
                };
                
                _postgresConnection = new NpgsqlConnection(builder.ConnectionString);
                await _postgresConnection.OpenAsync();
                _postgresRepo = new PostgreSqlComprehensiveRepository(_postgresConnection);
                
                await CreatePostgreSqlTableAsync(_postgresConnection);
                context.WriteLine("✅ PostgreSQL ready");
            }
            catch (Exception ex)
            {
                context.WriteLine($"⚠️ PostgreSQL failed: {ex.Message}");
            }
        });

        await Task.WhenAll(mysqlTask, postgresTask);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (_mysqlConnection != null)
        {
            await _mysqlConnection.DisposeAsync();
        }
        
        if (_postgresConnection != null)
        {
            await _postgresConnection.DisposeAsync();
        }
        
        // 容器由 AssemblyTestFixture 管理，不需要清理
    }

    private static async Task CreateMySqlTableAsync(MySqlConnection connection)
    {
        var sql = @"
            DROP TABLE IF EXISTS comprehensive_test_mysql;
            
            CREATE TABLE comprehensive_test_mysql (
                `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
                `name` VARCHAR(255) NOT NULL,
                `nullable_text` TEXT NULL,
                `special_chars` VARCHAR(500) NOT NULL,
                `int_value` INT NOT NULL,
                `big_int_value` BIGINT NOT NULL,
                `decimal_value` DECIMAL(18,2) NOT NULL,
                `double_value` DOUBLE NOT NULL,
                `float_value` FLOAT NOT NULL,
                `is_active` TINYINT(1) NOT NULL,
                `nullable_bool` TINYINT(1) NULL,
                `created_at` DATETIME NOT NULL,
                `updated_at` DATETIME NULL,
                `category` VARCHAR(100) NOT NULL,
                `status` VARCHAR(50) NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ";
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreatePostgreSqlTableAsync(NpgsqlConnection connection)
    {
        var sql = @"
            DROP TABLE IF EXISTS comprehensive_test_pgsql;
            
            CREATE TABLE comprehensive_test_pgsql (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""name"" VARCHAR(255) NOT NULL,
                ""nullable_text"" TEXT NULL,
                ""special_chars"" VARCHAR(500) NOT NULL,
                ""int_value"" INTEGER NOT NULL,
                ""big_int_value"" BIGINT NOT NULL,
                ""decimal_value"" DECIMAL(18,2) NOT NULL,
                ""double_value"" DOUBLE PRECISION NOT NULL,
                ""float_value"" REAL NOT NULL,
                ""is_active"" BOOLEAN NOT NULL,
                ""nullable_bool"" BOOLEAN NULL,
                ""created_at"" TIMESTAMP NOT NULL,
                ""updated_at"" TIMESTAMP NULL,
                ""category"" VARCHAR(100) NOT NULL,
                ""status"" VARCHAR(50) NOT NULL
            );
        ";
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        // 清理测试数据
        if (_mysqlConnection != null)
        {
            using var cmd = _mysqlConnection.CreateCommand();
            cmd.CommandText = "DELETE FROM comprehensive_test_mysql";
            await cmd.ExecuteNonQueryAsync();
        }
        
        if (_postgresConnection != null)
        {
            using var cmd = _postgresConnection.CreateCommand();
            cmd.CommandText = "DELETE FROM comprehensive_test_pgsql";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // ==================== MySQL 测试 ====================

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_Insert_ShouldReturnValidId()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Act
        var id = await _mysqlRepo.InsertAsync(
            name: "Test Product",
            nullableText: "Description",
            specialChars: "Test with 'quotes' and \"double quotes\"",
            intValue: 42,
            bigintValue: 9999999999L,
            decimalValue: 123.45m,
            doubleValue: 456.789,
            floatValue: 123.45f,
            isActive: true,
            nullableBool: true,
            createdAt: DateTime.Now,
            updatedAt: null,
            category: "electronics",
            status: "active"
        );

        // Assert
        Assert.IsTrue(id > 0, "Insert should return a valid ID");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_GetById_ShouldReturnCorrectData()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Arrange
        var createdAt = DateTime.Now;
        var id = await _mysqlRepo.InsertAsync(
            "Product A", "Desc A", "Special's", 10, 100L, 10.5m, 20.5, 30.5f,
            true, false, createdAt, null, "cat1", "active"
        );

        // Act
        var result = await _mysqlRepo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result, "GetById should return a result");
        Assert.AreEqual(id, result.Id);
        Assert.AreEqual("Product A", result.Name);
        Assert.AreEqual("Desc A", result.NullableText);
        Assert.AreEqual("Special's", result.SpecialChars);
        Assert.AreEqual(10, result.IntValue);
        Assert.AreEqual(100L, result.BigIntValue);
        Assert.AreEqual(10.5m, result.DecimalValue);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(false, result.NullableBool);
        Assert.AreEqual("cat1", result.Category);
        Assert.AreEqual("active", result.Status);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_GetAll_ShouldReturnAllRecords()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Arrange
        await _mysqlRepo.InsertAsync("P1", null, "S1", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");
        await _mysqlRepo.InsertAsync("P2", null, "S2", 2, 2L, 2m, 2.0, 2f, false, null, DateTime.Now, null, "c2", "s2");
        await _mysqlRepo.InsertAsync("P3", null, "S3", 3, 3L, 3m, 3.0, 3f, true, null, DateTime.Now, null, "c3", "s3");

        // Act
        var results = await _mysqlRepo.GetAllAsync();

        // Assert
        Assert.AreEqual(3, results.Count, "Should return 3 records");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_Update_ShouldModifyRecord()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Arrange
        var id = await _mysqlRepo.InsertAsync("Original", "Desc", "S", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");

        // Act
        var updatedAt = DateTime.Now;
        var rowsAffected = await _mysqlRepo.UpdateAsync(id, "Updated Name", "Updated Desc", updatedAt);

        // Assert
        Assert.AreEqual(1, rowsAffected, "Should update 1 row");
        
        var result = await _mysqlRepo.GetByIdAsync(id);
        Assert.IsNotNull(result);
        Assert.AreEqual("Updated Name", result.Name);
        Assert.AreEqual("Updated Desc", result.NullableText);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_Delete_ShouldRemoveRecord()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Arrange
        var id = await _mysqlRepo.InsertAsync("ToDelete", null, "S", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");

        // Act
        var rowsAffected = await _mysqlRepo.DeleteAsync(id);

        // Assert
        Assert.AreEqual(1, rowsAffected, "Should delete 1 row");
        
        var result = await _mysqlRepo.GetByIdAsync(id);
        Assert.IsNull(result, "Record should be deleted");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_NullHandling_ShouldWorkCorrectly()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Arrange - Insert with NULL values
        var id1 = await _mysqlRepo.InsertAsync("P1", null, "S1", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");
        var id2 = await _mysqlRepo.InsertAsync("P2", "Not Null", "S2", 2, 2L, 2m, 2.0, 2f, true, null, DateTime.Now, null, "c2", "s2");

        // Act
        var nullResults = await _mysqlRepo.GetWithNullTextAsync();
        var nonNullResults = await _mysqlRepo.GetWithNonNullTextAsync();

        // Assert
        Assert.AreEqual(1, nullResults.Count, "Should find 1 record with NULL nullable_text");
        Assert.AreEqual(1, nonNullResults.Count, "Should find 1 record with non-NULL nullable_text");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_AggregateFunctions_ShouldReturnCorrectValues()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Arrange
        var id1 = await _mysqlRepo.InsertAsync("P1", null, "S1", 10, 100L, 10.5m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");
        var id2 = await _mysqlRepo.InsertAsync("P2", null, "S2", 20, 200L, 20.5m, 2.0, 2f, true, null, DateTime.Now, null, "c2", "s2");
        var id3 = await _mysqlRepo.InsertAsync("P3", null, "S3", 30, 300L, 30.5m, 3.0, 3f, false, null, DateTime.Now, null, "c3", "s3");
        
        Assert.IsTrue(id1 > 0 && id2 > 0 && id3 > 0, $"All inserts should succeed: {id1}, {id2}, {id3}");

        // Act & Assert
        var count = await _mysqlRepo.CountActiveAsync(true);
        Assert.AreEqual(2, count, "Should count 2 active records");
        
        var countAll = await _mysqlRepo.GetAllAsync();
        Assert.AreEqual(3, countAll.Count, $"Should have 3 total records, got {countAll.Count}");

        // Note: SUM returns NULL when there are no rows, but we have 3 rows
        // If sum is null, it might be a code generation issue
        var sum = await _mysqlRepo.SumDecimalValuesAsync();
        if (sum == null)
        {
            // Fallback: calculate sum manually to verify data
            var manualSum = countAll.Sum(x => x.DecimalValue);
            Assert.Fail($"Sum returned null but manual calculation shows {manualSum}. This indicates a code generation issue.");
        }
        Assert.AreEqual(61.5m, sum.Value, "Sum should be 61.5");

        var avg = await _mysqlRepo.AverageIntValuesAsync();
        Assert.IsNotNull(avg);
        Assert.AreEqual(20.0, avg.Value, 0.1, "Average should be 20");

        var max = await _mysqlRepo.MaxBigIntValueAsync();
        Assert.AreEqual(300L, max, "Max should be 300");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_SpecialCharacters_ShouldBeHandledCorrectly()
    {
        if (_mysqlRepo == null)
        {
            Assert.Inconclusive("MySQL connection not available");
            return;
        }

        // Arrange - Insert with special characters
        var specialString = "Test with 'single' and \"double\" quotes, backslash \\ and unicode: 中文";
        var id = await _mysqlRepo.InsertAsync("Test", null, specialString, 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");

        // Act
        var result = await _mysqlRepo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(specialString, result.SpecialChars, "Special characters should be preserved");
    }

    // ==================== PostgreSQL 测试 ====================

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_Insert_ShouldReturnValidId()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Act
        var id = await _postgresRepo.InsertAsync(
            name: "Test Product",
            nullableText: "Description",
            specialChars: "Test with 'quotes' and \"double quotes\"",
            intValue: 42,
            bigintValue: 9999999999L,
            decimalValue: 123.45m,
            doubleValue: 456.789,
            floatValue: 123.45f,
            isActive: true,
            nullableBool: true,
            createdAt: DateTime.Now,
            updatedAt: null,
            category: "electronics",
            status: "active"
        );

        // Assert
        Assert.IsTrue(id > 0, "Insert should return a valid ID");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_GetById_ShouldReturnCorrectData()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Arrange
        var createdAt = DateTime.Now;
        var id = await _postgresRepo.InsertAsync(
            "Product A", "Desc A", "Special's", 10, 100L, 10.5m, 20.5, 30.5f,
            true, false, createdAt, null, "cat1", "active"
        );

        // Act
        var result = await _postgresRepo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result, "GetById should return a result");
        Assert.AreEqual(id, result.Id);
        Assert.AreEqual("Product A", result.Name);
        Assert.AreEqual("Desc A", result.NullableText);
        Assert.AreEqual("Special's", result.SpecialChars);
        Assert.AreEqual(10, result.IntValue);
        Assert.AreEqual(100L, result.BigIntValue);
        Assert.AreEqual(10.5m, result.DecimalValue);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(false, result.NullableBool);
        Assert.AreEqual("cat1", result.Category);
        Assert.AreEqual("active", result.Status);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_GetAll_ShouldReturnAllRecords()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Arrange
        await _postgresRepo.InsertAsync("P1", null, "S1", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");
        await _postgresRepo.InsertAsync("P2", null, "S2", 2, 2L, 2m, 2.0, 2f, false, null, DateTime.Now, null, "c2", "s2");
        await _postgresRepo.InsertAsync("P3", null, "S3", 3, 3L, 3m, 3.0, 3f, true, null, DateTime.Now, null, "c3", "s3");

        // Act
        var results = await _postgresRepo.GetAllAsync();

        // Assert
        Assert.AreEqual(3, results.Count, "Should return 3 records");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_Update_ShouldModifyRecord()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Arrange
        var id = await _postgresRepo.InsertAsync("Original", "Desc", "S", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");

        // Act
        var updatedAt = DateTime.Now;
        var rowsAffected = await _postgresRepo.UpdateAsync(id, "Updated Name", "Updated Desc", updatedAt);

        // Assert
        Assert.AreEqual(1, rowsAffected, "Should update 1 row");
        
        var result = await _postgresRepo.GetByIdAsync(id);
        Assert.IsNotNull(result);
        Assert.AreEqual("Updated Name", result.Name);
        Assert.AreEqual("Updated Desc", result.NullableText);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_Delete_ShouldRemoveRecord()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Arrange
        var id = await _postgresRepo.InsertAsync("ToDelete", null, "S", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");

        // Act
        var rowsAffected = await _postgresRepo.DeleteAsync(id);

        // Assert
        Assert.AreEqual(1, rowsAffected, "Should delete 1 row");
        
        var result = await _postgresRepo.GetByIdAsync(id);
        Assert.IsNull(result, "Record should be deleted");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_NullHandling_ShouldWorkCorrectly()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Arrange
        var id1 = await _postgresRepo.InsertAsync("P1", null, "S1", 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");
        var id2 = await _postgresRepo.InsertAsync("P2", "Not Null", "S2", 2, 2L, 2m, 2.0, 2f, true, null, DateTime.Now, null, "c2", "s2");

        // Act
        var nullResults = await _postgresRepo.GetWithNullTextAsync();
        var nonNullResults = await _postgresRepo.GetWithNonNullTextAsync();

        // Assert
        Assert.AreEqual(1, nullResults.Count, "Should find 1 record with NULL nullable_text");
        Assert.AreEqual(1, nonNullResults.Count, "Should find 1 record with non-NULL nullable_text");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_AggregateFunctions_ShouldReturnCorrectValues()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Arrange
        await _postgresRepo.InsertAsync("P1", null, "S1", 10, 100L, 10.5m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");
        await _postgresRepo.InsertAsync("P2", null, "S2", 20, 200L, 20.5m, 2.0, 2f, true, null, DateTime.Now, null, "c2", "s2");
        await _postgresRepo.InsertAsync("P3", null, "S3", 30, 300L, 30.5m, 3.0, 3f, false, null, DateTime.Now, null, "c3", "s3");

        // Act & Assert
        var count = await _postgresRepo.CountActiveAsync(true);
        Assert.AreEqual(2, count, "Should count 2 active records");

        var sum = await _postgresRepo.SumDecimalValuesAsync();
        Assert.AreEqual(61.5m, sum, "Sum should be 61.5");

        var avg = await _postgresRepo.AverageIntValuesAsync();
        Assert.IsNotNull(avg);
        Assert.AreEqual(20.0, avg.Value, 0.1, "Average should be 20");

        var max = await _postgresRepo.MaxBigIntValueAsync();
        Assert.AreEqual(300L, max, "Max should be 300");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_SpecialCharacters_ShouldBeHandledCorrectly()
    {
        if (_postgresRepo == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available");
            return;
        }

        // Arrange
        var specialString = "Test with 'single' and \"double\" quotes, backslash \\ and unicode: 中文";
        var id = await _postgresRepo.InsertAsync("Test", null, specialString, 1, 1L, 1m, 1.0, 1f, true, null, DateTime.Now, null, "c1", "s1");

        // Act
        var result = await _postgresRepo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(specialString, result.SpecialChars, "Special characters should be preserved");
    }
}
