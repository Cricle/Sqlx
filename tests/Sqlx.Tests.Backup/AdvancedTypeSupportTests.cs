using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sqlx.Tests;

/// <summary>
/// TDD tests for advanced type support: mixed properties, readonly properties, structs, tuples, and dictionaries.
/// </summary>
[TestClass]
public class AdvancedTypeSupportTests
{
    #region Mixed Record and Properties Tests

    [TestMethod]
    public void MixedRecord_WithAdditionalProperties_GeneratesResultReader()
    {
        var reader = MixedRecordResultReader.Default;
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void MixedRecord_WithAdditionalProperties_ReadsFromDatabase()
    {
        var reader = MixedRecordResultReader.Default;
        var records = new[]
        {
            new MixedRecord(1, "Alice") { Email = "alice@example.com", Age = 25 }
        };
        using var dbReader = new MixedRecordDbReader(records);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("alice@example.com", results[0].Email);
        Assert.AreEqual(25, results[0].Age);
    }

    #endregion

    #region Readonly Property Tests

    [TestMethod]
    public void ReadonlyProperty_IsIgnored_InGeneration()
    {
        var reader = EntityWithReadonlyResultReader.Default;
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void ReadonlyProperty_IsIgnored_InReading()
    {
        var reader = EntityWithReadonlyResultReader.Default;
        var entities = new[]
        {
            new EntityWithReadonly { Id = 1, Name = "Test" }
        };
        using var dbReader = new EntityWithReadonlyDbReader(entities);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Test", results[0].Name);
    }

    #endregion

    #region Struct Tests

    [TestMethod]
    public void Struct_GeneratesResultReader()
    {
        var reader = UserStructResultReader.Default;
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void Struct_ReadsFromDatabase()
    {
        var reader = UserStructResultReader.Default;
        var structs = new[]
        {
            new UserStruct { Id = 1, Name = "Alice", Email = "alice@example.com" }
        };
        using var dbReader = new UserStructDbReader(structs);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("alice@example.com", results[0].Email);
    }

    #endregion

    #region Struct Record Tests

    [TestMethod]
    public void StructRecord_GeneratesResultReader()
    {
        var reader = UserStructRecordResultReader.Default;
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void StructRecord_ReadsFromDatabase()
    {
        var reader = UserStructRecordResultReader.Default;
        var records = new[]
        {
            new UserStructRecord(1, "Bob", "bob@example.com")
        };
        using var dbReader = new UserStructRecordDbReader(records);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Bob", results[0].Name);
        Assert.AreEqual("bob@example.com", results[0].Email);
    }

    #endregion

    #region Struct Constructor Tests

    [TestMethod]
    public void StructWithConstructor_GeneratesResultReader()
    {
        var reader = StructWithConstructorResultReader.Default;
        Assert.IsNotNull(reader);
    }


    [TestMethod]
    public void StructWithConstructor_ReadsFromDatabase()
    {
        var reader = StructWithConstructorResultReader.Default;
        var structs = new[]
        {
            new StructWithConstructor(1, "Charlie", "charlie@example.com")
        };
        using var dbReader = new StructWithConstructorDbReader(structs);
        
        var results = reader.ToList(dbReader);
        
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Charlie", results[0].Name);
        Assert.AreEqual("charlie@example.com", results[0].Email);
    }

    #endregion
}

#region Test Entity Definitions

/// <summary>
/// Mixed record with primary constructor and additional properties.
/// </summary>
[Sqlx]
[TableName("users")]
public record MixedRecord(long Id, string Name)
{
    [Key]
    public long Id { get; init; } = Id;
    public string Name { get; init; } = Name;
    public string Email { get; set; } = "";
    public int Age { get; set; }
}

/// <summary>
/// Entity with readonly property that should be ignored.
/// </summary>
[Sqlx]
[TableName("entities")]
public class EntityWithReadonly
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = "";
    
    // Readonly property - should be ignored
    public string ComputedValue => $"Computed_{Name}";
}

/// <summary>
/// Struct type for testing.
/// </summary>
[Sqlx]
[TableName("users")]
public struct UserStruct
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

/// <summary>
/// Struct record type for testing.
/// </summary>
[Sqlx]
[TableName("users")]
public readonly record struct UserStructRecord(long Id, string Name, string Email)
{
    [Key]
    public long Id { get; init; } = Id;
    public string Name { get; init; } = Name;
    public string Email { get; init; } = Email;
}

/// <summary>
/// Struct with constructor.
/// </summary>
[Sqlx]
[TableName("users")]
public struct StructWithConstructor
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    
    public StructWithConstructor(long id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }
}

#endregion

#region Test DbDataReader Implementations

public class MixedRecordDbReader : DbDataReader
{
    private readonly MixedRecord[] _records;
    private int _currentIndex = -1;

    public MixedRecordDbReader(MixedRecord[] records) => _records = records;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _records.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "email" => 2,
        "age" => 3,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override long GetInt64(int ordinal) => _records[_currentIndex].Id;
    public override int GetInt32(int ordinal) => _records[_currentIndex].Age;
    public override string GetString(int ordinal) => ordinal switch
    {
        1 => _records[_currentIndex].Name,
        2 => _records[_currentIndex].Email,
        _ => throw new InvalidOperationException()
    };
    public override bool IsDBNull(int ordinal) => false;

    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 4;
    public override bool HasRows => _records.Length > 0;
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
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(long),
        1 => typeof(string),
        2 => typeof(string),
        3 => typeof(int),
        _ => throw new IndexOutOfRangeException($"Ordinal {ordinal} out of range")
    };
    public override float GetFloat(int ordinal) => throw new NotImplementedException();
    public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
    public override short GetInt16(int ordinal) => throw new NotImplementedException();
    public override string GetName(int ordinal) => throw new NotImplementedException();
    public override object GetValue(int ordinal) => throw new NotImplementedException();
    public override int GetValues(object[] values) => throw new NotImplementedException();
    public override bool NextResult() => false;
}

public class EntityWithReadonlyDbReader : DbDataReader
{
    private readonly EntityWithReadonly[] _entities;
    private int _currentIndex = -1;

