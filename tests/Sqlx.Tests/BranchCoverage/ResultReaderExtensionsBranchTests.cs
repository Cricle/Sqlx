using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Tests.BranchCoverage;

// ── Entities ─────────────────────────────────────────────────────────────────

[Sqlx, TableName("bc8_items")]
public class Bc8Item
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

// ── IResultReader branch coverage ─────────────────────────────────────────────

/// <summary>
/// A reader with PropertyCount=0 to hit the "else" branch in ToList/ToListAsync.
/// </summary>
file class ZeroPropReader : IResultReader<string>
{
    public int PropertyCount => 0;
    public string Read(IDataReader reader) => reader.GetString(0);
    public string Read(IDataReader reader, ReadOnlySpan<int> ordinals) => reader.GetString(0);
    public void GetOrdinals(IDataReader reader, Span<int> ordinals) { }
}

/// <summary>
/// A reader that implements IArrayOrdinalReader to hit the array fast-path.
/// </summary>
file class ArrayOrdinalReader : IResultReader<int>, IArrayOrdinalReader<int>
{
    public int PropertyCount => 1;
    public int Read(IDataReader reader) => reader.GetInt32(0);
    public int Read(IDataReader reader, ReadOnlySpan<int> ordinals) => reader.GetInt32(ordinals[0]);
    public int Read(IDataReader reader, int[] ordinals) => reader.GetInt32(ordinals[0]);
    public void GetOrdinals(IDataReader reader, Span<int> ordinals) => ordinals[0] = reader.GetOrdinal("id");
}

/// <summary>
/// A reader with PropertyCount > 32 to hit the heap-alloc path in sync ToList.
/// </summary>
file class LargePropReader : IResultReader<int>
{
    public int PropertyCount => 33; // > StackAllocOrdinalThreshold
    public int Read(IDataReader reader) => reader.GetInt32(0);
    public int Read(IDataReader reader, ReadOnlySpan<int> ordinals) => reader.GetInt32(ordinals[0]);
    public void GetOrdinals(IDataReader reader, Span<int> ordinals)
    {
        for (int i = 0; i < ordinals.Length; i++) ordinals[i] = 0;
    }
}

/// <summary>
/// Minimal in-memory DbDataReader for async tests.
/// </summary>
file class SimpleDbReader : DbDataReader
{
    private readonly List<int> _rows;
    private int _pos = -1;
    public SimpleDbReader(List<int> rows) => _rows = rows;
    public override bool Read() => ++_pos < _rows.Count;
    public override Task<bool> ReadAsync(CancellationToken ct) => Task.FromResult(Read());
    public override int GetInt32(int i) => _rows[_pos];
    public override string GetString(int i) => _rows[_pos].ToString();
    public override int GetOrdinal(string name) => 0;
    public override bool IsDBNull(int i) => false;
    public override int FieldCount => 1;
    public override object GetValue(int i) => _rows[_pos];
    public override bool GetBoolean(int i) => false;
    public override byte GetByte(int i) => 0;
    public override long GetBytes(int i, long o, byte[]? b, int bo, int l) => 0;
    public override char GetChar(int i) => ' ';
    public override long GetChars(int i, long o, char[]? b, int bo, int l) => 0;
    public override string GetDataTypeName(int i) => "int";
    public override DateTime GetDateTime(int i) => default;
    public override decimal GetDecimal(int i) => 0;
    public override double GetDouble(int i) => 0;
    public override Type GetFieldType(int i) => typeof(int);
    public override float GetFloat(int i) => 0;
    public override Guid GetGuid(int i) => Guid.Empty;
    public override short GetInt16(int i) => 0;
    public override long GetInt64(int i) => 0;
    public override string GetName(int i) => "id";
    public override int GetValues(object[] values) => 0;
    public override bool HasRows => _rows.Count > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;
    public override object this[int i] => _rows[_pos];
    public override object this[string name] => _rows[_pos];
    public override int Depth => 0;
    public override bool NextResult() => false;
    public override System.Collections.IEnumerator GetEnumerator() => _rows.GetEnumerator();
}

