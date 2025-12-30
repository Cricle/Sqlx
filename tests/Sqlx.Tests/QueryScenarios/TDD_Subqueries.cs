using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.QueryScenarios;

/// <summary>
/// TDD: 子查询测试 - WHERE、SELECT、FROM子查询
/// Phase 2.2: 9个子查询测试
/// </summary>
[TestClass]
public class TDD_Subqueries
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Subquery")]
    [TestCategory("Phase2")]
    public void Subquery_InWhereClause_ShouldFilterCorrectly()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE employees (id INTEGER PRIMARY KEY, name TEXT, salary REAL);
            CREATE TABLE departments (id INTEGER PRIMARY KEY, avg_salary REAL);
        ");

        connection.Execute("INSERT INTO employees VALUES (1, 'Alice', 5000), (2, 'Bob', 3000), (3, 'Charlie', 4000)");
        connection.Execute("INSERT INTO departments VALUES (1, 3500)");

        var repo = new SubqueryTestRepository(connection);

        // Act: 查找工资高于平均工资的员工
        var results = repo.GetHighSalaryEmployeesAsync().Result;

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(e => e.Name == "Alice"));

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Subquery")]
    [TestCategory("Phase2")]
    public void Subquery_WithIN_ShouldMatchMultipleValues()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE orders (id INTEGER PRIMARY KEY, customer_id INTEGER, amount REAL);
            CREATE TABLE vip_customers (customer_id INTEGER);
        ");

        connection.Execute("INSERT INTO orders VALUES (1, 1, 100), (2, 2, 200), (3, 3, 50)");
        connection.Execute("INSERT INTO vip_customers VALUES (1), (2)");

        var repo = new SubqueryTestRepository(connection);

        // Act: 查找VIP客户的订单
        var results = repo.GetVIPOrdersAsync().Result;

        // Assert
        Assert.AreEqual(2, results.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Subquery")]
    [TestCategory("Phase2")]
    public void Subquery_WithEXISTS_ShouldCheckExistence()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE customers (id INTEGER PRIMARY KEY, name TEXT);
            CREATE TABLE orders (id INTEGER PRIMARY KEY, customer_id INTEGER);
        ");

        connection.Execute("INSERT INTO customers VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Charlie')");
        connection.Execute("INSERT INTO orders VALUES (1, 1), (2, 1), (3, 2)");

        var repo = new SubqueryTestRepository(connection);

        // Act: 查找有订单的客户
        var results = repo.GetCustomersWithOrdersAsync().Result;

        // Assert
        Assert.AreEqual(2, results.Count);

        connection.Dispose();
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Subquery")]
    [TestCategory("Phase2")]
    public void Subquery_Nested_TwoLevels_ShouldWork()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute("CREATE TABLE nums (value INTEGER)");
        connection.Execute("INSERT INTO nums VALUES (1), (2), (3), (4), (5)");

        var repo = new SubqueryTestRepository(connection);

        // Act: 二层嵌套子查询
        var results = repo.GetNestedTwoLevelsAsync().Result;

        // Assert
        Assert.IsTrue(results.Count > 0);

        connection.Dispose();
    }
}

// Models
public class SubqueryEmployee
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public double Salary { get; set; }
}

public class SubqueryOrder
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public double Amount { get; set; }
}

public class SubqueryCustomer
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

public class SubqueryNum
{
    public int Value { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISubqueryTestRepository))]
public partial class SubqueryTestRepository(IDbConnection connection) : ISubqueryTestRepository { }

public interface ISubqueryTestRepository
{
    [SqlTemplate("SELECT id, name, salary FROM employees WHERE salary > (SELECT avg_salary FROM departments LIMIT 1)")]
    Task<List<SubqueryEmployee>> GetHighSalaryEmployeesAsync();

    [SqlTemplate("SELECT id, customer_id, amount FROM orders WHERE customer_id IN (SELECT customer_id FROM vip_customers)")]
    Task<List<SubqueryOrder>> GetVIPOrdersAsync();

    [SqlTemplate("SELECT id, name FROM customers WHERE EXISTS (SELECT 1 FROM orders WHERE orders.customer_id = customers.id)")]
    Task<List<SubqueryCustomer>> GetCustomersWithOrdersAsync();

    [SqlTemplate("SELECT value FROM nums WHERE value > (SELECT AVG(value) FROM (SELECT value FROM nums WHERE value > 2))")]
    Task<List<SubqueryNum>> GetNestedTwoLevelsAsync();
}


