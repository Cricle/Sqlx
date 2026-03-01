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
/// TDD tests for record and DTO conversion support.
/// Tests verify that Sqlx can generate ResultReaders for record types and support DTO conversions.
/// </summary>
[TestClass]
public class RecordAndDtoSupportTests
{
    #region Record Type Tests

    [TestMethod]
    public void RecordType_BasicRecord_GeneratesResultReader()
    {
        // Arrange: Define a basic record type
        var reader = UserRecordResultReader.Default;
        
        // Assert: ResultReader should be generated
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void RecordType_BasicRecord_ReadsFromDatabase()
    {
        // Arrange
        var reader = UserRecordResultReader.Default;
        var records = new[]
        {
            new UserRecord(1, "Alice", "alice@example.com")
        };
        using var dbReader = new UserRecordDbReader(records);
        
        // Act
        var results = reader.ToList(dbReader);
        
        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("alice@example.com", results[0].Email);
    }


    [TestMethod]
    public async Task RecordType_BasicRecord_ReadsFromDatabaseAsync()
    {
        // Arrange
        var reader = UserRecordResultReader.Default;
        var records = new[]
        {
            new UserRecord(1, "Bob", "bob@example.com"),
            new UserRecord(2, "Charlie", "charlie@example.com")
        };
        using var dbReader = new UserRecordDbReader(records);
        
        // Act
        var results = await reader.ToListAsync(dbReader);
        
        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Name);
        Assert.AreEqual("Charlie", results[1].Name);
    }

    [TestMethod]
    public void RecordType_WithNullableProperties_HandlesNull()
    {
        // Arrange
        var reader = UserRecordWithNullableResultReader.Default;
        var records = new[]
        {
            new UserRecordWithNullable(1, "Alice", null)
        };
        using var dbReader = new UserRecordWithNullableDbReader(records);
        
        // Act
        var results = reader.ToList(dbReader);
        
        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.IsNull(results[0].Email);
    }

    [TestMethod]
    public void RecordType_WithNullableProperties_HandlesValue()
    {
        // Arrange
        var reader = UserRecordWithNullableResultReader.Default;
        var records = new[]
        {
            new UserRecordWithNullable(1, "Alice", "alice@example.com")
        };
        using var dbReader = new UserRecordWithNullableDbReader(records);
        
        // Act
        var results = reader.ToList(dbReader);
        
        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("alice@example.com", results[0].Email);
    }

    #endregion

    #region DTO Conversion Tests

    [TestMethod]
    public void DtoConversion_ClassToDto_GeneratesResultReader()
    {
        // Arrange: Define a DTO type
        var reader = UserDtoResultReader.Default;
        
        // Assert: ResultReader should be generated
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void DtoConversion_ClassToDto_ReadsFromDatabase()
    {
        // Arrange
        var reader = UserDtoResultReader.Default;
        var dtos = new[]
        {
            new UserDto { Id = 1, Name = "Alice", Email = "alice@example.com" }
        };
        using var dbReader = new UserDtoDbReader(dtos);
        
        // Act
        var results = reader.ToList(dbReader);
        
        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("alice@example.com", results[0].Email);
    }

    [TestMethod]
    public async Task DtoConversion_ClassToDto_ReadsFromDatabaseAsync()
    {
        // Arrange
        var reader = UserDtoResultReader.Default;
        var dtos = new[]
        {
            new UserDto { Id = 1, Name = "Alice", Email = "alice@example.com" },
            new UserDto { Id = 2, Name = "Bob", Email = "bob@example.com" }
        };
        using var dbReader = new UserDtoDbReader(dtos);
        
        // Act
        var results = await reader.ToListAsync(dbReader);
        
        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Bob", results[1].Name);
    }

    #endregion

    #region Complex Record Tests

    [TestMethod]
    public void RecordType_ComplexRecord_WithMultipleTypes_ReadsCorrectly()
    {
        // Arrange
        var reader = ComplexRecordResultReader.Default;
        var records = new[]
        {
            new ComplexRecord(1, "Test", 25, true, DateTime.Now, 99.99m)
        };
        using var dbReader = new ComplexRecordDbReader(records);
        
        // Act
        var results = reader.ToList(dbReader);
        
        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Test", results[0].Name);
        Assert.AreEqual(25, results[0].Age);
        Assert.IsTrue(results[0].IsActive);
        Assert.AreEqual(99.99m, results[0].Balance);
    }

    #endregion
}

#region Test Entity Definitions

/// <summary>
/// Basic record type for testing.
/// </summary>
[Sqlx]
[TableName("users")]
public record UserRecord(long Id, string Name, string Email)
{
    [Key]
    public long Id { get; init; } = Id;
    public string Name { get; init; } = Name;
    public string Email { get; init; } = Email;
}

/// <summary>
/// Record type with nullable properties.
/// </summary>
[Sqlx]
[TableName("users")]
public record UserRecordWithNullable(long Id, string Name, string? Email)
{
    [Key]
    public long Id { get; init; } = Id;
    public string Name { get; init; } = Name;
    public string? Email { get; init; } = Email;
}

/// <summary>
/// DTO class for testing.
/// </summary>
[Sqlx]
[TableName("users")]
public class UserDto
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>
/// Complex record with multiple property types.
/// </summary>
[Sqlx]
[TableName("complex")]
public record ComplexRecord(long Id, string Name, int Age, bool IsActive, DateTime CreatedAt, decimal Balance)
{
    [Key]
    public long Id { get; init; } = Id;
    public string Name { get; init; } = Name;
    public int Age { get; init; } = Age;
    public bool IsActive { get; init; } = IsActive;
    public DateTime CreatedAt { get; init; } = CreatedAt;
    public decimal Balance { get; init; } = Balance;
}

#endregion

#region Test DbDataReader Implementations

/// <summary>
/// Test DbDataReader for UserRecord.
/// </summary>
public class UserRecordDbReader : DbDataReader
{
    private readonly UserRecord[] _records;
    private int _currentIndex = -1;

