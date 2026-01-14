using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Data;
using System.Data.Common;

namespace Sqlx.Tests;

/// <summary>
/// High-quality tests for IResultReader optimizations including:
/// - Static ordinals optimization
/// - Extension methods with pre-computed ordinals
/// - Edge cases and boundary conditions
/// </summary>
[TestClass]
public class ResultReaderOptimizationTests
{
    #region IResultReader Interface Contract Tests

    [TestMethod]
    public void IResultReader_Read_WithIDataReader_ReturnsEntity()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[] { new OptimizedEntity { Id = 42, Name = "test", Value = 3.14m } };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        dbReader.Read(); // Position at first row
        
        var result = reader.Read(dbReader);
        
        Assert.AreEqual(42, result.Id);
        Assert.AreEqual("test", result.Name);
        Assert.AreEqual(3.14m, result.Value);
    }

    [TestMethod]
    public void IResultReader_Read_WithOrdinals_ReturnsEntity()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[] { new OptimizedEntity { Id = 99, Name = "ordinal", Value = 1.5m } };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        dbReader.Read();
        
        var ordinals = reader.GetOrdinals(dbReader);
        var result = reader.Read(dbReader, ordinals);
        
        Assert.AreEqual(99, result.Id);
        Assert.AreEqual("ordinal", result.Name);
        Assert.AreEqual(1.5m, result.Value);
    }

    [TestMethod]
    public void IResultReader_GetOrdinals_ReturnsCorrectOrdinals()
    {
        var reader = OptimizedEntityResultReader.Default;
        using var dbReader = new OptimizedEntityDbDataReader(Array.Empty<OptimizedEntity>());
        
        var ordinals = reader.GetOrdinals(dbReader);
        
        Assert.AreEqual(3, ordinals.Length);
        Assert.AreEqual(0, ordinals[0]); // id
        Assert.AreEqual(1, ordinals[1]); // name
        Assert.AreEqual(2, ordinals[2]); // value
    }

    [TestMethod]
    public void IResultReader_GetOrdinals_CanBeReusedAcrossRows()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[]
        {
            new OptimizedEntity { Id = 1, Name = "first", Value = 1.0m },
            new OptimizedEntity { Id = 2, Name = "second", Value = 2.0m },
            new OptimizedEntity { Id = 3, Name = "third", Value = 3.0m },
        };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        
        // Get ordinals once
        dbReader.Read();
        var ordinals = reader.GetOrdinals(dbReader);
        
        // Reuse for all rows
        var results = new List<OptimizedEntity>();
        do
        {
            results.Add(reader.Read(dbReader, ordinals));
        } while (dbReader.Read());
        
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("first", results[0].Name);
        Assert.AreEqual("second", results[1].Name);
        Assert.AreEqual("third", results[2].Name);
    }

    #endregion

    #region Static Ordinals Extension Method Tests

    [TestMethod]
    public void ToList_WithStaticOrdinals_EmptyReader_ReturnsEmptyList()
    {
        var reader = OptimizedEntityResultReader.Default;
        using var dbReader = new OptimizedEntityDbDataReader(Array.Empty<OptimizedEntity>());
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.ToList(dbReader, ordinals);
        
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ToList_WithStaticOrdinals_SingleRow_ReturnsSingleEntity()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[] { new OptimizedEntity { Id = 1, Name = "single", Value = 100m } };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.ToList(dbReader, ordinals);
        
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual("single", result[0].Name);
        Assert.AreEqual(100m, result[0].Value);
    }

    [TestMethod]
    public void ToList_WithStaticOrdinals_MultipleRows_ReturnsAllEntities()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = Enumerable.Range(1, 100)
            .Select(i => new OptimizedEntity { Id = i, Name = $"entity{i}", Value = i * 1.1m })
            .ToArray();
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.ToList(dbReader, ordinals);
        
        Assert.AreEqual(100, result.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i + 1, result[i].Id);
            Assert.AreEqual($"entity{i + 1}", result[i].Name);
        }
    }

    [TestMethod]
    public void FirstOrDefault_WithStaticOrdinals_EmptyReader_ReturnsDefault()
    {
        var reader = OptimizedEntityResultReader.Default;
        using var dbReader = new OptimizedEntityDbDataReader(Array.Empty<OptimizedEntity>());
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.FirstOrDefault(dbReader, ordinals);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FirstOrDefault_WithStaticOrdinals_SingleRow_ReturnsEntity()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[] { new OptimizedEntity { Id = 42, Name = "answer", Value = 42.0m } };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.FirstOrDefault(dbReader, ordinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Id);
        Assert.AreEqual("answer", result.Name);
    }

    [TestMethod]
    public void FirstOrDefault_WithStaticOrdinals_MultipleRows_ReturnsFirstOnly()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[]
        {
            new OptimizedEntity { Id = 1, Name = "first", Value = 1.0m },
            new OptimizedEntity { Id = 2, Name = "second", Value = 2.0m },
        };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.FirstOrDefault(dbReader, ordinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("first", result.Name);
    }

    #endregion

    #region Async Static Ordinals Extension Method Tests

    [TestMethod]
    public async Task ToListAsync_WithStaticOrdinals_EmptyReader_ReturnsEmptyList()
    {
        var reader = OptimizedEntityResultReader.Default;
        using var dbReader = new OptimizedEntityDbDataReader(Array.Empty<OptimizedEntity>());
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = await reader.ToListAsync(dbReader, ordinals);
        
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task ToListAsync_WithStaticOrdinals_MultipleRows_ReturnsAllEntities()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = Enumerable.Range(1, 50)
            .Select(i => new OptimizedEntity { Id = i, Name = $"async{i}", Value = i * 2.0m })
            .ToArray();
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = await reader.ToListAsync(dbReader, ordinals);
        
        Assert.AreEqual(50, result.Count);
        Assert.AreEqual("async1", result[0].Name);
        Assert.AreEqual("async50", result[49].Name);
    }

    [TestMethod]
    public async Task FirstOrDefaultAsync_WithStaticOrdinals_EmptyReader_ReturnsDefault()
    {
        var reader = OptimizedEntityResultReader.Default;
        using var dbReader = new OptimizedEntityDbDataReader(Array.Empty<OptimizedEntity>());
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = await reader.FirstOrDefaultAsync(dbReader, ordinals);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task FirstOrDefaultAsync_WithStaticOrdinals_SingleRow_ReturnsEntity()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[] { new OptimizedEntity { Id = 123, Name = "async-single", Value = 999.99m } };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = await reader.FirstOrDefaultAsync(dbReader, ordinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(123, result.Id);
        Assert.AreEqual("async-single", result.Name);
        Assert.AreEqual(999.99m, result.Value);
    }

    [TestMethod]
    public async Task FirstOrDefaultAsync_WithCancellationToken_Completes()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[] { new OptimizedEntity { Id = 1, Name = "cancel-test", Value = 1.0m } };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        using var cts = new CancellationTokenSource();
        
        var result = await reader.FirstOrDefaultAsync(dbReader, ordinals, cts.Token);
        
        Assert.IsNotNull(result);
        Assert.AreEqual("cancel-test", result.Name);
    }

    #endregion

    #region Comparison Tests - With vs Without Static Ordinals

    [TestMethod]
    public void ToList_WithAndWithoutOrdinals_ProduceSameResults()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[]
        {
            new OptimizedEntity { Id = 1, Name = "a", Value = 1.0m },
            new OptimizedEntity { Id = 2, Name = "b", Value = 2.0m },
            new OptimizedEntity { Id = 3, Name = "c", Value = 3.0m },
        };
        
        // Without static ordinals
        using var dbReader1 = new OptimizedEntityDbDataReader(entities);
        var result1 = reader.ToList(dbReader1);
        
        // With static ordinals
        using var dbReader2 = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        var result2 = reader.ToList(dbReader2, ordinals);
        
        Assert.AreEqual(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.AreEqual(result1[i].Id, result2[i].Id);
            Assert.AreEqual(result1[i].Name, result2[i].Name);
            Assert.AreEqual(result1[i].Value, result2[i].Value);
        }
    }

    [TestMethod]
    public async Task ToListAsync_WithAndWithoutOrdinals_ProduceSameResults()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[]
        {
            new OptimizedEntity { Id = 10, Name = "x", Value = 10.0m },
            new OptimizedEntity { Id = 20, Name = "y", Value = 20.0m },
        };
        
        // Without static ordinals
        using var dbReader1 = new OptimizedEntityDbDataReader(entities);
        var result1 = await reader.ToListAsync(dbReader1);
        
        // With static ordinals
        using var dbReader2 = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        var result2 = await reader.ToListAsync(dbReader2, ordinals);
        
        Assert.AreEqual(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.AreEqual(result1[i].Id, result2[i].Id);
            Assert.AreEqual(result1[i].Name, result2[i].Name);
            Assert.AreEqual(result1[i].Value, result2[i].Value);
        }
    }

    #endregion

    #region Nullable Field Tests with Static Ordinals

    [TestMethod]
    public void ToList_WithStaticOrdinals_NullableFields_HandlesNullCorrectly()
    {
        var reader = NullableEntityResultReader.Default;
        var entities = new[]
        {
            new NullableEntity { Id = 1, Name = "with-desc", Description = "has description" },
            new NullableEntity { Id = 2, Name = "no-desc", Description = null },
            new NullableEntity { Id = 3, Name = "empty-desc", Description = "" },
        };
        using var dbReader = new NullableEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.ToList(dbReader, ordinals);
        
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("has description", result[0].Description);
        Assert.IsNull(result[1].Description);
        Assert.AreEqual("", result[2].Description);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ToList_WithStaticOrdinals_LargeDataset_HandlesCorrectly()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new OptimizedEntity { Id = i, Name = $"large{i}", Value = i * 0.01m })
            .ToArray();
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.ToList(dbReader, ordinals);
        
        Assert.AreEqual(10000, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(10000, result[9999].Id);
    }

    [TestMethod]
    public void FirstOrDefault_WithStaticOrdinals_DoesNotConsumeAllRows()
    {
        var reader = OptimizedEntityResultReader.Default;
        var entities = new[]
        {
            new OptimizedEntity { Id = 1, Name = "first", Value = 1.0m },
            new OptimizedEntity { Id = 2, Name = "second", Value = 2.0m },
        };
        using var dbReader = new OptimizedEntityDbDataReader(entities);
        var ordinals = new int[] { 0, 1, 2 };
        
        var result = reader.FirstOrDefault(dbReader, ordinals);
        
        // Should only have read once (for the first row)
        Assert.AreEqual(1, dbReader.ReadCount);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
    }

    #endregion
}

