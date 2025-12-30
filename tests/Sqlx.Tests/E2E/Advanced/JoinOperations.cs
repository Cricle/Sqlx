// -----------------------------------------------------------------------
// <copyright file="JoinOperations.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Advanced;

/// <summary>
/// E2E tests for JOIN operations.
/// **Validates: Requirements 3.1**
/// </summary>

#region Data Models

public class Customer
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Purchase
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PurchaseDate { get; set; }
}

public class CustomerWithPurchases
{
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public long? PurchaseId { get; set; }
    public string? ProductName { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? PurchaseDate { get; set; }
}

#endregion

#region Repository Interfaces

public partial interface ICustomerRepository
{
    [SqlTemplate("INSERT INTO customers (name, email, created_at) VALUES (@name, @email, @createdAt)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string email, DateTime createdAt);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Customer?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY name")]
    Task<List<Customer>> GetAllAsync();
}

public partial interface IPurchaseRepository
{
    [SqlTemplate("INSERT INTO purchases (customer_id, product_name, amount, purchase_date) VALUES (@customerId, @productName, @amount, @purchaseDate)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(long customerId, string productName, decimal amount, DateTime purchaseDate);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE customer_id = @customerId ORDER BY purchase_date DESC")]
    Task<List<Purchase>> GetByCustomerIdAsync(long customerId);

