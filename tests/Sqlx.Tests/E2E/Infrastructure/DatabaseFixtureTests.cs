// <copyright file="DatabaseFixtureTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.Infrastructure;

[TestClass]
public class DatabaseFixtureTests
{
    [TestMethod]
    public async Task InsertTestDataAsync_WithAnnotatedEntity_InsertsRowsAndSkipsDefaultKey()
    {
        await using var fixture = new DatabaseFixture(DatabaseType.SQLite, "Data Source=:memory:");
        await fixture.InitializeAsync();
        await fixture.CreateSchemaAsync("""
            CREATE TABLE fixture_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                full_name TEXT NOT NULL,
                age INTEGER NOT NULL
            )
            """);

        var inserted = await fixture.InsertTestDataAsync(new[]
        {
            new FixtureUser { Name = "Alice", Age = 25 },
            new FixtureUser { Name = "Bob", Age = 30 },
        });

        Assert.AreEqual(2, inserted);

        var connection = fixture.Connection;
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*), MIN(age), MAX(age) FROM fixture_users";

        using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(2L, reader.GetInt64(0));
        Assert.AreEqual(25, reader.GetInt32(1));
        Assert.AreEqual(30, reader.GetInt32(2));
    }

    [TestMethod]
    public async Task InsertTestDataAsync_WithExplicitKey_PreservesProvidedKey()
    {
        await using var fixture = new DatabaseFixture(DatabaseType.SQLite, "Data Source=:memory:");
        await fixture.InitializeAsync();
        await fixture.CreateSchemaAsync("""
            CREATE TABLE keyed_users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            )
            """);

        var inserted = await fixture.InsertTestDataAsync(new[]
        {
            new KeyedFixtureUser { Id = 42, Name = "Explicit" },
        });

        Assert.AreEqual(1, inserted);

        var connection = fixture.Connection;
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM keyed_users";

        using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(42L, reader.GetInt64(0));
        Assert.AreEqual("Explicit", reader.GetString(1));
    }

    [TestMethod]
    public async Task InsertTestDataAsync_WithDynamicTableNameMethod_UsesResolvedTableName()
    {
        await using var fixture = new DatabaseFixture(DatabaseType.SQLite, "Data Source=:memory:");
        await fixture.InitializeAsync();
        await fixture.CreateSchemaAsync("""
            CREATE TABLE runtime_fixture_users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            )
            """);

        var inserted = await fixture.InsertTestDataAsync(new[]
        {
            new RuntimeFixtureUser { Id = 7, Name = "Runtime" },
        });

        Assert.AreEqual(1, inserted);

        var connection = fixture.Connection;
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM runtime_fixture_users";

        using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(7L, reader.GetInt64(0));
        Assert.AreEqual("Runtime", reader.GetString(1));
    }

    [Sqlx]
    [TableName("fixture_users")]
    public class FixtureUser
    {
        [Key]
        public long Id { get; set; }

        [Column("full_name")]
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }

    [Sqlx]
    [TableName("keyed_users")]
    public class KeyedFixtureUser
    {
        [Key]
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Sqlx]
    [TableName("fallback_runtime_fixture_users", Method = nameof(GetTableName))]
    public class RuntimeFixtureUser
    {
        [Key]
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public static string GetTableName() => "runtime_fixture_users";
    }
}
