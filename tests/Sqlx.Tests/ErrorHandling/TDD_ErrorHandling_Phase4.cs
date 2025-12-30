using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.ErrorHandling;

/// <summary>
/// Phase 4 Batch 2: 错误处理增强测试
/// 新增12个错误处理测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("ErrorHandling")]
[TestCategory("Phase4")]
public class TDD_ErrorHandling_Phase4
{
    private IDbConnection? _connection;
    private IErrorHandlingRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE error_test (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                value INTEGER
            )");

        _repo = new ErrorHandlingRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("SQL injection via parameter should be safe")]
    public async Task Error_SQLInjection_ShouldBeSafe()
    {
        _connection!.Execute("INSERT INTO error_test (id, name, value) VALUES (1, 'test', 100)");

        // Attempt SQL injection
        var maliciousInput = "test' OR '1'='1";
        var result = await _repo!.GetByNameAsync(maliciousInput);

        // Should return null (no match) because parameter is properly escaped
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Empty string parameter should work")]
    public async Task Error_EmptyString_ShouldWork()
    {
        _connection!.Execute("INSERT INTO error_test (id, name, value) VALUES (1, '', 100)");

        var result = await _repo!.GetByNameAsync("");
        Assert.IsNotNull(result);
        Assert.AreEqual("", result.name);
    }

    [TestMethod]
    [Description("Null parameter should work")]
    public async Task Error_NullParameter_ShouldWork()
    {
        var result = await _repo!.GetByNameAsync(null!);
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Very long string parameter should work")]
    public async Task Error_VeryLongString_ShouldWork()
    {
        var longString = new string('A', 10000);
        _connection!.Execute($"INSERT INTO error_test (id, name, value) VALUES (1, '{longString}', 100)");

        var result = await _repo!.GetByNameAsync(longString);
        Assert.IsNotNull(result);
        Assert.AreEqual(10000, result.name.Length);
    }

    [TestMethod]
    [Description("Special characters in parameter should work")]
    public async Task Error_SpecialCharacters_ShouldWork()
    {
        var specialChars = "test'\";--/**/\r\n\t";
        // Escape single quotes for SQL
        var escaped = specialChars.Replace("'", "''");
        _connection!.Execute($"INSERT INTO error_test (id, name, value) VALUES (1, '{escaped}', 100)");

        var result = await _repo!.GetByNameAsync(specialChars);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [Description("Query on non-existent table should throw exception")]
    public async Task Error_NonExistentTable_ShouldThrow()
    {
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await _repo!.GetFromNonExistentTableAsync();
        });
    }

    [TestMethod]
    [Description("Insert with constraint violation should throw exception")]
    public async Task Error_ConstraintViolation_ShouldThrow()
    {
        _connection!.Execute("INSERT INTO error_test (id, name, value) VALUES (1, 'test', 100)");

        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            // Try to insert duplicate primary key
            await _repo!.InsertDuplicateIdAsync();
        });
    }

    [TestMethod]
    [Description("Query with syntax error should throw exception")]
    public async Task Error_SyntaxError_ShouldThrow()
    {
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await _repo!.ExecuteSyntaxErrorAsync();
        });
    }

    [TestMethod]
    [Description("Closed connection should throw exception")]
    public async Task Error_ClosedConnection_ShouldThrow()
    {
        _connection!.Close();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _repo!.GetByIdAsync(1);
        });
    }

    [TestMethod]
    [Description("Multiple parameters with same name should work")]
    public async Task Error_DuplicateParameterNames_ShouldWork()
    {
        _connection!.Execute("INSERT INTO error_test (id, name, value) VALUES (1, 'test', 100)");

        var result = await _repo!.GetByValueRangeAsync(50, 150);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [Description("Zero value parameter should work")]
    public async Task Error_ZeroValue_ShouldWork()
    {
        _connection!.Execute("INSERT INTO error_test (id, name, value) VALUES (1, 'test', 0)");

        var result = await _repo!.GetByValueAsync(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.value);
    }

    [TestMethod]
    [Description("Negative value parameter should work")]
    public async Task Error_NegativeValue_ShouldWork()
    {
        _connection!.Execute("INSERT INTO error_test (id, name, value) VALUES (1, 'test', -100)");

        var result = await _repo!.GetByValueAsync(-100);
        Assert.IsNotNull(result);
        Assert.AreEqual(-100, result.value);
    }
}

// Repository interface
public interface IErrorHandlingRepository
{
    [SqlTemplate("SELECT * FROM error_test WHERE name = @name")]
    Task<ErrorTestModel?> GetByNameAsync(string name);

    [SqlTemplate("SELECT * FROM error_test WHERE id = @id")]
    Task<ErrorTestModel?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM error_test WHERE value = @value")]
    Task<ErrorTestModel?> GetByValueAsync(int value);

    [SqlTemplate("SELECT * FROM error_test WHERE value >= @min AND value <= @max")]
    Task<ErrorTestModel?> GetByValueRangeAsync(int min, int max);

    [SqlTemplate("SELECT * FROM non_existent_table")]
    Task<ErrorTestModel?> GetFromNonExistentTableAsync();

    [SqlTemplate("INSERT INTO error_test (id, name, value) VALUES (1, 'duplicate', 100)")]
    Task<int> InsertDuplicateIdAsync();

    [SqlTemplate("SELECT * FORM error_test")]  // Syntax error: FORM instead of FROM
    Task<ErrorTestModel?> ExecuteSyntaxErrorAsync();
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IErrorHandlingRepository))]
public partial class ErrorHandlingRepository(IDbConnection connection) : IErrorHandlingRepository { }

// Model
public class ErrorTestModel
{
    public long id { get; set; }
    public string name { get; set; } = "";
    public int? value { get; set; }
}

// Extension method - removed to avoid conflict with existing ErrorConnectionExtensions

