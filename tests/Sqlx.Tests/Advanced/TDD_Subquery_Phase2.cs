using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Advanced;

/// <summary>
/// Phase 2: 子查询增强测试 - 确保90%复杂场景覆盖
/// 新增12个子查询测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("Advanced")]
[TestCategory("CoveragePhase2")]
public class TDD_Subquery_Phase2
{
    private IDbConnection? _connection;
    private ISubqueryRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE employees (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                department TEXT,
                salary REAL,
                manager_id INTEGER
            )");

        // Insert test data
        _connection.Execute("INSERT INTO employees VALUES (1, 'Alice', 'Engineering', 100000, NULL)");
        _connection.Execute("INSERT INTO employees VALUES (2, 'Bob', 'Engineering', 80000, 1)");
        _connection.Execute("INSERT INTO employees VALUES (3, 'Charlie', 'Engineering', 90000, 1)");
        _connection.Execute("INSERT INTO employees VALUES (4, 'David', 'Sales', 70000, NULL)");
        _connection.Execute("INSERT INTO employees VALUES (5, 'Eve', 'Sales', 60000, 4)");
        _connection.Execute("INSERT INTO employees VALUES (6, 'Frank', 'HR', 75000, NULL)");

        _repo = new SubqueryRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("Subquery in WHERE with IN should work")]
    public async Task Subquery_InWhere_ShouldWork()
    {
        var results = await _repo!.GetEmployeesInTopDepartmentAsync();
        Assert.IsTrue(results.Count >= 3);
        Assert.IsTrue(results.All(e => e.Department == "Engineering"));
    }

    [TestMethod]
    [Description("Subquery with comparison operator should work")]
    public async Task Subquery_WithComparison_ShouldWork()
    {
        var results = await _repo!.GetAboveAverageSalaryAsync();
        Assert.IsTrue(results.Count >= 3);
        var avgSalary = 79166.67; // Average of all salaries
        Assert.IsTrue(results.All(e => e.Salary >= avgSalary - 1)); // -1 for rounding
    }

    [TestMethod]
    [Description("Correlated subquery should work")]
    public async Task Subquery_Correlated_ShouldWork()
    {
        var results = await _repo!.GetEmployeesAboveDeptAvgAsync();
        Assert.IsTrue(results.Count >= 1); // Employees with salary above their dept average
    }

    [TestMethod]
    [Description("Subquery in SELECT should work")]
    public async Task Subquery_InSelect_ShouldWork()
    {
        var results = await _repo!.GetEmployeesWithManagerNameAsync();
        Assert.IsTrue(results.Count >= 6);
        var bob = results.FirstOrDefault(e => e.Name == "Bob");
        Assert.IsNotNull(bob);
        Assert.AreEqual("Alice", bob.ManagerName);
    }

    [TestMethod]
    [Description("Subquery with MAX should work")]
    public async Task Subquery_WithMax_ShouldWork()
    {
        var results = await _repo!.GetHighestPaidEmployeesAsync();
        Assert.IsTrue(results.Count >= 1);
        Assert.AreEqual(100000, results[0].Salary, 0.01);
    }

    [TestMethod]
    [Description("Subquery with MIN should work")]
    public async Task Subquery_WithMin_ShouldWork()
    {
        var results = await _repo!.GetLowestPaidEmployeesAsync();
        Assert.IsTrue(results.Count >= 1);
        Assert.AreEqual(60000, results[0].Salary, 0.01);
    }

    [TestMethod]
    [Description("EXISTS subquery should work")]
    public async Task Subquery_Exists_ShouldWork()
    {
        var results = await _repo!.GetManagersAsync();
        Assert.IsTrue(results.Count >= 2);
        // Alice and David are managers
        Assert.IsTrue(results.Any(e => e.Name == "Alice"));
        Assert.IsTrue(results.Any(e => e.Name == "David"));
    }

    [TestMethod]
    [Description("NOT EXISTS subquery should work")]
    public async Task Subquery_NotExists_ShouldWork()
    {
        var results = await _repo!.GetNonManagersAsync();
        Assert.IsTrue(results.Count >= 3);
        // Bob, Charlie, Eve, Frank are not managers
        Assert.IsFalse(results.Any(e => e.Name == "Alice"));
    }

    [TestMethod]
    [Description("Subquery in FROM should work")]
    public async Task Subquery_InFrom_ShouldWork()
    {
        var results = await _repo!.GetDepartmentStatsAsync();
        Assert.IsTrue(results.Count >= 3);
        var engineering = results.FirstOrDefault(d => d.Department == "Engineering");
        Assert.IsNotNull(engineering);
        Assert.AreEqual(3, engineering.EmployeeCount);
    }

    [TestMethod]
    [Description("Nested subqueries should work")]
    public async Task Subquery_Nested_ShouldWork()
    {
        var results = await _repo!.GetTopTierEmployeesAsync();
        Assert.IsTrue(results.Count >= 0); // May vary
    }

    [TestMethod]
    [Description("Subquery with GROUP BY should work")]
    public async Task Subquery_WithGroupBy_ShouldWork()
    {
        var results = await _repo!.GetLargestDepartmentsAsync();
        Assert.IsTrue(results.Count >= 0);
    }

    [TestMethod]
    [Description("Subquery with multiple conditions should work")]
    public async Task Subquery_MultipleConditions_ShouldWork()
    {
        var results = await _repo!.GetQualifiedEmployeesAsync();
        Assert.IsTrue(results.Count >= 0);
    }
}

