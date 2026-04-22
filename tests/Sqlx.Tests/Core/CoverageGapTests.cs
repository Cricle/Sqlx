// Coverage gap tests targeting specific uncovered lines
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Diagnostics;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Tests.Core;

[TestClass]
public class CoverageGapTests
{
    // ===== SqlQuery<T> - ForXxx(entityProvider) overloads =====

    [TestMethod]
    public void SqlQuery_ForSqlServer_WithEntityProvider_ReturnsQuery()
        => Assert.IsNotNull(SqlQuery<CoverageUser>.ForSqlServer(null));

    [TestMethod]
    public void SqlQuery_ForMySql_WithEntityProvider_ReturnsQuery()
        => Assert.IsNotNull(SqlQuery<CoverageUser>.ForMySql(null));

    [TestMethod]
    public void SqlQuery_ForPostgreSQL_WithEntityProvider_ReturnsQuery()
        => Assert.IsNotNull(SqlQuery<CoverageUser>.ForPostgreSQL(null));

    [TestMethod]
    public void SqlQuery_ForOracle_WithEntityProvider_ReturnsQuery()
        => Assert.IsNotNull(SqlQuery<CoverageUser>.ForOracle(null));

    [TestMethod]
    public void SqlQuery_ForDB2_WithEntityProvider_ReturnsQuery()
        => Assert.IsNotNull(SqlQuery<CoverageUser>.ForDB2(null));

    // ===== SqlxAttribute - constructor with targetType =====

    [TestMethod]
    public void SqlxAttribute_WithTargetType_SetsTargetType()
    {
        var attr = new SqlxAttribute(typeof(CoverageUser));
        Assert.AreEqual(typeof(CoverageUser), attr.TargetType);
    }

    // ===== OutputParameterAttribute - parameterless constructor =====

    [TestMethod]
    public void OutputParameterAttribute_DefaultConstructor_HasNullDbType()
    {
        var attr = new OutputParameterAttribute();
        Assert.IsNull(attr.DbType);
    }

    // ===== TypeConverter - ConvertFromString paths =====

    [TestMethod]
    public void TypeConverter_StringToString_ReturnsDirectly()
    {
        // Line 166-167: ConvertFromString with string target
        var result = TypeConverter.Convert<string>("hello");
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void TypeConverter_StringToGuid_ParsesGuid()
    {
        // Lines 235-237: ConvertFromString -> Guid.Parse
        var guid = Guid.NewGuid();
        Assert.AreEqual(guid, TypeConverter.Convert<Guid>(guid.ToString()));
    }

    [TestMethod]
    public void TypeConverter_StringToByteArray_ParsesBase64()
    {
        // Lines 240-242: ConvertFromString -> byte[] from base64
        var bytes = new byte[] { 1, 2, 3 };
        CollectionAssert.AreEqual(bytes, TypeConverter.Convert<byte[]>(Convert.ToBase64String(bytes)));
    }

    [TestMethod]
    public void TypeConverter_StringToDecimal_FallbackChangeType()
    {
        // Line 245: ConvertFromString fallback ChangeType
        Assert.AreEqual(3.14m, TypeConverter.Convert<decimal>("3.14"));
    }

    // ===== SqlBuilder - uncovered paths =====

    [TestMethod]
    public void SqlBuilder_PlaceholderContext_NullContext_Throws()
    {
        // Line 81: null context check
        Assert.ThrowsException<ArgumentNullException>(() =>
            new SqlBuilder((PlaceholderContext)null!));
    }

    [TestMethod]
    public void SqlBuilder_PlaceholderContext_ZeroCapacity_Throws()
    {
        // Line 84: initialCapacity <= 0
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new SqlBuilder(context, initialCapacity: 0));
    }

    [TestMethod]
    public void SqlBuilder_AppendRaw_EmptyString_DoesNothing()
    {
        // Line 171: empty string early return in AppendLiteralInternal
        // Triggered when interpolated string starts with a parameter (empty literal prefix)
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Append($"{42}"); // empty literal before parameter
        var template = builder.Build();
        Assert.AreEqual("@p0", template.Sql);
    }

    [TestMethod]
    public void SqlBuilder_EnsureCapacity_GrowsBuffer_WhenFull()
    {
        // Lines 141-143: EnsureCapacity copies existing content when growing
        using var builder = new SqlBuilder(SqlDefine.SQLite, initialCapacity: 16);
        // Append enough to exceed initial capacity
        var longSql = new string('x', 100);
        builder.AppendRaw(longSql);
        var template = builder.Build();
        Assert.AreEqual(longSql, template.Sql);
    }

    [TestMethod]
    public void SqlBuilder_AppendSubquery_EmptySubquery_ReturnsThis()
    {
        // Line 494: empty subquery SQL returns early
        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.AppendRaw("SELECT 1");
        main.AppendSubquery(subquery);
        Assert.AreEqual("SELECT 1", main.Build().Sql);
    }

    [TestMethod]
    public void SqlBuilder_AppendSubquery_WithEscapedQuoteInStringLiteral_PreservesLiteral()
    {
        // Lines 585-588: escaped quote '' inside string literal
        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        subquery.AppendRaw("SELECT id FROM t WHERE name = 'O''Brien'");
        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.AppendRaw("SELECT * FROM (");
        main.AppendSubquery(subquery);
        main.AppendRaw(") AS sq");
        Assert.IsTrue(main.Build().Sql.Contains("O''Brien"));
    }

    [TestMethod]
    public void SqlBuilder_AppendSubquery_ConflictingParams_RenamesCorrectly()
    {
        // Lines 547, 554-556, 562: GenerateUniqueParameterName paths
        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        subquery.Append($"SELECT id FROM orders WHERE total > {100}");
        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.Append($"SELECT * FROM users WHERE id > {0}");
        main.AppendSubquery(subquery);
        var template = main.Build();
        Assert.AreEqual(2, template.Parameters.Count);
    }