[TestClass]
public class ResultReaderExtensionsBranch8Tests
{
    // L136: ToList with capacityHint, propCount=0 (else branch)
    [TestMethod]
    public void ToList_WithCapacityHint_ZeroPropCount_UsesReadWithoutOrdinals()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 'hello'";
        using var dr = cmd.ExecuteReader();
        var reader = new ZeroPropReader();
        var result = reader.ToList(dr, capacityHint: 5);
        Assert.AreEqual(1, result.Count);
    }

    // L98: ToList (no capacity), propCount=0 (else branch)
    [TestMethod]
    public void ToList_ZeroPropCount_UsesReadWithoutOrdinals()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 'hello'";
        using var dr = cmd.ExecuteReader();
        var reader = new ZeroPropReader();
        var result = reader.ToList(dr);
        Assert.AreEqual(1, result.Count);
    }

    // L119: ToList with capacityHint, IArrayOrdinalReader path
    [TestMethod]
    public void ToList_WithCapacityHint_ArrayOrdinalReader_UsesArrayPath()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE t(id INTEGER); INSERT INTO t VALUES(1),(2)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT id FROM t";
        using var dr = cmd.ExecuteReader();
        var reader = new ArrayOrdinalReader();
        var result = reader.ToList(dr, capacityHint: 2);
        Assert.AreEqual(2, result.Count);
    }

    // L150: ToList with capacityHint, large propCount (heap alloc path)
    [TestMethod]
    public void ToList_WithCapacityHint_LargePropCount_UsesHeapAlloc()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 42";
        using var dr = cmd.ExecuteReader();
        var reader = new LargePropReader();
        var result = reader.ToList(dr, capacityHint: 1);
        Assert.AreEqual(1, result.Count);
    }

    // L177: ToListAsync (no capacity), propCount=0 (else branch)
    [TestMethod]
    public async Task ToListAsync_ZeroPropCount_UsesReadWithoutOrdinals()
    {
        var dr = new SimpleDbReader(new List<int> { 1, 2 });
        var reader = new ZeroPropReader();
        var result = await reader.ToListAsync(dr);
        Assert.AreEqual(2, result.Count);
    }

    // L206: ToListAsync with capacityHint, propCount=0 (else branch)
    [TestMethod]
    public async Task ToListAsync_WithCapacityHint_ZeroPropCount_UsesReadWithoutOrdinals()
    {
        var dr = new SimpleDbReader(new List<int> { 1, 2, 3 });
        var reader = new ZeroPropReader();
        var result = await reader.ToListAsync(dr, capacityHint: 3);
        Assert.AreEqual(3, result.Count);
    }

    // L168/170: ToListAsync (no capacity), IArrayOrdinalReader path
    [TestMethod]
    public async Task ToListAsync_ArrayOrdinalReader_UsesArrayPath()
    {
        var dr = new SimpleDbReader(new List<int> { 10, 20 });
        var reader = new ArrayOrdinalReader();
        var result = await reader.ToListAsync(dr);
        Assert.AreEqual(2, result.Count);
    }

    // L189: ToListAsync with capacityHint, IArrayOrdinalReader path
    [TestMethod]
    public async Task ToListAsync_WithCapacityHint_ArrayOrdinalReader_UsesArrayPath()
    {
        var dr = new SimpleDbReader(new List<int> { 5, 6, 7 });
        var reader = new ArrayOrdinalReader();
        var result = await reader.ToListAsync(dr, capacityHint: 3);
        Assert.AreEqual(3, result.Count);
    }

    // L220: ToListAsync with capacityHint, non-array reader (else path)
    [TestMethod]
    public async Task ToListAsync_WithCapacityHint_NonArrayReader_UsesOrdinalPath()
    {
        var dr = new SimpleDbReader(new List<int> { 1 });
        // Use a reader with propCount=1 but not IArrayOrdinalReader
        var reader = new LargePropReader();
        var result = await reader.ToListAsync(dr, capacityHint: 1);
        Assert.AreEqual(1, result.Count);
    }

    // L282: ToListAsync (no capacity), non-array reader with propCount>0
    [TestMethod]
    public async Task ToListAsync_NonArrayReader_WithProps_UsesOrdinalPath()
    {
        var dr = new SimpleDbReader(new List<int> { 99 });
        var reader = new LargePropReader();
        var result = await reader.ToListAsync(dr);
        Assert.AreEqual(1, result.Count);
    }
}

// ── ValidationHelper branch coverage ─────────────────────────────────────────

[TestClass]
public class ValidationHelperBranch8Tests
{
    // L74: ValidateValue with empty attributes array (early return)
    [TestMethod]
    public void ValidateValue_EmptyAttributes_ReturnsWithoutValidating()
    {
        // Should not throw even with null value
        ValidationHelper.ValidateValue(null, "param");
    }

    // L74: ValidateValue with attributes, valid value
    [TestMethod]
    public void ValidateValue_WithAttributes_ValidValue_DoesNotThrow()
    {
        ValidationHelper.ValidateValue("hello", "name", new RequiredAttribute());
    }

    // L74: ValidateValue with attributes, invalid value
    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public void ValidateValue_WithAttributes_InvalidValue_Throws()
    {
        ValidationHelper.ValidateValue(null, "name", new RequiredAttribute());
    }
}

