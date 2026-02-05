// <copyright file="StrictGeneratorValidationTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sqlx.Tests;

/// <summary>
/// Strict validation tests for source generator output.
/// Focuses on SQL generation correctness, ordinal caching, and edge cases.
/// </summary>
[TestClass]
public class StrictGeneratorValidationTests
{
    #region ResultReader Ordinal Caching Tests

    [TestMethod]
    public void ResultReader_PropertyCount_MatchesActualProperties()
    {
        // Arrange
        var reader = StrictTestEntityResultReader.Default;
        
        // Act
        int propertyCount = reader.PropertyCount;
        
        // Assert
        Assert.AreEqual(5, propertyCount, "PropertyCount must match the number of properties");
    }

    [TestMethod]
    public void ResultReader_GetOrdinals_FillsAllPositions()
    {
        // Arrange
        var reader = StrictTestEntityResultReader.Default;
        var mockReader = new MockDataReader(new[] { "id", "name", "email", "age", "created_at" });
        
        // Act
        Span<int> ordinals = stackalloc int[5];
        reader.GetOrdinals(mockReader, ordinals);
        
        // Assert
        Assert.AreEqual(0, ordinals[0], "id should be at ordinal 0");
        Assert.AreEqual(1, ordinals[1], "name should be at ordinal 1");
        Assert.AreEqual(2, ordinals[2], "email should be at ordinal 2");
        Assert.AreEqual(3, ordinals[3], "age should be at ordinal 3");
        Assert.AreEqual(4, ordinals[4], "created_at should be at ordinal 4");
    }

    [TestMethod]
    public void ResultReader_ReadWithOrdinals_UsesPrecomputedValues()
    {
        // Arrange
        var reader = StrictTestEntityResultReader.Default;
        var mockReader = new MockDataReader(
            new[] { "id", "name", "email", "age", "created_at" },
            new object[] { 1, "John", "john@test.com", 30, new DateTime(2024, 1, 1) });
        
        // Act
        mockReader.Read();
        Span<int> ordinals = stackalloc int[5];
        reader.GetOrdinals(mockReader, ordinals);
        
        int getOrdinalCallsBefore = mockReader.GetOrdinalCallCount;
        var result = reader.Read(mockReader, ordinals);
        int getOrdinalCallsAfter = mockReader.GetOrdinalCallCount;
        
        // Assert
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("John", result.Name);
        Assert.AreEqual("john@test.com", result.Email);
        Assert.AreEqual(30, result.Age);
        Assert.AreEqual(new DateTime(2024, 1, 1), result.CreatedAt);
        Assert.AreEqual(getOrdinalCallsBefore, getOrdinalCallsAfter, 
            "Read with ordinals should NOT call GetOrdinal");
    }

    [TestMethod]
    public void ResultReader_ReadWithoutOrdinals_CallsGetOrdinal()
    {
        // Arrange
        var reader = StrictTestEntityResultReader.Default;
        var mockReader = new MockDataReader(
            new[] { "id", "name", "email", "age", "created_at" },
            new object[] { 2, "Jane", "jane@test.com", 25, new DateTime(2024, 2, 1) });
        
        // Act
        mockReader.Read();
        int getOrdinalCallsBefore = mockReader.GetOrdinalCallCount;
        var result = reader.Read(mockReader);
        int getOrdinalCallsAfter = mockReader.GetOrdinalCallCount;
        
        // Assert
        Assert.AreEqual(2, result.Id);
        Assert.AreEqual("Jane", result.Name);
        Assert.AreEqual(5, getOrdinalCallsAfter - getOrdinalCallsBefore, 
            "Read without ordinals should call GetOrdinal 5 times (once per property)");
    }

    #endregion

    #region Nullable Handling Tests

    [TestMethod]
    public void ResultReader_NullableFields_HandlesNullCorrectly()
    {
        // Arrange
        var reader = NullableEntityResultReader.Default;
        var mockReader = new MockDataReader(
            new[] { "id", "name", "description", "score" },
            new object[] { 1, "Test", DBNull.Value, DBNull.Value });
        
        // Act
        mockReader.Read();
        var result = reader.Read(mockReader);
        
        // Assert
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test", result.Name);
        Assert.IsNull(result.Description, "Nullable string should be null");
        Assert.IsNull(result.Score, "Nullable int should be null");
    }

