// -----------------------------------------------------------------------
// <copyright file="ConstraintTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.DataIntegrity;

/// <summary>
/// E2E tests for database constraints (foreign key, unique, check).
/// **Validates: Requirements 4.1, 4.2, 4.3**
/// </summary>

#region Data Models

public class Department
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class Employee
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public decimal Salary { get; set; }
}

public class InventoryProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

#endregion

#region Repository Interfaces

public partial interface IDepartmentRepository
{
    [SqlTemplate("INSERT INTO departments (name, code) VALUES (@name, @code)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string code);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Department?> GetByIdAsync(long id);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);
}

public partial interface IEmployeeRepository
{
    [SqlTemplate("INSERT INTO employees (name, email, department_id, salary) VALUES (@name, @email, @departmentId, @salary)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string email, long departmentId, decimal salary);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Employee?> GetByIdAsync(long id);
}

public partial interface IInventoryProductRepository
{
    [SqlTemplate("INSERT INTO inventory_products (name, sku, price, stock) VALUES (@name, @sku, @price, @stock)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string sku, decimal price, int stock);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<InventoryProduct?> GetByIdAsync(long id);
}

#endregion

#region Repository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("departments")]
[RepositoryFor(typeof(IDepartmentRepository))]
public partial class DepartmentRepository(DbConnection connection) : IDepartmentRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("employees")]
[RepositoryFor(typeof(IEmployeeRepository))]
public partial class EmployeeRepository(DbConnection connection) : IEmployeeRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("inventory_products")]
[RepositoryFor(typeof(IInventoryProductRepository))]
public partial class InventoryProductRepository(DbConnection connection) : IInventoryProductRepository { }

#endregion

#region SQLite Implementation

[TestClass]
[TestCategory("E2E")]
[TestCategory("DataIntegrity")]
[TestCategory("Constraints")]
[TestCategory("SQLite")]
public class ConstraintTests_SQLite
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
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS departments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                code TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS employees (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                department_id INTEGER NOT NULL,
                salary DECIMAL(10,2) NOT NULL CHECK (salary >= 0),
                FOREIGN KEY (department_id) REFERENCES departments(id)
            );

            CREATE TABLE IF NOT EXISTS inventory_products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                sku TEXT NOT NULL UNIQUE,
                price DECIMAL(10,2) NOT NULL CHECK (price > 0),
                stock INTEGER NOT NULL CHECK (stock >= 0)
            );
        ";

        cmd.ExecuteNonQuery();
    }

    [TestMethod]
    public async Task ForeignKeyConstraint_InvalidReference_ShouldFail()
    {
        // Arrange: Try to create employee with non-existent department
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var employeeRepo = new EmployeeRepository(connection);

        // Act & Assert: Should throw exception for foreign key violation
        var exception = await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await employeeRepo.CreateAsync("John Doe", "john@example.com", 999, 50000);
        });

