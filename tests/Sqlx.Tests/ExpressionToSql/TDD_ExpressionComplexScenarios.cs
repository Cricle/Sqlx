using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.ExpressionToSql;

/// <summary>
/// 表达式树复杂场景测试 - 覆盖嵌套、组合、链式等高级场景
/// </summary>
[TestClass]
public class TDD_ExpressionComplexScenarios
{
    private IDbConnection _connection = null!;
    private IExpressionComplexRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE expr_test (
                id INTEGER PRIMARY KEY,
                name TEXT,
                age INTEGER,
                salary REAL,
                is_active INTEGER,
                created_at TEXT,
                category TEXT,
                score REAL,
                department TEXT,
                email TEXT
            )";
        cmd.ExecuteNonQuery();

        // 插入测试数据
        cmd.CommandText = @"
            INSERT INTO expr_test (id, name, age, salary, is_active, created_at, category, score, department, email) VALUES
            (1, 'Alice', 30, 50000.0, 1, '2024-01-15', 'Sales', 85.5, 'Dept-A', 'alice@test.com'),
            (2, 'Bob', 25, 45000.0, 1, '2024-02-20', 'IT', 90.0, 'Dept-B', 'bob@test.com'),
            (3, 'Charlie', 35, 60000.0, 0, '2024-03-10', 'Sales', 78.0, 'Dept-A', 'charlie@test.com'),
            (4, 'David', 28, 48000.0, 1, '2024-04-05', 'HR', 88.5, 'Dept-C', 'david@test.com'),
            (5, 'Eve', 32, 55000.0, 1, '2024-05-12', 'IT', 92.0, 'Dept-B', 'eve@test.com'),
            (6, 'Frank', 40, 70000.0, 0, '2024-06-18', 'Management', 95.0, 'Dept-A', 'frank@test.com'),
            (7, 'Grace', 26, 46000.0, 1, '2024-07-22', 'Sales', 82.0, 'Dept-C', 'grace@test.com'),
            (8, 'Henry', 38, 65000.0, 1, '2024-08-30', 'IT', 89.5, 'Dept-B', 'henry@test.com')";
        cmd.ExecuteNonQuery();

        _repo = new ExpressionComplexRepository(_connection);
    }

    [TestCleanup]
    public void TearDown()
    {
        _connection?.Dispose();
    }

    #region 1. 嵌套AND/OR组合表达式

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_NestedAndOr_ShouldWork()
    {
        // (age > 25 AND salary > 50000) OR (is_active = 1 AND score > 90)
        var result = await _repo.GetByComplexConditionAsync(25, 50000.0, true, 90.0);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0);
        // 应该匹配: Eve (active, score=92), Charlie (age=35, salary=60000), Frank (age=40, salary=70000)
        Assert.IsTrue(result.Any(x => x.name == "Eve" || x.name == "Charlie" || x.name == "Frank"));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_MultipleAndConditions_ShouldWork()
    {
        // age >= 30 AND salary >= 55000 AND is_active = 1
        var result = await _repo.GetByMultipleAndAsync(30, 55000.0, true);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count); // Eve, Henry
        Assert.IsTrue(result.All(x => x.age >= 30 && x.salary >= 55000.0 && x.is_active));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_MultipleOrConditions_ShouldWork()
    {
        // category = 'Sales' OR category = 'IT' OR department = 'Dept-A'
        var result = await _repo.GetByMultipleOrAsync();
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count >= 6); // Sales(3) + IT(3) - overlaps with Dept-A
    }

    #endregion

    #region 2. 字符串操作表达式

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_StringContains_ShouldWork()
    {
        // name.Contains("a")
        var result = await _repo.GetByNameContainsAsync("a");
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0);
        Assert.IsTrue(result.All(x => x.name.Contains("a", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_StringStartsWith_ShouldWork()
    {
        // name.StartsWith("A")
        var result = await _repo.GetByNameStartsWithAsync("A");
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Any(x => x.name.StartsWith("A")));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_StringEndsWith_ShouldWork()
    {
        // email.EndsWith("@test.com")
        var result = await _repo.GetByEmailEndsWithAsync("@test.com");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Count); // All emails
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_StringLength_ShouldWork()
    {
        // name.Length > 5
        var result = await _repo.GetByNameLengthAsync(5);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.name.Length > 5));
    }

    #endregion

    #region 3. 数学运算表达式

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_MathAddition_ShouldWork()
    {
        // age + 5 > 35
        var result = await _repo.GetByAgeAdditionAsync(5, 35);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.age + 5 > 35));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_MathMultiplication_ShouldWork()
    {
        // salary * 1.1 > 60000
        var result = await _repo.GetBySalaryMultiplyAsync(1.1, 60000.0);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.salary * 1.1 > 60000.0));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_MathModulo_ShouldWork()
    {
        // age % 2 = 0 (even ages)
        var result = await _repo.GetByAgeModuloAsync(2, 0);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.age % 2 == 0));
    }

    #endregion

    #region 4. 比较运算表达式

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_BetweenCondition_ShouldWork()
    {
        // age >= 28 AND age <= 35
        var result = await _repo.GetByAgeBetweenAsync(28, 35);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.age >= 28 && x.age <= 35));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_NotEqual_ShouldWork()
    {
        // category != 'Sales'
        var result = await _repo.GetByCategoryNotEqualAsync("Sales");
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.category != "Sales"));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_NegatedCondition_ShouldWork()
    {
        // !(is_active = 0) => is_active = 1
        var result = await _repo.GetByNegatedActiveAsync();
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.is_active));
    }

    #endregion

    #region 5. 复合条件组合

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_ComplexBusinessLogic_ShouldWork()
    {
        // (age > 30 AND salary > 60000) OR (is_active = 1 AND score > 85 AND category = 'IT')
        var result = await _repo.GetByComplexBusinessLogicAsync();
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0);
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_ThreeLayerNesting_ShouldWork()
    {
        // ((age > 25 AND salary > 45000) OR (score > 80)) AND is_active = 1
        var result = await _repo.GetByThreeLayerNestingAsync();
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.is_active));
    }

    [TestMethod]
    [TestCategory("ExpressionComplex")]
    public async Task Expression_MixedOperators_ShouldWork()
    {
        // (age * 2 > 60) AND (salary / 1000 < 60) AND (score >= 85)
        var result = await _repo.GetByMixedOperatorsAsync();
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(x => x.age * 2 > 60 && x.salary / 1000 < 60 && x.score >= 85));
    }

    #endregion
}