    [TestMethod]
    public void ResultReader_NullableFields_HandlesValueCorrectly()
    {
        // Arrange
        var reader = NullableEntityResultReader.Default;
        var mockReader = new MockDataReader(
            new[] { "id", "name", "description", "score" },
            new object[] { 2, "Test2", "Description", 95 });
        
        // Act
        mockReader.Read();
        var result = reader.Read(mockReader);
        
        // Assert
        Assert.AreEqual(2, result.Id);
        Assert.AreEqual("Test2", result.Name);
        Assert.AreEqual("Description", result.Description);
        Assert.AreEqual(95, result.Score);
    }

    #endregion

    #region Type Mapping Tests

    [TestMethod]
    public void ResultReader_AllSupportedTypes_MapCorrectly()
    {
        // Arrange
        var reader = AllTypesEntityResultReader.Default;
        var mockReader = new MockDataReader(
            new[] { "int_val", "long_val", "short_val", "byte_val", "bool_val", 
                   "string_val", "date_time_val", "decimal_val", "double_val", 
                   "float_val", "guid_val" },
            new object[] { 
                42, 
                123456789L, 
                (short)100, 
                (byte)255, 
                true, 
                "test", 
                new DateTime(2024, 1, 1), 
                123.45m, 
                456.78, 
                789.01f, 
                Guid.Parse("12345678-1234-1234-1234-123456789012") 
            });
        
        // Act
        mockReader.Read();
        var result = reader.Read(mockReader);
        
        // Assert
        Assert.AreEqual(42, result.IntVal);
        Assert.AreEqual(123456789L, result.LongVal);
        Assert.AreEqual((short)100, result.ShortVal);
        Assert.AreEqual((byte)255, result.ByteVal);
        Assert.AreEqual(true, result.BoolVal);
        Assert.AreEqual("test", result.StringVal);
        Assert.AreEqual(new DateTime(2024, 1, 1), result.DateTimeVal);
        Assert.AreEqual(123.45m, result.DecimalVal);
        Assert.AreEqual(456.78, result.DoubleVal);
        Assert.AreEqual(789.01f, result.FloatVal, 0.01f);
        Assert.AreEqual(Guid.Parse("12345678-1234-1234-1234-123456789012"), result.GuidVal);
    }

    #endregion

    #region Column Order Independence Tests

