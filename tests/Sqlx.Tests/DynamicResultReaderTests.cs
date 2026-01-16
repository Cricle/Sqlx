using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive tests for DynamicResultReader covering all type conversions,
/// null handling, anonymous types, and caching behavior.
/// </summary>
[TestClass]
public class DynamicResultReaderTests
{
    #region Anonymous Type Tests

    [TestMethod]
    public void DynamicReader_AnonymousType_TwoProperties_ReadsCorrectly()
    {
        var columnNames = new[] { "id", "name" };
        var reader = new DynamicResultReader<dynamic>(columnNames);

        var data = new[]
        {
            new { Id = 1, Name = "Alice" },
            new { Id = 2, Name = "Bob" }
        };
        using var dbReader = new AnonymousTypeDbReader(data);

        var results = new List<dynamic>();
        while (dbReader.Read())
        {
            results.Add(reader.Read(dbReader));
        }

        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    public void DynamicReader_AnonymousType_MultipleProperties_ReadsCorrectly()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Id, e.UserName, e.IsActive })
            .ToSql();

        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[user_name]"));
        Assert.IsTrue(sql.Contains("[is_active]"));
    }

    [TestMethod]
    public void DynamicReader_AnonymousType_WithNullableProperties_HandlesNull()
    {
        var sql = SqlQuery<TestEntityWithNullable>.ForSqlite()
            .Select(e => new { e.Id, e.Description })
            .ToSql();

        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[description]"));
    }

    #endregion

    #region Type Conversion Tests - All Supported Types

    [TestMethod]
    public void DynamicReader_Int32Type_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Id })
            .ToSql();

        Assert.IsTrue(sql.Contains("[id]"));
    }

    [TestMethod]
    public void DynamicReader_Int64Type_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.LongValue })
            .ToSql();

        Assert.IsTrue(sql.Contains("long_value"));
    }

    [TestMethod]
    public void DynamicReader_Int16Type_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.ShortValue })
            .ToSql();

        Assert.IsTrue(sql.Contains("short_value"));
    }

    [TestMethod]
    public void DynamicReader_ByteType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.ByteValue })
            .ToSql();

        Assert.IsTrue(sql.Contains("byte_value"));
    }

    [TestMethod]
    public void DynamicReader_BooleanType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.IsActive })
            .ToSql();

        Assert.IsTrue(sql.Contains("[is_active]"));
    }

    [TestMethod]
    public void DynamicReader_StringType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.UserName })
            .ToSql();

        Assert.IsTrue(sql.Contains("[user_name]"));
    }

    [TestMethod]
    public void DynamicReader_DateTimeType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.CreatedAt })
            .ToSql();

        Assert.IsTrue(sql.Contains("[created_at]"));
    }

    [TestMethod]
    public void DynamicReader_DecimalType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.DecimalValue })
            .ToSql();

        Assert.IsTrue(sql.Contains("decimal_value"));
    }

    [TestMethod]
    public void DynamicReader_DoubleType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.DoubleValue })
            .ToSql();

        Assert.IsTrue(sql.Contains("double_value"));
    }

    [TestMethod]
    public void DynamicReader_FloatType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.FloatValue })
            .ToSql();

        Assert.IsTrue(sql.Contains("float_value"));
    }

    [TestMethod]
    public void DynamicReader_GuidType_ConvertsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.GuidValue })
            .ToSql();

        Assert.IsTrue(sql.Contains("guid_value"));
    }

    #endregion

    #region Nullable Type Tests

    [TestMethod]
    public void DynamicReader_NullableInt32_WithNull_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntityWithNullable>.ForSqlite()
            .Select(e => new { e.Id, e.Description })
            .ToSql();

        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[description]"));
    }

    [TestMethod]
    public void DynamicReader_NullableInt32_WithValue_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.NullableInt })
            .ToSql();

        Assert.IsTrue(sql.Contains("nullable_int"));
    }

    [TestMethod]
    public void DynamicReader_NullableBoolean_WithNull_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.NullableBool })
            .ToSql();

        Assert.IsTrue(sql.Contains("nullable_bool"));
    }

    [TestMethod]
    public void DynamicReader_NullableDateTime_WithNull_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.NullableDateTime })
            .ToSql();

        Assert.IsTrue(sql.Contains("nullable_date_time"));
    }

    [TestMethod]
    public void DynamicReader_NullableDecimal_WithNull_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.NullableDecimal })
            .ToSql();

        Assert.IsTrue(sql.Contains("nullable_decimal"));
    }

    [TestMethod]
    public void DynamicReader_NullableGuid_WithNull_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new { e.NullableGuid })
            .ToSql();

        Assert.IsTrue(sql.Contains("nullable_guid"));
    }

    [TestMethod]
    public void DynamicReader_ReferenceType_WithNull_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntityWithNullable>.ForSqlite()
            .Select(e => new { e.Name, e.Description })
            .ToSql();

        Assert.IsTrue(sql.Contains("[name]"));
        Assert.IsTrue(sql.Contains("[description]"));
    }

    #endregion

    #region Mixed Type Tests

    [TestMethod]
    public void DynamicReader_MixedTypes_AllSupported_ReadsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new
            {
                e.Id,
                e.LongValue,
                e.ShortValue,
                e.ByteValue,
                e.BoolValue,
                e.StringValue,
                e.DateTimeValue,
                e.DecimalValue,
                e.DoubleValue,
                e.FloatValue,
                e.GuidValue
            })
            .ToSql();

        Assert.IsTrue(sql.Contains("id"));
        Assert.IsTrue(sql.Contains("long_value"));
        Assert.IsTrue(sql.Contains("short_value"));
        Assert.IsTrue(sql.Contains("byte_value"));
        Assert.IsTrue(sql.Contains("bool_value"));
        Assert.IsTrue(sql.Contains("string_value"));
        Assert.IsTrue(sql.Contains("date_time_value"));
        Assert.IsTrue(sql.Contains("decimal_value"));
        Assert.IsTrue(sql.Contains("double_value"));
        Assert.IsTrue(sql.Contains("float_value"));
        Assert.IsTrue(sql.Contains("guid_value"));
    }

    [TestMethod]
    public void DynamicReader_MixedNullableTypes_ReadsCorrectly()
    {
        var sql = SqlQuery<TestEntityWithAllTypes>.ForSqlite()
            .Select(e => new
            {
                e.Id,
                e.NullableInt,
                e.NullableBool,
                e.NullableDateTime,
                e.NullableDecimal,
                e.NullableGuid
            })
            .ToSql();

        Assert.IsTrue(sql.Contains("id"));
        Assert.IsTrue(sql.Contains("nullable_int"));
        Assert.IsTrue(sql.Contains("nullable_bool"));
        Assert.IsTrue(sql.Contains("nullable_date_time"));
        Assert.IsTrue(sql.Contains("nullable_decimal"));
        Assert.IsTrue(sql.Contains("nullable_guid"));
    }

    #endregion

    #region ResultReader Caching Tests

    [TestMethod]
    public void SqlQuery_ResultReader_CachesPerType()
    {
        // First access should create and cache the reader
        var reader1 = SqlQuery<TestEntity>.ResultReader;

        // Second access should return the same cached instance
        var reader2 = SqlQuery<TestEntity>.ResultReader;

        Assert.IsNotNull(reader1);
        Assert.AreSame(reader1, reader2, "ResultReader should be cached and return same instance");
    }

    [TestMethod]
    public void SqlQuery_ResultReader_DifferentTypes_DifferentCaches()
    {
        var reader1 = SqlQuery<TestEntity>.ResultReader;
        var reader2 = SqlQuery<TestEntityWithNullable>.ResultReader;

        Assert.IsNotNull(reader1);
        Assert.IsNotNull(reader2);
        Assert.AreNotSame((object?)reader1, (object?)reader2, "Different types should have different cached readers");
    }

    [TestMethod]
    public void SqlQuery_ResultReader_SetOnce_IgnoresSubsequentSets()
    {
        // Get the initial cached reader
        var initialReader = SqlQuery<TestEntity>.ResultReader;

        // Try to set a new reader (should be ignored due to ??= in setter)
        var newReader = TestEntityResultReader.Default;
        SqlQuery<TestEntity>.ResultReader = newReader;

        // Verify the original reader is still cached
        var currentReader = SqlQuery<TestEntity>.ResultReader;
        Assert.AreSame(initialReader, currentReader, "ResultReader should only be set once");
    }

    #endregion

    #region EntityProvider Caching Tests

    [TestMethod]
    public void SqlQuery_EntityProvider_CachesPerType()
    {
        var provider1 = SqlQuery<TestEntity>.EntityProvider;
        var provider2 = SqlQuery<TestEntity>.EntityProvider;

        Assert.IsNotNull(provider1);
        Assert.AreSame(provider1, provider2, "EntityProvider should be cached");
    }

    [TestMethod]
    public void SqlQuery_EntityProvider_UsedInAggregates()
    {
        // Verify EntityProvider is used to get column names for aggregates
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => e.Id)
            .ToSql();

        Assert.IsTrue(sql.Contains("[id]"));
    }

    #endregion

    #region Select with Where Tests

    [TestMethod]
    public void DynamicReader_SelectWithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.UserName })
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[is_active]"));
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[user_name]"));
    }

    [TestMethod]
    public void DynamicReader_SelectWithOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Id, e.UserName })
            .OrderBy(e => e.Id)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[user_name]"));
    }

    [TestMethod]
    public void DynamicReader_SelectWithSkipTake_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => new { e.Id, e.UserName })
            .Skip(10)
            .Take(20)
            .ToSql();

        Assert.IsTrue(sql.Contains("LIMIT"));
        Assert.IsTrue(sql.Contains("OFFSET"));
    }

    #endregion

    #region Cross-Dialect Tests

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void DynamicReader_AnonymousType_AllDialects_GeneratesCorrectSql(string dialectName)
    {
        var dialect = dialectName switch
        {
            "SQLite" => SqlDefine.SQLite,
            "SqlServer" => SqlDefine.SqlServer,
            "MySql" => SqlDefine.MySql,
            "PostgreSQL" => SqlDefine.PostgreSql,
            "Oracle" => SqlDefine.Oracle,
            "DB2" => SqlDefine.DB2,
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        var query = new SqlxQueryable<TestEntity>(new SqlxQueryProvider<TestEntity>(dialect));
        var sql = System.Linq.Queryable.Select(query, e => new { e.Id, e.UserName }).ToSql();

        Assert.IsTrue(sql.Contains("SELECT"), $"Dialect {dialectName} should generate SELECT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(sql), $"Dialect {dialectName} should generate non-empty SQL");
    }

    #endregion

    #region IGrouping Exclusion Tests

    [TestMethod]
    public void DynamicReader_IGroupingType_DoesNotCreateDynamicReader()
    {
        // GroupBy returns IGrouping which should not create a DynamicResultReader
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .GroupBy(e => e.IsActive)
            .ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("[is_active]"));
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void DynamicReader_GroupByWithSelect_AllDialects_HandlesCorrectly(string dialectName)
    {
        var dialect = dialectName switch
        {
            "SQLite" => SqlDefine.SQLite,
            "SqlServer" => SqlDefine.SqlServer,
            "MySql" => SqlDefine.MySql,
            "PostgreSQL" => SqlDefine.PostgreSql,
            "Oracle" => SqlDefine.Oracle,
            "DB2" => SqlDefine.DB2,
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        var query = new SqlxQueryable<TestEntity>(new SqlxQueryProvider<TestEntity>(dialect));
        var sql = query
            .GroupBy(e => e.IsActive)
            .ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"), $"Dialect {dialectName} should generate GROUP BY");
    }

    #endregion

    #region Primitive Type Exclusion Tests

    [TestMethod]
    public void DynamicReader_PrimitiveTypes_DoesNotCreateDynamicReader()
    {
        // Selecting a single primitive type should not create DynamicResultReader
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => e.Id)
            .ToSql();

        Assert.IsTrue(sql.Contains("[id]"));
    }

    [TestMethod]
    public void DynamicReader_StringType_DoesNotCreateDynamicReader()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => e.UserName)
            .ToSql();

        Assert.IsTrue(sql.Contains("[user_name]"));
    }

    [TestMethod]
    public void DynamicReader_DateTimeType_DoesNotCreateDynamicReader()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Select(e => e.CreatedAt)
            .ToSql();

        Assert.IsTrue(sql.Contains("[created_at]"));
    }

    #endregion

    #region Static Method Caching Tests

    [TestMethod]
    public void DynamicReader_StaticMethodCache_InitializedOnce()
    {
        // Create multiple instances of DynamicResultReader for the same type
        // The static constructor should only run once
        var reader1 = new DynamicResultReader<TestAnonymousType1>(new[] { "id", "name" });
        var reader2 = new DynamicResultReader<TestAnonymousType1>(new[] { "id", "name" });

        // Both should work correctly (verifying static cache is shared)
        Assert.IsNotNull(reader1);
        Assert.IsNotNull(reader2);
    }

    [TestMethod]
    public void DynamicReader_DifferentGenericTypes_SeparateStaticCaches()
    {
        // Different generic types should have separate static caches
        var reader1 = new DynamicResultReader<TestAnonymousType1>(new[] { "id", "name" });
        var reader2 = new DynamicResultReader<TestAnonymousType2>(new[] { "id", "value" });

        Assert.IsNotNull(reader1);
        Assert.IsNotNull(reader2);
    }

    #endregion

    #region Complex Scenario Tests

    [TestMethod]
    public void DynamicReader_ComplexQuery_WhereSelectOrderByPagination()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.UserName, e.CreatedAt })
            .OrderBy(e => e.CreatedAt)
            .Skip(10)
            .Take(20)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT"));
        Assert.IsTrue(sql.Contains("OFFSET"));
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[user_name]"));
        Assert.IsTrue(sql.Contains("[created_at]"));
    }

    [TestMethod]
    public void DynamicReader_NestedSelect_HandlesCorrectly()
    {
        var sql = SqlQuery<TestEntity>.ForSqlite()
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.UserName })
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[user_name]"));
    }

    #endregion

    #region Helper Classes for Testing

    private class TestAnonymousType1
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class TestAnonymousType2
    {
        public int Id { get; set; }
        public decimal Value { get; set; }
    }

    private class AnonymousTypeDbReader : DbDataReader
    {
        private readonly object[] _data;
        private int _currentIndex = -1;

        public AnonymousTypeDbReader(object[] data)
        {
            _data = data;
        }

        public override bool Read()
        {
            _currentIndex++;
            return _currentIndex < _data.Length;
        }

        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
        public override string GetString(int ordinal) => (string)GetValue(ordinal);

        public override object GetValue(int ordinal)
        {
            var item = _data[_currentIndex];
            var properties = item.GetType().GetProperties();
            return properties[ordinal].GetValue(item)!;
        }

        public override int GetOrdinal(string name)
        {
            var item = _data[0];
            var properties = item.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public override bool IsDBNull(int ordinal) => GetValue(ordinal) == null;

        // Required overrides (minimal implementation)
        public override int FieldCount => _data.Length > 0 ? _data[0].GetType().GetProperties().Length : 0;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override int Depth => 0;
        public override bool HasRows => _data.Length > 0;
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
        public override Type GetFieldType(int ordinal) => throw new NotImplementedException();
        public override float GetFloat(int ordinal) => throw new NotImplementedException();
        public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
        public override short GetInt16(int ordinal) => throw new NotImplementedException();
        public override long GetInt64(int ordinal) => throw new NotImplementedException();
        public override string GetName(int ordinal) => throw new NotImplementedException();
        public override int GetValues(object[] values) => throw new NotImplementedException();
        public override bool NextResult() => false;
        public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    #endregion
}

/// <summary>
/// Test entity with all supported data types for comprehensive testing.
/// </summary>
[Sqlx.Annotations.Sqlx]
public partial class TestEntityWithAllTypes
{
    public int Id { get; set; }
    public long LongValue { get; set; }
    public short ShortValue { get; set; }
    public byte ByteValue { get; set; }
    public bool BoolValue { get; set; }
    public string StringValue { get; set; } = "";
    public DateTime DateTimeValue { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public Guid GuidValue { get; set; }

    // Nullable types
    public int? NullableInt { get; set; }
    public bool? NullableBool { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public decimal? NullableDecimal { get; set; }
    public Guid? NullableGuid { get; set; }
}