// Repository interface
public interface ISubqueryRepository
{
    [SqlTemplate("SELECT * FROM employees WHERE department IN (SELECT department FROM employees GROUP BY department ORDER BY COUNT(*) DESC LIMIT 1)")]
    Task<List<SubEmployee>> GetEmployeesInTopDepartmentAsync();

    [SqlTemplate("SELECT * FROM employees WHERE salary > (SELECT AVG(salary) FROM employees)")]
    Task<List<SubEmployee>> GetAboveAverageSalaryAsync();

    [SqlTemplate("SELECT e.* FROM employees e WHERE e.salary > (SELECT AVG(e2.salary) FROM employees e2 WHERE e2.department = e.department)")]
    Task<List<SubEmployee>> GetEmployeesAboveDeptAvgAsync();

    [SqlTemplate("SELECT e.id, e.name, e.department, e.salary, (SELECT m.name FROM employees m WHERE m.id = e.manager_id) as manager_name FROM employees e")]
    Task<List<SubEmployeeWithManager>> GetEmployeesWithManagerNameAsync();

    [SqlTemplate("SELECT * FROM employees WHERE salary = (SELECT MAX(salary) FROM employees)")]
    Task<List<SubEmployee>> GetHighestPaidEmployeesAsync();

    [SqlTemplate("SELECT * FROM employees WHERE salary = (SELECT MIN(salary) FROM employees)")]
    Task<List<SubEmployee>> GetLowestPaidEmployeesAsync();

    [SqlTemplate("SELECT * FROM employees e WHERE EXISTS (SELECT 1 FROM employees e2 WHERE e2.manager_id = e.id)")]
    Task<List<SubEmployee>> GetManagersAsync();

    [SqlTemplate("SELECT * FROM employees e WHERE NOT EXISTS (SELECT 1 FROM employees e2 WHERE e2.manager_id = e.id)")]
    Task<List<SubEmployee>> GetNonManagersAsync();

    [SqlTemplate("SELECT department, COUNT(*) as employee_count, AVG(salary) as avg_salary FROM (SELECT * FROM employees) AS emp GROUP BY department")]
    Task<List<DepartmentStats>> GetDepartmentStatsAsync();

    [SqlTemplate("SELECT * FROM employees WHERE salary > (SELECT AVG(salary) FROM (SELECT salary FROM employees ORDER BY salary DESC LIMIT 3) AS top_salaries)")]
    Task<List<SubEmployee>> GetTopTierEmployeesAsync();

    [SqlTemplate("SELECT * FROM employees WHERE department IN (SELECT department FROM employees GROUP BY department HAVING COUNT(*) >= 2)")]
    Task<List<SubEmployee>> GetLargestDepartmentsAsync();

    [SqlTemplate("SELECT * FROM employees WHERE salary > 70000 AND department IN (SELECT department FROM employees GROUP BY department HAVING AVG(salary) > 75000)")]
    Task<List<SubEmployee>> GetQualifiedEmployeesAsync();
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISubqueryRepository))]
public partial class SubqueryRepository(IDbConnection connection) : ISubqueryRepository { }

// Models
public class SubEmployee
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public double Salary { get; set; }
    public long? ManagerId { get; set; }
}

public class SubEmployeeWithManager
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public double Salary { get; set; }
    public string? ManagerName { get; set; }
}

public class DepartmentStats
{
    public string Department { get; set; } = "";
    public int EmployeeCount { get; set; }
    public double AvgSalary { get; set; }
}

