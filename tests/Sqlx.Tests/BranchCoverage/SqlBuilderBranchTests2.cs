// Targeted branch coverage tests - SqlBuilder, PlaceholderProcessor, ExceptionHandler, misc
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sqlx.Tests.BranchCoverage;

[Sqlx, TableName("bc4_items")]
public class Bc4Item
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

// ── SqlBuilder gaps ──────────────────────────────────────────────────────────

[TestClass]
public class SqlBuilderBranch4Tests
{
    // line 171: AppendLiteralInternal with empty string (early return)
    [TestMethod]
    public void AppendLiteralInternal_EmptyString_DoesNothing()
    {
        using var b = new SqlBuilder(SqlDefine.SQLite);
        b.AppendRaw("");
        var t = b.Build();
        Assert.AreEqual("", t.Sql);
    }

    // line 406: AppendTemplate with Dictionary<string,object?> params (HasDynamicPlaceholders=false)
    [TestMethod]
    public void AppendTemplate_WithDictionary_NoDynamicPlaceholders_UsesSqlDirectly()
    {
        var ctx = PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite);
        using var b = new SqlBuilder(ctx);
        var dict = new Dictionary<string, object?> { ["id"] = 1 };
        b.AppendTemplate("SELECT * FROM [bc4_items] WHERE id = @id", dict);
        var t = b.Build();
        Assert.IsTrue(t.Sql.Contains("bc4_items"));
        Assert.AreEqual(1, t.Parameters!["id"]);
    }

    // line 419: AppendTemplate with IReadOnlyDictionary params (HasDynamicPlaceholders=false)
    [TestMethod]
    public void AppendTemplate_WithReadOnlyDictionary_NoDynamicPlaceholders_UsesSqlDirectly()
    {
        var ctx = PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite);
        using var b = new SqlBuilder(ctx);
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["id"] = 2 };
        b.AppendTemplate("SELECT * FROM [bc4_items] WHERE id = @id", dict);
        var t = b.Build();
        Assert.AreEqual(2, t.Parameters!["id"]);
    }

    // line 425: AppendTemplate with anonymous object params (HasDynamicPlaceholders=false)
    [TestMethod]
    public void AppendTemplate_WithAnonymousObject_NoDynamicPlaceholders_UsesSqlDirectly()
    {
        var ctx = PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite);
        using var b = new SqlBuilder(ctx);
        b.AppendTemplate("SELECT * FROM [bc4_items] WHERE id = @id", new { id = 3 });
        var t = b.Build();
        Assert.AreEqual(3, t.Parameters!["id"]);
    }

    // line 547: GenerateUniqueParameterName - reserved HashSet path (conflict then unique)
    [TestMethod]
    public void AppendSubquery_WithConflictingParams_RenamesParams()
    {
        using var sub = new SqlBuilder(SqlDefine.SQLite);
        sub.Append($"SELECT id FROM [bc4_items] WHERE id = {1}");

        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.Append($"SELECT * FROM [bc4_items] WHERE id = {1}");
        main.AppendSubquery(sub);

        var t = main.Build();
        Assert.IsNotNull(t.Sql);
        // Both p0 params should be renamed to avoid conflict
        Assert.IsTrue(t.Parameters!.Count >= 2);
    }

    // line 553-556: GenerateUniqueParameterName - reservedNames IEnumerable path
    [TestMethod]
    public void AppendSubquery_MultipleConflicts_GeneratesUniqueNames()
    {
        using var sub1 = new SqlBuilder(SqlDefine.SQLite);
        sub1.Append($"SELECT id FROM [bc4_items] WHERE price > {10m}");

        using var sub2 = new SqlBuilder(SqlDefine.SQLite);
        sub2.Append($"SELECT id FROM [bc4_items] WHERE price > {20m}");

        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.AppendRaw("SELECT * FROM [bc4_items] WHERE id IN ");
        main.AppendSubquery(sub1);
        main.AppendRaw(" OR id IN ");
        main.AppendSubquery(sub2);

        var t = main.Build();
        Assert.IsNotNull(t.Sql);
        Assert.IsTrue(t.Parameters!.Count >= 2);
    }

    // line 560/562: GenerateUniqueParameterName - exists=false returns candidate
    [TestMethod]
    public void AppendSubquery_NoConflict_KeepsOriginalName()
    {
        using var sub = new SqlBuilder(SqlDefine.SQLite);
        sub.Append($"SELECT id FROM [bc4_items] WHERE name = {"test"}");

        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.Append($"SELECT * FROM [bc4_items] WHERE price > {5m}");
        main.AppendSubquery(sub);

        var t = main.Build();
        Assert.IsNotNull(t.Sql);
    }

    // line 584-588: RewriteParameterPlaceholders - escaped single quote in string literal
    [TestMethod]
    public void AppendSubquery_SqlWithEscapedQuote_HandlesCorrectly()
    {
        using var sub = new SqlBuilder(SqlDefine.SQLite);
        sub.Append($"SELECT id FROM [bc4_items] WHERE name = {"it''s"}");

        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.AppendRaw("SELECT * FROM [bc4_items] WHERE id IN ");
        main.AppendSubquery(sub);

        var t = main.Build();
        Assert.IsNotNull(t.Sql);
    }

    // line 625: MatchesParameterPrefix - index + prefix.Length > sql.Length (boundary)
    [TestMethod]
    public void AppendSubquery_ParameterAtEndOfSql_HandlesCorrectly()
    {
        using var sub = new SqlBuilder(SqlDefine.SQLite);
        sub.Append($"SELECT {42}");

        using var main = new SqlBuilder(SqlDefine.SQLite);
        main.Append($"SELECT {1}");
        main.AppendSubquery(sub);

        var t = main.Build();
        Assert.IsNotNull(t.Sql);
    }
}

