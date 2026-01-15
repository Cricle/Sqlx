// -----------------------------------------------------------------------
// <copyright file="SqlxJoinTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests;

[SqlxEntity]
public partial class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

[SqlxEntity]
public partial class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}

[SqlxEntity]
public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

[SqlxEntity]
public partial class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Comprehensive tests for JOIN functionality.
/// Tests: Regular queries, Grouped queries, JOIN queries, and Combined queries.
/// </summary>
[TestClass]
public class SqlxJoinTests
{
    #region Regular Queries (Baseline)

    [TestMethod]
    public void Regular_SimpleSelect_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Customer>().ToSql();
        
        Assert.IsTrue(sql.Contains("SELECT [id], [name], [email]"));
        Assert.IsTrue(sql.Contains("FROM [Customer]"));
        Assert.IsFalse(sql.Contains("JOIN"));
    }

    [TestMethod]
    public void Regular_WithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Customer>()
            .Where(c => c.Id > 10)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[id] > 10"));
    }

    [TestMethod]
    public void Regular_WithOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Customer>()
            .OrderBy(c => c.Name)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("ORDER BY [name] ASC"));
    }

    [TestMethod]
    public void Regular_WithPagination_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Customer>()
            .Skip(10)
            .Take(20)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("LIMIT 20"));
        Assert.IsTrue(sql.Contains("OFFSET 10"));
    }

    [TestMethod]
    public void Regular_ComplexQuery_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Customer>()
            .Where(c => c.Name.Contains("test"))
            .OrderBy(c => c.Name)
            .ThenByDescending(c => c.Id)
            .Skip(5)
            .Take(10)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT 10"));
        Assert.IsTrue(sql.Contains("OFFSET 5"));
    }

    #endregion

    #region Grouped Queries

    [TestMethod]
    public void Grouped_SimpleGroupBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Order>()
            .GroupBy(o => o.CustomerId)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("GROUP BY [customer_id]"));
    }

    [TestMethod]
    public void Grouped_WithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Order>()
            .Where(o => o.Amount > 100)
            .GroupBy(o => o.CustomerId)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[amount] > 100"));
        Assert.IsTrue(sql.Contains("GROUP BY [customer_id]"));
    }

    [TestMethod]
    public void Grouped_WithOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Order>()
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key })
            .ToSql();
        
        Assert.IsTrue(sql.Contains("GROUP BY [customer_id]"));
    }

    [TestMethod]
    public void Grouped_ComplexQuery_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<Order>()
            .Where(o => o.Status == "completed")
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key })
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("GROUP BY"));
    }

    #endregion

    #region JOIN Queries

    [TestMethod]
    public void Join_InnerJoin_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount })
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("[Order]"));
        Assert.IsTrue(sql.Contains("ON"));
    }

    [TestMethod]
    public void Join_WithWhere_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount })
            .Where(x => x.Amount > 100)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    public void Join_WithOrderBy_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount })
            .OrderBy(x => x.Name)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    public void Join_WithPagination_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount })
            .Skip(10)
            .Take(20)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("LIMIT 20"));
        Assert.IsTrue(sql.Contains("OFFSET 10"));
    }

    [TestMethod]
    public void Join_MultipleJoins_GeneratesCorrectSql()
    {
        var orders = SqlQuery.ForSqlite<Order>();
        var customers = SqlQuery.ForSqlite<Customer>();
        var orderItems = SqlQuery.ForSqlite<OrderItem>();
        
        var sql = orders
            .Join(customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { Order = o, Customer = c })
            .Join(orderItems,
                x => x.Order.Id,
                oi => oi.OrderId,
                (x, oi) => new { x.Customer.Name, x.Order.Amount, oi.Quantity })
            .ToSql();
        
        // Should have 2 INNER JOINs
        var joinCount = sql.Split(new[] { "INNER JOIN" }, System.StringSplitOptions.None).Length - 1;
        Assert.AreEqual(2, joinCount, $"Expected 2 INNER JOINs, SQL: {sql}");
    }

    #endregion

    #region Combined Queries (JOIN + GROUP BY)

    [TestMethod]
    public void Combined_JoinWithGroupBy_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount })
            .GroupBy(x => x.Name)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("GROUP BY"));
    }

    [TestMethod]
    public void Combined_JoinWithWhereAndGroupBy_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount, o.Status })
            .Where(x => x.Status == "completed")
            .GroupBy(x => x.Name)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("GROUP BY"));
    }

    [TestMethod]
    public void Combined_FullComplexQuery_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount, o.Status })
            .Where(x => x.Amount > 100)
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key })
            .Skip(5)
            .Take(10)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"), "Should contain INNER JOIN");
        Assert.IsTrue(sql.Contains("WHERE"), "Should contain WHERE");
        Assert.IsTrue(sql.Contains("GROUP BY"), "Should contain GROUP BY");
        Assert.IsTrue(sql.Contains("LIMIT 10"), "Should contain LIMIT");
        Assert.IsTrue(sql.Contains("OFFSET 5"), "Should contain OFFSET");
    }

    #endregion

    #region Cross-Dialect JOIN Tests

    [TestMethod]
    [DataRow("SQLite", "[Customer]", "[Order]")]
    [DataRow("SqlServer", "[Customer]", "[Order]")]
    [DataRow("MySql", "`Customer`", "`Order`")]
    [DataRow("PostgreSQL", "\"Customer\"", "\"Order\"")]
    [DataRow("Oracle", "\"Customer\"", "\"Order\"")]
    [DataRow("DB2", "\"Customer\"", "\"Order\"")]
    public void Join_AllDialects_UsesCorrectQuoting(string dialect, string expectedCustomer, string expectedOrder)
    {
        var customers = GetQuery<Customer>(dialect);
        var orders = GetQuery<Order>(dialect);
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount })
            .ToSql();
        
        Assert.IsTrue(sql.Contains(expectedCustomer), $"[{dialect}] Should contain {expectedCustomer}. SQL: {sql}");
        Assert.IsTrue(sql.Contains(expectedOrder), $"[{dialect}] Should contain {expectedOrder}. SQL: {sql}");
        Assert.IsTrue(sql.Contains("INNER JOIN"), $"[{dialect}] Should contain INNER JOIN. SQL: {sql}");
    }

    private static IQueryable<T> GetQuery<T>(string dialect) => dialect switch
    {
        "SQLite" => SqlQuery.ForSqlite<T>(),
        "SqlServer" => SqlQuery.ForSqlServer<T>(),
        "MySql" => SqlQuery.ForMySql<T>(),
        "PostgreSQL" => SqlQuery.ForPostgreSQL<T>(),
        "Oracle" => SqlQuery.ForOracle<T>(),
        "DB2" => SqlQuery.ForDB2<T>(),
        _ => throw new System.ArgumentException($"Unknown dialect: {dialect}")
    };

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Join_EmptyResult_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Amount })
            .Where(x => x.Amount < 0) // Impossible condition
            .ToSql();
        
        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("WHERE"));
    }

    [TestMethod]
    public void Join_WithDistinct_GeneratesCorrectSql()
    {
        var customers = SqlQuery.ForSqlite<Customer>();
        var orders = SqlQuery.ForSqlite<Order>();
        
        var sql = customers
            .Join(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name })
            .Distinct()
            .ToSql();
        
        Assert.IsTrue(sql.Contains("SELECT DISTINCT"));
        Assert.IsTrue(sql.Contains("INNER JOIN"));
    }

    #endregion
}
