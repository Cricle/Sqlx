// -----------------------------------------------------------------------
// <copyright file="NullHandlingTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.BoundaryTests;

#region Test Entity

[TableName("null_test_users")]
public class NullTestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NullableEmail { get; set; }
    public int? NullableAge { get; set; }
    public decimal? NullableSalary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? NullableUpdatedAt { get; set; }
}

#endregion

#region Repository

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IQueryRepository<NullTestUser, long>))]
public partial class NullTestUserRepository(IDbConnection connection) : IQueryRepository<NullTestUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

/// <summary>
/// E2E tests for NULL value handling in predefined interfaces.
/// Tests NULL strings, nullable fields, and NULL parameter handling.
/// Requirements: 12.3
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("BoundaryTests")]
[TestCategory("NullHandling")]
public class NullHandlingTests : IDisposable
{
    private SqliteConnection? _connection;
    private NullTestUserRepository? _repo;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await CreateTableAsync();
        _repo = new NullTestUserRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    private async Task CreateTableAsync()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS null_test_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                nullable_email TEXT,
                nullable_age INTEGER,
                nullable_salary REAL,
                created_at TEXT NOT NULL,
                nullable_updated_at TEXT
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<long> InsertUserAsync(string name, string? email = null, int? age = null, decimal? salary = null)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO null_test_users (name, nullable_email, nullable_age, nullable_salary, created_at)
            VALUES (@name, @email, @age, @salary, @createdAt);
            SELECT last_insert_rowid();";
        cmd.Parameters.Add(new SqliteParameter("@name", name));
        cmd.Parameters.Add(new SqliteParameter("@email", (object?)email ?? DBNull.Value));
        cmd.Parameters.Add(new SqliteParameter("@age", (object?)age ?? DBNull.Value));
        cmd.Parameters.Add(new SqliteParameter("@salary", (object?)salary ?? DBNull.Value));
        cmd.Parameters.Add(new SqliteParameter("@createdAt", DateTime.UtcNow.ToString("o")));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    [TestMethod]
    public async Task NullHandling_GetByIdAsync_WithNullFields_ShouldReturnEntityWithNulls()
    {
        // Arrange - Insert user with NULL fields
        var id = await InsertUserAsync("User With Nulls");

        // Act
        var user = await _repo!.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("User With Nulls", user.Name);
        Assert.IsNull(user.NullableEmail);
        Assert.IsNull(user.NullableAge);
        Assert.IsNull(user.NullableSalary);
        Assert.IsNull(user.NullableUpdatedAt);
    }

    [TestMethod]
    public async Task NullHandling_GetByIdAsync_WithNonNullFields_ShouldReturnEntityWithValues()
    {
        // Arrange - Insert user with all fields populated
        var id = await InsertUserAsync("User With Values", "test@example.com", 30, 50000m);

        // Act
        var user = await _repo!.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("User With Values", user.Name);
        Assert.AreEqual("test@example.com", user.NullableEmail);
        Assert.AreEqual(30, user.NullableAge);
        Assert.AreEqual(50000m, user.NullableSalary);
    }

    [TestMethod]
    public async Task NullHandling_GetWhereAsync_FilteringNullFields_ShouldWork()
    {
        // Arrange
        await InsertUserAsync("User1", "user1@test.com", 25);
        await InsertUserAsync("User2", null, 30);
        await InsertUserAsync("User3", "user3@test.com", null);

        // Act - Filter for users with non-null email
        var usersWithEmail = await _repo!.GetWhereAsync(u => u.NullableEmail != null);

        // Assert
        Assert.AreEqual(2, usersWithEmail.Count);
    }

    [TestMethod]
    public async Task NullHandling_GetWhereAsync_FilteringNullAge_ShouldWork()
    {
        // Arrange
        await InsertUserAsync("User1", null, 25);
        await InsertUserAsync("User2", null, null);
        await InsertUserAsync("User3", null, 35);

        // Act - Filter for users with non-null age
        var usersWithAge = await _repo!.GetWhereAsync(u => u.NullableAge != null);

        // Assert
        Assert.AreEqual(2, usersWithAge.Count);
    }

    [TestMethod]
    public async Task NullHandling_GetAllAsync_MixedNullValues_ShouldReturnAllEntities()
    {
        // Arrange - Insert users with various null combinations
        await InsertUserAsync("AllNull");
        await InsertUserAsync("PartialNull", "email@test.com");
        await InsertUserAsync("NoNull", "email2@test.com", 25, 50000m);

        // Act
        var users = await _repo!.GetAllAsync();

        // Assert
        Assert.AreEqual(3, users.Count);
    }

    [TestMethod]
    public async Task NullHandling_ExistsWhereAsync_WithNullCheck_ShouldWork()
    {
        // Arrange
        await InsertUserAsync("User1", null);
        await InsertUserAsync("User2", "email@test.com");

        // Act
        var hasNullEmail = await _repo!.ExistsWhereAsync(u => u.NullableEmail == null);
        var hasNonNullEmail = await _repo!.ExistsWhereAsync(u => u.NullableEmail != null);

        // Assert
        Assert.IsTrue(hasNullEmail);
        Assert.IsTrue(hasNonNullEmail);
    }
}