    [TestMethod]
    public void ResultReader_DifferentColumnOrder_MapsCorrectly()
    {
        // Arrange
        var reader = StrictTestEntityResultReader.Default;
        // Columns in different order than property order
        var mockReader = new MockDataReader(
            new[] { "email", "created_at", "id", "age", "name" },
            new object[] { "test@example.com", new DateTime(2024, 3, 1), 99, 40, "Bob" });
        
        // Act
        mockReader.Read();
        Span<int> ordinals = stackalloc int[5];
        reader.GetOrdinals(mockReader, ordinals);
        var result = reader.Read(mockReader, ordinals);
        
        // Assert
        Assert.AreEqual(99, result.Id, "Id should map correctly despite column order");
        Assert.AreEqual("Bob", result.Name, "Name should map correctly despite column order");
        Assert.AreEqual("test@example.com", result.Email, "Email should map correctly despite column order");
        Assert.AreEqual(40, result.Age, "Age should map correctly despite column order");
        Assert.AreEqual(new DateTime(2024, 3, 1), result.CreatedAt, "CreatedAt should map correctly despite column order");
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public void ResultReader_EmptyString_HandlesCorrectly()
    {
        // Arrange
        var reader = StrictTestEntityResultReader.Default;
        var mockReader = new MockDataReader(
            new[] { "id", "name", "email", "age", "created_at" },
            new object[] { 1, "", "", 0, DateTime.MinValue });
        
        // Act
        mockReader.Read();
        var result = reader.Read(mockReader);
        
        // Assert
        Assert.AreEqual("", result.Name, "Empty string should be preserved");
        Assert.AreEqual("", result.Email, "Empty string should be preserved");
    }

    [TestMethod]
    public void ResultReader_MinMaxValues_HandlesCorrectly()
    {
        // Arrange
        var reader = AllTypesEntityResultReader.Default;
        var mockReader = new MockDataReader(
            new[] { "int_val", "long_val", "short_val", "byte_val", "bool_val", 
                   "string_val", "date_time_val", "decimal_val", "double_val", 
                   "float_val", "guid_val" },
            new object[] { 
                int.MaxValue, 
                long.MaxValue, 
                short.MaxValue, 
                byte.MaxValue, 
                false, 
                "max", 
                DateTime.MaxValue, 
                decimal.MaxValue, 
                double.MaxValue, 
                float.MaxValue, 
                Guid.Empty 
            });
        
        // Act
        mockReader.Read();
        var result = reader.Read(mockReader);
        
        // Assert
        Assert.AreEqual(int.MaxValue, result.IntVal);
        Assert.AreEqual(long.MaxValue, result.LongVal);
        Assert.AreEqual(short.MaxValue, result.ShortVal);
        Assert.AreEqual(byte.MaxValue, result.ByteVal);
        Assert.AreEqual(Guid.Empty, result.GuidVal);
    }

    #endregion

    #region Performance Validation Tests

    [TestMethod]
    public void ResultReader_LargeDataset_MaintainsCorrectness()
    {
        // Arrange
        var reader = StrictTestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new object[] { i, $"Name{i}", $"email{i}@test.com", i % 100, DateTime.Now.AddDays(i) })
            .ToList();
        
        // Act
        var results = new List<StrictTestEntity>();
        foreach (var entityData in entities)
        {
            var mockReader = new MockDataReader(
                new[] { "id", "name", "email", "age", "created_at" },
                entityData);
            mockReader.Read();
            results.Add(reader.Read(mockReader));
        }
        
        // Assert
        Assert.AreEqual(1000, results.Count);
        for (int i = 0; i < 1000; i++)
        {
            Assert.AreEqual(i + 1, results[i].Id, $"Row {i}: Id mismatch");
            Assert.AreEqual($"Name{i + 1}", results[i].Name, $"Row {i}: Name mismatch");
            Assert.AreEqual($"email{i + 1}@test.com", results[i].Email, $"Row {i}: Email mismatch");
        }
    }

    #endregion

    #region SQL Generation Validation Tests

    [TestMethod]
    public void EntityProvider_Columns_CorrectCount()
    {
        // Arrange
        var provider = StrictTestEntityEntityProvider.Default;
        
        // Act
        int columnCount = provider.Columns.Count;
        
        // Assert
        Assert.AreEqual(5, columnCount, "Should have exactly 5 columns");
    }

    [TestMethod]
    public void EntityProvider_Columns_CorrectNames()
    {
        // Arrange
        var provider = StrictTestEntityEntityProvider.Default;
        
        // Act
        var columnNames = provider.Columns.Select(c => c.Name).ToList();
        
        // Assert
        CollectionAssert.Contains(columnNames, "id");
        CollectionAssert.Contains(columnNames, "name");
        CollectionAssert.Contains(columnNames, "email");
        CollectionAssert.Contains(columnNames, "age");
        CollectionAssert.Contains(columnNames, "created_at");
    }

    [TestMethod]
    public void EntityProvider_Columns_CorrectDbTypes()
    {
        // Arrange
        var provider = StrictTestEntityEntityProvider.Default;
        
        // Act & Assert
        var idCol = provider.Columns.First(c => c.Name == "id");
        Assert.AreEqual(DbType.Int32, idCol.DbType);
        
        var nameCol = provider.Columns.First(c => c.Name == "name");
        Assert.AreEqual(DbType.String, nameCol.DbType);
        
        var ageCol = provider.Columns.First(c => c.Name == "age");
        Assert.AreEqual(DbType.Int32, ageCol.DbType);
        
        var createdAtCol = provider.Columns.First(c => c.Name == "created_at");
        Assert.AreEqual(DbType.DateTime, createdAtCol.DbType);
    }

    [TestMethod]
    public void PlaceholderContext_SelectAllColumns_GeneratesCorrectSQL()
    {
        // Arrange
        var provider = StrictTestEntityEntityProvider.Default;
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            provider.Columns);
        
