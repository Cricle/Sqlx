// Branch coverage 5 - TypeConverter, ResultReaderExtensions, SqlxQueryable, AggregateParser
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sqlx.Tests.BranchCoverage;

// ── TypeConverter uncovered branches ────────────────────────────────────────

[TestClass]
public class TypeConverterBranch5Tests
{
    // L69: Guid from non-string, non-byte[] (return as-is)
    [TestMethod]
    public void Convert_Guid_FromGuid_ReturnsDirect()
    {
        var g = Guid.NewGuid();
        Assert.AreEqual(g, TypeConverter.Convert<Guid>(g));
    }

    // L80: byte[] from non-string (return as-is)
    [TestMethod]
    public void Convert_ByteArray_FromByteArray_ReturnsDirect()
    {
        var b = new byte[] { 1, 2, 3 };
        CollectionAssert.AreEqual(b, TypeConverter.Convert<byte[]>(b));
    }

    // L95: DateTimeOffset from non-DateTime (return as-is)
    [TestMethod]
    public void Convert_DateTimeOffset_FromDateTimeOffset_ReturnsDirect()
    {
        var dto = DateTimeOffset.UtcNow;
        Assert.AreEqual(dto, TypeConverter.Convert<DateTimeOffset>(dto));
    }

    // L111: DateOnly from non-DateTime (return as-is)
    [TestMethod]
    public void Convert_DateOnly_FromDateOnly_ReturnsDirect()
    {
        var d = new DateOnly(2024, 6, 15);
        Assert.AreEqual(d, TypeConverter.Convert<DateOnly>(d));
    }

    // L122: DateOnly from DateTimeOffset
    [TestMethod]
    public void Convert_DateOnly_FromDateTimeOffset_Converts()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var result = TypeConverter.Convert<DateOnly>(dto);
        Assert.AreEqual(new DateOnly(2024, 6, 15), result);
    }

    // L143: TimeSpan from non-IConvertible (return as-is)
    [TestMethod]
    public void Convert_TimeSpan_FromTimeSpan_ReturnsDirect()
    {
        var ts = TimeSpan.FromHours(2);
        Assert.AreEqual(ts, TypeConverter.Convert<TimeSpan>(ts));
    }

    // L153: TypeCode != Object path (int from short)
    [TestMethod]
    public void Convert_Int_FromShort_UsesTypeCode()
    {
        Assert.AreEqual(42, TypeConverter.Convert<int>((short)42));
    }

    // L165: ConvertFromString - bool
    [TestMethod]
    public void Convert_Bool_FromString_ParsesTrue()
    {
        Assert.IsTrue(TypeConverter.Convert<bool>("true"));
    }

    // L175: ConvertFromString - float
    [TestMethod]
    public void Convert_Float_FromString_Parses()
    {
        Assert.AreEqual(3.14f, TypeConverter.Convert<float>("3.14"), 0.001f);
    }

    // L180: ConvertFromString - DateTime
    [TestMethod]
    public void Convert_DateTime_FromString_Parses()
    {
        var result = TypeConverter.Convert<DateTime>("2024-01-15");
        Assert.AreEqual(2024, result.Year);
    }

    // L185: ConvertFromString - DateTimeOffset
    [TestMethod]
    public void Convert_DateTimeOffset_FromString_Parses()
    {
        var result = TypeConverter.Convert<DateTimeOffset>("2024-01-15T00:00:00Z");
        Assert.AreEqual(2024, result.Year);
    }

    // L200: ConvertFromString - TimeSpan
    [TestMethod]
    public void Convert_TimeSpan_FromString_Parses()
    {
        var result = TypeConverter.Convert<TimeSpan>("01:30:00");
        Assert.AreEqual(TimeSpan.FromMinutes(90), result);
    }

    // L230: ConvertFromString - TimeOnly
    [TestMethod]
    public void Convert_TimeOnly_FromString_Parses()
    {
        var result = TypeConverter.Convert<TimeOnly>("14:30:00");
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    // L321/L323: BuildGetValueExpression - nullable types
    [TestMethod]
    public void Convert_NullableInt_FromInt_Converts()
    {
        Assert.AreEqual((int?)42, TypeConverter.Convert<int?>(42));
    }

    [TestMethod]
    public void Convert_NullableInt_FromNull_ReturnsNull()
    {
        Assert.IsNull(TypeConverter.Convert<int?>(null));
    }

    // L327/L333/L339/L345/L351: BuildGetValueExpression - various DB types
    [TestMethod]
    public void Convert_Long_FromInt_Converts()
    {
        Assert.AreEqual(42L, TypeConverter.Convert<long>(42));
    }

    [TestMethod]
    public void Convert_Double_FromFloat_Converts()
    {
        Assert.AreEqual(3.14, TypeConverter.Convert<double>(3.14f), 0.001);
    }

    [TestMethod]
    public void Convert_Decimal_FromDouble_Converts()
    {
        Assert.AreEqual(3.14m, TypeConverter.Convert<decimal>(3.14));
    }

    // L357/L369/L382/L394: BuildGetValueExpression - DateOnly/TimeOnly from DB
    [TestMethod]
    public void Convert_DateOnly_FromDateTime_Converts()
    {
        var dt = new DateTime(2024, 6, 15);
        Assert.AreEqual(new DateOnly(2024, 6, 15), TypeConverter.Convert<DateOnly>(dt));
    }

    [TestMethod]
    public void Convert_TimeOnly_FromDateTime_Converts()
    {
        var dt = new DateTime(2024, 1, 1, 14, 30, 0);
        Assert.AreEqual(new TimeOnly(14, 30, 0), TypeConverter.Convert<TimeOnly>(dt));
    }
}

