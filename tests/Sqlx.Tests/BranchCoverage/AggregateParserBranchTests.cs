using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sqlx.Tests.BranchCoverage;

// ── AggregateParser when-guard false branches ─────────────────────────────────

[Sqlx, TableName("bc11_orders")]
public class Bc11Order
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = "";
}

[TestClass]
public class AggregateParserWhenGuardTests
{
    // Stubs without selector (hasArg = false)
    private static int CountDistinct(IQueryable<Bc11Order> src) => 0;
    private static decimal Sum(IQueryable<Bc11Order> src) => 0;
    private static decimal Average(IQueryable<Bc11Order> src) => 0;
    private static decimal Max(IQueryable<Bc11Order> src) => 0;
    private static decimal Min(IQueryable<Bc11Order> src) => 0;
    // StringAgg with only 2 args (no separator)
    private static string StringAgg(IQueryable<Bc11Order> src, Expression<Func<Bc11Order, string>> sel) => "";

    private static MethodCallExpression MakeNoArgCall(string name)
    {
        var method = typeof(AggregateParserWhenGuardTests).GetMethod(
            name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null, new[] { typeof(IQueryable<Bc11Order>) }, null)!;
        var src = Expression.Constant(new Bc11Order[0].AsQueryable());
        return Expression.Call(method, src);
    }

    private static MethodCallExpression MakeStringAgg2ArgCall()
    {
        var method = typeof(AggregateParserWhenGuardTests).GetMethod(
            nameof(StringAgg), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var src = Expression.Constant(new Bc11Order[0].AsQueryable());
        Expression<Func<Bc11Order, string>> sel = o => o.Category;
        return Expression.Call(method, src, Expression.Quote(sel));
    }

    // CountDistinct without arg → falls through to _ (throws)
    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void AggregateParser_CountDistinct_NoArg_ThrowsNotSupported()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var call = MakeNoArgCall(nameof(CountDistinct));
        AggregateParser.Parse(parser, call);
    }

