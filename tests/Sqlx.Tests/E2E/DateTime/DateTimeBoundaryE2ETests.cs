// <copyright file="DateTimeBoundaryE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.DateTimeBoundary;

/// <summary>
/// E2E tests for date-time boundary values in Sqlx across all supported databases.
/// Tests focus on Sqlx's parameter binding and type conversion with date-time edge cases.
/// </summary>
[TestClass]
public class DateTimeBoundaryE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE datetime_test (
                    id INT PRIMARY KEY AUTO_INCREMENT,
                    test_datetime DATETIME(6),
                    test_date DATE,
                    test_time TIME(6)
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE datetime_test (
                    id SERIAL PRIMARY KEY,
                    test_datetime TIMESTAMP,
                    test_date DATE,
                    test_time TIME
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE datetime_test (
                    id INT PRIMARY KEY IDENTITY(1,1),
                    test_datetime DATETIME2(7),
                    test_date DATE,
                    test_time TIME(7)
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE datetime_test (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    test_datetime TEXT,
                    test_date TEXT,
                    test_time TEXT
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

    // ==================== Min/Max Date Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_MinDate_RoundTrip()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var minDate = DateTimeDataGenerator.GetMinDate(DatabaseType.MySQL);

        // Act - Test Sqlx parameter binding with min date
        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
        AddParameter(insertCmd, "@datetime", minDate);
        await insertCmd.ExecuteNonQueryAsync();

        // Assert - Test Sqlx type conversion preserves min date
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = LAST_INSERT_ID()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        // MySQL DATETIME has second precision, so compare with tolerance
        Assert.AreEqual(minDate.Year, retrieved.Year);
        Assert.AreEqual(minDate.Month, retrieved.Month);
        Assert.AreEqual(minDate.Day, retrieved.Day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_MinDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var minDate = DateTimeDataGenerator.GetMinDate(DatabaseType.PostgreSQL);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime) RETURNING id";
        AddParameter(insertCmd, "@datetime", minDate);
        var id = await insertCmd.ExecuteScalarAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = @id";
        AddParameter(selectCmd, "@id", id);
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        Assert.AreEqual(minDate.Year, retrieved.Year);
        Assert.AreEqual(minDate.Month, retrieved.Month);
        Assert.AreEqual(minDate.Day, retrieved.Day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_MinDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var minDate = DateTimeDataGenerator.GetMinDate(DatabaseType.SqlServer);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime); SELECT SCOPE_IDENTITY()";
        AddParameter(insertCmd, "@datetime", minDate);
        var id = await insertCmd.ExecuteScalarAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = @id";
        AddParameter(selectCmd, "@id", id);
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        Assert.AreEqual(minDate.Year, retrieved.Year);
        Assert.AreEqual(minDate.Month, retrieved.Month);
        Assert.AreEqual(minDate.Day, retrieved.Day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_MinDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var minDate = DateTimeDataGenerator.GetMinDate(DatabaseType.SQLite);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
        AddParameter(insertCmd, "@datetime", minDate.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = last_insert_rowid()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrievedStr = reader.GetString(0);
        var retrieved = System.DateTime.Parse(retrievedStr);

        Assert.AreEqual(minDate.Year, retrieved.Year);
        Assert.AreEqual(minDate.Month, retrieved.Month);
        Assert.AreEqual(minDate.Day, retrieved.Day);
    }

    // ==================== Max Date Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_MaxDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var maxDate = DateTimeDataGenerator.GetMaxDate(DatabaseType.MySQL);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
        AddParameter(insertCmd, "@datetime", maxDate);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = LAST_INSERT_ID()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        Assert.AreEqual(maxDate.Year, retrieved.Year);
        Assert.AreEqual(maxDate.Month, retrieved.Month);
        Assert.AreEqual(maxDate.Day, retrieved.Day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_MaxDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var maxDate = DateTimeDataGenerator.GetMaxDate(DatabaseType.PostgreSQL);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime) RETURNING id";
        AddParameter(insertCmd, "@datetime", maxDate);
        var id = await insertCmd.ExecuteScalarAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = @id";
        AddParameter(selectCmd, "@id", id);
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        Assert.AreEqual(maxDate.Year, retrieved.Year);
        Assert.AreEqual(maxDate.Month, retrieved.Month);
        Assert.AreEqual(maxDate.Day, retrieved.Day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_MaxDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var maxDate = DateTimeDataGenerator.GetMaxDate(DatabaseType.SqlServer);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime); SELECT SCOPE_IDENTITY()";
        AddParameter(insertCmd, "@datetime", maxDate);
        var id = await insertCmd.ExecuteScalarAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = @id";
        AddParameter(selectCmd, "@id", id);
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        Assert.AreEqual(maxDate.Year, retrieved.Year);
        Assert.AreEqual(maxDate.Month, retrieved.Month);
        Assert.AreEqual(maxDate.Day, retrieved.Day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_MaxDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var maxDate = DateTimeDataGenerator.GetMaxDate(DatabaseType.SQLite);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
        AddParameter(insertCmd, "@datetime", maxDate.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_datetime FROM datetime_test WHERE id = last_insert_rowid()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrievedStr = reader.GetString(0);
        var retrieved = System.DateTime.Parse(retrievedStr);

        Assert.AreEqual(maxDate.Year, retrieved.Year);
        Assert.AreEqual(maxDate.Month, retrieved.Month);
        Assert.AreEqual(maxDate.Day, retrieved.Day);
    }

    // ==================== Time Precision Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_MidnightAndEndOfDay_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var midnight = DateTimeDataGenerator.GetMidnightDate();
        var endOfDay = DateTimeDataGenerator.GetEndOfDayDate();

        // Test midnight
        using (var insertCmd1 = fixture.Connection.CreateCommand())
        {
            insertCmd1.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
            AddParameter(insertCmd1, "@datetime", midnight);
            await insertCmd1.ExecuteNonQueryAsync();
        }

        // Test end of day
        using (var insertCmd2 = fixture.Connection.CreateCommand())
        {
            insertCmd2.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
            AddParameter(insertCmd2, "@datetime", endOfDay);
            await insertCmd2.ExecuteNonQueryAsync();
        }

        // Verify midnight
        using (var selectCmd1 = fixture.Connection.CreateCommand())
        {
            selectCmd1.CommandText = "SELECT test_datetime FROM datetime_test ORDER BY id LIMIT 1";
            using var reader1 = await selectCmd1.ExecuteReaderAsync();
            Assert.IsTrue(await reader1.ReadAsync());
            var retrievedMidnight = reader1.GetDateTime(0);
            Assert.AreEqual(0, retrievedMidnight.Hour);
            Assert.AreEqual(0, retrievedMidnight.Minute);
            Assert.AreEqual(0, retrievedMidnight.Second);
        }

        // Verify end of day
        using (var selectCmd2 = fixture.Connection.CreateCommand())
        {
            selectCmd2.CommandText = "SELECT test_datetime FROM datetime_test ORDER BY id DESC LIMIT 1";
            using var reader2 = await selectCmd2.ExecuteReaderAsync();
            Assert.IsTrue(await reader2.ReadAsync());
            var retrievedEndOfDay = reader2.GetDateTime(0);
            Assert.AreEqual(23, retrievedEndOfDay.Hour);
            Assert.AreEqual(59, retrievedEndOfDay.Minute);
            Assert.AreEqual(59, retrievedEndOfDay.Second);
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_MidnightAndEndOfDay_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var midnight = DateTimeDataGenerator.GetMidnightDate();
        var endOfDay = DateTimeDataGenerator.GetEndOfDayDate();

        using (var insertCmd1 = fixture.Connection.CreateCommand())
        {
            insertCmd1.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
            AddParameter(insertCmd1, "@datetime", midnight);
            await insertCmd1.ExecuteNonQueryAsync();
        }

        using (var insertCmd2 = fixture.Connection.CreateCommand())
        {
            insertCmd2.CommandText = "INSERT INTO datetime_test (test_datetime) VALUES (@datetime)";
            AddParameter(insertCmd2, "@datetime", endOfDay);
            await insertCmd2.ExecuteNonQueryAsync();
        }

        using (var selectCmd1 = fixture.Connection.CreateCommand())
        {
            selectCmd1.CommandText = "SELECT test_datetime FROM datetime_test ORDER BY id LIMIT 1";
            using var reader1 = await selectCmd1.ExecuteReaderAsync();
            Assert.IsTrue(await reader1.ReadAsync());
            var retrievedMidnight = reader1.GetDateTime(0);
            Assert.AreEqual(0, retrievedMidnight.Hour);
            Assert.AreEqual(0, retrievedMidnight.Minute);
            Assert.AreEqual(0, retrievedMidnight.Second);
        }

        using (var selectCmd2 = fixture.Connection.CreateCommand())
        {
            selectCmd2.CommandText = "SELECT test_datetime FROM datetime_test ORDER BY id DESC LIMIT 1";
            using var reader2 = await selectCmd2.ExecuteReaderAsync();
            Assert.IsTrue(await reader2.ReadAsync());
            var retrievedEndOfDay = reader2.GetDateTime(0);
            Assert.AreEqual(23, retrievedEndOfDay.Hour);
            Assert.AreEqual(59, retrievedEndOfDay.Minute);
            Assert.AreEqual(59, retrievedEndOfDay.Second);
        }
    }

    // ==================== Leap Year Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_LeapYearDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var leapYearDate = DateTimeDataGenerator.GetLeapYearDate();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_date) VALUES (@date)";
        AddParameter(insertCmd, "@date", leapYearDate);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_date FROM datetime_test WHERE id = LAST_INSERT_ID()";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        Assert.AreEqual(leapYearDate.Year, retrieved.Year);
        Assert.AreEqual(leapYearDate.Month, retrieved.Month);
        Assert.AreEqual(leapYearDate.Day, retrieved.Day);
        Assert.AreEqual(2, retrieved.Month); // February
        Assert.AreEqual(29, retrieved.Day); // 29th
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTime")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_LeapYearDate_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var leapYearDate = DateTimeDataGenerator.GetLeapYearDate();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO datetime_test (test_date) VALUES (@date) RETURNING id";
        AddParameter(insertCmd, "@date", leapYearDate);
        var id = await insertCmd.ExecuteScalarAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT test_date FROM datetime_test WHERE id = @id";
        AddParameter(selectCmd, "@id", id);
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetDateTime(0);

        Assert.AreEqual(leapYearDate.Year, retrieved.Year);
        Assert.AreEqual(2, retrieved.Month);
        Assert.AreEqual(29, retrieved.Day);
    }
}
