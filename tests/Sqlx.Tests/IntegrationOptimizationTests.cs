using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Integration tests that verify the complete optimization flow:
/// - SqlTemplate preparation with PlaceholderContext
/// - Static ordinals generation based on {{columns}} usage
/// - ResultReader with static ordinals
/// </summary>
[TestClass]
public class IntegrationOptimizationTests
{
    #region Complete Flow Tests

    [TestMethod]
    public void CompleteFlow_SelectWithColumns_UsesStaticOrdinals()
    {
        // Setup: Create PlaceholderContext like the generator does
        var columns = IntegrationEntityEntityProvider.Default.Columns;
        var context = new PlaceholderContext(
            dialect: SqlDefine.SQLite,
            tableName: "integration_entities",
            columns: columns);

        // Step 1: Prepare SQL template with {{columns}}
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE id = @id", context);
        
        // Verify SQL is generated correctly
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[score]"));
        Assert.IsTrue(template.Sql.Contains("[integration_entities]"));
        
        // Step 2: Generate static ordinals (simulating what generator does)
        // When SQL uses {{columns}}, column order matches EntityProvider.Columns order
        var staticOrdinals = Enumerable.Range(0, columns.Count).ToArray();
        Assert.AreEqual(3, staticOrdinals.Length);
        
        // Step 3: Use ResultReader with static ordinals
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[]
        {
            new IntegrationEntity { Id = 1, Name = "test", Score = 95.5 },
        };
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        
        var result = reader.FirstOrDefault(dbReader, staticOrdinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("test", result.Name);
        Assert.AreEqual(95.5, result.Score);
    }

    [TestMethod]
    public void CompleteFlow_SelectWithColumnsExclude_UsesStaticOrdinals()
    {
        var columns = IntegrationEntityEntityProvider.Default.Columns;
        var context = new PlaceholderContext(
            dialect: SqlDefine.SQLite,
            tableName: "integration_entities",
            columns: columns);

        // Template with --exclude
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id}} FROM {{table}}", context);
        
        // Verify excluded column is not in SQL
        Assert.IsFalse(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[score]"));
    }

    [TestMethod]
    public void CompleteFlow_InsertWithValues_NoStaticOrdinals()
    {
        var columns = IntegrationEntityEntityProvider.Default.Columns;
        var context = new PlaceholderContext(
            dialect: SqlDefine.SQLite,
            tableName: "integration_entities",
            columns: columns);

        // INSERT template doesn't use {{columns}} for SELECT, so no static ordinals needed
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", 
            context);
        
        // Verify INSERT SQL is correct
        Assert.IsTrue(template.Sql.Contains("INSERT INTO"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("@name"));
    }

    #endregion

    #region Multiple Dialect Tests

    [TestMethod]
    [DataRow("SQLite", "[", "]", "@")]
    [DataRow("MySql", "`", "`", "@")]
    [DataRow("PostgreSql", "\"", "\"", "@")]
    [DataRow("SqlServer", "[", "]", "@")]
    public void CompleteFlow_DifferentDialects_GenerateCorrectSql(
        string dialectName, string quoteStart, string quoteEnd, string paramPrefix)
    {
        var dialect = dialectName switch
        {
            "SQLite" => SqlDefine.SQLite,
            "MySql" => SqlDefine.MySql,
            "PostgreSql" => SqlDefine.PostgreSql,
            "SqlServer" => SqlDefine.SqlServer,
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        var columns = IntegrationEntityEntityProvider.Default.Columns;
        var context = new PlaceholderContext(
            dialect: dialect,
            tableName: "test_table",
            columns: columns);

        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        // Verify dialect-specific quoting
        Assert.IsTrue(template.Sql.Contains($"{quoteStart}id{quoteEnd}"));
        Assert.IsTrue(template.Sql.Contains($"{quoteStart}name{quoteEnd}"));
        Assert.IsTrue(template.Sql.Contains($"{quoteStart}test_table{quoteEnd}"));
    }

    #endregion

    #region Performance Comparison Tests

    [TestMethod]
    public void Performance_StaticOrdinals_SkipsGetOrdinalCalls()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 100)
            .Select(i => new IntegrationEntity { Id = i, Name = $"entity{i}", Score = i * 1.1 })
            .ToArray();
        