// ── ResultReaderExtensions - IArrayOrdinalReader path ───────────────────────

[Sqlx, TableName("bc5_items")]
public class Bc5Item
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>Non-array reader with PropertyCount > 0 to hit the stackalloc path.</summary>
internal class Bc5SpanReader : IResultReader<Bc5Item>
{
    public int PropertyCount => 2;
    public Bc5Item Read(IDataReader r) => new() { Id = r.GetInt32(0), Name = r.GetString(1) };
    public Bc5Item Read(IDataReader r, ReadOnlySpan<int> ordinals) => new() { Id = r.GetInt32(ordinals[0]), Name = r.GetString(ordinals[1]) };
    public void GetOrdinals(IDataReader r, Span<int> ordinals) { ordinals[0] = r.GetOrdinal("id"); ordinals[1] = r.GetOrdinal("name"); }
}

[TestClass]
public class ResultReaderExtensionsBranch5Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc5_items (id INTEGER PRIMARY KEY, name TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO bc5_items VALUES (1,'a'),(2,'b')";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // L98/L119: non-IArrayOrdinalReader with propCount>0 → stackalloc path
    [TestMethod]
    public void ToList_SpanReader_UsesStackalloc()
    {
        using var conn = CreateConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM bc5_items";
        using var dr = cmd.ExecuteReader();
        var reader = new Bc5SpanReader();
        var list = reader.ToList(dr);
        Assert.AreEqual(2, list.Count);
    }

    // L136/L150: IArrayOrdinalReader with capacity hint (DynamicResultReader)
    [TestMethod]
    public async Task ToListAsync_WithTake_UsesCapacityHint()
    {
        using var conn = CreateConn();
        var results = await SqlQuery<Bc5Item>.ForSqlite()
            .Take(1)
            .WithConnection(conn)
            .ToListAsync();
        Assert.AreEqual(1, results.Count);
    }

    // L168/L170: ToListAsync with IArrayOrdinalReader (no capacity hint)
    [TestMethod]
    public async Task ToListAsync_DynamicReader_UsesArrayOrdinalPath()
    {
        using var conn = CreateConn();
        var results = await SqlQuery<Bc5Item>.ForSqlite()
            .WithConnection(conn)
            .ToListAsync();
        Assert.AreEqual(2, results.Count);
    }

    // L177: ToListAsync with non-IArrayOrdinalReader (span path)
    [TestMethod]
    public async Task ToListAsync_SpanReader_UsesSpanPath()
    {
        using var conn = CreateConn();
        var q = SqlQuery<Bc5Item>.ForSqlite()
            .WithConnection(conn)
            .WithReader(new Bc5SpanReader());
        var results = await q.ToListAsync();
        Assert.AreEqual(2, results.Count);
    }

    // L189/L206/L220: ToListAsync with capacity hint + non-IArrayOrdinalReader
    [TestMethod]
    public async Task ToListAsync_SpanReader_WithTake_UsesCapacityHint()
    {
        using var conn = CreateConn();
        var q = SqlQuery<Bc5Item>.ForSqlite()
            .Take(1)
            .WithConnection(conn)
            .WithReader(new Bc5SpanReader());
        var results = await q.ToListAsync();
        Assert.AreEqual(1, results.Count);
    }
}