        // Act
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        // Assert
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[email]"));
        Assert.IsTrue(template.Sql.Contains("[age]"));
        Assert.IsTrue(template.Sql.Contains("[created_at]"));
        Assert.IsTrue(template.Sql.Contains("[users]"));
    }

    [TestMethod]
    public void PlaceholderContext_ExcludeColumn_GeneratesCorrectSQL()
    {
        // Arrange
        var provider = StrictTestEntityEntityProvider.Default;
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            provider.Columns);
        
        // Act
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", 
            context);
        
        // Assert
        Assert.IsFalse(template.Sql.Contains("[id]"), "Excluded column should not appear");
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("@name"));
        Assert.IsFalse(template.Sql.Contains("@id"), "Excluded parameter should not appear");
    }

    #endregion
}

#region Test Entities

[Sqlx]
public class StrictTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; }
}

[Sqlx]
public class NullableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Score { get; set; }
}

[Sqlx]
public class AllTypesEntity
{
    public int IntVal { get; set; }
    public long LongVal { get; set; }
    public short ShortVal { get; set; }
    public byte ByteVal { get; set; }
    public bool BoolVal { get; set; }
    public string StringVal { get; set; } = string.Empty;
    public DateTime DateTimeVal { get; set; }
    public decimal DecimalVal { get; set; }
    public double DoubleVal { get; set; }
    public float FloatVal { get; set; }
    public Guid GuidVal { get; set; }
}

#endregion

#region Mock Data Reader

public class MockDataReader : IDataReader
{
    private readonly string[] _columnNames;
    private readonly object[] _values;
    private bool _hasRead;
    public int GetOrdinalCallCount { get; private set; }

    public MockDataReader(string[] columnNames, object[]? values = null)
    {
        _columnNames = columnNames;
        _values = values ?? new object[columnNames.Length];
    }

    public bool Read()
    {
        if (_hasRead) return false;
        _hasRead = true;
        return true;
    }

    public int GetOrdinal(string name)
    {
        GetOrdinalCallCount++;
        for (int i = 0; i < _columnNames.Length; i++)
        {
            if (_columnNames[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new IndexOutOfRangeException($"Column '{name}' not found");
    }

    public bool IsDBNull(int ordinal) => _values[ordinal] is DBNull;
    public int GetInt32(int ordinal) => Convert.ToInt32(_values[ordinal]);
    public long GetInt64(int ordinal) => Convert.ToInt64(_values[ordinal]);
    public short GetInt16(int ordinal) => Convert.ToInt16(_values[ordinal]);
    public byte GetByte(int ordinal) => Convert.ToByte(_values[ordinal]);
    public bool GetBoolean(int ordinal) => Convert.ToBoolean(_values[ordinal]);
    public string GetString(int ordinal) => Convert.ToString(_values[ordinal]) ?? string.Empty;
    public DateTime GetDateTime(int ordinal) => Convert.ToDateTime(_values[ordinal]);
    public decimal GetDecimal(int ordinal) => Convert.ToDecimal(_values[ordinal]);
    public double GetDouble(int ordinal) => Convert.ToDouble(_values[ordinal]);
    public float GetFloat(int ordinal) => Convert.ToSingle(_values[ordinal]);
    public Guid GetGuid(int ordinal) => (Guid)_values[ordinal];
    public object GetValue(int ordinal) => _values[ordinal];

    // Not implemented for this test
    public void Dispose() { }
    public void Close() { }
    public DataTable? GetSchemaTable() => null;
    public bool NextResult() => false;
    public int Depth => 0;
    public bool IsClosed => false;
    public int RecordsAffected => 0;
    public object this[int i] => _values[i];
    public object this[string name] => _values[GetOrdinal(name)];
    public int FieldCount => _columnNames.Length;
    public string GetName(int i) => _columnNames[i];
    public string GetDataTypeName(int i) => _values[i].GetType().Name;
    public Type GetFieldType(int i) => _values[i].GetType();
    public int GetValues(object[] values) => throw new NotImplementedException();
    public char GetChar(int i) => throw new NotImplementedException();
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
    public IDataReader GetData(int i) => throw new NotImplementedException();
}

#endregion