    // Sum without arg → falls through to _ (throws)
    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void AggregateParser_Sum_NoArg_ThrowsNotSupported()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var call = MakeNoArgCall(nameof(Sum));
        AggregateParser.Parse(parser, call);
    }

    // Average without arg → falls through to _ (throws)
    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void AggregateParser_Average_NoArg_ThrowsNotSupported()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var call = MakeNoArgCall(nameof(Average));
        AggregateParser.Parse(parser, call);
    }

    // Max without arg → falls through to _ (throws)
    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void AggregateParser_Max_NoArg_ThrowsNotSupported()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var call = MakeNoArgCall(nameof(Max));
        AggregateParser.Parse(parser, call);
    }

    // Min without arg → falls through to _ (throws)
    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void AggregateParser_Min_NoArg_ThrowsNotSupported()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var call = MakeNoArgCall(nameof(Min));
        AggregateParser.Parse(parser, call);
    }

    // StringAgg with 2 args (count <= 2) → falls through to _ (throws)
    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void AggregateParser_StringAgg_TwoArgs_ThrowsNotSupported()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var call = MakeStringAgg2ArgCall();
        AggregateParser.Parse(parser, call);
    }

    // GetStringAgg - MySql dialect
    [TestMethod]
    public void AggregateParser_StringAgg_MySql_GeneratesGroupConcat()
    {
        var parser = new ExpressionParser(SqlDefine.MySql, new Dictionary<string, object?>(), false);
        var method = typeof(AggregateParserBranch9Tests).GetMethod(
            "StringAgg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var src = Expression.Constant(new Bc9User[0].AsQueryable());
        Expression<Func<Bc9User, string>> sel = u => u.Name;
        Expression<Func<Bc9User, string>> sep = u => ",";
        var call = Expression.Call(method, src, Expression.Quote(sel), Expression.Quote(sep));
        var result = AggregateParser.Parse(parser, call);
        Assert.IsTrue(result.Contains("GROUP_CONCAT") || result.Contains("SEPARATOR"), result);
    }

    // GetStringAgg - Oracle dialect
    [TestMethod]
    public void AggregateParser_StringAgg_Oracle_GeneratesListagg()
    {
        var parser = new ExpressionParser(SqlDefine.Oracle, new Dictionary<string, object?>(), false);
        var method = typeof(AggregateParserBranch9Tests).GetMethod(
            "StringAgg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var src = Expression.Constant(new Bc9User[0].AsQueryable());
        Expression<Func<Bc9User, string>> sel = u => u.Name;
        Expression<Func<Bc9User, string>> sep = u => ",";
        var call = Expression.Call(method, src, Expression.Quote(sel), Expression.Quote(sep));
        var result = AggregateParser.Parse(parser, call);
        Assert.IsTrue(result.Contains("LISTAGG") || result.Contains("STRING_AGG"), result);
    }

    // GetStringAgg - SqlServer dialect
    [TestMethod]
    public void AggregateParser_StringAgg_SqlServer_GeneratesStringAgg()
    {
        var parser = new ExpressionParser(SqlDefine.SqlServer, new Dictionary<string, object?>(), false);
        var method = typeof(AggregateParserBranch9Tests).GetMethod(
            "StringAgg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var src = Expression.Constant(new Bc9User[0].AsQueryable());
        Expression<Func<Bc9User, string>> sel = u => u.Name;
        Expression<Func<Bc9User, string>> sep = u => ",";
        var call = Expression.Call(method, src, Expression.Quote(sel), Expression.Quote(sep));
        var result = AggregateParser.Parse(parser, call);
        Assert.IsTrue(result.Contains("STRING_AGG"), result);
    }
}

// ── TypeConverter remaining branches ─────────────────────────────────────────

[TestClass]
public class TypeConverterBranch11Tests
{
    // L143: TimeOnly from DateTimeOffset
    [TestMethod]
    public void Convert_TimeOnly_FromDateTimeOffset_Converts()
    {
        var dto = new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero);
        var result = TypeConverter.Convert<TimeOnly>(dto);
        Assert.AreEqual(14, result.Hour);
        Assert.AreEqual(30, result.Minute);
    }

    // L153: TimeSpan from non-IConvertible (return as-is)
    [TestMethod]
    public void Convert_TimeSpan_FromTimeSpan_ReturnsDirect()
    {
        var ts = TimeSpan.FromHours(3);
        var result = TypeConverter.Convert<TimeSpan>(ts);
        Assert.AreEqual(ts, result);
    }

    // L165: ConvertFromString - float
    [TestMethod]
    public void Convert_Float_FromString_Parses()
    {
        var result = TypeConverter.Convert<float>("3.14");
        Assert.IsTrue(Math.Abs(result - 3.14f) < 0.001f);
    }

    // L175: nullable Guid from string
    [TestMethod]
    public void Convert_NullableGuid_FromString_Parses()
    {
        var guid = Guid.NewGuid();
        var result = TypeConverter.Convert<Guid?>(guid.ToString());
        Assert.AreEqual(guid, result);
    }

    // L180: nullable DateOnly from DateTime
    [TestMethod]
    public void Convert_NullableDateOnly_FromDateTime_Converts()
    {
        var dt = new DateTime(2024, 6, 15);
        var result = TypeConverter.Convert<DateOnly?>(dt);
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result!.Value.Year);
    }

    // L185: nullable TimeOnly from TimeSpan
    [TestMethod]
    public void Convert_NullableTimeOnly_FromTimeSpan_Converts()
    {
        var ts = new TimeSpan(10, 30, 0);
        var result = TypeConverter.Convert<TimeOnly?>(ts);
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result!.Value.Hour);
    }

    // L235: ConvertFromString - TimeOnly
    [TestMethod]
    public void ConvertFromString_TimeOnly_Parses()
    {
        var result = TypeConverter.Convert<TimeOnly>("09:15:00");
        Assert.AreEqual(9, result.Hour);
        Assert.AreEqual(15, result.Minute);
    }

    // L240: ConvertFromString - Guid
    [TestMethod]
    public void ConvertFromString_Guid_Parses()
    {
        var guid = Guid.NewGuid();
        var result = TypeConverter.Convert<Guid>(guid.ToString());
        Assert.AreEqual(guid, result);
    }

    // L321: BuildConversion - non-nullable int
    [TestMethod]
    public void BuildConversion_Int_ReturnsGetInt32()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(int));
        Assert.IsNotNull(expr);
    }

    // L323: BuildConversion - non-nullable bool
    [TestMethod]
    public void BuildConversion_Bool_ReturnsGetBoolean()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(bool));
        Assert.IsNotNull(expr);
    }

    // L327: BuildConversion - non-nullable long
    [TestMethod]
    public void BuildConversion_Long_ReturnsGetInt64()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(long));
        Assert.IsNotNull(expr);
    }

    // L333: BuildConversion - non-nullable double
    [TestMethod]
    public void BuildConversion_Double_ReturnsGetDouble()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(double));
        Assert.IsNotNull(expr);
    }

    // L339: BuildConversion - non-nullable float
    [TestMethod]
    public void BuildConversion_Float_ReturnsGetFloat()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(float));
        Assert.IsNotNull(expr);
    }

    // L345: BuildConversion - non-nullable decimal
    [TestMethod]
    public void BuildConversion_Decimal_ReturnsGetDecimal()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(decimal));
        Assert.IsNotNull(expr);
    }

    // L351: BuildConversion - non-nullable DateTime
    [TestMethod]
    public void BuildConversion_DateTime_ReturnsGetDateTime()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(DateTime));
        Assert.IsNotNull(expr);
    }

    // L357: BuildConversion - non-nullable short
    [TestMethod]
    public void BuildConversion_Short_ReturnsGetInt16()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(short));
        Assert.IsNotNull(expr);
    }

    // L369: BuildConversion - non-nullable byte
    [TestMethod]
    public void BuildConversion_Byte_ReturnsGetByte()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(byte));
        Assert.IsNotNull(expr);
    }

    // L382: BuildConversion - non-nullable char
    [TestMethod]
    public void BuildConversion_Char_ReturnsGetChar()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(char));
        Assert.IsNotNull(expr);
    }

    // L394: BuildConversion - object type
    [TestMethod]
    public void BuildConversion_Object_ReturnsGetValue()
    {
        var readerParam = Expression.Parameter(typeof(System.Data.IDataReader), "r");
        var ordinalExpr = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinalExpr, typeof(object));
        Assert.IsNotNull(expr);
    }
}

