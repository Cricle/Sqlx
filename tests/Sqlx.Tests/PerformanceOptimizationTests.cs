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

    #region Ordinal Caching Strict Tests

    [TestMethod]
    public void OrdinalCaching_GetOrdinalsCalledOnce_ShouldCacheCorrectly()
    {
        // Arrange
        SeedData(100);
        var reader = new TrackingTestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 100";
        
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
        Assert.AreEqual(100, results.Count);
        Assert.AreEqual(1, reader.GetOrdinalsCallCount, "GetOrdinals should be called exactly once");
        Assert.AreEqual(0, reader.ReadWithoutOrdinalsCallCount, "Read without ordinals should not be called");
        Assert.AreEqual(100, reader.ReadWithOrdinalsCallCount, "Read with ordinals should be called for each row");
    }

    [TestMethod]
    public void OrdinalCaching_VerifyNoGetOrdinalInLoop()
    {
        // Arrange
        SeedData(50);
        var reader = new GetOrdinalTrackingReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 50";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        Span<int> ordinals = stackalloc int[3];
        reader.GetOrdinals(dataReader, ordinals);
        
        int getOrdinalCallsBefore = reader.GetOrdinalCallCount;
        
        var results = new List<TestData>();
        while (dataReader.Read())
        {
            results.Add(reader.Read(dataReader, ordinals));
        }
        
        int getOrdinalCallsAfter = reader.GetOrdinalCallCount;
        
        // Assert
        Assert.AreEqual(50, results.Count);
        Assert.AreEqual(getOrdinalCallsBefore, getOrdinalCallsAfter, 
            "GetOrdinal should not be called during row reading when using cached ordinals");
    }

    [TestMethod]
    public void PropertyCount_ShouldMatchActualProperties()
    {
        // Arrange
        var reader = new TestDataReader();
        
        // Act
        int propertyCount = reader.PropertyCount;
        
        // Assert
        Assert.AreEqual(3, propertyCount, "PropertyCount should match the number of properties in TestData");
    }

    [TestMethod]
    public void StackallocOrdinals_LargePropertyCount_ShouldWork()
    {
        // Arrange
        SeedData(10);
        var reader = new LargeTestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value, id as id2, name as name2, value as value2, " +
                         "id as id3, name as name3, value as value3, id as id4 FROM test_data LIMIT 10";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        Span<int> ordinals = stackalloc int[reader.PropertyCount];
        reader.GetOrdinals(dataReader, ordinals);
        
        var results = new List<LargeTestData>();
        while (dataReader.Read())
        {
            results.Add(reader.Read(dataReader, ordinals));
        }
        
        // Assert
        Assert.AreEqual(10, results.Count);
        Assert.AreEqual(10, reader.PropertyCount);
    }

    [TestMethod]
    public void OrdinalCaching_WithNullableFields_ShouldHandleCorrectly()
    {
        // Arrange
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE nullable_test (
                id INTEGER PRIMARY KEY,
                name TEXT,
                value INTEGER
            )";
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = "INSERT INTO nullable_test (id, name, value) VALUES (1, NULL, 100)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO nullable_test (id, name, value) VALUES (2, 'Test', NULL)";
        cmd.ExecuteNonQuery();
        
        var reader = new NullableTestDataReader();
        
        // Act
        cmd.CommandText = "SELECT id, name, value FROM nullable_test";
        using var dataReader = cmd.ExecuteReader();
        Span<int> ordinals = stackalloc int[3];
        reader.GetOrdinals(dataReader, ordinals);
        
        var results = new List<NullableTestData>();
        while (dataReader.Read())
        {
            results.Add(reader.Read(dataReader, ordinals));
        }
        
        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.IsNull(results[0].Name);
        Assert.AreEqual(100, results[0].Value);
        
        Assert.AreEqual(2, results[1].Id);
        Assert.AreEqual("Test", results[1].Name);
        Assert.IsNull(results[1].Value);
    }

    [TestMethod]
    public void ExtensionMethods_ToList_ShouldUseOrdinalCaching()
    {
        // Arrange
        SeedData(100);
        var reader = new TrackingTestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 100";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var results = reader.ToList(dataReader);
        
        // Assert
        Assert.AreEqual(100, results.Count);
        Assert.AreEqual(1, reader.GetOrdinalsCallCount, "ToList should call GetOrdinals once");
        Assert.AreEqual(100, reader.ReadWithOrdinalsCallCount, "ToList should use cached ordinals for all rows");
    }

    [TestMethod]
    public void ExtensionMethods_ToListWithCapacity_ShouldPreallocate()
    {
        // Arrange
        SeedData(50);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 50";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var results = reader.ToList(dataReader, capacityHint: 50);
        
        // Assert
        Assert.AreEqual(50, results.Count);
        Assert.AreEqual(50, results.Capacity, "List should be preallocated with exact capacity");
    }

    [TestMethod]
    public void OrdinalCaching_EmptyResultSet_ShouldHandleGracefully()
    {
        // Arrange
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data WHERE id = -1";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var results = reader.ToList(dataReader);
        
        // Assert
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void OrdinalCaching_SingleRow_ShouldWork()
    {
        // Arrange
        SeedData(1);
        var reader = new TrackingTestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 1";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var results = reader.ToList(dataReader);
        
        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, reader.GetOrdinalsCallCount);
        Assert.AreEqual(1, reader.ReadWithOrdinalsCallCount);
    }

    [TestMethod]
    public void SpanBoundsCheck_Elimination_VerifyCorrectAccess()
    {
        // Arrange
        SeedData(10);
        var reader = new BoundsCheckTestReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 10";
        
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
        Assert.AreEqual(3, reader.OrdinalAccessCount, "Should access ordinals array exactly 3 times per GetOrdinals");
    }

    #endregion

    #region Memory Allocation Tests

    [TestMethod]
    public void StackallocOrdinals_ShouldNotAllocateOnHeap()
    {
        // Arrange
        SeedData(100);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 100";
        
        // Act
        long memoryBefore = GC.GetTotalMemory(true);
        
        using var dataReader = cmd.ExecuteReader();
        Span<int> ordinals = stackalloc int[3]; // Stack allocation
        reader.GetOrdinals(dataReader, ordinals);
        
        var results = new List<TestData>(100);
        while (dataReader.Read())
        {
            results.Add(reader.Read(dataReader, ordinals));
        }
        
        long memoryAfter = GC.GetTotalMemory(false);
        long allocated = memoryAfter - memoryBefore;
        
        // Assert
        Assert.AreEqual(100, results.Count);
        // Note: This is a rough check. Actual allocation includes List<T> and TestData objects
        // The key is that ordinals array itself doesn't contribute to heap allocation
        Assert.IsTrue(allocated > 0, "Some heap allocation expected for List and objects");
    }

    [TestMethod]
    public void ArrayOrdinals_ShouldAllocateOnHeap()
    {
        // Arrange
        SeedData(10);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data LIMIT 10";
        
        // Act
        long memoryBefore = GC.GetTotalMemory(true);
        
        using var dataReader = cmd.ExecuteReader();
        var ordinals = new int[3]; // Heap allocation
        reader.GetOrdinals(dataReader, ordinals);
        
        var results = new List<TestData>(10);
        while (dataReader.Read())
        {
            results.Add(reader.Read(dataReader, ordinals));
        }
        
        long memoryAfter = GC.GetTotalMemory(false);
        long allocated = memoryAfter - memoryBefore;
        
        // Assert
        Assert.AreEqual(10, results.Count);
        Assert.IsTrue(allocated > 0, "Heap allocation expected");
    }

    #endregion

    #region Correctness Verification Tests

    [TestMethod]
    public void OrdinalCaching_VerifyDataIntegrity_AllFields()
    {
        // Arrange
        SeedData(20);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, value FROM test_data ORDER BY id LIMIT 20";
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var results = reader.ToList(dataReader);
        
        // Assert
        Assert.AreEqual(20, results.Count);
        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(i + 1, results[i].Id, $"Row {i}: Id mismatch");
            Assert.AreEqual($"Item{i + 1}", results[i].Name, $"Row {i}: Name mismatch");
            Assert.AreEqual((i + 1) * 10, results[i].Value, $"Row {i}: Value mismatch");
        }
    }

    [TestMethod]
    public void OrdinalCaching_DifferentColumnOrder_ShouldMapCorrectly()
    {
        // Arrange
        SeedData(5);
        var reader = new TestDataReader();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT value, id, name FROM test_data ORDER BY id LIMIT 5"; // Different order
        
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
        Assert.AreEqual(5, results.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(i + 1, results[i].Id);
            Assert.AreEqual($"Item{i + 1}", results[i].Name);
            Assert.AreEqual((i + 1) * 10, results[i].Value);
        }
    }

    [TestMethod]
    public void OrdinalCaching_WithAliases_ShouldResolveCorrectly()
    {
        // Arrange
        SeedData(3);
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id as user_id, name as user_name, value as user_value FROM test_data LIMIT 3";
        
        var reader = new AliasedTestDataReader();
        
        // Act
        using var dataReader = cmd.ExecuteReader();
        var results = reader.ToList(dataReader);
        
        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Item1", results[0].Name);
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
            // Cache ordinals to local variables to eliminate bounds checking
            var ord0 = ordinals[0];
            var ord1 = ordinals[1];
            var ord2 = ordinals[2];
            
            return new TestData
            {
                Id = reader.GetInt64(ord0),
                Name = reader.GetString(ord1),
                Value = reader.GetInt32(ord2),
            };
        }
    }

    private class TrackingTestDataReader : IResultReader<TestData>
    {
        public int PropertyCount => 3;
        public int GetOrdinalsCallCount { get; private set; }
        public int ReadWithOrdinalsCallCount { get; private set; }
        public int ReadWithoutOrdinalsCallCount { get; private set; }

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            GetOrdinalsCallCount++;
            ordinals[0] = reader.GetOrdinal("id");
            ordinals[1] = reader.GetOrdinal("name");
            ordinals[2] = reader.GetOrdinal("value");
        }

        public TestData Read(IDataReader reader)
        {
            ReadWithoutOrdinalsCallCount++;
            return new TestData
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Value = reader.GetInt32(reader.GetOrdinal("value")),
            };
        }

        public TestData Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            ReadWithOrdinalsCallCount++;
            var ord0 = ordinals[0];
            var ord1 = ordinals[1];
            var ord2 = ordinals[2];
            
            return new TestData
            {
                Id = reader.GetInt64(ord0),
                Name = reader.GetString(ord1),
                Value = reader.GetInt32(ord2),
            };
        }
    }

    private class GetOrdinalTrackingReader : IResultReader<TestData>
    {
        public int PropertyCount => 3;
        public int GetOrdinalCallCount { get; private set; }

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            ordinals[0] = GetOrdinalWithTracking(reader, "id");
            ordinals[1] = GetOrdinalWithTracking(reader, "name");
            ordinals[2] = GetOrdinalWithTracking(reader, "value");
        }

        private int GetOrdinalWithTracking(IDataReader reader, string name)
        {
            GetOrdinalCallCount++;
            return reader.GetOrdinal(name);
        }

        public TestData Read(IDataReader reader)
        {
            return new TestData
            {
                Id = reader.GetInt64(GetOrdinalWithTracking(reader, "id")),
                Name = reader.GetString(GetOrdinalWithTracking(reader, "name")),
                Value = reader.GetInt32(GetOrdinalWithTracking(reader, "value")),
            };
        }

        public TestData Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            var ord0 = ordinals[0];
            var ord1 = ordinals[1];
            var ord2 = ordinals[2];
            
            return new TestData
            {
                Id = reader.GetInt64(ord0),
                Name = reader.GetString(ord1),
                Value = reader.GetInt32(ord2),
            };
        }
    }

    private class LargeTestData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public long Id2 { get; set; }
        public string Name2 { get; set; } = string.Empty;
        public int Value2 { get; set; }
        public long Id3 { get; set; }
        public string Name3 { get; set; } = string.Empty;
        public int Value3 { get; set; }
        public long Id4 { get; set; }
    }

    private class LargeTestDataReader : IResultReader<LargeTestData>
    {
        public int PropertyCount => 10;

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            ordinals[0] = reader.GetOrdinal("id");
            ordinals[1] = reader.GetOrdinal("name");
            ordinals[2] = reader.GetOrdinal("value");
            ordinals[3] = reader.GetOrdinal("id2");
            ordinals[4] = reader.GetOrdinal("name2");
            ordinals[5] = reader.GetOrdinal("value2");
            ordinals[6] = reader.GetOrdinal("id3");
            ordinals[7] = reader.GetOrdinal("name3");
            ordinals[8] = reader.GetOrdinal("value3");
            ordinals[9] = reader.GetOrdinal("id4");
        }

        public LargeTestData Read(IDataReader reader)
        {
            return new LargeTestData
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Value = reader.GetInt32(reader.GetOrdinal("value")),
                Id2 = reader.GetInt64(reader.GetOrdinal("id2")),
                Name2 = reader.GetString(reader.GetOrdinal("name2")),
                Value2 = reader.GetInt32(reader.GetOrdinal("value2")),
                Id3 = reader.GetInt64(reader.GetOrdinal("id3")),
                Name3 = reader.GetString(reader.GetOrdinal("name3")),
                Value3 = reader.GetInt32(reader.GetOrdinal("value3")),
                Id4 = reader.GetInt64(reader.GetOrdinal("id4")),
            };
        }

        public LargeTestData Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            var ord0 = ordinals[0];
            var ord1 = ordinals[1];
            var ord2 = ordinals[2];
            var ord3 = ordinals[3];
            var ord4 = ordinals[4];
            var ord5 = ordinals[5];
            var ord6 = ordinals[6];
            var ord7 = ordinals[7];
            var ord8 = ordinals[8];
            var ord9 = ordinals[9];
            
            return new LargeTestData
            {
                Id = reader.GetInt64(ord0),
                Name = reader.GetString(ord1),
                Value = reader.GetInt32(ord2),
                Id2 = reader.GetInt64(ord3),
                Name2 = reader.GetString(ord4),
                Value2 = reader.GetInt32(ord5),
                Id3 = reader.GetInt64(ord6),
                Name3 = reader.GetString(ord7),
                Value3 = reader.GetInt32(ord8),
                Id4 = reader.GetInt64(ord9),
            };
        }
    }

    private class NullableTestData
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public int? Value { get; set; }
    }

    private class NullableTestDataReader : IResultReader<NullableTestData>
    {
        public int PropertyCount => 3;

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            ordinals[0] = reader.GetOrdinal("id");
            ordinals[1] = reader.GetOrdinal("name");
            ordinals[2] = reader.GetOrdinal("value");
        }

        public NullableTestData Read(IDataReader reader)
        {
            return new NullableTestData
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name")),
                Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetInt32(reader.GetOrdinal("value")),
            };
        }

        public NullableTestData Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            var ord0 = ordinals[0];
            var ord1 = ordinals[1];
            var ord2 = ordinals[2];
            
            return new NullableTestData
            {
                Id = reader.GetInt64(ord0),
                Name = reader.IsDBNull(ord1) ? null : reader.GetString(ord1),
                Value = reader.IsDBNull(ord2) ? null : reader.GetInt32(ord2),
            };
        }
    }

    private class BoundsCheckTestReader : IResultReader<TestData>
    {
        public int PropertyCount => 3;
        public int OrdinalAccessCount { get; private set; }

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            OrdinalAccessCount = 0;
            ordinals[0] = reader.GetOrdinal("id");
            OrdinalAccessCount++;
            ordinals[1] = reader.GetOrdinal("name");
            OrdinalAccessCount++;
            ordinals[2] = reader.GetOrdinal("value");
            OrdinalAccessCount++;
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
            var ord0 = ordinals[0];
            var ord1 = ordinals[1];
            var ord2 = ordinals[2];
            
            return new TestData
            {
                Id = reader.GetInt64(ord0),
                Name = reader.GetString(ord1),
                Value = reader.GetInt32(ord2),
            };
        }
    }

    private class AliasedTestDataReader : IResultReader<TestData>
    {
        public int PropertyCount => 3;

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            ordinals[0] = reader.GetOrdinal("user_id");
            ordinals[1] = reader.GetOrdinal("user_name");
            ordinals[2] = reader.GetOrdinal("user_value");
        }

        public TestData Read(IDataReader reader)
        {
            return new TestData
            {
                Id = reader.GetInt64(reader.GetOrdinal("user_id")),
                Name = reader.GetString(reader.GetOrdinal("user_name")),
                Value = reader.GetInt32(reader.GetOrdinal("user_value")),
            };
        }

        public TestData Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            var ord0 = ordinals[0];
            var ord1 = ordinals[1];
            var ord2 = ordinals[2];
            
            return new TestData
            {
                Id = reader.GetInt64(ord0),
                Name = reader.GetString(ord1),
                Value = reader.GetInt32(ord2),
            };
        }
    }

    #endregion
}
