using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Data;
using System.Data.Common;

namespace Sqlx.Tests;

/// <summary>
/// Tests for generated IResultReader implementations and extension methods.
/// </summary>
[TestClass]
public class ResultReaderTests
{
    #region Basic Read Tests

    [TestMethod]
    public void ResultReader_SingletonInstance()
    {
        var reader1 = TestEntityResultReader.Default;
        var reader2 = TestEntityResultReader.Default;
        
        Assert.AreSame(reader1, reader2);
    }

    [TestMethod]
    public void ResultReader_GetOrdinals_ReturnsCorrectCount()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());
        
        var ordinals = reader.GetOrdinals(dbReader);
        
        Assert.AreEqual(4, ordinals.Length);
    }

    [TestMethod]
    public void ResultReader_Read_WithOrdinals_ReturnsEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
        };
        using var dbReader = new TestDbDataReader(entities);
        dbReader.Read(); // Position at first row
        
        var ordinals = reader.GetOrdinals(dbReader);
        var result = reader.Read(dbReader, ordinals);
        
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("test", result.UserName);
        Assert.IsTrue(result.IsActive);
    }

    #endregion

    #region Extension Method Tests - ToList

    [TestMethod]
    public void ResultReader_ToList_EmptyReader_ReturnsEmpty()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void ResultReader_ToList_SingleRow_ReturnsOneEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("test", results[0].UserName);
        Assert.IsTrue(results[0].IsActive);
    }

    [TestMethod]
    public void ResultReader_ToList_MultipleRows_ReturnsAllEntities()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "user1", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 2, UserName = "user2", IsActive = false, CreatedAt = DateTime.Now },
            new TestEntity { Id = 3, UserName = "user3", IsActive = true, CreatedAt = DateTime.Now },
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("user1", results[0].UserName);
        Assert.AreEqual("user2", results[1].UserName);
        Assert.AreEqual("user3", results[2].UserName);
    }

    [TestMethod]
    public async Task ResultReader_ToListAsync_EmptyReader_ReturnsEmpty()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());
        
        var results = await reader.ToListAsync(dbReader);
        
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task ResultReader_ToListAsync_MultipleRows_ReturnsAllEntities()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "user1", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 2, UserName = "user2", IsActive = false, CreatedAt = DateTime.Now },
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var results = await reader.ToListAsync(dbReader);
        
        Assert.AreEqual(2, results.Count);
    }

    #endregion

    #region Extension Method Tests - FirstOrDefault

    [TestMethod]
    public void ResultReader_FirstOrDefault_EmptyReader_ReturnsDefault()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());
        
        var result = reader.FirstOrDefault(dbReader);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ResultReader_FirstOrDefault_SingleRow_ReturnsEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var result = reader.FirstOrDefault(dbReader);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("test", result.UserName);
        Assert.IsTrue(result.IsActive);
    }

    [TestMethod]
    public void ResultReader_FirstOrDefault_MultipleRows_ReturnsFirstEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "user1", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 2, UserName = "user2", IsActive = false, CreatedAt = DateTime.Now },
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var result = reader.FirstOrDefault(dbReader);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("user1", result.UserName);
    }

    [TestMethod]
    public async Task ResultReader_FirstOrDefaultAsync_EmptyReader_ReturnsDefault()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());
        
        var result = await reader.FirstOrDefaultAsync(dbReader);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ResultReader_FirstOrDefaultAsync_SingleRow_ReturnsEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var result = await reader.FirstOrDefaultAsync(dbReader);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("test", result.UserName);
    }

    #endregion

    #region Nullable Field Tests

    [TestMethod]
    public void ResultReader_NullableField_HandlesNull()
    {
        var reader = TestEntityWithNullableResultReader.Default;
        var entities = new[]
        {
            new TestEntityWithNullable { Id = 1, Name = "test", Description = null }
        };
        using var dbReader = new TestDbDataReaderWithNullable(entities);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.IsNull(results[0].Description);
    }

    [TestMethod]
    public void ResultReader_NullableField_HandlesValue()
    {
        var reader = TestEntityWithNullableResultReader.Default;
        var entities = new[]
        {
            new TestEntityWithNullable { Id = 1, Name = "test", Description = "desc" }
        };
        using var dbReader = new TestDbDataReaderWithNullable(entities);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("desc", results[0].Description);
    }

    #endregion

    #region Static Ordinals Tests

    [TestMethod]
    public void ResultReader_ToList_WithStaticOrdinals_ReturnsAllEntities()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "user1", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 2, UserName = "user2", IsActive = false, CreatedAt = DateTime.Now },
        };
        using var dbReader = new TestDbDataReader(entities);
        
        // Pre-computed ordinals (simulating static ordinals from generator)
        var ordinals = new int[] { 0, 1, 2, 3 };
        var results = reader.ToList(dbReader, ordinals);
        
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("user1", results[0].UserName);
        Assert.AreEqual("user2", results[1].UserName);
    }

    [TestMethod]
    public void ResultReader_FirstOrDefault_WithStaticOrdinals_ReturnsFirstEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "user1", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 2, UserName = "user2", IsActive = false, CreatedAt = DateTime.Now },
        };
        using var dbReader = new TestDbDataReader(entities);
        
        // Pre-computed ordinals
        var ordinals = new int[] { 0, 1, 2, 3 };
        var result = reader.FirstOrDefault(dbReader, ordinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("user1", result.UserName);
    }

    [TestMethod]
    public async Task ResultReader_ToListAsync_WithStaticOrdinals_ReturnsAllEntities()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "user1", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 2, UserName = "user2", IsActive = false, CreatedAt = DateTime.Now },
        };
        using var dbReader = new TestDbDataReader(entities);
        
        // Pre-computed ordinals
        var ordinals = new int[] { 0, 1, 2, 3 };
        var results = await reader.ToListAsync(dbReader, ordinals);
        
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    public async Task ResultReader_FirstOrDefaultAsync_WithStaticOrdinals_ReturnsFirstEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "user1", IsActive = true, CreatedAt = DateTime.Now },
        };
        using var dbReader = new TestDbDataReader(entities);
        
        // Pre-computed ordinals
        var ordinals = new int[] { 0, 1, 2, 3 };
        var result = await reader.FirstOrDefaultAsync(dbReader, ordinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
    }

    #endregion
}

