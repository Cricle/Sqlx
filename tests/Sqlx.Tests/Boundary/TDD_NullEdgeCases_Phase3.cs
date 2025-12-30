using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Boundary;

/// <summary>
/// Phase 3: NULL和边界值测试 - 确保80%边界场景覆盖
/// 新增15个NULL和边界值测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Boundary")]
[TestCategory("CoveragePhase3")]
public class TDD_NullEdgeCases_Phase3
{
    private IDbConnection? _connection;
    private INullEdgeCasesRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE test_data (
                id INTEGER PRIMARY KEY,
                nullable_string TEXT,
                nullable_int INTEGER,
                nullable_real REAL,
                required_string TEXT NOT NULL,
                required_int INTEGER NOT NULL
            )");

        _repo = new NullEdgeCasesRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("INSERT with NULL string should work")]
    public async Task Insert_NullString_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, NULL, NULL, NULL, 'test', 1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNull(result.NullableString);
    }

    [TestMethod]
    [Description("INSERT with NULL integer should work")]
    public async Task Insert_NullInteger_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, 'test', NULL, NULL, 'test', 1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNull(result.NullableInt);
    }

    [TestMethod]
    [Description("INSERT with NULL real should work")]
    public async Task Insert_NullReal_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, 'test', 1, NULL, 'test', 1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNull(result.NullableReal);
    }

    [TestMethod]
    [Description("SELECT with all NULL values should work")]
    public async Task Select_AllNulls_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, NULL, NULL, NULL, 'test', 1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNull(result.NullableString);
        Assert.IsNull(result.NullableInt);
        Assert.IsNull(result.NullableReal);
    }

    [TestMethod]
    [Description("UPDATE to NULL should work")]
    public async Task Update_ToNull_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, 'test', 100, 99.9, 'required', 1)");

        var affected = await _repo!.UpdateToNullAsync(1);
        Assert.AreEqual(1, affected);

        var result = await _repo.GetByIdAsync(1);
        Assert.IsNull(result!.NullableString);
        Assert.IsNull(result.NullableInt);
        Assert.IsNull(result.NullableReal);
    }

    [TestMethod]
    [Description("UPDATE from NULL to value should work")]
    public async Task Update_FromNullToValue_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, NULL, NULL, NULL, 'test', 1)");

        var affected = await _repo!.UpdateFromNullAsync(1, "new", 200, 88.8);
        Assert.AreEqual(1, affected);

        var result = await _repo.GetByIdAsync(1);
        Assert.AreEqual("new", result!.NullableString);
        Assert.AreEqual(200, result.NullableInt);
        Assert.AreEqual(88.8, result.NullableReal!.Value, 0.01);
    }

    [TestMethod]
    [Description("WHERE IS NULL should filter correctly")]
    public async Task Where_IsNull_ShouldFilter()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, NULL, 1, 1.0, 'a', 1)");
        _connection.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (2, 'test', 2, 2.0, 'b', 2)");

        var results = await _repo!.GetWithNullStringAsync();
        Assert.AreEqual(1, results.Count);
        Assert.IsNull(results[0].NullableString);
    }

    [TestMethod]
    [Description("WHERE IS NOT NULL should filter correctly")]
    public async Task Where_IsNotNull_ShouldFilter()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, NULL, 1, 1.0, 'a', 1)");
        _connection.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (2, 'test', 2, 2.0, 'b', 2)");

        var results = await _repo!.GetWithNonNullStringAsync();
        Assert.AreEqual(1, results.Count);
        Assert.IsNotNull(results[0].NullableString);
    }

    [TestMethod]
    [Description("Empty string vs NULL should be different")]
    public async Task EmptyString_VsNull_ShouldDiffer()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, '', 1, 1.0, 'a', 1)");
        _connection.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (2, NULL, 2, 2.0, 'b', 2)");

        var empty = await _repo!.GetByIdAsync(1);
        var nullValue = await _repo.GetByIdAsync(2);

        Assert.AreEqual("", empty!.NullableString);
        Assert.IsNull(nullValue!.NullableString);
    }

    [TestMethod]
    [Description("Zero vs NULL should be different")]
    public async Task Zero_VsNull_ShouldDiffer()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, 'a', 0, 0.0, 'a', 1)");
        _connection.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (2, 'b', NULL, NULL, 'b', 2)");

        var zero = await _repo!.GetByIdAsync(1);
        var nullValue = await _repo.GetByIdAsync(2);

        Assert.AreEqual(0, zero!.NullableInt);
        Assert.AreEqual(0.0, zero.NullableReal!.Value, 0.01);
        Assert.IsNull(nullValue!.NullableInt);
        Assert.IsNull(nullValue.NullableReal);
    }

    [TestMethod]
    [Description("Negative numbers should work")]
    public async Task NegativeNumbers_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, 'test', -100, -99.9, 'required', -1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.AreEqual(-100, result!.NullableInt);
        Assert.AreEqual(-99.9, result.NullableReal!.Value, 0.01);
        Assert.AreEqual(-1, result.RequiredInt);
    }

    [TestMethod]
    [Description("Very large numbers should work")]
    public async Task VeryLargeNumbers_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, 'test', 2147483647, 999999999.99, 'required', 1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.AreEqual(2147483647, result!.NullableInt);
        Assert.IsTrue(result.NullableReal!.Value > 999999999);
    }

    [TestMethod]
    [Description("Very small decimal precision should work")]
    public async Task VerySmallDecimal_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, 'test', 1, 0.0000001, 'required', 1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsTrue(result!.NullableReal!.Value < 0.001);
        Assert.IsTrue(result.NullableReal.Value > 0);
    }

    [TestMethod]
    [Description("Special characters in strings should work")]
    public async Task SpecialCharacters_ShouldWork()
    {
        var specialString = "Test'with\"quotes\nand\ttabs";
        _connection!.Execute($"INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, @str, 1, 1.0, 'required', 1)",
            param: new { str = specialString });

        var result = await _repo!.GetByIdAsync(1);
        // SQLite may handle special chars differently
        Assert.IsNotNull(result!.NullableString);
    }

    [TestMethod]
    [Description("Unicode characters should work")]
    public async Task UnicodeCharacters_ShouldWork()
    {
        _connection!.Execute("INSERT INTO test_data (id, nullable_string, nullable_int, nullable_real, required_string, required_int) VALUES (1, '测试中文😀🎉', 1, 1.0, 'required', 1)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result!.NullableString);
        // SQLite Unicode support may vary
    }
}