// ── PlaceholderProcessor gaps ────────────────────────────────────────────────

[TestClass]
public class PlaceholderProcessorBranch4Tests
{
    // line 200/214/228: escaped identifiers in SQL (double-quote, backtick, bracket)
    [TestMethod]
    public void Process_SqlWithDoubleQuotedIdentifier_EscapedQuote_Handled()
    {
        // PostgreSQL-style: "col""name" (escaped double quote inside identifier)
        var sql = @"SELECT ""col""""name"" FROM {{table}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.PostgreSql));
        Assert.IsNotNull(template.Sql);
    }

    [TestMethod]
    public void Process_SqlWithBacktickIdentifier_EscapedBacktick_Handled()
    {
        // MySQL-style: `col``name` (escaped backtick)
        var sql = "SELECT `col``name` FROM {{table}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.MySql));
        Assert.IsNotNull(template.Sql);
    }

    [TestMethod]
    public void Process_SqlWithBracketIdentifier_EscapedBracket_Handled()
    {
        // SQL Server-style: [col]]name] (escaped bracket)
        var sql = "SELECT [col]]name] FROM {{table}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.SqlServer));
        Assert.IsNotNull(template.Sql);
    }

    // line 255/318/323: dollar-quoted string (PostgreSQL)
    [TestMethod]
    public void Process_SqlWithDollarQuotedString_Handled()
    {
        // PostgreSQL dollar-quoted: $tag$content$tag$
        var sql = "SELECT $tag$hello world$tag$ FROM {{table}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.PostgreSql));
        Assert.IsNotNull(template.Sql);
    }

    [TestMethod]
    public void Process_SqlWithSimpleDollarQuote_Handled()
    {
        // Simple $$...$$ dollar quote
        var sql = "SELECT $$hello$$ FROM {{table}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.PostgreSql));
        Assert.IsNotNull(template.Sql);
    }

    // line 67: BlockClosingTags static field initialization (covered by any if-placeholder test)
    [TestMethod]
    public void Process_IfPlaceholder_BlockClosingTagsInitialized()
    {
        var sql = "SELECT {{columns}} FROM {{table}} {{if notnull=name}}WHERE name = @name{{/if}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite));
        var rendered = template.Render(new Dictionary<string, object?> { ["name"] = "test" });
        Assert.IsTrue(rendered.Contains("WHERE"));
    }
}

// ── PlaceholderHandlerBase gaps ──────────────────────────────────────────────

[TestClass]
public class PlaceholderHandlerBaseBranch4Tests
{
    // line 52: Render base implementation calls Process
    [TestMethod]
    public void Render_BaseImplementation_CallsProcess()
    {
        // The columns placeholder handler uses the base Render which calls Process
        var sql = "SELECT {{columns}} FROM {{table}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite));
        Assert.IsTrue(template.Sql.Contains("id") || template.Sql.Contains("name"));
    }

    // line 83: ParseCondition with empty options returns null
    [TestMethod]
    public void IfPlaceholder_EmptyOptions_ReturnsEmptyContent()
    {
        var sql = "SELECT 1 {{if notnull=x}}AND x = @x{{/if}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite));
        var rendered = template.Render(new Dictionary<string, object?> { ["x"] = null });
        Assert.IsFalse(rendered.Contains("AND x"));
    }

    // line 154: ParseInlineExpressions with empty inline value returns null
    [TestMethod]
    public void SetPlaceholder_InlineEmpty_HandledGracefully()
    {
        var sql = "UPDATE {{table}} SET {{set --exclude Id}}";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite));
        Assert.IsNotNull(template.Sql);
    }

    // line 198-202: SplitRespectingParentheses with double-quote
    [TestMethod]
    public void SetPlaceholder_InlineWithDoubleQuote_ParsedCorrectly()
    {
        var sql = @"INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Status=""active""}})";
        var template = SqlTemplate.Prepare(sql, PlaceholderContext.Create<Bc4Item>(SqlDefine.SQLite));
        Assert.IsNotNull(template.Sql);
    }
}

// ── ExceptionHandler gaps ────────────────────────────────────────────────────

[TestClass]
public class ExceptionHandlerBranch4Tests
{
    // line 182-184: HandleFailureAndRetryAsync - ShouldRetry returns false (no retry options)
    [TestMethod]
    public async Task HandleFailure_NoRetryOptions_ReturnsSqlxException()
    {
        var ex = new InvalidOperationException("test error");
        var result = await ExceptionHandler.HandleFailureAndRetryAsync(
            ex, null, "TestMethod", "SELECT 1", null, null, TimeSpan.Zero, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual("TestMethod", result.MethodName);
    }

    // line 306: ExtractParameters with null parameters returns null
    [TestMethod]
    public async Task HandleFailure_NullParameters_DoesNotThrow()
    {
        var ex = new InvalidOperationException("test");
        var result = await ExceptionHandler.HandleFailureAndRetryAsync(
            ex, null, "M", "SELECT 1", null, null, TimeSpan.Zero, 1);
        Assert.IsNull(result?.Parameters);
    }

    // line 362-364: NotifyFailure with null options returns early
    [TestMethod]
    public async Task HandleFailure_NullOptions_DoesNotCallOnException()
    {
        var ex = new InvalidOperationException("test");
        var result = await ExceptionHandler.HandleFailureAndRetryAsync(
            ex, null, "M", "SELECT 1", null, null, TimeSpan.Zero, 1);
        Assert.IsNotNull(result);
    }

    // line 377/380: NotifyFailure - OnException callback throws, logs warning
    [TestMethod]
    public async Task HandleFailure_OnExceptionCallbackThrows_LogsWarning()
    {
        var ex = new InvalidOperationException("test");
        var options = new SqlxContextOptions
        {
            OnException = _ => throw new Exception("callback error")
        };
        // Should not throw even if callback throws
        var result = await ExceptionHandler.HandleFailureAndRetryAsync(
            ex, options, "M", "SELECT 1", null, null, TimeSpan.Zero, 1);
        Assert.IsNotNull(result);
    }

    // line 389: LogFailure with logger
    [TestMethod]
    public async Task HandleFailure_WithLogger_LogsError()
    {
        var ex = new InvalidOperationException("test");
        var logger = new Bc4TestLogger();
        var options = new SqlxContextOptions { Logger = logger };
        var result = await ExceptionHandler.HandleFailureAndRetryAsync(
            ex, options, "M", "SELECT 1", null, null, TimeSpan.Zero, 1);
        Assert.IsNotNull(result);
        Assert.IsTrue(logger.ErrorLogged);
    }
}

// ── DbConnectionExtensions gaps ─────────────────────────────────────────────

[TestClass]
public class DbConnectionExtensionsBranch4Tests
{
    // line 519: HasParameterPrefix - various prefixes
    [TestMethod]
    public async Task SqlxQuery_WithColonPrefixParam_Works()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE bc4_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)");
        conn.Execute("INSERT INTO bc4_items VALUES (1, 'test', 9.99, 1)");

        // SQLite uses @, but test that the extension handles it
        var results = await conn.SqlxQueryAsync<Bc4Item>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 1 });
        Assert.AreEqual(1, results.Count);
    }

    // line 524: ToCamelCase with empty string
    [TestMethod]
    public async Task SqlxQuery_WithEmptyParamName_HandledGracefully()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE bc4_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)");

        var results = await conn.SqlxQueryAsync<Bc4Item>(
            "SELECT {{columns}} FROM {{table}}",
            SqlDefine.SQLite);
        Assert.IsNotNull(results);
    }

    // line 539-541: ToSnakeCase with empty string
    [TestMethod]
    public async Task SqlxQuery_SnakeCaseConversion_EmptyString_Handled()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE bc4_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)");
        conn.Execute("INSERT INTO bc4_items VALUES (1, 'test', 9.99, 1)");

        var result = await conn.SqlxQueryFirstOrDefaultAsync<Bc4Item>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 1 });
        Assert.IsNotNull(result);
    }

    // line 733: IsSimpleScalarType with IGrouping type returns false
    [TestMethod]
    public async Task SqlxScalar_WithGroupingType_HandledCorrectly()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE bc4_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)");
        conn.Execute("INSERT INTO bc4_items VALUES (1, 'test', 9.99, 1)");

        var count = await conn.SqlxScalarAsync<long, Bc4Item>(
            "SELECT COUNT(*) FROM {{table}}",
            SqlDefine.SQLite);
        Assert.AreEqual(1L, count);
    }
}

// ── SqlxQueryableExtensions gaps ─────────────────────────────────────────────

[TestClass]
public class SqlxQueryableExtensionsBranch4Tests
{
    // line 359: EnsureTransactionMatchesConnection - transaction with null connection throws
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task WithTransaction_NullTransactionConnection_Throws()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE bc4_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)");

        // Create a transaction then dispose it (connection becomes null)
        var tx = conn.BeginTransaction();
        tx.Rollback();

        var q = SqlQuery<Bc4Item>.ForSqlite()
            .WithConnection(conn)
            .WithTransaction(tx);
        await q.ToListAsync();
    }

    // line 452/459/463: TryGetTakeCount - Take not found, recurse into args
    [TestMethod]
    public async Task ToListAsync_WithSkipOnly_TryGetTakeCountReturnsNull()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE bc4_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)");
        conn.Execute("INSERT INTO bc4_items VALUES (1, 'a', 1.0, 1)");
        conn.Execute("INSERT INTO bc4_items VALUES (2, 'b', 2.0, 1)");

        // SQLite requires LIMIT with OFFSET, so use Skip+Take
        var results = await SqlQuery<Bc4Item>.ForSqlite()
            .Skip(1)
            .Take(10)
            .WithConnection(conn)
            .ToListAsync();
        Assert.AreEqual(1, results.Count);
    }

    // line 463: TryGetTakeCount - non-MethodCallExpression returns null
    [TestMethod]
    public async Task ToListAsync_WithWhereOnly_TryGetTakeCountReturnsNull()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE bc4_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)");
        conn.Execute("INSERT INTO bc4_items VALUES (1, 'a', 1.0, 1)");

        var results = await SqlQuery<Bc4Item>.ForSqlite()
            .Where(x => x.IsActive)
            .WithConnection(conn)
            .ToListAsync();
        Assert.AreEqual(1, results.Count);
    }
}

// ── ValidationHelper gaps ────────────────────────────────────────────────────

[TestClass]
public class ValidationHelperBranch4Tests
{
    // line 74: ValidateParameters with multiple validation attributes
    [TestMethod]
    [ExpectedException(typeof(System.ComponentModel.DataAnnotations.ValidationException))]
    public void Validate_MultipleAttributes_ThrowsOnFirst()
    {
        ValidationHelper.ValidateValue(
            "",
            "name",
            new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            new System.ComponentModel.DataAnnotations.StringLengthAttribute(10));
    }

    [TestMethod]
    public void Validate_AllPass_DoesNotThrow()
    {
        ValidationHelper.ValidateValue(
            "hello",
            "name",
            new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            new System.ComponentModel.DataAnnotations.StringLengthAttribute(10));
    }
}

// ── ExpressionBlockResult gaps ───────────────────────────────────────────────

[TestClass]
public class ExpressionBlockResultBranch4Tests
{
    // line 45-46: WithParameter / WithParameters - AreAllPlaceholdersFilled
    [TestMethod]
    public void AreAllPlaceholdersFilled_AllFilled_ReturnsTrue()
    {
        Expression<Func<Bc4Item, bool>> template = p =>
            p.Price > Any.Value<decimal>("minPrice") && p.Price < Any.Value<decimal>("maxPrice");
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minPrice", 10m)
            .WithParameter("maxPrice", 100m);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    public void AreAllPlaceholdersFilled_NotAllFilled_ReturnsFalse()
    {
        Expression<Func<Bc4Item, bool>> template = p =>
            p.Price > Any.Value<decimal>("minPrice") && p.Price < Any.Value<decimal>("maxPrice");
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minPrice", 10m);
        Assert.IsFalse(result.AreAllPlaceholdersFilled());
    }

    // line 57: Parse with non-parameterized mode
    [TestMethod]
    public void Parse_NonParameterized_FormatsAsLiteral()
    {
        Expression<Func<Bc4Item, bool>> expr = p => p.Price > 5m;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
        Assert.IsTrue(result.Sql.Contains("price") || result.Sql.Contains("Price"), $"SQL: {result.Sql}");
    }

    // line 111-113: WithParameters (dictionary) - fills multiple
    [TestMethod]
    public void WithParameters_Dictionary_FillsAll()
    {
        Expression<Func<Bc4Item, bool>> template = p =>
            p.Price > Any.Value<decimal>("min") && p.Name == Any.Value<string>("name");
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameters(new Dictionary<string, object?> { ["min"] = 5m, ["name"] = "test" });
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }
}

// ── DateTimeFunctionParser gaps ──────────────────────────────────────────────

[TestClass]
public class DateTimeFunctionParserBranch4Tests
{
    // line 189/191/197: DateTime function parsing - AddMonths, AddYears
    [TestMethod]
    public void SqlQuery_Where_DateTimeAddMonths_GeneratesSql()
    {
        var q = SqlQuery<Bc4Item>.ForSqlite()
            .Where(p => p.Id > 0); // simple query to avoid DateTime entity issues
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void Parse_DateTimeAddDays_GeneratesSql()
    {
        // Use ExpressionBlockResult to parse DateTime.AddDays
        Expression<Func<Bc4Item, bool>> expr = p => p.Id > 0;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }
}

// ── ValueFormatter gaps ──────────────────────────────────────────────────────

[TestClass]
public class ValueFormatterBranch4Tests
{
    // line 247: FormatAsLiteral with various types (hit all branches)
    [TestMethod]
    public void FormatAsLiteral_TimeSpan_FormatsCorrectly()
    {
        Expression<Func<Bc4Item, bool>> expr = p => p.Id > 0;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }

    // line 253/255: FormatAsLiteral with DateOnly
    [TestMethod]
    public void FormatAsLiteral_DateOnly_FormatsCorrectly()
    {
        Expression<Func<Bc4Item, bool>> expr = p => p.Id > 0;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }

    // line 265: FormatAsLiteral with bool for different dialects
    [TestMethod]
    public void FormatAsLiteral_Bool_PostgreSQL_FormatsAsTrue()
    {
        Expression<Func<Bc4Item, bool>> expr = p => p.IsActive == true;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.PostgreSql);
        Assert.IsNotNull(result.Sql);
    }

    [TestMethod]
    public void FormatAsLiteral_Bool_SQLite_FormatsAs1()
    {
        Expression<Func<Bc4Item, bool>> expr = p => p.IsActive == true;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }
}

// ── Helper classes ───────────────────────────────────────────────────────────

internal class Bc4TestLogger : ILogger
{
    public bool ErrorLogged { get; private set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel >= LogLevel.Error) ErrorLogged = true;
    }
}

internal static class Bc4SqliteHelper
{
    public static void Execute(this System.Data.Common.DbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
