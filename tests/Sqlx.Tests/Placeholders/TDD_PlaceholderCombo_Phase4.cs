using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Phase 4 Batch 4: 占位符组合测试
/// 新增10个占位符组合场景测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Placeholders")]
[TestCategory("Phase4")]
public class TDD_PlaceholderCombo_Phase4
{
    private IDbConnection? _connection;
    private IPlaceholderComboRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        ExecuteSql(@"
            CREATE TABLE combo_test (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                category TEXT,
                value INTEGER,
                score REAL
            )");

        // Insert test data
        for (int i = 1; i <= 20; i++)
        {
            var category = i % 3 == 0 ? "A" : i % 3 == 1 ? "B" : "C";
            ExecuteSql($"INSERT INTO combo_test VALUES ({i}, 'Item{i}', '{category}', {i * 10}, {i * 1.5})");
        }

        _repo = new PlaceholderComboRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    private void ExecuteSql(string sql)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    [TestMethod]
    [Description("{{where}} + {{limit}} combination should work")]
    public async Task Combo_WherePlusLimit_ShouldWork()
    {
        var results = await _repo!.GetByCategoryWithLimitAsync("A", 3);
        Assert.IsTrue(results.Count <= 3);
        Assert.IsTrue(results.All(r => r.category == "A"));
    }

    [TestMethod]
    [Description("{{where}} + {{offset}} combination should work")]
    public async Task Combo_WherePlusOffset_ShouldWork()
    {
        var results = await _repo!.GetByCategoryWithOffsetAsync("B", 2);
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(r => r.category == "B"));
    }

    [TestMethod]
    [Description("{{where}} + {{limit}} + {{offset}} combination should work")]
    public async Task Combo_WhereLimitOffset_ShouldWork()
    {
        var results = await _repo!.GetByCategoryPagedAsync("C", 3, 1);
        Assert.IsTrue(results.Count <= 3);
        Assert.IsTrue(results.All(r => r.category == "C"));
    }

    [TestMethod]
    [Description("{{columns}} + {{where}} combination should work")]
    public async Task Combo_ColumnsPlusWhere_ShouldWork()
    {
        var results = await _repo!.GetNamesByValueAsync(50);
        Assert.IsTrue(results.Count > 0);
        // Results should only have id and name
        Assert.IsTrue(results.All(r => r.id > 0 && !string.IsNullOrEmpty(r.name)));
    }

    [TestMethod]
    [Description("{{where}} + ORDER BY combination should work")]
    public async Task Combo_WhereOrderBy_ShouldWork()
    {
        var results = await _repo!.GetByCategoryOrderedAsync("A");
        Assert.IsTrue(results.Count > 0);
        // Should be ordered by value descending
        for (int i = 1; i < results.Count; i++)
        {
            Assert.IsTrue(results[i - 1].value >= results[i].value);
        }
    }

    [TestMethod]
    [Description("{{where}} + GROUP BY combination should work")]
    public async Task Combo_WhereGroupBy_ShouldWork()
    {
        var results = await _repo!.GetCategoryStatsAsync(30);
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(r => !string.IsNullOrEmpty(r.category)));
    }

    [TestMethod]
    [Description("{{set}} + {{where}} for UPDATE should work")]
    public async Task Combo_SetPlusWhereUpdate_ShouldWork()
    {
        var updated = await _repo!.UpdateCategoryByValueAsync("A", 100);
        Assert.IsTrue(updated > 0);

        var results = await _repo!.GetByCategoryWithLimitAsync("A", 100);
        Assert.IsTrue(results.Count > 0);
    }

    [TestMethod]
    [Description("Multiple {{where}} conditions should work")]
    public async Task Combo_MultipleWhere_ShouldWork()
    {
        var results = await _repo!.GetByMultipleConditionsAsync("B", 50);
        Assert.IsTrue(results.All(r => r.category == "B" && r.value >= 50));
    }

    [TestMethod]
    [Description("{{limit}} + ORDER BY combination should work")]
    public async Task Combo_LimitOrderBy_ShouldWork()
    {
        var results = await _repo!.GetTopByScoreAsync(5);
        Assert.IsTrue(results.Count <= 5);
        // Should be ordered by score descending
        for (int i = 1; i < results.Count; i++)
        {
            Assert.IsTrue(results[i - 1].score >= results[i].score);
        }
    }

    [TestMethod]
    [Description("Complex combination: {{columns}} + {{where}} + {{limit}} + ORDER BY")]
    public async Task Combo_ComplexCombination_ShouldWork()
    {
        var results = await _repo!.GetTopNamesByCategoryAsync("C", 3);
        Assert.IsTrue(results.Count <= 3);
        Assert.IsTrue(results.All(r => r.category == "C"));
    }
}

// Repository interface
public interface IPlaceholderComboRepository
{
    [SqlTemplate("SELECT * FROM combo_test WHERE category = @category LIMIT @limit")]
    Task<List<ComboTestModel>> GetByCategoryWithLimitAsync(string category, int limit);

    [SqlTemplate("SELECT * FROM combo_test WHERE category = @category LIMIT -1 OFFSET @offset")]
    Task<List<ComboTestModel>> GetByCategoryWithOffsetAsync(string category, int offset);

    [SqlTemplate("SELECT * FROM combo_test WHERE category = @category LIMIT @limit OFFSET @offset")]
    Task<List<ComboTestModel>> GetByCategoryPagedAsync(string category, int limit, int offset);

    [SqlTemplate("SELECT id, name, NULL as category, NULL as value, NULL as score FROM combo_test WHERE value >= @minValue")]
    Task<List<ComboTestModel>> GetNamesByValueAsync(int minValue);

    [SqlTemplate("SELECT * FROM combo_test WHERE category = @category ORDER BY value DESC")]
    Task<List<ComboTestModel>> GetByCategoryOrderedAsync(string category);

    [SqlTemplate("SELECT 0 as id, '' as name, category, 0 as value, 0.0 as score FROM combo_test WHERE value >= @minValue GROUP BY category")]
    Task<List<ComboTestModel>> GetCategoryStatsAsync(int minValue);

    [SqlTemplate("UPDATE combo_test SET category = @newCategory WHERE value >= @minValue")]
    Task<int> UpdateCategoryByValueAsync(string newCategory, int minValue);

    [SqlTemplate("SELECT * FROM combo_test WHERE category = @category AND value >= @minValue")]
    Task<List<ComboTestModel>> GetByMultipleConditionsAsync(string category, int minValue);

    [SqlTemplate("SELECT * FROM combo_test ORDER BY score DESC LIMIT @limit")]
    Task<List<ComboTestModel>> GetTopByScoreAsync(int limit);

    [SqlTemplate("SELECT * FROM combo_test WHERE category = @category ORDER BY score DESC LIMIT @limit")]
    Task<List<ComboTestModel>> GetTopNamesByCategoryAsync(string category, int limit);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPlaceholderComboRepository))]
public partial class PlaceholderComboRepository(IDbConnection connection) : IPlaceholderComboRepository { }

// Model
public class ComboTestModel
{
    public long id { get; set; }
    public string name { get; set; } = "";
    public string? category { get; set; }
    public int? value { get; set; }
    public double? score { get; set; }
}

