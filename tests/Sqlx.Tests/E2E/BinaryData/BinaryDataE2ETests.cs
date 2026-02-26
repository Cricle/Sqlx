// <copyright file="BinaryDataE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.BinaryData;

/// <summary>
/// E2E tests for binary data handling in Sqlx across all supported databases.
/// Tests focus on Sqlx's parameter binding and type conversion with binary data.
/// </summary>
[TestClass]
public class BinaryDataE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE binary_test (
                    id INT PRIMARY KEY AUTO_INCREMENT,
                    data BLOB,
                    small_data VARBINARY(512),
                    medium_data MEDIUMBLOB,
                    large_data LONGBLOB
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE binary_test (
                    id SERIAL PRIMARY KEY,
                    data BYTEA,
                    small_data BYTEA,
                    medium_data BYTEA,
                    large_data BYTEA
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE binary_test (
                    id INT PRIMARY KEY IDENTITY(1,1),
                    data VARBINARY(MAX),
                    small_data VARBINARY(512),
                    medium_data VARBINARY(MAX),
                    large_data VARBINARY(MAX)
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE binary_test (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    data BLOB,
                    small_data BLOB,
                    medium_data BLOB,
                    large_data BLOB
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== Empty Binary Data Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_EmptyBinaryData_DistinguishedFromNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var emptyData = BinaryDataGenerator.GenerateEmpty();

        // Act - Test Sqlx parameter binding with empty binary data
        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertCmd, "@data", emptyData);
            await insertCmd.ExecuteNonQueryAsync();
        }

        // Insert NULL for comparison
        using (var insertNullCmd = fixture.Connection.CreateCommand())
        {
            insertNullCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertNullCmd, "@data", null);
            await insertNullCmd.ExecuteNonQueryAsync();
        }

        // Assert - Verify empty array is distinguished from NULL
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT data FROM binary_test ORDER BY id";
        using var reader = await selectCmd.ExecuteReaderAsync();

        // First record: empty array
        Assert.IsTrue(await reader.ReadAsync());
        Assert.IsFalse(reader.IsDBNull(0), "Empty array should not be NULL");
        var retrieved = (byte[])reader.GetValue(0);
        Assert.AreEqual(0, retrieved.Length, "Empty array should have length 0");

        // Second record: NULL
        Assert.IsTrue(await reader.ReadAsync());
        Assert.IsTrue(reader.IsDBNull(0), "NULL should be NULL");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_EmptyBinaryData_DistinguishedFromNull()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var emptyData = BinaryDataGenerator.GenerateEmpty();

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertCmd, "@data", emptyData);
            await insertCmd.ExecuteNonQueryAsync();
        }

        using (var insertNullCmd = fixture.Connection.CreateCommand())
        {
            insertNullCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertNullCmd, "@data", null);
            await insertNullCmd.ExecuteNonQueryAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT data FROM binary_test ORDER BY id";
        using var reader = await selectCmd.ExecuteReaderAsync();

        Assert.IsTrue(await reader.ReadAsync());
        Assert.IsFalse(reader.IsDBNull(0));
        var retrieved = (byte[])reader.GetValue(0);
        Assert.AreEqual(0, retrieved.Length);

        Assert.IsTrue(await reader.ReadAsync());
        Assert.IsTrue(reader.IsDBNull(0));
    }

    // ==================== Small Binary Data Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_SmallBinaryData_ByteForByteAccuracy()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var smallData = BinaryDataGenerator.GenerateSmall(200);

        // Act - Test Sqlx parameter binding with small binary data
        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (small_data) VALUES (@data)";
            AddParameter(insertCmd, "@data", smallData);
            await insertCmd.ExecuteNonQueryAsync();
        }

        // Assert - Test Sqlx type conversion preserves all bytes
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT small_data FROM binary_test WHERE id = LAST_INSERT_ID()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(smallData.Length, retrieved.Length, "Length should match");
        CollectionAssert.AreEqual(smallData, retrieved, "Bytes should match exactly");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_SmallBinaryData_ByteForByteAccuracy()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var smallData = BinaryDataGenerator.GenerateSmall(200);

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (small_data) VALUES (@data) RETURNING id";
            AddParameter(insertCmd, "@data", smallData);
            var id = await insertCmd.ExecuteScalarAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT small_data FROM binary_test WHERE id = (SELECT MAX(id) FROM binary_test)";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(smallData.Length, retrieved.Length);
        CollectionAssert.AreEqual(smallData, retrieved);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_SmallBinaryData_ByteForByteAccuracy()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var smallData = BinaryDataGenerator.GenerateSmall(200);

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (small_data) VALUES (@data); SELECT SCOPE_IDENTITY()";
            AddParameter(insertCmd, "@data", smallData);
            var id = await insertCmd.ExecuteScalarAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT small_data FROM binary_test WHERE id = (SELECT MAX(id) FROM binary_test)";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(smallData.Length, retrieved.Length);
        CollectionAssert.AreEqual(smallData, retrieved);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_SmallBinaryData_ByteForByteAccuracy()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var smallData = BinaryDataGenerator.GenerateSmall(200);

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (small_data) VALUES (@data)";
            AddParameter(insertCmd, "@data", smallData);
            await insertCmd.ExecuteNonQueryAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT small_data FROM binary_test WHERE id = last_insert_rowid()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(smallData.Length, retrieved.Length);
        CollectionAssert.AreEqual(smallData, retrieved);
    }

    // ==================== Medium Binary Data Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_MediumBinaryData_ByteForByteAccuracy()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var mediumData = BinaryDataGenerator.GenerateMedium();

        // Act - Test Sqlx parameter binding with medium binary data (1KB-1MB)
        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (medium_data) VALUES (@data)";
            AddParameter(insertCmd, "@data", mediumData);
            await insertCmd.ExecuteNonQueryAsync();
        }

        // Assert - Test Sqlx type conversion preserves all bytes
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT medium_data FROM binary_test WHERE id = LAST_INSERT_ID()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(mediumData.Length, retrieved.Length);
        CollectionAssert.AreEqual(mediumData, retrieved);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_MediumBinaryData_ByteForByteAccuracy()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var mediumData = BinaryDataGenerator.GenerateMedium();

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (medium_data) VALUES (@data)";
            AddParameter(insertCmd, "@data", mediumData);
            await insertCmd.ExecuteNonQueryAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT medium_data FROM binary_test WHERE id = (SELECT MAX(id) FROM binary_test)";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(mediumData.Length, retrieved.Length);
        CollectionAssert.AreEqual(mediumData, retrieved);
    }

    // ==================== All Bytes Test ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_AllByteValues_NoCorruption()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var allBytes = BinaryDataGenerator.GenerateAllBytes();

        // Act - Test Sqlx handles all byte values (0-255)
        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertCmd, "@data", allBytes);
            await insertCmd.ExecuteNonQueryAsync();
        }

        // Assert - Verify no byte value is corrupted
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT data FROM binary_test WHERE id = LAST_INSERT_ID()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(256, retrieved.Length, "Should have all 256 byte values");
        for (int i = 0; i < 256; i++)
        {
            Assert.AreEqual((byte)i, retrieved[i], $"Byte value {i} should be preserved");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_AllByteValues_NoCorruption()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var allBytes = BinaryDataGenerator.GenerateAllBytes();

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertCmd, "@data", allBytes);
            await insertCmd.ExecuteNonQueryAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT data FROM binary_test WHERE id = (SELECT MAX(id) FROM binary_test)";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(256, retrieved.Length);
        for (int i = 0; i < 256; i++)
        {
            Assert.AreEqual((byte)i, retrieved[i], $"Byte value {i} should be preserved");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_AllByteValues_NoCorruption()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var allBytes = BinaryDataGenerator.GenerateAllBytes();

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertCmd, "@data", allBytes);
            await insertCmd.ExecuteNonQueryAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT data FROM binary_test WHERE id = (SELECT MAX(id) FROM binary_test)";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(256, retrieved.Length);
        for (int i = 0; i < 256; i++)
        {
            Assert.AreEqual((byte)i, retrieved[i], $"Byte value {i} should be preserved");
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BinaryData")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_AllByteValues_NoCorruption()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var allBytes = BinaryDataGenerator.GenerateAllBytes();

        using (var insertCmd = fixture.Connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO binary_test (data) VALUES (@data)";
            AddParameter(insertCmd, "@data", allBytes);
            await insertCmd.ExecuteNonQueryAsync();
        }

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT data FROM binary_test WHERE id = last_insert_rowid()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = (byte[])reader.GetValue(0);

        Assert.AreEqual(256, retrieved.Length);
        for (int i = 0; i < 256; i++)
        {
            Assert.AreEqual((byte)i, retrieved[i], $"Byte value {i} should be preserved");
        }
    }
}