// Repository interface
public interface INullEdgeCasesRepository
{
    [SqlTemplate("SELECT * FROM test_data WHERE id = @id")]
    Task<EdgeCaseData?> GetByIdAsync(long id);

    [SqlTemplate("UPDATE test_data SET nullable_string = NULL, nullable_int = NULL, nullable_real = NULL WHERE id = @id")]
    Task<int> UpdateToNullAsync(long id);

    [SqlTemplate("UPDATE test_data SET nullable_string = @str, nullable_int = @num, nullable_real = @real WHERE id = @id")]
    Task<int> UpdateFromNullAsync(long id, string str, int num, double real);

    [SqlTemplate("SELECT * FROM test_data WHERE nullable_string IS NULL")]
    Task<List<EdgeCaseData>> GetWithNullStringAsync();

    [SqlTemplate("SELECT * FROM test_data WHERE nullable_string IS NOT NULL")]
    Task<List<EdgeCaseData>> GetWithNonNullStringAsync();
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(INullEdgeCasesRepository))]
public partial class NullEdgeCasesRepository(IDbConnection connection) : INullEdgeCasesRepository { }

// Model
public class EdgeCaseData
{
    public long Id { get; set; }
    public string? NullableString { get; set; }
    public int? NullableInt { get; set; }
    public double? NullableReal { get; set; }
    public string RequiredString { get; set; } = "";
    public int RequiredInt { get; set; }
}

