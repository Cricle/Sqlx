// -----------------------------------------------------------------------
// <copyright file="PerformanceOptimizationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for performance optimizations including:
/// - Synchronous ResultReader operations
/// - Struct ordinals
/// - List capacity optimization
/// </summary>
[TestClass]
public class PerformanceOptimizationTests
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Initialize()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_data (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                value INTEGER NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region Synchronous ResultReader Tests

    [TestMethod]
    public void SyncToList_WithStructOrdinals_ShouldReadAllRows()
    {
        // Arrange
        SeedData(10);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var result = new List<TestData>();
        while (dataReader.Read())
        {
            result.Add(reader.Read(dataReader));
        }
        
        // Assert
        Assert.AreEqual(10, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(10, result[9].Id);
    }

    [TestMethod]
    public void SyncToList_LargeDataSet_ShouldHandleCorrectly()
    {
        // Arrange
        SeedData(1000);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 1000";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var result = new List<TestData>(1000);
        while (dataReader.Read())
        {
            result.Add(reader.Read(dataReader));
        }
        
        // Assert
        Assert.AreEqual(1000, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(1000, result[999].Id);
    }

    #endregion

    #region Struct Ordinals Tests

    [TestMethod]
    public void StructOrdinals_ShouldInitializeCorrectly()
    {
        // Arrange
        SeedData(1);
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data";
        
        // Act
        using var reader = cmd.ExecuteReader();
        var testReader = new TestDataReader();
        Span<int> ordinals = stackalloc int[3];
        testReader.GetOrdinals(reader, ordinals);
        
        // Assert
        Assert.AreEqual(0, ordinals[0]); // Id
        Assert.AreEqual(1, ordinals[1]); // Name
        Assert.AreEqual(2, ordinals[2]); // Value
    }

    [TestMethod]
    public void ArrayOrdinals_ShouldReadCorrectly()
    {
        // Arrange
        SeedData(10);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        Span<int> ordinals = stackalloc int[3];
        reader.GetOrdinals(dataReader, ordinals);
        var results = new List<TestData>();
        
        while (dataReader.Read())
        {
            results.Add(reader.Read(dataReader, ordinals));
        }
        
        // Assert
        Assert.AreEqual(10, results.Count);
        for (int i = 0; i < results.Count; i++)
        {
            Assert.AreEqual(i + 1, results[i].Id);
            Assert.AreEqual($"Item{i + 1}", results[i].Name);
        }
    }

    #endregion

    #region List Capacity Optimization Tests

    [DataTestMethod]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(1000)]
    public void ListWithCapacity_ShouldPreallocateCorrectly(int count)
    {
        // Arrange
        SeedData(count);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $"SELECT id, name, value FROM test_data LIMIT {count}";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var result = new List<TestData>(count); // Preallocate
        while (dataReader.Read())
        {
            result.Add(reader.Read(dataReader));
        }
        
        // Assert
        Assert.AreEqual(count, result.Count);
        Assert.AreEqual(count, result.Capacity); // No reallocation
    }

    [TestMethod]
    public void ListWithoutCapacity_MayRequireReallocations()
    {
        // Arrange
        SeedData(100);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 100";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var result = new List<TestData>(); // No preallocation
        while (dataReader.Read())
        {
            result.Add(reader.Read(dataReader));
        }
        
        // Assert
        Assert.AreEqual(100, result.Count);
        Assert.IsTrue(result.Capacity >= 100); // May be larger due to growth
    }

    #endregion

    #region Helper Methods

    private void SeedData(int count)
    {
        using var cmd = _connection!.CreateCommand();
        for (int i = 1; i <= count; i++)
        {
            cmd.CommandText = $"INSERT INTO test_data (id, name, value) VALUES ({i}, 'Item{i}', {i * 10})";
            cmd.ExecuteNonQuery();
        }
    }

    #endregion

    #region Test Classes

    private class TestData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class TestDataReader : IResultReader<TestData>
    {
        public int PropertyCount => 3;

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            ordinals[0] = reader.GetOrdinal("id");
            ordinals[1] = reader.GetOrdinal("name");
            ordinals[2] = reader.GetOrdinal("value");
        }

        public TestData Read(IDataReader reader)
        {
            return new TestData
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Value = reader.GetInt32(reader.GetOrdinal("value")),
            };
        }

        public TestData Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            return new TestData
            {
                Id = reader.GetInt64(ordinals[0]),
                Name = reader.GetString(ordinals[1]),
                Value = reader.GetInt32(ordinals[2]),
            };
        }
    }

    #endregion
}
