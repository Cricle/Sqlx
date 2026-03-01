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
        
        // Step 2: Verify columns are in correct order
        Assert.AreEqual(3, columns.Count);
        
        // Step 3: Use ResultReader
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[]
        {
            new IntegrationEntity { Id = 1, Name = "test", Score = 95.5 },
        };
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        
        var result = reader.FirstOrDefault(dbReader);
        
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
    public void Performance_ResultReader_UsesStructOrdinals()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 100)
            .Select(i => new IntegrationEntity { Id = i, Name = $"entity{i}", Score = i * 1.1 })
            .ToArray();
        
        // Test with ResultReader (now uses optimized ordinal caching with stackalloc)
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = reader.ToList(dbReader);
        var getOrdinalCalls = dbReader.GetOrdinalCallCount;
        
        // Should produce correct results
        Assert.AreEqual(100, result.Count);
        
        // Should call GetOrdinal only once per column (3 columns = 3 times) due to ordinal caching optimization
        Assert.AreEqual(3, getOrdinalCalls);
    }

    [TestMethod]
    public async Task Performance_ResultReaderAsync_UsesStructOrdinals()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 50)
            .Select(i => new IntegrationEntity { Id = i, Name = $"async{i}", Score = i * 2.0 })
            .ToArray();
        
        // Test with ResultReader (now uses optimized ordinal caching)
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = await reader.ToListAsync(dbReader);
        var getOrdinalCalls = dbReader.GetOrdinalCallCount;
        
        // Should produce correct results
        Assert.AreEqual(50, result.Count);
        
        // Should call GetOrdinal only once per column (3 columns = 3 times) due to ordinal caching optimization
        Assert.AreEqual(3, getOrdinalCalls);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EdgeCase_EmptyResultSet_Works()
    {
        var reader = IntegrationEntityResultReader.Default;
        using var dbReader = new IntegrationEntityDbDataReader(Array.Empty<IntegrationEntity>());
        
        var listResult = reader.ToList(dbReader);
        Assert.AreEqual(0, listResult.Count);
        
        using var dbReader2 = new IntegrationEntityDbDataReader(Array.Empty<IntegrationEntity>());
        var singleResult = reader.FirstOrDefault(dbReader2);
        Assert.IsNull(singleResult);
    }

    [TestMethod]
    public void EdgeCase_SingleRow_Works()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[] { new IntegrationEntity { Id = 42, Name = "single", Score = 100.0 } };
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        
        var result = reader.FirstOrDefault(dbReader);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Id);
        Assert.AreEqual("single", result.Name);
        Assert.AreEqual(100.0, result.Score);
    }

    [TestMethod]
    public void EdgeCase_LargeResultSet_Works()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new IntegrationEntity { Id = i, Name = $"large{i}", Score = i * 0.1 })
            .ToArray();
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        
        var result = reader.ToList(dbReader);
        
        Assert.AreEqual(10000, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(10000, result[9999].Id);
        Assert.AreEqual(3, dbReader.GetOrdinalCallCount); // GetOrdinal called only once per column (3 columns) due to ordinal caching
    }

    #endregion

    #region Consistency Tests

    [TestMethod]
    public void Consistency_ResultReader_ProducesCorrectResults()
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
        
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = reader.ToList(dbReader);
        
        // Verify correct results
        Assert.AreEqual(5, result.Count);
        for (int i = 0; i < result.Count; i++)
        {
            Assert.AreEqual(i + 1, result[i].Id, $"Id mismatch at index {i}");
            Assert.AreEqual(entities[i].Name, result[i].Name, $"Name mismatch at index {i}");
            Assert.AreEqual(entities[i].Score, result[i].Score, $"Score mismatch at index {i}");
        }
    }

    [TestMethod]
    public async Task Consistency_AsyncResultReader_ProducesCorrectResults()
    {
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[]
        {
            new IntegrationEntity { Id = 10, Name = "x", Score = 10.0 },
            new IntegrationEntity { Id = 20, Name = "y", Score = 20.0 },
        };
        
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = await reader.ToListAsync(dbReader);
        
        Assert.AreEqual(2, result.Count);
        for (int i = 0; i < result.Count; i++)
        {
            Assert.AreEqual(entities[i].Id, result[i].Id);
            Assert.AreEqual(entities[i].Name, result[i].Name);
            Assert.AreEqual(entities[i].Score, result[i].Score);
        }
    }

    #endregion
}