// ── ExpressionBlockResult branch coverage ────────────────────────────────────

[TestClass]
public class ExpressionBlockResultBranch8Tests
{
    // L45/46: Parse with non-null expression that has no parameters
    [TestMethod]
    public void Parse_WithConstantExpression_ReturnsResult()
    {
        Expression<Func<Bc8Item, bool>> expr = x => x.Id == 1;
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        Assert.IsFalse(string.IsNullOrEmpty(result.Sql));
    }

    // L57: ParseUpdate with non-MemberInit body
    [TestMethod]
    public void ParseUpdate_NonMemberInitBody_ReturnsEmpty()
    {
        Expression<Func<Bc8Item, Bc8Item>> expr = x => x; // not MemberInit
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.AreEqual(string.Empty, result.Sql);
    }

    // L111: WithParameter on existing placeholder
    [TestMethod]
    public void WithParameter_ExistingPlaceholder_UpdatesValue()
    {
        Expression<Func<Bc8Item, bool>> expr = x => x.Age >= Any.Value<int>("minAge");
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        result = result.WithParameter("minAge", 18);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }
}

// ── DbBatchExecutor branch coverage ──────────────────────────────────────────

file class SimpleBinder : IParameterBinder<Bc8Item>
{
    public void BindEntity(DbCommand cmd, Bc8Item entity, string parameterPrefix = "@")
    {
        var p = cmd.CreateParameter();
        p.ParameterName = parameterPrefix + "name";
        p.Value = entity.Name;
        cmd.Parameters.Add(p);
    }

    public void BindEntity(DbBatchCommand cmd, Bc8Item entity, Func<DbParameter> factory, string parameterPrefix = "@")
    {
        var p = factory();
        p.ParameterName = parameterPrefix + "name";
        p.Value = entity.Name;
        cmd.Parameters.Add(p);
    }
}

[TestClass]
public class DbBatchExecutorBranch8Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc8_items (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT)";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // L34: ExecuteAsync with empty list returns 0
    [TestMethod]
    public async Task ExecuteAsync_EmptyList_ReturnsZero()
    {
        using var conn = CreateConn();
        var result = await DbBatchExecutor.ExecuteAsync(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            new List<Bc8Item>(), new SimpleBinder());
        Assert.AreEqual(0, result);
    }

    // L34: ExecuteAsync with null list returns 0
    [TestMethod]
    public async Task ExecuteAsync_NullList_ReturnsZero()
    {
        using var conn = CreateConn();
        var result = await DbBatchExecutor.ExecuteAsync(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            null!, new SimpleBinder());
        Assert.AreEqual(0, result);
    }

    // L62/64: ExecuteAsync with items, connection already open
    [TestMethod]
    public async Task ExecuteAsync_WithItems_ConnectionOpen_Executes()
    {
        using var conn = CreateConn();
        var items = new List<Bc8Item> { new() { Name = "a" }, new() { Name = "b" } };
        var result = await DbBatchExecutor.ExecuteAsync(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            items, new SimpleBinder());
        Assert.AreEqual(2, result);
    }

    // L78/87/91/94/98: ExecuteAsync with batchSize=1 (multiple batches)
    [TestMethod]
    public async Task ExecuteAsync_MultipleBatches_ExecutesAll()
    {
        using var conn = CreateConn();
        var items = new List<Bc8Item>
        {
            new() { Name = "x" }, new() { Name = "y" }, new() { Name = "z" }
        };
        var result = await DbBatchExecutor.ExecuteAsync(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            items, new SimpleBinder(), batchSize: 1);
        Assert.AreEqual(3, result);
    }

    // L153: EnsureConnectionOpenAsync - connection already open
    [TestMethod]
    public async Task ExecuteAsync_ConnectionAlreadyOpen_DoesNotReopen()
    {
        using var conn = CreateConn(); // already open
        var items = new List<Bc8Item> { new() { Name = "test" } };
        var result = await DbBatchExecutor.ExecuteAsync(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            items, new SimpleBinder());
        Assert.AreEqual(1, result);
    }

    // L177: CloseConnection - with transaction (should not close)
    [TestMethod]
    public async Task ExecuteAsync_WithTransaction_DoesNotCloseConnection()
    {
        using var conn = CreateConn();
        using var tx = conn.BeginTransaction();
        var items = new List<Bc8Item> { new() { Name = "tx" } };
        var result = await DbBatchExecutor.ExecuteAsync(
            conn, tx, "INSERT INTO bc8_items (name) VALUES (@name)",
            items, new SimpleBinder());
        tx.Commit();
        Assert.AreEqual(1, result);
        Assert.AreEqual(System.Data.ConnectionState.Open, conn.State);
    }

    // ExecuteAsync<TEntity, TParameter> - CanCreateBatch path
    [TestMethod]
    public async Task ExecuteAsync_WithTypedParameter_CanCreateBatch_ExecutesBatch()
    {
        using var conn = CreateConn();
        var items = new List<Bc8Item> { new() { Name = "batch1" }, new() { Name = "batch2" } };
        var result = await DbBatchExecutor.ExecuteAsync<Bc8Item, Microsoft.Data.Sqlite.SqliteParameter>(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            items, new SimpleBinder());
        Assert.AreEqual(2, result);
    }

    // ExecuteAsync<TEntity, TParameter> - empty list returns 0
    [TestMethod]
    public async Task ExecuteAsync_WithTypedParameter_EmptyList_ReturnsZero()
    {
        using var conn = CreateConn();
        var result = await DbBatchExecutor.ExecuteAsync<Bc8Item, Microsoft.Data.Sqlite.SqliteParameter>(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            new List<Bc8Item>(), new SimpleBinder());
        Assert.AreEqual(0, result);
    }

    // ExecuteAsync<TEntity, TParameter> - with commandTimeout
    [TestMethod]
    public async Task ExecuteAsync_WithTypedParameter_WithCommandTimeout_Works()
    {
        using var conn = CreateConn();
        var items = new List<Bc8Item> { new() { Name = "timeout_test" } };
        var result = await DbBatchExecutor.ExecuteAsync<Bc8Item, Microsoft.Data.Sqlite.SqliteParameter>(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            items, new SimpleBinder(), commandTimeout: 30);
        Assert.AreEqual(1, result);
    }

    // ExecuteAsync<TEntity, TParameter> - multiple batches
    [TestMethod]
    public async Task ExecuteAsync_WithTypedParameter_MultipleBatches_ExecutesAll()
    {
        using var conn = CreateConn();
        var items = new List<Bc8Item>
        {
            new() { Name = "b1" }, new() { Name = "b2" }, new() { Name = "b3" }
        };
        var result = await DbBatchExecutor.ExecuteAsync<Bc8Item, Microsoft.Data.Sqlite.SqliteParameter>(
            conn, null, "INSERT INTO bc8_items (name) VALUES (@name)",
            items, new SimpleBinder(), batchSize: 1);
        Assert.AreEqual(3, result);
    }
}