#region Test DbDataReader Implementations

/// <summary>
/// Test DbDataReader for TestEntity.
/// </summary>
public class TestDbDataReader : DbDataReader
{
    private readonly TestEntity[] _entities;
    private int _currentIndex = -1;
    public int ReadCount { get; private set; }

    public TestDbDataReader(TestEntity[] entities)
    {
        _entities = entities;
    }

    public override bool Read()
    {
        ReadCount++;
        _currentIndex++;
        return _currentIndex < _entities.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Read());
    }

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "user_name" => 1,
        "is_active" => 2,
        "created_at" => 3,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override int GetInt32(int ordinal) => _entities[_currentIndex].Id;
    public override string GetString(int ordinal) => ordinal switch
    {
        1 => _entities[_currentIndex].UserName,
        _ => throw new InvalidOperationException()
    };
    public override bool GetBoolean(int ordinal) => _entities[_currentIndex].IsActive;
    public override DateTime GetDateTime(int ordinal) => _entities[_currentIndex].CreatedAt;
    public override bool IsDBNull(int ordinal) => false;

    // Required abstract members
    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 4;
    public override bool HasRows => _entities.Length > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;
    public override byte GetByte(int ordinal) => throw new NotImplementedException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override char GetChar(int ordinal) => throw new NotImplementedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
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

/// <summary>
/// Test DbDataReader for TestEntityWithNullable.
/// </summary>
public class TestDbDataReaderWithNullable : DbDataReader
{
    private readonly TestEntityWithNullable[] _entities;
    private int _currentIndex = -1;

    public TestDbDataReaderWithNullable(TestEntityWithNullable[] entities)
    {
        _entities = entities;
    }

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _entities.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Read());
    }

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

    // Required abstract members
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