        // Verify it's a foreign key constraint error
        Assert.IsTrue(exception.Message.Contains("FOREIGN KEY constraint failed") || 
                      exception.SqliteErrorCode == 787, // SQLITE_CONSTRAINT_FOREIGNKEY
            $"Expected foreign key constraint error, got: {exception.Message}");
    }

    [TestMethod]
    public async Task ForeignKeyConstraint_ValidReference_ShouldSucceed()
    {
        // Arrange: Create department first
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var deptRepo = new DepartmentRepository(connection);
        var employeeRepo = new EmployeeRepository(connection);

        var deptId = await deptRepo.CreateAsync("Engineering", "ENG");

        // Act: Create employee with valid department reference
        var employeeId = await employeeRepo.CreateAsync("Jane Smith", "jane@example.com", deptId, 60000);

        // Assert: Employee should be created successfully
        Assert.IsTrue(employeeId > 0, "Employee should be created");
        var employee = await employeeRepo.GetByIdAsync(employeeId);
        Assert.IsNotNull(employee, "Employee should exist");
        Assert.AreEqual(deptId, employee.DepartmentId, "Department ID should match");
    }

    [TestMethod]
    public async Task ForeignKeyConstraint_DeleteReferencedRow_ShouldFail()
    {
        // Arrange: Create department and employee
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var deptRepo = new DepartmentRepository(connection);
        var employeeRepo = new EmployeeRepository(connection);

        var deptId = await deptRepo.CreateAsync("Sales", "SALES");
        await employeeRepo.CreateAsync("Bob Johnson", "bob@example.com", deptId, 55000);

        // Act & Assert: Should fail to delete department with employees
        var exception = await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await deptRepo.DeleteAsync(deptId);
        });

        // Verify it's a foreign key constraint error
        Assert.IsTrue(exception.Message.Contains("FOREIGN KEY constraint failed") || 
                      exception.SqliteErrorCode == 787,
            $"Expected foreign key constraint error, got: {exception.Message}");
    }

    [TestMethod]
    public async Task UniqueConstraint_DuplicateValue_ShouldFail()
    {
        // Arrange: Create first employee
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var deptRepo = new DepartmentRepository(connection);
        var employeeRepo = new EmployeeRepository(connection);

        var deptId = await deptRepo.CreateAsync("HR", "HR");
        await employeeRepo.CreateAsync("Alice Brown", "alice@example.com", deptId, 45000);

        // Act & Assert: Should fail to create employee with duplicate email
        var exception = await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await employeeRepo.CreateAsync("Alice Smith", "alice@example.com", deptId, 50000);
        });

        // Verify it's a unique constraint error
        Assert.IsTrue(exception.Message.Contains("UNIQUE constraint failed") || 
                      exception.SqliteErrorCode == 2067, // SQLITE_CONSTRAINT_UNIQUE
            $"Expected unique constraint error, got: {exception.Message}");
    }

    [TestMethod]
    public async Task UniqueConstraint_DifferentValues_ShouldSucceed()
    {
        // Arrange: Create department
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var deptRepo = new DepartmentRepository(connection);
        var employeeRepo = new EmployeeRepository(connection);

        var deptId = await deptRepo.CreateAsync("IT", "IT");

        // Act: Create employees with different emails
        var emp1Id = await employeeRepo.CreateAsync("Charlie Davis", "charlie@example.com", deptId, 70000);
        var emp2Id = await employeeRepo.CreateAsync("Diana Evans", "diana@example.com", deptId, 65000);

        // Assert: Both employees should be created
        Assert.IsTrue(emp1Id > 0, "First employee should be created");
        Assert.IsTrue(emp2Id > 0, "Second employee should be created");
        Assert.AreNotEqual(emp1Id, emp2Id, "Employees should have different IDs");
    }

    [TestMethod]
    public async Task CheckConstraint_InvalidValue_ShouldFail()
    {
        // Arrange: Try to create product with negative price
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var productRepo = new InventoryProductRepository(connection);

        // Act & Assert: Should fail with check constraint error
        var exception = await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await productRepo.CreateAsync("Invalid Product", "INV-001", -10.00m, 100);
        });

        // Verify it's a check constraint error
        Assert.IsTrue(exception.Message.Contains("CHECK constraint failed") || 
                      exception.SqliteErrorCode == 275, // SQLITE_CONSTRAINT_CHECK
            $"Expected check constraint error, got: {exception.Message}");
    }

    [TestMethod]
    public async Task CheckConstraint_ValidValue_ShouldSucceed()
    {
        // Arrange: Create product with valid values
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var productRepo = new InventoryProductRepository(connection);

        // Act: Create product with positive price and non-negative stock
        var productId = await productRepo.CreateAsync("Valid Product", "VAL-001", 29.99m, 50);

        // Assert: Product should be created successfully
        Assert.IsTrue(productId > 0, "Product should be created");
        var product = await productRepo.GetByIdAsync(productId);
        Assert.IsNotNull(product, "Product should exist");
        Assert.AreEqual(29.99m, product.Price, "Price should match");
        Assert.AreEqual(50, product.Stock, "Stock should match");
    }

    [TestMethod]
    public async Task CheckConstraint_BoundaryValue_ShouldSucceed()
    {
        // Arrange: Create product with boundary values (price = 0.01, stock = 0)
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var productRepo = new InventoryProductRepository(connection);

        // Act: Create product with minimum valid values
        var productId = await productRepo.CreateAsync("Boundary Product", "BND-001", 0.01m, 0);

        // Assert: Product should be created successfully
        Assert.IsTrue(productId > 0, "Product should be created");
        var product = await productRepo.GetByIdAsync(productId);
        Assert.IsNotNull(product, "Product should exist");
        Assert.AreEqual(0.01m, product.Price, "Price should be 0.01");
        Assert.AreEqual(0, product.Stock, "Stock should be 0");
    }
}

#endregion