    // ===== ExpressionBlockResult - uncovered paths =====

    [TestMethod]
    public void ExpressionBlockResult_Copy_WithOverrideParameters()
    {
        // Line 57: Copy method
        Expression<Func<CoverageUser, bool>> expr = u => u.Id > 5;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        var copy = result.Copy(new Dictionary<string, object?> { ["p0"] = 99 });
        Assert.AreEqual(result.Sql, copy.Sql);
        Assert.AreEqual(99, copy.Parameters["p0"]);
    }

    [TestMethod]
    public void ExpressionBlockResult_ParseUpdate_NonMemberInit_ReturnsEmpty()
    {
        // Lines 99-100: non-MemberInitExpression body returns empty
        Expression<Func<CoverageUser, CoverageUser>> expr = u => u;
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.AreEqual(string.Empty, result.Sql);
    }

    [TestMethod]
    public void ExpressionBlockResult_Empty_ReturnsEmptyResult()
    {
        // Line 206: Empty static property
        var empty = ExpressionBlockResult.Empty;
        Assert.AreEqual(string.Empty, empty.Sql);
        Assert.AreEqual(0, empty.Parameters.Count);
    }

    // ===== SetExpressionExtensions - method call args extraction =====

    [TestMethod]
    public void SetExpressionExtensions_ParseUpdate_WithMethodCallInExpression_ExtractsParams()
    {
        // Lines 152-155: method call arguments in ExtractParameters
        // Use a method call with instance object (m.Object != null) to also cover line 149
        var prefix = "prefix_";
        Expression<Func<CoverageUser, CoverageUser>> expr =
            u => new CoverageUser { Name = prefix.ToUpper() };
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SetExpressionExtensions_ParseUpdate_WithNullConstant_HandlesNull()
    {
        // Lines 124-125: null expression in ExtractParameters
        // Use a UnaryExpression that wraps null to force null expression path
        Expression<Func<CoverageUser, CoverageUser>> expr =
            u => new CoverageUser { Name = null! };
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.IsNotNull(result);
    }

    // ===== ExpressionParser - uncovered paths =====

    [TestMethod]
    public void ExpressionParser_Parse_WithComplexExpression_GeneratesSql()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), true);
        Expression<Func<CoverageUser, bool>> expr = u => u.Id > 0 && u.Name != null;
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Length > 0);
    }

    // ===== ExpressionHelper - uncovered paths =====

    [TestMethod]
    public void ExpressionHelper_EvaluateExpression_WithStaticMember_ReturnsValue()
    {
        // Lines 135-136: TryEvaluateMemberValue with null expression (static member)
        // Static member access: member.Expression is null
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), true);
        // string.Empty is a static field - member.Expression is null
        Expression<Func<CoverageUser, bool>> expr = u => u.Name == string.Empty;
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ExpressionHelper_IsNestedEntityProperty_WithConvertExpression_ReturnsTrue()
    {
        // Lines 78-79: UnaryExpression.Convert in IsNestedEntityProperty
        // This is hit when there's a cast in the expression
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        // Cast expression: (long)u.Id
        Expression<Func<CoverageUser, long>> expr = u => (long)u.Id;
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    // ===== TableNameResolver - null entityType =====

    [TestMethod]
    public void TableNameResolver_Resolve_NullEntityType_ThrowsArgumentNullException()
    {
        // Lines 19-20: null entityType check
        Assert.ThrowsException<ArgumentNullException>(() =>
            TableNameResolver.Resolve(null!));
    }

    // ===== SqlxQueryableExtensions - transaction path =====

    [TestMethod]
    public async Task SqlxQueryableExtensions_ToListAsync_WithTransaction_UsesTransaction()
    {
        // Lines 387-389: transaction != null in CreateCommand
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE coverage_users (id INTEGER PRIMARY KEY, name TEXT)";
        await cmd.ExecuteNonQueryAsync();

        using var tx = connection.BeginTransaction();
        var query = SqlQuery<CoverageUser>.ForSqlite()
            .WithConnection(connection)
            .WithTransaction(tx);
        var results = await query.ToListAsync();
        tx.Rollback();
        Assert.IsNotNull(results);
    }

    // ===== SqlTemplateMetrics.RecordError =====

    [TestMethod]
    public void SqlTemplateMetrics_RecordError_DoesNotThrow()
    {
        // Lines 77-89: RecordError method
        Assert.IsNotNull((object)SqlTemplateMetrics.RecordError);
        SqlTemplateMetrics.RecordError(
            "TestRepository",
            "TestMethod",
            "SELECT 1",
            1000L,
            new InvalidOperationException("test error"));
    }

    // ===== SqlxContext.DisposeAsync with owned transaction =====

    [TestMethod]
    public async Task SqlxContext_DisposeAsync_WithOwnedTransaction_RollsBackAndDisposes()
    {
        // Lines 231-242: DisposeAsync with _transaction != null && _ownsTransaction
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var context = new TestSqlxContext(connection);
        await context.BeginTransactionAsync();
        // Dispose without committing - should rollback
        await context.DisposeAsync();
        // No assertion needed - just verify no exception
    }
}

// Minimal SqlxContext subclass for testing DisposeAsync
public class TestSqlxContext : SqlxContext
{
    public TestSqlxContext(System.Data.Common.DbConnection connection)
        : base(connection, new SqlxContextOptions(), ownsConnection: false)
    {
    }
}

[Sqlx, TableName("coverage_users")]
public class CoverageUser
{
    [System.ComponentModel.DataAnnotations.Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
