// -----------------------------------------------------------------------
// <copyright file="UnionOperations.cs" company="Cricle">
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
/// E2E tests for UNION operations.
/// **Validates: Requirements 3.5**
/// </summary>

#region Data Models

public class Contact
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

#endregion

#region Repository Interfaces

public partial interface ICustomerContactRepository
{
    [SqlTemplate("INSERT INTO customer_contacts (name, email) VALUES (@name, @email)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string email);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Contact?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id as Id, name as Name, email as Email, 'Customer' as Type FROM customer_contacts ORDER BY name")]
    Task<List<Contact>> GetAllAsync();
}

public partial interface ISupplierContactRepository
{
    [SqlTemplate("INSERT INTO supplier_contacts (name, email) VALUES (@name, @email)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string email);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Contact?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id as Id, name as Name, email as Email, 'Supplier' as Type FROM supplier_contacts ORDER BY name")]
    Task<List<Contact>> GetAllAsync();
}

public partial interface IUnifiedContactRepository
{
    [SqlTemplate("SELECT id as Id, name as Name, email as Email, 'Customer' as Type FROM customer_contacts UNION SELECT id as Id, name as Name, email as Email, 'Supplier' as Type FROM supplier_contacts ORDER BY name")]
    Task<List<Dictionary<string, object?>>> GetAllContactsUnionAsync();

    [SqlTemplate("SELECT id as Id, name as Name, email as Email, 'Customer' as Type FROM customer_contacts UNION ALL SELECT id as Id, name as Name, email as Email, 'Supplier' as Type FROM supplier_contacts ORDER BY name")]
    Task<List<Dictionary<string, object?>>> GetAllContactsUnionAllAsync();

    [SqlTemplate("SELECT name as Name, email as Email FROM customer_contacts WHERE email LIKE @pattern UNION SELECT name as Name, email as Email FROM supplier_contacts WHERE email LIKE @pattern ORDER BY name")]
    Task<List<Dictionary<string, object?>>> GetExampleDomainContactsAsync(string pattern);
}

#endregion

#region Repository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("customer_contacts")]
[RepositoryFor(typeof(ICustomerContactRepository))]
public partial class CustomerContactRepository(DbConnection connection) : ICustomerContactRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("supplier_contacts")]
[RepositoryFor(typeof(ISupplierContactRepository))]
public partial class SupplierContactRepository(DbConnection connection) : ISupplierContactRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("customer_contacts")] // Dummy table name since UNION queries don't use {{table}}
[RepositoryFor(typeof(IUnifiedContactRepository))]
public partial class UnifiedContactRepository(DbConnection connection) : IUnifiedContactRepository { }

#endregion

#region SQLite Implementation

