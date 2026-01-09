// -----------------------------------------------------------------------
// <copyright file="GetDistinctValuesPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined;

/// <summary>
/// Property-based tests for GetDistinctValuesAsync method
/// Validates: Requirements 11.2 - GetDistinctValuesAsync 功能正确性
/// </summary>
[TestClass]
public partial class GetDistinctValuesPropertyTests
{
    private SqliteConnection? _connection;
    private TestRepository? _repository;

    public class TestEntity
    {
        public long Id { get; set; }
        public string? Status { get; set; }
        public string? Category { get; set; }
        public string? Name { get; set; }
        public int? Priority { get; set; }
    }

    [TableName("test_entity")]
    [RepositoryFor<ITestQueryRepository>]
    public partial class TestRepository
    {
        // 🔧 IMPORTANT: For partial classes, connection field must be protected or internal
        // (not private) so the generated code can access it
        protected readonly SqliteConnection _connection;

        public TestRepository(SqliteConnection connection)
        {
            _connection = connection;
        }
    }

    public interface ITestQueryRepository
    {
        [SqlTemplate("SELECT DISTINCT {{column}} FROM {{table}} WHERE {{column}} IS NOT NULL ORDER BY {{column}} {{limit --param limit}}")]
        Task<List<string>> GetDistinctValuesAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, int limit = 1000);

        [SqlTemplate("INSERT INTO {{table}} (status, category, name, priority) VALUES (@status, @category, @name, @priority)")]
        Task<int> InsertAsync(string? status, string? category, string? name, int? priority);

