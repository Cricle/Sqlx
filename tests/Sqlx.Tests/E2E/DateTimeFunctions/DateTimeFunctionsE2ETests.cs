// <copyright file="DateTimeFunctionsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.DateTimeFunctions;

/// <summary>
/// E2E tests for SQL date/time functions.
/// </summary>
[TestClass]
public class DateTimeFunctionsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_events (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    event_name VARCHAR(100) NOT NULL,
                    event_date DATE NOT NULL,
                    event_timestamp DATETIME NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_events (
                    id BIGSERIAL PRIMARY KEY,
                    event_name VARCHAR(100) NOT NULL,
                    event_date DATE NOT NULL,
                    event_timestamp TIMESTAMP NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_events (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    event_name NVARCHAR(100) NOT NULL,
                    event_date DATE NOT NULL,
                    event_timestamp DATETIME2 NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    event_name TEXT NOT NULL,
                    event_date TEXT NOT NULL,
                    event_timestamp TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        var events = new[]
        {
            ("Event A", "2024-01-15", "2024-01-15 10:30:00"),
            ("Event B", "2024-02-20", "2024-02-20 14:45:00"),
            ("Event C", "2024-03-10", "2024-03-10 09:15:00"),
        };

        foreach (var (name, date, timestamp) in events)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_events (event_name, event_date, event_timestamp) VALUES (@name, @date, @timestamp)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@date", date);
            AddParameter(cmd, "@timestamp", timestamp);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== YEAR/MONTH/DAY Extraction Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_ExtractDateParts_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT event_name,
                   YEAR(event_date) as year,
                   MONTH(event_date) as month,
                   DAY(event_date) as day
            FROM test_events
            WHERE event_name = 'Event A'";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var name = reader.GetString(0);
        var year = reader.GetInt32(1);
        var month = reader.GetInt32(2);
        var day = reader.GetInt32(3);

        // Assert
        Assert.AreEqual("Event A", name);
        Assert.AreEqual(2024, year);
        Assert.AreEqual(1, month);
        Assert.AreEqual(15, day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ExtractDateParts_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT event_name,
                   EXTRACT(YEAR FROM event_date) as year,
                   EXTRACT(MONTH FROM event_date) as month,
                   EXTRACT(DAY FROM event_date) as day
            FROM test_events
            WHERE event_name = 'Event A'";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var name = reader.GetString(0);
        var year = Convert.ToInt32(reader.GetDouble(1));
        var month = Convert.ToInt32(reader.GetDouble(2));
        var day = Convert.ToInt32(reader.GetDouble(3));

        // Assert
        Assert.AreEqual("Event A", name);
        Assert.AreEqual(2024, year);
        Assert.AreEqual(1, month);
        Assert.AreEqual(15, day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ExtractDateParts_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT event_name,
                   YEAR(event_date) as year,
                   MONTH(event_date) as month,
                   DAY(event_date) as day
            FROM test_events
            WHERE event_name = 'Event A'";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var name = reader.GetString(0);
        var year = reader.GetInt32(1);
        var month = reader.GetInt32(2);
        var day = reader.GetInt32(3);

        // Assert
        Assert.AreEqual("Event A", name);
        Assert.AreEqual(2024, year);
        Assert.AreEqual(1, month);
        Assert.AreEqual(15, day);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_ExtractDateParts_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT event_name,
                   CAST(strftime('%Y', event_date) AS INTEGER) as year,
                   CAST(strftime('%m', event_date) AS INTEGER) as month,
                   CAST(strftime('%d', event_date) AS INTEGER) as day
            FROM test_events
            WHERE event_name = 'Event A'";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var name = reader.GetString(0);
        var year = reader.GetInt64(1);
        var month = reader.GetInt64(2);
        var day = reader.GetInt64(3);

        // Assert
        Assert.AreEqual("Event A", name);
        Assert.AreEqual(2024, year);
        Assert.AreEqual(1, month);
        Assert.AreEqual(15, day);
    }

    // ==================== Date Arithmetic Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_DateAdd_AddsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT event_name,
                   DATE_ADD(event_date, INTERVAL 7 DAY) as week_later
            FROM test_events
            WHERE event_name = 'Event A'";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var weekLater = reader.GetDateTime(1);

        // Assert
        Assert.AreEqual(new DateTime(2024, 1, 22), weekLater.Date);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_DateAdd_AddsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT event_name,
                   date(event_date, '+7 days') as week_later
            FROM test_events
            WHERE event_name = 'Event A'";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var weekLater = reader.GetString(1);

        // Assert
        Assert.AreEqual("2024-01-22", weekLater);
    }

    // ==================== Date Difference Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_DateDiff_CalculatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT DATEDIFF(
                (SELECT event_date FROM test_events WHERE event_name = 'Event B'),
                (SELECT event_date FROM test_events WHERE event_name = 'Event A')
            ) as days_diff";

        var daysDiff = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert - Feb 20 - Jan 15 = 36 days
        Assert.AreEqual(36, daysDiff);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("DateTimeFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_DateDiff_CalculatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT CAST(julianday(
                (SELECT event_date FROM test_events WHERE event_name = 'Event B')
            ) - julianday(
                (SELECT event_date FROM test_events WHERE event_name = 'Event A')
            ) AS INTEGER) as days_diff";

        var daysDiff = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(36, daysDiff);
    }
}
