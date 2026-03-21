using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.Helpers;
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
    public void ResultReader_GetOrdinals_Works()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());
        
        Span<int> ordinals = stackalloc int[4];
        reader.GetOrdinals(dbReader, ordinals);
        
        // Verify ordinals are initialized correctly (all positive for standard types)
        Assert.IsTrue(ordinals[0] >= 0); // Id
        Assert.IsTrue(ordinals[1] >= 0); // UserName
        Assert.IsTrue(ordinals[2] >= 0); // IsActive
        Assert.IsTrue(ordinals[3] >= 0); // CreatedAt
    }

    [TestMethod]
    public void ResultReader_Read_WithOrdinals_ReturnsEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            TestEntityFactory.CreateTestEntity(id: 1, userName: "test", createdAt: new DateTime(2024, 1, 1))
        };
        using var dbReader = new TestDbDataReader(entities);
        dbReader.Read(); // Position at first row
        
        var result = reader.Read(dbReader);
        
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "test", createdAt: new DateTime(2024, 1, 1))
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
            TestEntityFactory.CreateTestEntity(id: 3, userName: "user3"),
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "test", createdAt: new DateTime(2024, 1, 1))
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "test", createdAt: new DateTime(2024, 1, 1))
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
            TestEntityFactory.CreateTestEntityWithNullable(id: 1, name: "test", description: null)
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
            TestEntityFactory.CreateTestEntityWithNullable(id: 1, name: "test", description: "desc")
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
        };
        using var dbReader = new TestDbDataReader(entities);
        
        // Use ToList which internally uses struct ordinals
        var results = reader.ToList(dbReader);
        
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
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var result = reader.FirstOrDefault(dbReader);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("user1", result.UserName);
    }

    [TestMethod]
    public async Task ResultReader_ToListAsync_ReturnsAllEntities()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var results = await reader.ToListAsync(dbReader);
        
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    public void ResultReader_ToList_ArrayOrdinalReader_UsesArrayFastPath()
    {
        var reader = new ArrayOrdinalTrackingReader();
        var entities = new[]
        {
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(1, reader.GetOrdinalsCallCount);
        Assert.AreEqual(2, reader.ReadWithArrayOrdinalsCallCount);
        Assert.AreEqual(0, reader.ReadWithSpanOrdinalsCallCount);
    }

    [TestMethod]
    public async Task ResultReader_ToListAsync_ArrayOrdinalReader_UsesArrayFastPath()
    {
        var reader = new ArrayOrdinalTrackingReader();
        var entities = new[]
        {
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
            TestEntityFactory.CreateTestEntity(id: 2, userName: "user2", isActive: false),
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = await reader.ToListAsync(dbReader);

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(1, reader.GetOrdinalsCallCount);
        Assert.AreEqual(2, reader.ReadWithArrayOrdinalsCallCount);
        Assert.AreEqual(0, reader.ReadWithSpanOrdinalsCallCount);
    }

    [TestMethod]
    public async Task ResultReader_FirstOrDefaultAsync_ReturnsFirstEntity()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            TestEntityFactory.CreateTestEntity(id: 1, userName: "user1"),
        };
        using var dbReader = new TestDbDataReader(entities);
        
        var result = await reader.FirstOrDefaultAsync(dbReader);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
    }

    #endregion

    private sealed class ArrayOrdinalTrackingReader : IResultReader<TestEntity>, IArrayOrdinalReader<TestEntity>
    {
        public int PropertyCount => 4;
        public int GetOrdinalsCallCount { get; private set; }
        public int ReadWithArrayOrdinalsCallCount { get; private set; }
        public int ReadWithSpanOrdinalsCallCount { get; private set; }

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            GetOrdinalsCallCount++;
            ordinals[0] = reader.GetOrdinal("id");
            ordinals[1] = reader.GetOrdinal("user_name");
            ordinals[2] = reader.GetOrdinal("is_active");
            ordinals[3] = reader.GetOrdinal("created_at");
        }

        public TestEntity Read(IDataReader reader)
        {
            return new TestEntity
            {
                Id = reader.GetInt32(0),
                UserName = reader.GetString(1),
                IsActive = reader.GetBoolean(2),
                CreatedAt = reader.GetDateTime(3),
            };
        }

        public TestEntity Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            ReadWithSpanOrdinalsCallCount++;
            return new TestEntity
            {
                Id = reader.GetInt32(ordinals[0]),
                UserName = reader.GetString(ordinals[1]),
                IsActive = reader.GetBoolean(ordinals[2]),
                CreatedAt = reader.GetDateTime(ordinals[3]),
            };
        }

        public TestEntity Read(IDataReader reader, int[] ordinals)
        {
            ReadWithArrayOrdinalsCallCount++;
            return new TestEntity
            {
                Id = reader.GetInt32(ordinals[0]),
                UserName = reader.GetString(ordinals[1]),
                IsActive = reader.GetBoolean(ordinals[2]),
                CreatedAt = reader.GetDateTime(ordinals[3]),
            };
        }
    }
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
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(int),
        1 => typeof(string),
        2 => typeof(bool),
        3 => typeof(DateTime),
        _ => throw new IndexOutOfRangeException($"Ordinal {ordinal} out of range")
    };
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
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(int),
        1 => typeof(string),
        2 => typeof(string),
        _ => throw new IndexOutOfRangeException($"Ordinal {ordinal} out of range")
    };
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