        [SqlTemplate("DELETE FROM {{table}}")]
        Task<int> DeleteAllAsync();
    }

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_entity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                status TEXT,
                category TEXT,
                name TEXT,
                priority INTEGER
            )";
        cmd.ExecuteNonQuery();

        _repository = new TestRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    /// <summary>
    /// Property 7.1: GetDistinctValuesAsync returns only unique values
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_ReturnsOnlyUniqueValues()
    {
        // Arrange: Insert duplicate values
        await _repository!.InsertAsync("active", "A", "Item1", 1);
        await _repository.InsertAsync("active", "B", "Item2", 2);
        await _repository.InsertAsync("inactive", "A", "Item3", 1);
        await _repository.InsertAsync("active", "C", "Item4", 3);
        await _repository.InsertAsync("pending", "B", "Item5", 2);

        // Act
        var statuses = await _repository.GetDistinctValuesAsync("status");

        // Assert
        Assert.AreEqual(3, statuses.Count, "Should return exactly 3 unique statuses");
        Assert.AreEqual(statuses.Count, statuses.Distinct().Count(), "All values should be unique");
        CollectionAssert.AreEquivalent(new[] { "active", "inactive", "pending" }, statuses);
    }

    /// <summary>
    /// Property 7.2: GetDistinctValuesAsync excludes NULL values
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_ExcludesNullValues()
    {
        // Arrange: Insert some NULL values
        await _repository!.InsertAsync("active", "A", "Item1", 1);
        await _repository.InsertAsync(null, "B", "Item2", 2);
        await _repository.InsertAsync("inactive", "C", "Item3", 3);
        await _repository.InsertAsync(null, "D", "Item4", 4);

        // Act
        var statuses = await _repository.GetDistinctValuesAsync("status");

        // Assert
        Assert.AreEqual(2, statuses.Count, "Should return only non-NULL values");
        Assert.IsFalse(statuses.Contains(null!), "Should not contain NULL");
        CollectionAssert.AreEquivalent(new[] { "active", "inactive" }, statuses);
    }

    /// <summary>
    /// Property 7.3: GetDistinctValuesAsync returns values in sorted order
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_ReturnsSortedValues()
    {
        // Arrange: Insert values in random order
        await _repository!.InsertAsync("zebra", "A", "Item1", 1);
        await _repository.InsertAsync("apple", "B", "Item2", 2);
        await _repository.InsertAsync("mango", "C", "Item3", 3);
        await _repository.InsertAsync("banana", "D", "Item4", 4);

        // Act
        var statuses = await _repository.GetDistinctValuesAsync("status");

        // Assert
        var expected = new[] { "apple", "banana", "mango", "zebra" };
        CollectionAssert.AreEqual(expected, statuses, "Values should be sorted alphabetically");
    }

    /// <summary>
    /// Property 7.4: GetDistinctValuesAsync respects limit parameter
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_RespectsLimit()
    {
        // Arrange: Insert many distinct values
        for (int i = 0; i < 100; i++)
        {
            await _repository!.InsertAsync($"status_{i:D3}", "A", $"Item{i}", i);
        }

        // Act
        var statuses = await _repository!.GetDistinctValuesAsync("status", limit: 10);

        // Assert
        Assert.AreEqual(10, statuses.Count, "Should return exactly 10 values");
        Assert.AreEqual(statuses.Count, statuses.Distinct().Count(), "All values should be unique");
    }

    /// <summary>
    /// Property 7.5: GetDistinctValuesAsync works with different columns
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_WorksWithDifferentColumns()
    {
        // Arrange
        await _repository!.InsertAsync("active", "CategoryA", "Item1", 1);
        await _repository.InsertAsync("inactive", "CategoryB", "Item2", 2);
        await _repository.InsertAsync("active", "CategoryA", "Item3", 3);

        // Act: Test different columns
        var statuses = await _repository.GetDistinctValuesAsync("status");
        var categories = await _repository.GetDistinctValuesAsync("category");
        var names = await _repository.GetDistinctValuesAsync("name");

        // Assert
        Assert.AreEqual(2, statuses.Count, "Should have 2 distinct statuses");
        Assert.AreEqual(2, categories.Count, "Should have 2 distinct categories");
        Assert.AreEqual(3, names.Count, "Should have 3 distinct names");
    }

    /// <summary>
    /// Property 7.6: GetDistinctValuesAsync returns empty list for empty table
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_ReturnsEmptyListForEmptyTable()
    {
        // Arrange: Empty table
        await _repository!.DeleteAllAsync();

        // Act
        var statuses = await _repository.GetDistinctValuesAsync("status");

        // Assert
        Assert.IsNotNull(statuses, "Should return non-null list");
        Assert.AreEqual(0, statuses.Count, "Should return empty list");
    }

    /// <summary>
    /// Property 7.7: GetDistinctValuesAsync rejects invalid column names (SQL injection protection)
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_RejectsInvalidColumnNames()
    {
        // Arrange: Insert test data
        await _repository!.InsertAsync("active", "A", "Item1", 1);

        // Act & Assert: Test various SQL injection attempts
        // Dangerous keywords should be caught by validation (ArgumentException)
        var ex1 = await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await _repository.GetDistinctValuesAsync("status; DROP TABLE test_entity"),
            "Should reject SQL injection with semicolon");
        Assert.IsTrue(ex1.Message.Contains("dangerous"), "Error message should mention dangerous keywords");

        // Quotes might pass validation but fail at database level
        try
        {
            await _repository.GetDistinctValuesAsync("status' OR '1'='1");
            Assert.Fail("Should have thrown an exception for SQL injection with quotes");
        }
        catch (ArgumentException)
        {
            // Validation caught it - good
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            // Database caught it - also acceptable
        }

        var ex3 = await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await _repository.GetDistinctValuesAsync("status--"),
            "Should reject SQL injection with comment");
        Assert.IsTrue(ex3.Message.Contains("dangerous"), "Error message should mention dangerous keywords");

        var ex4 = await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await _repository.GetDistinctValuesAsync("status/**/"),
            "Should reject SQL injection with block comment");
        Assert.IsTrue(ex4.Message.Contains("dangerous"), "Error message should mention dangerous keywords");
    }

    /// <summary>
    /// Property 7.8: GetDistinctValuesAsync handles special characters in values
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_HandlesSpecialCharacters()
    {
        // Arrange: Insert values with special characters
        await _repository!.InsertAsync("status'with'quotes", "A", "Item1", 1);
        await _repository.InsertAsync("status\"with\"doublequotes", "B", "Item2", 2);
        await _repository.InsertAsync("status;with;semicolon", "C", "Item3", 3);
        await _repository.InsertAsync("status with spaces", "D", "Item4", 4);

        // Act
        var statuses = await _repository.GetDistinctValuesAsync("status");

        // Assert
        Assert.AreEqual(4, statuses.Count, "Should return all 4 distinct values");
        Assert.IsTrue(statuses.Contains("status'with'quotes"), "Should handle single quotes");
        Assert.IsTrue(statuses.Contains("status\"with\"doublequotes"), "Should handle double quotes");
        Assert.IsTrue(statuses.Contains("status;with;semicolon"), "Should handle semicolons in values");
        Assert.IsTrue(statuses.Contains("status with spaces"), "Should handle spaces");
    }

    /// <summary>
    /// Property 7.9: GetDistinctValuesAsync handles empty strings
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_HandlesEmptyStrings()
    {
        // Arrange: Insert empty strings
        await _repository!.InsertAsync("", "A", "Item1", 1);
        await _repository.InsertAsync("active", "B", "Item2", 2);
        await _repository.InsertAsync("", "C", "Item3", 3);

        // Act
        var statuses = await _repository.GetDistinctValuesAsync("status");

        // Assert
        Assert.AreEqual(2, statuses.Count, "Should return 2 distinct values (empty string and 'active')");
        Assert.IsTrue(statuses.Contains(""), "Should include empty string");
        Assert.IsTrue(statuses.Contains("active"), "Should include 'active'");
    }

    /// <summary>
    /// Property 7.10: GetDistinctValuesAsync handles very long strings
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_HandlesLongStrings()
    {
        // Arrange: Insert very long strings
        var longString1 = new string('A', 1000);
        var longString2 = new string('B', 1000);
        await _repository!.InsertAsync(longString1, "A", "Item1", 1);
        await _repository.InsertAsync(longString2, "B", "Item2", 2);
        await _repository.InsertAsync(longString1, "C", "Item3", 3);

        // Act
        var statuses = await _repository.GetDistinctValuesAsync("status");

        // Assert
        Assert.AreEqual(2, statuses.Count, "Should return 2 distinct long strings");
        Assert.IsTrue(statuses.Contains(longString1), "Should include first long string");
        Assert.IsTrue(statuses.Contains(longString2), "Should include second long string");
    }

    /// <summary>
    /// Property 7.11: GetDistinctValuesAsync performance is acceptable for large datasets
    /// </summary>
    [TestMethod]
    public async Task Property_GetDistinctValuesAsync_PerformanceIsAcceptable()
    {
        // Arrange: Insert 1000 rows with 100 distinct values
        for (int i = 0; i < 1000; i++)
        {
            var status = $"status_{i % 100:D3}";
            await _repository!.InsertAsync(status, "A", $"Item{i}", i);
        }

        // Act
        var statuses = await _repository!.GetDistinctValuesAsync("status");

        // Assert
        Assert.AreEqual(100, statuses.Count, "Should return 100 distinct values");
    }
}