// ── SqlxQueryable - GetEnumerator/GetAsyncEnumerator null checks ─────────────

[TestClass]
public class SqlxQueryableBranch5Tests
{
    // L148: GetEnumerator with no connection throws
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GetEnumerator_NoConnection_Throws()
    {
        var q = SqlQuery<Bc5Item>.ForSqlite();
        _ = q.GetEnumerator();
    }

    // L166: GetEnumerator with connection but no reader - DynamicResultReader is auto-set
    // so test the sync enumeration path instead
    [TestMethod]
    public void GetEnumerator_WithConnectionAndReader_Enumerates()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc5_items (id INTEGER PRIMARY KEY, name TEXT)";
        cmd.ExecuteNonQuery();
        var q = SqlQuery<Bc5Item>.ForSqlite()
            .WithConnection(conn)
            .WithReader(new Bc5SpanReader());
        var list = q.ToList();
        Assert.IsNotNull(list);
    }

    // L171: GetAsyncEnumerator with no connection throws
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GetAsyncEnumerator_NoConnection_Throws()
    {
        var q = (IAsyncEnumerable<Bc5Item>)SqlQuery<Bc5Item>.ForSqlite();
        _ = q.GetAsyncEnumerator();
    }
}

// ── AggregateParser - CountDistinct, StringAgg ───────────────────────────────

[Sqlx, TableName("bc5_orders")]
public class Bc5Order
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
}

[TestClass]
public class AggregateParserBranch5Tests
{
    private static readonly System.Reflection.MethodInfo _parseMethod =
        typeof(SqlDialect).Assembly.GetType("Sqlx.Expressions.AggregateParser")!
            .GetMethod("Parse", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

    private static string InvokeParse(ExpressionParser parser, MethodCallExpression call)
        => (string)_parseMethod.Invoke(null, new object[] { parser, call })!;

    // Stub methods - names must match what AggregateParser expects
    private static int CountDistinct(IQueryable<Bc5Order> src, Expression<Func<Bc5Order, int>> sel) => 0;
    private static decimal Sum(IQueryable<Bc5Order> src, Expression<Func<Bc5Order, decimal>> sel) => 0;
    private static decimal Average(IQueryable<Bc5Order> src, Expression<Func<Bc5Order, decimal>> sel) => 0;
    private static decimal Max(IQueryable<Bc5Order> src, Expression<Func<Bc5Order, decimal>> sel) => 0;
    private static decimal Min(IQueryable<Bc5Order> src, Expression<Func<Bc5Order, decimal>> sel) => 0;
    private static string StringAgg(IQueryable<Bc5Order> src, Expression<Func<Bc5Order, string>> sel, Expression<Func<Bc5Order, string>> sep) => "";

    private static MethodCallExpression MakeCall(string stubName, params Expression[] args)
    {
        var method = typeof(AggregateParserBranch5Tests).GetMethod(stubName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var src = Expression.Constant(new Bc5Order[0].AsQueryable());
        var quotedArgs = args.Select(a => (Expression)Expression.Quote((LambdaExpression)a)).ToArray();
        var allArgs = new Expression[] { src }.Concat(quotedArgs).ToArray();
        return Expression.Call(method, allArgs);
    }

    // L223: Count with predicate → SUM(CASE WHEN)
    [TestMethod]
    public void AggregateParser_CountWithPredicate_GeneratesSumCaseWhen()
    {
        var q = SqlQuery<Bc5Order>.ForSqlite()
            .GroupBy(o => o.Category)
            .Select(g => new { g.Key, Active = g.Count(o => o.Amount > 0) });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("SUM") || sql.Contains("CASE"), $"SQL: {sql}");
    }

    // L224: CountDistinct via reflection
    [TestMethod]
    public void AggregateParser_CountDistinct_GeneratesCountDistinct()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<Bc5Order, int>> sel = o => o.Id;
        var call = MakeCall(nameof(CountDistinct), sel);
        var result = InvokeParse(parser, call);
        Assert.IsTrue(result.Contains("DISTINCT"), result);
    }