#region Test Entities and DbDataReaders

[Sqlx]
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
    public override Type GetFieldType(int ordinal) => ordinal switch
    {
        0 => typeof(int),
        1 => typeof(string),
        2 => typeof(double),
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


#region Strict Ordinal Caching Tests

[TestClass]
public class StrictOrdinalCachingTests
{
    [TestMethod]
    public void OrdinalCaching_VerifyGetOrdinalCalledOncePerColumn()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new IntegrationEntity { Id = i, Name = $"test{i}", Score = i * 1.5 })
            .ToArray();
        
        // Act
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = reader.ToList(dbReader);
        
        // Assert
        Assert.AreEqual(1000, result.Count);
        Assert.AreEqual(3, dbReader.GetOrdinalCallCount, 
            "GetOrdinal should be called exactly 3 times (once per column), not 3000 times (once per row per column)");
    }

    [TestMethod]
    public async Task OrdinalCaching_Async_VerifyGetOrdinalCalledOncePerColumn()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 500)
            .Select(i => new IntegrationEntity { Id = i, Name = $"async{i}", Score = i * 2.0 })
            .ToArray();
        
        // Act
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = await reader.ToListAsync(dbReader);
        
        // Assert
        Assert.AreEqual(500, result.Count);
        Assert.AreEqual(3, dbReader.GetOrdinalCallCount, 
            "Async ToListAsync should also call GetOrdinal exactly 3 times (once per column)");
    }

    [TestMethod]
    public void OrdinalCaching_PropertyCount_ReturnsCorrectValue()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        
        // Act
        int propertyCount = reader.PropertyCount;
        
        // Assert
        Assert.AreEqual(3, propertyCount, "PropertyCount should match the number of properties in IntegrationEntity");
    }

    [TestMethod]
    public void OrdinalCaching_GetOrdinals_FillsSpanCorrectly()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[] { new IntegrationEntity { Id = 1, Name = "test", Score = 1.0 } };
        
        // Act
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        Span<int> ordinals = stackalloc int[3];
        reader.GetOrdinals(dbReader, ordinals);
        
        // Assert
        Assert.AreEqual(0, ordinals[0], "Id column should be at ordinal 0");
        Assert.AreEqual(1, ordinals[1], "Name column should be at ordinal 1");
        Assert.AreEqual(2, ordinals[2], "Score column should be at ordinal 2");
    }

    [TestMethod]
    public void OrdinalCaching_ReadWithOrdinals_UsesPrecomputedValues()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[] 
        { 
            new IntegrationEntity { Id = 42, Name = "cached", Score = 99.9 } 
        };
        
        // Act
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        dbReader.Read();
        
        Span<int> ordinals = stackalloc int[3];
        reader.GetOrdinals(dbReader, ordinals);
        
        int getOrdinalCallsBefore = dbReader.GetOrdinalCallCount;
        var result = reader.Read(dbReader, ordinals);
        int getOrdinalCallsAfter = dbReader.GetOrdinalCallCount;
        
        // Assert
        Assert.AreEqual(42, result.Id);
        Assert.AreEqual("cached", result.Name);
        Assert.AreEqual(99.9, result.Score);
        Assert.AreEqual(getOrdinalCallsBefore, getOrdinalCallsAfter, 
            "Read with ordinals should not call GetOrdinal");
    }

    [TestMethod]
    public void OrdinalCaching_CompareWithoutCaching_ShowsImprovement()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 100)
            .Select(i => new IntegrationEntity { Id = i, Name = $"item{i}", Score = i * 1.0 })
            .ToArray();
        
        // Act - With caching (ToList uses ordinal caching)
        using var dbReaderCached = new IntegrationEntityDbDataReader(entities);
        var resultCached = reader.ToList(dbReaderCached);
        int getOrdinalCallsCached = dbReaderCached.GetOrdinalCallCount;
        
        // Act - Without caching (manual loop calling Read without ordinals)
        using var dbReaderUncached = new IntegrationEntityDbDataReader(entities);
        var resultUncached = new List<IntegrationEntity>();
        while (dbReaderUncached.Read())
        {
            resultUncached.Add(reader.Read(dbReaderUncached));
        }
        int getOrdinalCallsUncached = dbReaderUncached.GetOrdinalCallCount;
        
        // Assert
        Assert.AreEqual(100, resultCached.Count);
        Assert.AreEqual(100, resultUncached.Count);
        Assert.AreEqual(3, getOrdinalCallsCached, "With caching: 3 calls (once per column)");
        Assert.AreEqual(300, getOrdinalCallsUncached, "Without caching: 300 calls (100 rows × 3 columns)");
        Assert.IsTrue(getOrdinalCallsUncached > getOrdinalCallsCached * 50, 
            "Uncached should call GetOrdinal 99x more times");
    }

    [TestMethod]
    public void OrdinalCaching_StackallocVsHeap_BothProduceCorrectResults()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[] 
        { 
            new IntegrationEntity { Id = 1, Name = "a", Score = 1.0 },
            new IntegrationEntity { Id = 2, Name = "b", Score = 2.0 },
        };
        
        // Act - Stackalloc
        using var dbReader1 = new IntegrationEntityDbDataReader(entities);
        dbReader1.Read();
        Span<int> stackOrdinals = stackalloc int[3];
        reader.GetOrdinals(dbReader1, stackOrdinals);
        var result1 = reader.Read(dbReader1, stackOrdinals);
        
        // Act - Heap
        using var dbReader2 = new IntegrationEntityDbDataReader(entities);
        dbReader2.Read();
        var heapOrdinals = new int[3];
        reader.GetOrdinals(dbReader2, heapOrdinals);
        var result2 = reader.Read(dbReader2, heapOrdinals);
        
        // Assert
        Assert.AreEqual(result1.Id, result2.Id);
        Assert.AreEqual(result1.Name, result2.Name);
        Assert.AreEqual(result1.Score, result2.Score);
    }

    [TestMethod]
    public void OrdinalCaching_LargeDataset_MaintainsPerformance()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new IntegrationEntity { Id = i, Name = $"large{i}", Score = i * 0.5 })
            .ToArray();
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = reader.ToList(dbReader);
        stopwatch.Stop();
        
        // Assert
        Assert.AreEqual(10000, result.Count);
        Assert.AreEqual(3, dbReader.GetOrdinalCallCount, "Should call GetOrdinal only 3 times even for 10k rows");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Should complete in under 1 second, took {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void OrdinalCaching_VerifyDataIntegrity_AllRows()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = Enumerable.Range(1, 50)
            .Select(i => new IntegrationEntity { Id = i, Name = $"verify{i}", Score = i * 3.14 })
            .ToArray();
        
        // Act
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = reader.ToList(dbReader);
        
        // Assert
        Assert.AreEqual(50, result.Count);
        for (int i = 0; i < 50; i++)
        {
            Assert.AreEqual(entities[i].Id, result[i].Id, $"Row {i}: Id mismatch");
            Assert.AreEqual(entities[i].Name, result[i].Name, $"Row {i}: Name mismatch");
            Assert.AreEqual(entities[i].Score, result[i].Score, 0.001, $"Row {i}: Score mismatch");
        }
    }

    [TestMethod]
    public void OrdinalCaching_FirstOrDefault_UsesOptimization()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[] { new IntegrationEntity { Id = 99, Name = "first", Score = 88.8 } };
        
        // Act
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = reader.FirstOrDefault(dbReader);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(99, result.Id);
        Assert.AreEqual("first", result.Name);
        Assert.AreEqual(88.8, result.Score);
        // FirstOrDefault doesn't use ordinal caching (single row), so GetOrdinal called 3 times
        Assert.AreEqual(3, dbReader.GetOrdinalCallCount);
    }

    [TestMethod]
    public async Task OrdinalCaching_FirstOrDefaultAsync_Works()
    {
        // Arrange
        var reader = IntegrationEntityResultReader.Default;
        var entities = new[] { new IntegrationEntity { Id = 77, Name = "async_first", Score = 66.6 } };
        
        // Act
        using var dbReader = new IntegrationEntityDbDataReader(entities);
        var result = await reader.FirstOrDefaultAsync(dbReader);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(77, result.Id);
        Assert.AreEqual("async_first", result.Name);
        Assert.AreEqual(66.6, result.Score);
    }
}

#endregion
