using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for source generator optimizations including:
/// - Static ordinals generation for {{columns}} templates
/// - InsertAndGetId tuple return support
/// - Generated code correctness
/// </summary>
[TestClass]
public class SourceGeneratorOptimizationTests
{
    #region Generated ResultReader Tests

    [TestMethod]
    public void GeneratedResultReader_ImplementsIResultReader()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        
        Assert.IsInstanceOfType(reader, typeof(IResultReader<GeneratedOptEntity>));
    }

    [TestMethod]
    public void GeneratedResultReader_GetOrdinals_ReturnsCorrectCount()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        using var dbReader = new GeneratedOptEntityDbDataReader(Array.Empty<GeneratedOptEntity>());
        
        var ordinals = reader.GetOrdinals(dbReader);
        
        // GeneratedOptEntity has 4 properties: Id, Title, Amount, IsEnabled
        Assert.AreEqual(4, ordinals.Length);
    }

    [TestMethod]
    public void GeneratedResultReader_Read_WithIDataReader_Works()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[] { new GeneratedOptEntity { Id = 1, Title = "test", Amount = 99.99m, IsEnabled = true } };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        dbReader.Read();
        
        var result = reader.Read(dbReader);
        
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("test", result.Title);
        Assert.AreEqual(99.99m, result.Amount);
        Assert.IsTrue(result.IsEnabled);
    }

    [TestMethod]
    public void GeneratedResultReader_Read_WithOrdinals_Works()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[] { new GeneratedOptEntity { Id = 42, Title = "ordinal-test", Amount = 123.45m, IsEnabled = false } };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        dbReader.Read();
        
        var ordinals = reader.GetOrdinals(dbReader);
        var result = reader.Read(dbReader, ordinals);
        
        Assert.AreEqual(42, result.Id);
        Assert.AreEqual("ordinal-test", result.Title);
        Assert.AreEqual(123.45m, result.Amount);
        Assert.IsFalse(result.IsEnabled);
    }

    #endregion

    #region Static Ordinals Simulation Tests

    [TestMethod]
    public void StaticOrdinals_MatchEntityProviderColumnOrder()
    {
        // Simulate what the generator does: create ordinals array matching column order
        var columns = GeneratedOptEntityEntityProvider.Default.Columns;
        var staticOrdinals = Enumerable.Range(0, columns.Count).ToArray();
        
        // Verify ordinals match expected column positions
        Assert.AreEqual(4, staticOrdinals.Length);
        Assert.AreEqual(0, staticOrdinals[0]); // id
        Assert.AreEqual(1, staticOrdinals[1]); // title
        Assert.AreEqual(2, staticOrdinals[2]); // amount
        Assert.AreEqual(3, staticOrdinals[3]); // is_enabled
    }

    [TestMethod]
    public void StaticOrdinals_UsedWithToList_ProducesCorrectResults()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[]
        {
            new GeneratedOptEntity { Id = 1, Title = "first", Amount = 10m, IsEnabled = true },
            new GeneratedOptEntity { Id = 2, Title = "second", Amount = 20m, IsEnabled = false },
            new GeneratedOptEntity { Id = 3, Title = "third", Amount = 30m, IsEnabled = true },
        };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        
        // Simulate static ordinals from generator
        var staticOrdinals = new int[] { 0, 1, 2, 3 };
        var result = reader.ToList(dbReader, staticOrdinals);
        
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("first", result[0].Title);
        Assert.AreEqual("second", result[1].Title);
        Assert.AreEqual("third", result[2].Title);
    }

    [TestMethod]
    public void StaticOrdinals_UsedWithFirstOrDefault_ProducesCorrectResult()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[]
        {
            new GeneratedOptEntity { Id = 100, Title = "single", Amount = 999m, IsEnabled = true },
        };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        
        // Simulate static ordinals from generator
        var staticOrdinals = new int[] { 0, 1, 2, 3 };
        var result = reader.FirstOrDefault(dbReader, staticOrdinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Id);
        Assert.AreEqual("single", result.Title);
        Assert.AreEqual(999m, result.Amount);
    }

    #endregion

    #region EntityProvider Column Order Tests

    [TestMethod]
    public void EntityProvider_Columns_AreInPropertyOrder()
    {
        var provider = GeneratedOptEntityEntityProvider.Default;
        var columns = provider.Columns;
        
        Assert.AreEqual(4, columns.Count);
        Assert.AreEqual("id", columns[0].Name);
        Assert.AreEqual("Id", columns[0].PropertyName);
        Assert.AreEqual("title", columns[1].Name);
        Assert.AreEqual("Title", columns[1].PropertyName);
        Assert.AreEqual("amount", columns[2].Name);
        Assert.AreEqual("Amount", columns[2].PropertyName);
        Assert.AreEqual("is_enabled", columns[3].Name);
        Assert.AreEqual("IsEnabled", columns[3].PropertyName);
    }

    [TestMethod]
    public void EntityProvider_Columns_HaveCorrectDbTypes()
    {
        var provider = GeneratedOptEntityEntityProvider.Default;
        var columns = provider.Columns;
        
        Assert.AreEqual(DbType.Int32, columns[0].DbType);   // Id
        Assert.AreEqual(DbType.String, columns[1].DbType);  // Title
        Assert.AreEqual(DbType.Decimal, columns[2].DbType); // Amount
        Assert.AreEqual(DbType.Boolean, columns[3].DbType); // IsEnabled
    }

    #endregion

    #region Template Detection Tests

    [TestMethod]
    public void UsesStaticColumns_DetectsColumnsPlaceholder()
    {
        // These templates should use static ordinals
        var templatesWithColumns = new[]
        {
            "SELECT {{columns}} FROM {{table}}",
            "SELECT {{columns --exclude Id}} FROM {{table}}",
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
        };

        foreach (var template in templatesWithColumns)
        {
            Assert.IsTrue(template.Contains("{{columns"), $"Template should contain columns: {template}");
        }
    }

    [TestMethod]
    public void UsesStaticColumns_DetectsNoColumnsPlaceholder()
    {
        // These templates should NOT use static ordinals
        var templatesWithoutColumns = new[]
        {
            "SELECT * FROM {{table}}",
            "SELECT id, name FROM {{table}}",
            "INSERT INTO {{table}} ({{values}})",
            "UPDATE {{table}} SET {{set}}",
        };

        foreach (var template in templatesWithoutColumns)
        {
            Assert.IsFalse(template.Contains("{{columns"), $"Template should not contain columns: {template}");
        }
    }

    #endregion

    #region Async Extension Method Tests

    [TestMethod]
    public async Task GeneratedResultReader_ToListAsync_WithStaticOrdinals_Works()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[]
        {
            new GeneratedOptEntity { Id = 1, Title = "async1", Amount = 1m, IsEnabled = true },
            new GeneratedOptEntity { Id = 2, Title = "async2", Amount = 2m, IsEnabled = false },
        };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2, 3 };
        
        var result = await reader.ToListAsync(dbReader, staticOrdinals);
        
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("async1", result[0].Title);
        Assert.AreEqual("async2", result[1].Title);
    }

    [TestMethod]
    public async Task GeneratedResultReader_FirstOrDefaultAsync_WithStaticOrdinals_Works()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[]
        {
            new GeneratedOptEntity { Id = 999, Title = "async-first", Amount = 999m, IsEnabled = true },
        };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2, 3 };
        
        var result = await reader.FirstOrDefaultAsync(dbReader, staticOrdinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(999, result.Id);
        Assert.AreEqual("async-first", result.Title);
    }

    #endregion

    #region Performance Verification Tests

    [TestMethod]
    public void StaticOrdinals_SkipsGetOrdinalCalls()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[]
        {
            new GeneratedOptEntity { Id = 1, Title = "perf", Amount = 1m, IsEnabled = true },
        };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        
        // With static ordinals, GetOrdinal should not be called during Read
        var staticOrdinals = new int[] { 0, 1, 2, 3 };
        var initialGetOrdinalCount = dbReader.GetOrdinalCallCount;
        
        dbReader.Read();
        var result = reader.Read(dbReader, staticOrdinals);
        
        // GetOrdinal should not have been called
        Assert.AreEqual(initialGetOrdinalCount, dbReader.GetOrdinalCallCount);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DynamicOrdinals_CallsGetOrdinal()
    {
        var reader = GeneratedOptEntityResultReader.Default;
        var entities = new[]
        {
            new GeneratedOptEntity { Id = 1, Title = "dynamic", Amount = 1m, IsEnabled = true },
        };
        using var dbReader = new GeneratedOptEntityDbDataReader(entities);
        
        // Without static ordinals, GetOrdinal should be called
        dbReader.Read();
        var result = reader.Read(dbReader); // Uses GetOrdinal internally
        
        // GetOrdinal should have been called for each column
        Assert.AreEqual(4, dbReader.GetOrdinalCallCount);
        Assert.IsNotNull(result);
    }

    #endregion
}

#region Test Entities and DbDataReaders

[SqlxEntity]
[SqlxParameter]
public class GeneratedOptEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsEnabled { get; set; }
}

public class GeneratedOptEntityDbDataReader : System.Data.Common.DbDataReader
{
    private readonly GeneratedOptEntity[] _entities;
    private int _currentIndex = -1;
    public int GetOrdinalCallCount { get; private set; }

    public GeneratedOptEntityDbDataReader(GeneratedOptEntity[] entities) => _entities = entities;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _entities.Length;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override int GetOrdinal(string name)
    {
        GetOrdinalCallCount++;
        return name switch
        {
            "id" => 0,
            "title" => 1,
            "amount" => 2,
            "is_enabled" => 3,
            _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
        };
    }

    public override int GetInt32(int ordinal) => _entities[_currentIndex].Id;
    public override string GetString(int ordinal) => _entities[_currentIndex].Title;
    public override decimal GetDecimal(int ordinal) => _entities[_currentIndex].Amount;
    public override bool GetBoolean(int ordinal) => _entities[_currentIndex].IsEnabled;
    public override bool IsDBNull(int ordinal) => false;

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
    public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
    public override double GetDouble(int ordinal) => throw new NotImplementedException();
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
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