// ── SqlxQueryable branch coverage ─────────────────────────────────────────────

// A plain POCO without [Sqlx] - no auto-registered ResultReader
public class Bc8PlainUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

[Sqlx, TableName("bc8_users")]
public class Bc8User
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

[TestClass]
public class SqlxQueryableBranch8Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc8_users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO bc8_users VALUES (1,'Alice',30),(2,'Bob',25)";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // L50: GetEnumerator - no connection throws
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GetEnumerator_NoConnection_Throws()
    {
        var q = SqlQuery<Bc8PlainUser>.ForSqlite();
        q.GetEnumerator();
    }

    // L61/62: GetEnumerator - with connection and reader works
    [TestMethod]
    public void GetEnumerator_WithConnectionAndReader_Works()
    {
        using var conn = CreateConn();
        var q = SqlQuery<Bc8User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc8UserResultReader.Default);
        var list = q.ToList();
        Assert.AreEqual(2, list.Count);
    }

    // L92/93: GetAsyncEnumerator - with connection and reader works
    [TestMethod]
    public async Task GetAsyncEnumerator_WithConnectionAndReader_Works2()
    {
        using var conn = CreateConn();
        var q = (IAsyncEnumerable<Bc8User>)SqlQuery<Bc8User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc8UserResultReader.Default);
        var list = new List<Bc8User>();
        await foreach (var item in q) list.Add(item);
        Assert.AreEqual(2, list.Count);
    }

    // L148: IEnumerable.GetEnumerator (non-generic)
    [TestMethod]
    public void IEnumerable_GetEnumerator_WithConnection_Works()
    {
        using var conn = CreateConn();
        var q = SqlQuery<Bc8User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc8UserResultReader.Default);
        var enumerable = (System.Collections.IEnumerable)q;
        var enumerator = enumerable.GetEnumerator();
        Assert.IsNotNull(enumerator);
        enumerator.MoveNext();
    }

    // L171: GetAsyncEnumerator with connection and reader works
    [TestMethod]
    public async Task GetAsyncEnumerator_WithConnectionAndReader_Works()
    {
        using var conn = CreateConn();
        var q = SqlQuery<Bc8User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc8UserResultReader.Default);
        var list = await q.ToListAsync();
        Assert.AreEqual(2, list.Count);
    }
}
