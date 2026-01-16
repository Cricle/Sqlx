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
        var sql = SqlQuery<Customer>.ForSqlite().ToSql();
        
        Assert.IsTrue(sql.Contains("SELECT [id], [name], [email]"));
        Assert.IsTrue(sql.Contains("FROM [Customer]"));
        Assert.IsFalse(sql.Contains("JOIN"));
    }

    [TestMethod]
    public void Regular_WithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Customer>.ForSqlite()
            .Where(c => c.Id > 10)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[id] > 10"));
    }

    [TestMethod]
    public void Regular_WithOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Customer>.ForSqlite()
            .OrderBy(c => c.Name)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("ORDER BY [name] ASC"));
    }

    [TestMethod]
    public void Regular_WithPagination_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Customer>.ForSqlite()
            .Skip(10)
            .Take(20)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("LIMIT 20"));
        Assert.IsTrue(sql.Contains("OFFSET 10"));
    }

    [TestMethod]
    public void Regular_ComplexQuery_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Customer>.ForSqlite()
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
        var sql = SqlQuery<Order>.ForSqlite()
            .GroupBy(o => o.CustomerId)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("GROUP BY [customer_id]"));
    }

    [TestMethod]
    public void Grouped_WithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Order>.ForSqlite()
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
        var sql = SqlQuery<Order>.ForSqlite()
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key })
            .ToSql();
        
        Assert.IsTrue(sql.Contains("GROUP BY [customer_id]"));
    }

    [TestMethod]
    public void Grouped_ComplexQuery_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Order>.ForSqlite()
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        var orders = SqlQuery<Order>.ForSqlite();
        var customers = SqlQuery<Customer>.ForSqlite();
        var orderItems = SqlQuery<OrderItem>.ForSqlite();
        
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        "SQLite" => SqlQuery<T>.ForSqlite(),
        "SqlServer" => SqlQuery<T>.ForSqlServer(),
        "MySql" => SqlQuery<T>.ForMySql(),
        "PostgreSQL" => SqlQuery<T>.ForPostgreSQL(),
        "Oracle" => SqlQuery<T>.ForOracle(),
        "DB2" => SqlQuery<T>.ForDB2(),
        _ => throw new System.ArgumentException($"Unknown dialect: {dialect}")
    };

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Join_EmptyResult_GeneratesCorrectSql()
    {
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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
        var customers = SqlQuery<Customer>.ForSqlite();
        var orders = SqlQuery<Order>.ForSqlite();
        
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