        // Test with static ordinals
        using var dbReader1 = new IntegrationEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2 };
        var result1 = reader.ToList(dbReader1, staticOrdinals);
        var getOrdinalCalls1 = dbReader1.GetOrdinalCallCount;
        
        // Test without static ordinals
        using var dbReader2 = new IntegrationEntityDbDataReader(entities);
        var result2 = reader.ToList(dbReader2);
        var getOrdinalCalls2 = dbReader2.GetOrdinalCallCount;
        
        // Both should produce same results
        Assert.AreEqual(result1.Count, result2.Count);
        
        // Static ordinals should have 0 GetOrdinal calls
        Assert.AreEqual(0, getOrdinalCalls1);
        
        // Dynamic ordinals should have 3 GetOrdinal calls (once per column)
        Assert.AreEqual(3, getOrdinalCalls2);
    }

    [TestMethod]
    public async Task Performance_StaticOrdinalsAsync_SkipsGetOrdinalCalls()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 50)
            .Select(i => new IntegrationEntity { Id = i, Name = $"async{i}", Score = i * 2.0 })
            .ToArray();
        
        // Test with static ordinals
        using var dbReader1 = new IntegrationEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2 };
        var result1 = await reader.ToListAsync(dbReader1, staticOrdinals);
        var getOrdinalCalls1 = dbReader1.GetOrdinalCallCount;
        
        // Test without static ordinals
        using var dbReader2 = new IntegrationEntityDbDataReader(entities);
        var result2 = await reader.ToListAsync(dbReader2);
        var getOrdinalCalls2 = dbReader2.GetOrdinalCallCount;
        
        // Both should produce same results
        Assert.AreEqual(result1.Count, result2.Count);
        Assert.AreEqual(0, getOrdinalCalls1);
        Assert.AreEqual(3, getOrdinalCalls2);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EdgeCase_EmptyResultSet_WithStaticOrdinals()
    {
        var reader = IntegrationEntityResultReader.Default;
        using var dbReader = new IntegrationEntityDbDataReader(Array.Empty<IntegrationEntity>());
        var staticOrdinals = new int[] { 0, 1, 2 };
        
        var listResult = reader.ToList(dbReader, staticOrdinals);
        Assert.AreEqual(0, listResult.Count);
        
        using var dbReader2 = new IntegrationEntityDbDataReader(Array.Empty<IntegrationEntity>());
        var singleResult = reader.FirstOrDefault(dbReader2, staticOrdinals);
        Assert.IsNull(singleResult);
    }

    [TestMethod]
    public void EdgeCase_SingleRow_WithStaticOrdinals()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[] { new IntegrationEntity { Id = 42, Name = "single", Score = 100.0 } };
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2 };
        
        var result = reader.FirstOrDefault(dbReader, staticOrdinals);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Id);
        Assert.AreEqual("single", result.Name);
        Assert.AreEqual(100.0, result.Score);
    }

    [TestMethod]
    public void EdgeCase_LargeResultSet_WithStaticOrdinals()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new IntegrationEntity { Id = i, Name = $"large{i}", Score = i * 0.1 })
            .ToArray();
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2 };
        
        var result = reader.ToList(dbReader, staticOrdinals);
        
        Assert.AreEqual(10000, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(10000, result[9999].Id);
        Assert.AreEqual(0, dbReader.GetOrdinalCallCount); // No GetOrdinal calls
    }

    #endregion

    #region Consistency Tests

    [TestMethod]
    public void Consistency_StaticAndDynamicOrdinals_ProduceSameResults()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[]
        {
            new IntegrationEntity { Id = 1, Name = "a", Score = 1.0 },
            new IntegrationEntity { Id = 2, Name = "b", Score = 2.0 },
            new IntegrationEntity { Id = 3, Name = "c", Score = 3.0 },
            new IntegrationEntity { Id = 4, Name = "d", Score = 4.0 },
            new IntegrationEntity { Id = 5, Name = "e", Score = 5.0 },
        };
        
        // With static ordinals
        using var dbReader1 = new IntegrationEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2 };
        var result1 = reader.ToList(dbReader1, staticOrdinals);
        
        // Without static ordinals
        using var dbReader2 = new IntegrationEntityDbDataReader(entities);
        var result2 = reader.ToList(dbReader2);
        
        // Verify identical results
        Assert.AreEqual(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.AreEqual(result1[i].Id, result2[i].Id, $"Id mismatch at index {i}");
            Assert.AreEqual(result1[i].Name, result2[i].Name, $"Name mismatch at index {i}");
            Assert.AreEqual(result1[i].Score, result2[i].Score, $"Score mismatch at index {i}");
        }
    }

    [TestMethod]
    public async Task Consistency_AsyncStaticAndDynamicOrdinals_ProduceSameResults()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[]
        {
            new IntegrationEntity { Id = 10, Name = "x", Score = 10.0 },
            new IntegrationEntity { Id = 20, Name = "y", Score = 20.0 },
        };
        
        // With static ordinals
        using var dbReader1 = new IntegrationEntityDbDataReader(entities);
        var staticOrdinals = new int[] { 0, 1, 2 };
        var result1 = await reader.ToListAsync(dbReader1, staticOrdinals);
        
        // Without static ordinals
        using var dbReader2 = new IntegrationEntityDbDataReader(entities);
        var result2 = await reader.ToListAsync(dbReader2);
        
        Assert.AreEqual(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.AreEqual(result1[i].Id, result2[i].Id);
            Assert.AreEqual(result1[i].Name, result2[i].Name);
            Assert.AreEqual(result1[i].Score, result2[i].Score);
        }
    }

    #endregion
}

#region Test Entities and DbDataReaders

[SqlxEntity]
[SqlxParameter]
public class IntegrationEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class IntegrationEntityDbDataReader : System.Data.Common.DbDataReader
{
    private readonly IntegrationEntity[] _entities;
    private int _currentIndex = -1;
    public int GetOrdinalCallCount { get; private set; }

    public IntegrationEntityDbDataReader(IntegrationEntity[] entities) => _entities = entities;

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
            "name" => 1,
            "score" => 2,
            _ => throw new IndexOutOfRangeException($"Column '{name}' not found")
        };
    }

    public override int GetInt32(int ordinal) => _entities[_currentIndex].Id;
    public override string GetString(int ordinal) => _entities[_currentIndex].Name;
    public override double GetDouble(int ordinal) => _entities[_currentIndex].Score;
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
    public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
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