    public EntityWithReadonlyDbReader(EntityWithReadonly[] entities) => _entities = entities;

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
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override long GetInt64(int ordinal) => _entities[_currentIndex].Id;
    public override string GetString(int ordinal) => _entities[_currentIndex].Name;
    public override bool IsDBNull(int ordinal) => false;

    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 2;
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
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(long),
        1 => typeof(string),
        _ => throw new IndexOutOfRangeException($"Ordinal {ordinal} out of range")
    };
    public override float GetFloat(int ordinal) => throw new NotImplementedException();
    public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
    public override short GetInt16(int ordinal) => throw new NotImplementedException();
    public override int GetInt32(int ordinal) => throw new NotImplementedException();
    public override string GetName(int ordinal) => throw new NotImplementedException();
    public override object GetValue(int ordinal) => throw new NotImplementedException();
    public override int GetValues(object[] values) => throw new NotImplementedException();
    public override bool NextResult() => false;
}

public class UserStructDbReader : DbDataReader
{
    private readonly UserStruct[] _structs;
    private int _currentIndex = -1;

    public UserStructDbReader(UserStruct[] structs) => _structs = structs;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _structs.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "email" => 2,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override long GetInt64(int ordinal) => _structs[_currentIndex].Id;
    public override string GetString(int ordinal) => ordinal switch
    {
        1 => _structs[_currentIndex].Name,
        2 => _structs[_currentIndex].Email,
        _ => throw new InvalidOperationException()
    };
    public override bool IsDBNull(int ordinal) => false;

    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 3;
    public override bool HasRows => _structs.Length > 0;
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
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(long),
        1 => typeof(string),
        2 => typeof(string),
        _ => throw new IndexOutOfRangeException($"Ordinal {ordinal} out of range")
    };
    public override float GetFloat(int ordinal) => throw new NotImplementedException();
    public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
    public override short GetInt16(int ordinal) => throw new NotImplementedException();
    public override int GetInt32(int ordinal) => throw new NotImplementedException();
    public override string GetName(int ordinal) => throw new NotImplementedException();
    public override object GetValue(int ordinal) => throw new NotImplementedException();
    public override int GetValues(object[] values) => throw new NotImplementedException();
    public override bool NextResult() => false;
}

public class UserStructRecordDbReader : DbDataReader
{
    private readonly UserStructRecord[] _records;
    private int _currentIndex = -1;

    public UserStructRecordDbReader(UserStructRecord[] records) => _records = records;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _records.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "email" => 2,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override long GetInt64(int ordinal) => _records[_currentIndex].Id;
    public override string GetString(int ordinal) => ordinal switch
    {
        1 => _records[_currentIndex].Name,
        2 => _records[_currentIndex].Email,
        _ => throw new InvalidOperationException()
    };
    public override bool IsDBNull(int ordinal) => false;

    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 3;
    public override bool HasRows => _records.Length > 0;
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
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(long),
        1 => typeof(string),
        2 => typeof(string),
        _ => throw new IndexOutOfRangeException($"Ordinal {ordinal} out of range")
    };
    public override float GetFloat(int ordinal) => throw new NotImplementedException();
    public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
    public override short GetInt16(int ordinal) => throw new NotImplementedException();
    public override int GetInt32(int ordinal) => throw new NotImplementedException();
    public override string GetName(int ordinal) => throw new NotImplementedException();
    public override object GetValue(int ordinal) => throw new NotImplementedException();
    public override int GetValues(object[] values) => throw new NotImplementedException();
    public override bool NextResult() => false;
}

public class StructWithConstructorDbReader : DbDataReader
{
    private readonly StructWithConstructor[] _structs;
    private int _currentIndex = -1;

    public StructWithConstructorDbReader(StructWithConstructor[] structs) => _structs = structs;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _structs.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "email" => 2,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override long GetInt64(int ordinal) => _structs[_currentIndex].Id;
    public override string GetString(int ordinal) => ordinal switch
    {
        1 => _structs[_currentIndex].Name,
        2 => _structs[_currentIndex].Email,
        _ => throw new InvalidOperationException()
    };
    public override bool IsDBNull(int ordinal) => false;

    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 3;
    public override bool HasRows => _structs.Length > 0;
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
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(long),
        1 => typeof(string),
        2 => typeof(string),
        _ => throw new IndexOutOfRangeException($"Ordinal {ordinal} out of range")
    };
    public override float GetFloat(int ordinal) => throw new NotImplementedException();
    public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
    public override short GetInt16(int ordinal) => throw new NotImplementedException();
    public override int GetInt32(int ordinal) => throw new NotImplementedException();
    public override string GetName(int ordinal) => throw new NotImplementedException();
    public override object GetValue(int ordinal) => throw new NotImplementedException();
    public override int GetValues(object[] values) => throw new NotImplementedException();
    public override bool NextResult() => false;
}

#endregion