    // L225: Sum
    [TestMethod]
    public void AggregateParser_Sum_GeneratesSumColumn()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<Bc5Order, decimal>> sel = o => o.Amount;
        var call = MakeCall(nameof(Sum), sel);
        var result = InvokeParse(parser, call);
        Assert.IsTrue(result.Contains("SUM"), result);
    }

    // L226: Average
    [TestMethod]
    public void AggregateParser_Average_GeneratesAvgColumn()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<Bc5Order, decimal>> sel = o => o.Amount;
        var call = MakeCall(nameof(Average), sel);
        var result = InvokeParse(parser, call);
        Assert.IsTrue(result.Contains("AVG"), result);
    }

    // L227: Max
    [TestMethod]
    public void AggregateParser_Max_GeneratesMaxColumn()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<Bc5Order, decimal>> sel = o => o.Amount;
        var call = MakeCall(nameof(Max), sel);
        var result = InvokeParse(parser, call);
        Assert.IsTrue(result.Contains("MAX"), result);
    }

    // L228: Min
    [TestMethod]
    public void AggregateParser_Min_GeneratesMinColumn()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<Bc5Order, decimal>> sel = o => o.Amount;
        var call = MakeCall(nameof(Min), sel);
        var result = InvokeParse(parser, call);
        Assert.IsTrue(result.Contains("MIN"), result);
    }

    // StringAgg - SQLite dialect
    [TestMethod]
    public void AggregateParser_StringAgg_SQLite_GeneratesGroupConcat()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<Bc5Order, string>> sel = o => o.Category;
        Expression<Func<Bc5Order, string>> sep = o => ",";
        var method = typeof(AggregateParserBranch5Tests).GetMethod(nameof(StringAgg),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var src = Expression.Constant(new Bc5Order[0].AsQueryable());
        var call = Expression.Call(method, src, Expression.Quote(sel), Expression.Quote(sep));
        var result = InvokeParse(parser, call);
        Assert.IsTrue(result.Contains("GROUP_CONCAT") || result.Contains("STRING_AGG"), result);
    }
}

// ── DateTimeFunctionParser - AddMonths, AddYears ─────────────────────────────

[Sqlx, TableName("bc5_events")]
public class Bc5Event
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public DateTime EventDate { get; set; }
}

[TestClass]
public class DateTimeFunctionParserBranch5Tests
{
    // L189: DateTime.AddMonths
    [TestMethod]
    public void Where_DateTimeAddMonths_GeneratesSql()
    {
        var now = DateTime.UtcNow;
        var q = SqlQuery<Bc5Event>.ForSqlite()
            .Where(e => e.EventDate > now.AddMonths(1));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L191: DateTime.AddYears
    [TestMethod]
    public void Where_DateTimeAddYears_GeneratesSql()
    {
        var now = DateTime.UtcNow;
        var q = SqlQuery<Bc5Event>.ForSqlite()
            .Where(e => e.EventDate > now.AddYears(1));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L189: DateTime.AddDays (already covered but ensure AddHours too)
    [TestMethod]
    public void Where_DateTimeAddHours_GeneratesSql()
    {
        var now = DateTime.UtcNow;
        var q = SqlQuery<Bc5Event>.ForSqlite()
            .Where(e => e.EventDate > now.AddHours(1));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }
}