#region Test Entities and DbDataReaders

[SqlxEntity]
[SqlxParameter]
public class OptimizedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

[SqlxEntity]
[SqlxParameter]
public class NullableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class OptimizedEntityDbDataReader : DbDataReader
{
    private readonly OptimizedEntity[] _entities;
    private int _currentIndex = -1;
    public int ReadCount { get; private set; }

    public OptimizedEntityDbDataReader(OptimizedEntity[] entities) => _entities = entities;

    public override bool Read()
    {
        ReadCount++;
        _currentIndex++;
        return _currentIndex < _entities.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "value" => 2,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override int GetInt32(int ordinal) => _entities[_currentIndex].Id;
    public override string GetString(int ordinal) => _entities[_currentIndex].Name;
    public override decimal GetDecimal(int ordinal) => _entities[_currentIndex].Value;
    public override bool IsDBNull(int ordinal) => false;

    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 3;
    public override bool HasRows => _entities.Length > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;
    public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
    public override byte GetByte(int ordinal) => throw new NotImplementedException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override char GetChar(int ordinal) => throw new NotImplementedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
    public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
    public override double GetDouble(int ordinal) => throw new NotImplementedException();
    public override IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => throw new NotImplementedException();
    public override float GetFloat(int ordinal) => throw new NotImplementedException();
    public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
    public override short GetInt16(int ordinal) => throw new NotImplementedException();
    public override long GetInt64(int ordinal) => throw new NotImplementedException();
    public override string GetName(int ordinal) => throw new NotImplementedException();
    public override object GetValue(int ordinal) => throw new NotImplementedException();
    public override int GetValues(object[] values) => throw new NotImplementedException();
    public override bool NextResult() => false;
}

public class NullableEntityDbDataReader : DbDataReader
{
    private readonly NullableEntity[] _entities;
    private int _currentIndex = -1;