[TestClass]
[TestCategory("E2E")]
[TestCategory("Advanced")]
[TestCategory("Union")]
[TestCategory("SQLite")]
public class UnionOperations_SQLite
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
            CREATE TABLE IF NOT EXISTS customer_contacts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS supplier_contacts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE
            );

            CREATE INDEX IF NOT EXISTS idx_customer_contacts_email ON customer_contacts(email);
            CREATE INDEX IF NOT EXISTS idx_supplier_contacts_email ON supplier_contacts(email);
        ";

        cmd.ExecuteNonQuery();
    }

    [TestMethod]
    public async Task Union_CompleteWorkflow_RemovesDuplicates()
    {
        // Arrange: Create contacts in both tables, including duplicates
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var customerRepo = new CustomerContactRepository(connection);
        var supplierRepo = new SupplierContactRepository(connection);
        var unifiedRepo = new UnifiedContactRepository(connection);

        // Create customer contacts
        var cust1 = await customerRepo.CreateAsync("Alice Johnson", "alice@example.com");
        var cust2 = await customerRepo.CreateAsync("Bob Smith", "bob@example.com");
        var cust3 = await customerRepo.CreateAsync("Charlie Brown", "charlie@example.com");

        // Create supplier contacts (Bob appears in both - same name but different email due to UNIQUE constraint)
        var supp1 = await supplierRepo.CreateAsync("Alice Johnson", "alice.supplier@example.com"); // Same name, different email
        var supp2 = await supplierRepo.CreateAsync("David Wilson", "david@example.com");
        var supp3 = await supplierRepo.CreateAsync("Eve Davis", "eve@example.com");

        // Verify data was created
        Assert.IsTrue(cust1 > 0, "Customer 1 should be created");
        Assert.IsTrue(cust2 > 0, "Customer 2 should be created");
        Assert.IsTrue(cust3 > 0, "Customer 3 should be created");
        Assert.IsTrue(supp1 > 0, "Supplier 1 should be created");
        Assert.IsTrue(supp2 > 0, "Supplier 2 should be created");
        Assert.IsTrue(supp3 > 0, "Supplier 3 should be created");

        // Verify individual tables have data
        var allCustomers = await customerRepo.GetAllAsync();
        var allSuppliers = await supplierRepo.GetAllAsync();
        Assert.AreEqual(3, allCustomers.Count, "Should have 3 customers");
        Assert.AreEqual(3, allSuppliers.Count, "Should have 3 suppliers");

        // Act: Query with UNION (removes duplicates based on all columns)
        var unionResults = await unifiedRepo.GetAllContactsUnionAsync();

        // Assert: UNION should return distinct records
        // Since emails are different, all 6 records should appear (no duplicates to remove)
        Assert.AreEqual(6, unionResults.Count, "Should have 6 distinct contacts");

        // Verify we have both customers and suppliers
        var customers = unionResults.Where(c => c["Type"]?.ToString() == "Customer").ToList();
        var suppliers = unionResults.Where(c => c["Type"]?.ToString() == "Supplier").ToList();

        Assert.AreEqual(3, customers.Count, "Should have 3 customers");
        Assert.AreEqual(3, suppliers.Count, "Should have 3 suppliers");

        // Verify ordering by name
        var names = unionResults.Select(c => c["Name"]?.ToString()).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        CollectionAssert.AreEqual(sortedNames, names, "Results should be ordered by name");

        // Verify specific contacts exist
        Assert.IsTrue(unionResults.Any(c => c["Name"]?.ToString() == "Alice Johnson" && c["Type"]?.ToString() == "Customer"), 
            "Should include Alice as customer");
        Assert.IsTrue(unionResults.Any(c => c["Name"]?.ToString() == "Alice Johnson" && c["Type"]?.ToString() == "Supplier"), 
            "Should include Alice as supplier");
        Assert.IsTrue(unionResults.Any(c => c["Name"]?.ToString() == "David Wilson" && c["Type"]?.ToString() == "Supplier"), 
            "Should include David as supplier");
    }

    [TestMethod]
    public async Task UnionAll_CompleteWorkflow_IncludesDuplicates()
    {
        // Arrange: Create identical records in both tables to test UNION ALL
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var customerRepo = new CustomerContactRepository(connection);
        var supplierRepo = new SupplierContactRepository(connection);
        var unifiedRepo = new UnifiedContactRepository(connection);

        // Create customer contacts
        await customerRepo.CreateAsync("Alice Johnson", "alice@example.com");
        await customerRepo.CreateAsync("Bob Smith", "bob@example.com");

        // Create supplier contacts
        await supplierRepo.CreateAsync("Charlie Brown", "charlie@example.com");
        await supplierRepo.CreateAsync("David Wilson", "david@example.com");

        // Act: Query with UNION ALL (includes all records, even duplicates)
        var unionAllResults = await unifiedRepo.GetAllContactsUnionAllAsync();

        // Assert: UNION ALL should return all records
        Assert.AreEqual(4, unionAllResults.Count, "Should have 4 total contacts (no deduplication)");

        // Verify we have both customers and suppliers
        var customers = unionAllResults.Where(c => c["Type"]?.ToString() == "Customer").ToList();
        var suppliers = unionAllResults.Where(c => c["Type"]?.ToString() == "Supplier").ToList();

        Assert.AreEqual(2, customers.Count, "Should have 2 customers");
        Assert.AreEqual(2, suppliers.Count, "Should have 2 suppliers");

        // Verify ordering by name
        var names = unionAllResults.Select(c => c["Name"]?.ToString()).ToList();
        Assert.AreEqual("Alice Johnson", names[0], "First should be Alice");
        Assert.AreEqual("Bob Smith", names[1], "Second should be Bob");
        Assert.AreEqual("Charlie Brown", names[2], "Third should be Charlie");
        Assert.AreEqual("David Wilson", names[3], "Fourth should be David");
    }

    [TestMethod]
    public async Task UnionWithFiltering_CompleteWorkflow_CombinesFilteredResults()
    {
        // Arrange: Create contacts with different email domains
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var customerRepo = new CustomerContactRepository(connection);
        var supplierRepo = new SupplierContactRepository(connection);
        var unifiedRepo = new UnifiedContactRepository(connection);

        // Create customer contacts with different domains
        await customerRepo.CreateAsync("Alice Johnson", "alice@example.com");
        await customerRepo.CreateAsync("Bob Smith", "bob@company.com");
        await customerRepo.CreateAsync("Charlie Brown", "charlie@example.com");

        // Create supplier contacts with different domains
        await supplierRepo.CreateAsync("David Wilson", "david@example.com");
        await supplierRepo.CreateAsync("Eve Davis", "eve@business.com");
        await supplierRepo.CreateAsync("Frank Miller", "frank@example.com");

        // Act: Query with UNION and WHERE filtering (only @example.com)
        var exampleDomainContacts = await unifiedRepo.GetExampleDomainContactsAsync("%@example.com");

        // Assert: Should only return contacts with @example.com domain
        Assert.AreEqual(4, exampleDomainContacts.Count, "Should have 4 contacts with @example.com domain");

        // Verify all emails end with @example.com
        Assert.IsTrue(exampleDomainContacts.All(c => c["Email"]?.ToString()?.EndsWith("@example.com") == true),
            "All contacts should have @example.com email");

        // Verify specific contacts
        var contactNames = exampleDomainContacts.Select(c => c["Name"]?.ToString()).ToList();
        Assert.IsTrue(contactNames.Contains("Alice Johnson"), "Should include Alice");
        Assert.IsTrue(contactNames.Contains("Charlie Brown"), "Should include Charlie");
        Assert.IsTrue(contactNames.Contains("David Wilson"), "Should include David");
        Assert.IsTrue(contactNames.Contains("Frank Miller"), "Should include Frank");

        // Verify excluded contacts
        Assert.IsFalse(contactNames.Contains("Bob Smith"), "Should not include Bob (company.com)");
        Assert.IsFalse(contactNames.Contains("Eve Davis"), "Should not include Eve (business.com)");

        // Verify ordering
        Assert.AreEqual("Alice Johnson", contactNames[0], "First should be Alice");
        Assert.AreEqual("Charlie Brown", contactNames[1], "Second should be Charlie");
        Assert.AreEqual("David Wilson", contactNames[2], "Third should be David");
        Assert.AreEqual("Frank Miller", contactNames[3], "Fourth should be Frank");
    }
}

#endregion