    public UserRecordDbReader(UserRecord[] records)
    {
        _records = records;
    }

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _records.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Read());
    }

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

    // Required abstract members
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

/// <summary>
/// Test DbDataReader for UserRecordWithNullable.
/// </summary>
public class UserRecordWithNullableDbReader : DbDataReader
{
    private readonly UserRecordWithNullable[] _records;
    private int _currentIndex = -1;

    public UserRecordWithNullableDbReader(UserRecordWithNullable[] records)
    {
        _records = records;
    }

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _records.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Read());
    }

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
        2 => _records[_currentIndex].Email!,
        _ => throw new InvalidOperationException()
    };
    public override bool IsDBNull(int ordinal) => ordinal == 2 && _records[_currentIndex].Email == null;

    // Required abstract members
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

/// <summary>
/// Test DbDataReader for UserDto.
/// </summary>
public class UserDtoDbReader : DbDataReader
{
    private readonly UserDto[] _dtos;
    private int _currentIndex = -1;

    public UserDtoDbReader(UserDto[] dtos)
    {
        _dtos = dtos;
    }

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _dtos.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Read());
    }

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "email" => 2,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override long GetInt64(int ordinal) => _dtos[_currentIndex].Id;
    public override string GetString(int ordinal) => ordinal switch
    {
        1 => _dtos[_currentIndex].Name,
        2 => _dtos[_currentIndex].Email,
        _ => throw new InvalidOperationException()
    };
    public override bool IsDBNull(int ordinal) => false;

    // Required abstract members
    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 3;
    public override bool HasRows => _dtos.Length > 0;
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

/// <summary>
/// Test DbDataReader for ComplexRecord.
/// </summary>
public class ComplexRecordDbReader : DbDataReader
{
    private readonly ComplexRecord[] _records;
    private int _currentIndex = -1;

    public ComplexRecordDbReader(ComplexRecord[] records)
    {
        _records = records;
    }

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _records.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Read());
    }

    public override int GetOrdinal(string name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "age" => 2,
        "is_active" => 3,
        "created_at" => 4,
        "balance" => 5,
        _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
    };

    public override long GetInt64(int ordinal) => _records[_currentIndex].Id;
    public override string GetString(int ordinal) => _records[_currentIndex].Name;
    public override int GetInt32(int ordinal) => _records[_currentIndex].Age;
    public override bool GetBoolean(int ordinal) => _records[_currentIndex].IsActive;
    public override DateTime GetDateTime(int ordinal) => _records[_currentIndex].CreatedAt;
    public override decimal GetDecimal(int ordinal) => _records[_currentIndex].Balance;
    public override bool IsDBNull(int ordinal) => false;

    // Required abstract members
    public override object this[int ordinal] => throw new NotImplementedException();
    public override object this[string name] => throw new NotImplementedException();
    public override int Depth => 0;
    public override int FieldCount => 6;
    public override bool HasRows => _records.Length > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;
    public override byte GetByte(int ordinal) => throw new NotImplementedException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override char GetChar(int ordinal) => throw new NotImplementedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
    public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
    public override double GetDouble(int ordinal) => throw new NotImplementedException();
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(long),
        1 => typeof(string),
        2 => typeof(int),
        3 => typeof(bool),
        4 => typeof(DateTime),
        5 => typeof(decimal),
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

#endregion