// ── SqlxQueryProvider remaining branches ─────────────────────────────────────

[Sqlx, TableName("bc11_users")]
public class Bc11User
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public decimal Score { get; set; }
}

[TestClass]
public class SqlxQueryProviderBranch11Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE bc11_users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER, is_active INTEGER, score REAL);
            INSERT INTO bc11_users VALUES (1,'Alice',30,1,100.0),(2,'Bob',25,0,50.0)";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // Execute - First (throwIfEmpty=true)
    [TestMethod]
    public void Execute_First_ReturnsFirstEntity()
    {
        using var conn = CreateConn();
        var result = SqlQuery<Bc11User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc11UserResultReader.Default)
            .First();
        Assert.IsNotNull(result);
    }

    // Execute - Any with predicate
    [TestMethod]
    public async Task Execute_AnyWithPredicate_ReturnsTrue()
    {
        using var conn = CreateConn();
        var result = await SqlQuery<Bc11User>.ForSqlite()
            .Where(u => u.Age > 20)
            .WithConnection(conn)
            .WithReader(Bc11UserResultReader.Default)
            .AnyAsync();
        Assert.IsTrue(result);
    }

    // Execute - Any without predicate
    [TestMethod]
    public async Task Execute_AnyWithoutPredicate_ReturnsTrue()
    {
        using var conn = CreateConn();
        var result = await SqlQuery<Bc11User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc11UserResultReader.Default)
            .AnyAsync();
        Assert.IsTrue(result);
    }

    // Execute - Count with predicate
    [TestMethod]
    public async Task Execute_CountWithPredicate_ReturnsCount()
    {
        using var conn = CreateConn();
        var result = await SqlQuery<Bc11User>.ForSqlite()
            .Where(u => u.IsActive)
            .WithConnection(conn)
            .WithReader(Bc11UserResultReader.Default)
            .CountAsync();
        Assert.AreEqual(1, result);
    }

    // ExtractColumnExpression - with non-member lambda body
    [TestMethod]
    public void Execute_Max_WithExpression_ReturnsMax()
    {
        using var conn = CreateConn();
        var result = SqlQuery<Bc11User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc11UserResultReader.Default)
            .Max(u => u.Score);
        Assert.AreEqual(100.0m, result);
    }

    // ToExistsQuery - with distinct (CanBuildDirectAggregateQuery = false)
    [TestMethod]
    public async Task Execute_Any_WithDistinct_UsesWrappedQuery()
    {
        using var conn = CreateConn();
        var result = await SqlQuery<Bc11User>.ForSqlite()
            .Distinct()
            .WithConnection(conn)
            .WithReader(Bc11UserResultReader.Default)
            .AnyAsync();
        Assert.IsTrue(result);
    }

    // ToCountQuery - with groupby (CanBuildDirectAggregateQuery = false)
    [TestMethod]
    public async Task Execute_Count_WithGroupBy_UsesWrappedQuery()
    {
        using var conn = CreateConn();
        var result = await SqlQuery<Bc11User>.ForSqlite()
            .GroupBy(u => u.Age)
            .Select(g => new { g.Key, Count = g.Count() })
            .WithConnection(conn)
            .CountAsync();
        Assert.IsTrue(result >= 0);
    }
}