    [SqlTemplate(@"
        SELECT 
            c.id as CustomerId,
            c.name as CustomerName,
            c.email as CustomerEmail,
            p.id as PurchaseId,
            p.product_name as ProductName,
            p.amount as Amount,
            p.purchase_date as PurchaseDate
        FROM customers c
        INNER JOIN purchases p ON c.id = p.customer_id
        WHERE c.id = @customerId
        ORDER BY p.purchase_date DESC")]
    Task<List<Dictionary<string, object?>>> GetCustomerWithPurchasesRawAsync(long customerId);

    [SqlTemplate(@"
        SELECT 
            c.id as CustomerId,
            c.name as CustomerName,
            c.email as CustomerEmail,
            p.id as PurchaseId,
            p.product_name as ProductName,
            p.amount as Amount,
            p.purchase_date as PurchaseDate
        FROM customers c
        LEFT JOIN purchases p ON c.id = p.customer_id
        ORDER BY c.name, p.purchase_date DESC")]
    Task<List<Dictionary<string, object?>>> GetAllCustomersWithPurchasesRawAsync();
}

#endregion

#region Repository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("customers")]
[RepositoryFor(typeof(ICustomerRepository))]
public partial class CustomerRepository(DbConnection connection) : ICustomerRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("purchases")]
[RepositoryFor(typeof(IPurchaseRepository))]
public partial class PurchaseRepository(DbConnection connection) : IPurchaseRepository { }

#endregion

#region SQLite Implementation

[TestClass]
[TestCategory("E2E")]
[TestCategory("Advanced")]
[TestCategory("JoinOperations")]
[TestCategory("SQLite")]
public class JoinOperations_SQLite
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
        CreateTables();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    private void CreateTables()
    {
        var conn = _fixture.GetConnection(SqlDefineTypes.SQLite);
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS purchases (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_id INTEGER NOT NULL,
                product_name TEXT NOT NULL,
                amount REAL NOT NULL,
                purchase_date TEXT NOT NULL,
                FOREIGN KEY (customer_id) REFERENCES customers(id)
            );

            CREATE INDEX IF NOT EXISTS idx_purchases_customer_id ON purchases(customer_id);
            CREATE INDEX IF NOT EXISTS idx_purchases_date ON purchases(purchase_date);
        ";

        cmd.ExecuteNonQuery();
    }

    [TestMethod]
    public async Task InnerJoin_CompleteWorkflow_ReturnsOnlyMatchingRecords()
    {
        // Arrange: Create customers and purchases
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var customerRepo = new CustomerRepository(connection);
        var purchaseRepo = new PurchaseRepository(connection);

        // Create customers
        var customer1Id = await customerRepo.CreateAsync("Alice Johnson", "alice@example.com", DateTime.UtcNow);
        var customer2Id = await customerRepo.CreateAsync("Bob Smith", "bob@example.com", DateTime.UtcNow);
        var customer3Id = await customerRepo.CreateAsync("Charlie Brown", "charlie@example.com", DateTime.UtcNow);

        // Create purchases for customer1 and customer2 only (customer3 has no purchases)
        await purchaseRepo.CreateAsync(customer1Id, "Laptop", 1200.00m, DateTime.UtcNow.AddDays(-5));
        await purchaseRepo.CreateAsync(customer1Id, "Mouse", 25.00m, DateTime.UtcNow.AddDays(-3));
        await purchaseRepo.CreateAsync(customer2Id, "Keyboard", 75.00m, DateTime.UtcNow.AddDays(-2));

        // Act: Query with INNER JOIN
        var customer1Purchases = await purchaseRepo.GetCustomerWithPurchasesRawAsync(customer1Id);
        var customer2Purchases = await purchaseRepo.GetCustomerWithPurchasesRawAsync(customer2Id);
        var customer3Purchases = await purchaseRepo.GetCustomerWithPurchasesRawAsync(customer3Id);

        // Assert: INNER JOIN only returns customers with purchases
        Assert.AreEqual(2, customer1Purchases.Count, "Customer 1 should have 2 purchases");
        Assert.IsTrue(customer1Purchases.All(p => Convert.ToInt64(p["CustomerId"]) == customer1Id), "All records should belong to customer 1");
        Assert.IsTrue(customer1Purchases.All(p => p["PurchaseId"] != null), "All records should have purchase data");
        Assert.IsTrue(customer1Purchases.Any(p => p["ProductName"]?.ToString() == "Laptop"), "Should include Laptop purchase");
        Assert.IsTrue(customer1Purchases.Any(p => p["ProductName"]?.ToString() == "Mouse"), "Should include Mouse purchase");

        Assert.AreEqual(1, customer2Purchases.Count, "Customer 2 should have 1 purchase");
        Assert.AreEqual("Keyboard", customer2Purchases[0]["ProductName"]?.ToString(), "Should be Keyboard purchase");

        Assert.AreEqual(0, customer3Purchases.Count, "Customer 3 should have no results (no purchases)");
    }

    [TestMethod]
    public async Task LeftJoin_CompleteWorkflow_ReturnsAllLeftRecordsWithNullHandling()
    {
        // Arrange: Create customers with varying purchase histories
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var customerRepo = new CustomerRepository(connection);
        var purchaseRepo = new PurchaseRepository(connection);

        // Create customers
        var customer1Id = await customerRepo.CreateAsync("Alice Johnson", "alice@example.com", DateTime.UtcNow);
        var customer2Id = await customerRepo.CreateAsync("Bob Smith", "bob@example.com", DateTime.UtcNow);
        var customer3Id = await customerRepo.CreateAsync("Charlie Brown", "charlie@example.com", DateTime.UtcNow);

        // Create purchases for customer1 and customer2 only
        await purchaseRepo.CreateAsync(customer1Id, "Laptop", 1200.00m, DateTime.UtcNow.AddDays(-5));
        await purchaseRepo.CreateAsync(customer1Id, "Mouse", 25.00m, DateTime.UtcNow.AddDays(-3));
        await purchaseRepo.CreateAsync(customer2Id, "Keyboard", 75.00m, DateTime.UtcNow.AddDays(-2));

        // Act: Query with LEFT JOIN
        var allCustomersWithPurchases = await purchaseRepo.GetAllCustomersWithPurchasesRawAsync();

        // Assert: LEFT JOIN returns all customers, even those without purchases
        Assert.IsTrue(allCustomersWithPurchases.Count >= 3, "Should have at least 3 records (one per customer minimum)");

        // Group by customer to analyze results
        var groupedByCustomer = allCustomersWithPurchases.GroupBy(p => Convert.ToInt64(p["CustomerId"])).ToList();
        Assert.AreEqual(3, groupedByCustomer.Count, "Should have 3 distinct customers");

        // Verify customer1 (has 2 purchases)
        var customer1Records = groupedByCustomer.First(g => g.Key == customer1Id).ToList();
        Assert.AreEqual(2, customer1Records.Count, "Customer 1 should have 2 purchase records");
        Assert.IsTrue(customer1Records.All(r => r["PurchaseId"] != null), "Customer 1 records should have purchase data");

        // Verify customer2 (has 1 purchase)
        var customer2Records = groupedByCustomer.First(g => g.Key == customer2Id).ToList();
        Assert.AreEqual(1, customer2Records.Count, "Customer 2 should have 1 purchase record");
        Assert.IsTrue(customer2Records.All(r => r["PurchaseId"] != null), "Customer 2 records should have purchase data");

        // Verify customer3 (has no purchases - NULL handling)
        var customer3Records = groupedByCustomer.First(g => g.Key == customer3Id).ToList();
        Assert.AreEqual(1, customer3Records.Count, "Customer 3 should have 1 record (with NULL purchase data)");
        Assert.IsTrue(customer3Records[0]["PurchaseId"] == null || customer3Records[0]["PurchaseId"] is DBNull, "Customer 3 should have NULL purchase ID");
        Assert.IsTrue(customer3Records[0]["ProductName"] == null || customer3Records[0]["ProductName"] is DBNull, "Customer 3 should have NULL product name");
        Assert.IsTrue(customer3Records[0]["Amount"] == null || customer3Records[0]["Amount"] is DBNull, "Customer 3 should have NULL amount");
        Assert.AreEqual("Charlie Brown", customer3Records[0]["CustomerName"]?.ToString(), "Customer name should still be populated");
    }

    [TestMethod]
    public async Task JoinOperations_OrderingAndFiltering_WorksCorrectly()
    {
        // Arrange: Create test data with specific dates for ordering verification
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var customerRepo = new CustomerRepository(connection);
        var purchaseRepo = new PurchaseRepository(connection);

        var customerId = await customerRepo.CreateAsync("Test Customer", "test@example.com", DateTime.UtcNow);

        // Create purchases with specific dates (most recent first in expected results)
        var date1 = DateTime.UtcNow.AddDays(-10);
        var date2 = DateTime.UtcNow.AddDays(-5);
        var date3 = DateTime.UtcNow.AddDays(-1);

        await purchaseRepo.CreateAsync(customerId, "Product A", 100m, date1);
        await purchaseRepo.CreateAsync(customerId, "Product B", 200m, date2);
        await purchaseRepo.CreateAsync(customerId, "Product C", 300m, date3);

        // Act: Query with JOIN and ORDER BY
        var results = await purchaseRepo.GetCustomerWithPurchasesRawAsync(customerId);

        // Assert: Verify ordering (DESC by purchase_date)
        Assert.AreEqual(3, results.Count, "Should have 3 purchases");
        Assert.AreEqual("Product C", results[0]["ProductName"]?.ToString(), "Most recent purchase should be first");
        Assert.AreEqual("Product B", results[1]["ProductName"]?.ToString(), "Second most recent should be second");
        Assert.AreEqual("Product A", results[2]["ProductName"]?.ToString(), "Oldest purchase should be last");

        // Verify amounts match
        Assert.AreEqual(300m, Convert.ToDecimal(results[0]["Amount"]));
        Assert.AreEqual(200m, Convert.ToDecimal(results[1]["Amount"]));
        Assert.AreEqual(100m, Convert.ToDecimal(results[2]["Amount"]));
    }
}

#endregion