    public NullableEntityDbDataReader(NullableEntity[] entities) => _entities = entities;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _entities.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "description" => 2,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override int GetInt32(int ordinal) => _entities[_currentIndex].Id;
    public override string GetString(int ordinal) => ordinal switch
    {
        1 => _entities[_currentIndex].Name,
        2 => _entities[_currentIndex].Description!,
        _ => throw new InvalidOperationException()
    };
    public override bool IsDBNull(int ordinal) => ordinal == 2 && _entities[_currentIndex].Description == null;

    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 3;
    public override bool HasRows => _entities.Length > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;
    public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
    public override byte GetByte(int ordinal) => throw new NotImplementedException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override char GetChar(int ordinal) => throw new NotImplementedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
    public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
    public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
    public override double GetDouble(int ordinal) => throw new NotImplementedException();
    public override IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => throw new NotImplementedException();
    public override float GetFloat(int ordinal) => throw new NotImplementedException();
    public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
    public override short GetInt16(int ordinal) => throw new NotImplementedException();
    public override long GetInt64(int ordinal) => throw new NotImplementedException();
    public override string GetName(int ordinal) => throw new NotImplementedException();
    public override object GetValue(int ordinal) => throw new NotImplementedException();
    public override int GetValues(object[] values) => throw new NotImplementedException();
    public override bool NextResult() => false;
}

#endregion
