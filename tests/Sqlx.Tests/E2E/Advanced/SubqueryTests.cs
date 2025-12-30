// -----------------------------------------------------------------------
// <copyright file="SubqueryTests.cs" company="Cricle">
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
/// E2E tests for subquery operations.
/// **Validates: Requirements 3.2**
/// </summary>

#region Data Models

public class Employee
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
}

public class Department
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

#endregion

#region Repository Interfaces

public partial interface IEmployeeRepository
{
    [SqlTemplate("INSERT INTO employees (name, department_id, salary, hire_date) VALUES (@name, @departmentId, @salary, @hireDate)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, long departmentId, decimal salary, DateTime hireDate);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Employee?> GetByIdAsync(long id);

    [SqlTemplate(@"
        SELECT {{columns}} 
        FROM {{table}} 
        WHERE salary > (SELECT AVG(salary) FROM employees)
        ORDER BY salary DESC")]
    Task<List<Employee>> GetAboveAverageSalaryAsync();

    [SqlTemplate(@"
        SELECT {{columns}}
        FROM {{table}} e
        WHERE salary > (
            SELECT AVG(salary) 
            FROM employees 
            WHERE department_id = e.department_id
        )
        ORDER BY department_id, salary DESC")]
    Task<List<Employee>> GetAboveDepartmentAverageAsync();

    [SqlTemplate(@"
        SELECT {{columns}}
        FROM {{table}}
        WHERE department_id IN (
            SELECT id FROM departments WHERE location = @location
        )
        ORDER BY name")]
    Task<List<Employee>> GetByDepartmentLocationAsync(string location);
}

public partial interface IDepartmentRepository
{
    [SqlTemplate("INSERT INTO departments (name, location) VALUES (@name, @location)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string name, string location);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Department?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY name")]
    Task<List<Department>> GetAllAsync();
}

#endregion

#region Repository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("employees")]
[RepositoryFor(typeof(IEmployeeRepository))]
public partial class EmployeeRepository(DbConnection connection) : IEmployeeRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("departments")]
[RepositoryFor(typeof(IDepartmentRepository))]
public partial class DepartmentRepository(DbConnection connection) : IDepartmentRepository { }

#endregion

#region SQLite Implementation

[TestClass]
[TestCategory("E2E")]
[TestCategory("Advanced")]
[TestCategory("Subquery")]
[TestCategory("SQLite")]
public class SubqueryTests_SQLite
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
            CREATE TABLE IF NOT EXISTS departments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                location TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS employees (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                department_id INTEGER NOT NULL,
                salary REAL NOT NULL,
                hire_date TEXT NOT NULL,
                FOREIGN KEY (department_id) REFERENCES departments(id)
            );

            CREATE INDEX IF NOT EXISTS idx_employees_department ON employees(department_id);
            CREATE INDEX IF NOT EXISTS idx_employees_salary ON employees(salary);
        ";

        cmd.ExecuteNonQuery();
    }

    [TestMethod]
    public async Task NonCorrelatedSubquery_CompleteWorkflow_FiltersCorrectly()
    {
        // Arrange: Create departments and employees with varying salaries
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var deptRepo = new DepartmentRepository(connection);
        var empRepo = new EmployeeRepository(connection);

        // Create departments
        var engineeringId = await deptRepo.CreateAsync("Engineering", "New York");
        var salesId = await deptRepo.CreateAsync("Sales", "Boston");

        // Create employees with salaries: 50k, 60k, 70k, 80k, 90k (average = 70k)
        await empRepo.CreateAsync("Alice", engineeringId, 50000m, DateTime.UtcNow.AddYears(-3));
        await empRepo.CreateAsync("Bob", engineeringId, 60000m, DateTime.UtcNow.AddYears(-2));
        await empRepo.CreateAsync("Charlie", salesId, 70000m, DateTime.UtcNow.AddYears(-2));
        await empRepo.CreateAsync("David", salesId, 80000m, DateTime.UtcNow.AddYears(-1));
        await empRepo.CreateAsync("Eve", engineeringId, 90000m, DateTime.UtcNow.AddMonths(-6));

        // Act: Query employees with salary above average using non-correlated subquery
        var aboveAverage = await empRepo.GetAboveAverageSalaryAsync();

        // Assert: Only employees with salary > 70k should be returned
        Assert.AreEqual(2, aboveAverage.Count, "Should have 2 employees above average salary");
        Assert.IsTrue(aboveAverage.All(e => e.Salary > 70000m), "All returned employees should have salary > 70k");
        Assert.IsTrue(aboveAverage.Any(e => e.Name == "David" && e.Salary == 80000m), "Should include David");
        Assert.IsTrue(aboveAverage.Any(e => e.Name == "Eve" && e.Salary == 90000m), "Should include Eve");
        
        // Verify ordering (DESC by salary)
        Assert.AreEqual("Eve", aboveAverage[0].Name, "Highest salary should be first");
        Assert.AreEqual("David", aboveAverage[1].Name, "Second highest should be second");
    }

    [TestMethod]
    public async Task CorrelatedSubquery_CompleteWorkflow_FiltersPerOuterRow()
    {
        // Arrange: Create departments with employees having different salary distributions
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var deptRepo = new DepartmentRepository(connection);
        var empRepo = new EmployeeRepository(connection);

        // Create departments
        var engineeringId = await deptRepo.CreateAsync("Engineering", "New York");
        var salesId = await deptRepo.CreateAsync("Sales", "Boston");

        // Engineering: 50k, 60k, 90k (avg = 66.67k)
        await empRepo.CreateAsync("Alice", engineeringId, 50000m, DateTime.UtcNow.AddYears(-3));
        await empRepo.CreateAsync("Bob", engineeringId, 60000m, DateTime.UtcNow.AddYears(-2));
        await empRepo.CreateAsync("Eve", engineeringId, 90000m, DateTime.UtcNow.AddMonths(-6));

        // Sales: 70k, 80k (avg = 75k)
        await empRepo.CreateAsync("Charlie", salesId, 70000m, DateTime.UtcNow.AddYears(-2));
        await empRepo.CreateAsync("David", salesId, 80000m, DateTime.UtcNow.AddYears(-1));

        // Act: Query employees above their department's average using correlated subquery
        var aboveDeptAverage = await empRepo.GetAboveDepartmentAverageAsync();

        // Assert: Should return employees above their own department's average
        // Engineering: Only Eve (90k > 66.67k)
        // Sales: Only David (80k > 75k)
        Assert.AreEqual(2, aboveDeptAverage.Count, "Should have 2 employees above their department average");
        
        var engineeringAbove = aboveDeptAverage.Where(e => e.DepartmentId == engineeringId).ToList();
        Assert.AreEqual(1, engineeringAbove.Count, "Engineering should have 1 employee above average");
        Assert.AreEqual("Eve", engineeringAbove[0].Name, "Eve should be above Engineering average");
        Assert.AreEqual(90000m, engineeringAbove[0].Salary);

        var salesAbove = aboveDeptAverage.Where(e => e.DepartmentId == salesId).ToList();
        Assert.AreEqual(1, salesAbove.Count, "Sales should have 1 employee above average");
        Assert.AreEqual("David", salesAbove[0].Name, "David should be above Sales average");
        Assert.AreEqual(80000m, salesAbove[0].Salary);
    }

    [TestMethod]
    public async Task SubqueryWithIN_CompleteWorkflow_FiltersBasedOnSubqueryResults()
    {
        // Arrange: Create departments in different locations
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var deptRepo = new DepartmentRepository(connection);
        var empRepo = new EmployeeRepository(connection);

        // Create departments in different locations
        var engineeringId = await deptRepo.CreateAsync("Engineering", "New York");
        var salesId = await deptRepo.CreateAsync("Sales", "New York");
        var hrId = await deptRepo.CreateAsync("HR", "Boston");
        var marketingId = await deptRepo.CreateAsync("Marketing", "Boston");

        // Create employees in each department
        await empRepo.CreateAsync("Alice", engineeringId, 80000m, DateTime.UtcNow);
        await empRepo.CreateAsync("Bob", engineeringId, 75000m, DateTime.UtcNow);
        await empRepo.CreateAsync("Charlie", salesId, 70000m, DateTime.UtcNow);
        await empRepo.CreateAsync("David", hrId, 60000m, DateTime.UtcNow);
        await empRepo.CreateAsync("Eve", marketingId, 65000m, DateTime.UtcNow);

        // Act: Query employees in New York departments using subquery with IN
        var nyEmployees = await empRepo.GetByDepartmentLocationAsync("New York");
        var bostonEmployees = await empRepo.GetByDepartmentLocationAsync("Boston");

        // Assert: Verify correct filtering based on department location
        Assert.AreEqual(3, nyEmployees.Count, "Should have 3 employees in New York");
        Assert.IsTrue(nyEmployees.All(e => e.DepartmentId == engineeringId || e.DepartmentId == salesId), 
            "All employees should be in NY departments");
        Assert.IsTrue(nyEmployees.Any(e => e.Name == "Alice"), "Should include Alice");
        Assert.IsTrue(nyEmployees.Any(e => e.Name == "Bob"), "Should include Bob");
        Assert.IsTrue(nyEmployees.Any(e => e.Name == "Charlie"), "Should include Charlie");

        Assert.AreEqual(2, bostonEmployees.Count, "Should have 2 employees in Boston");
        Assert.IsTrue(bostonEmployees.All(e => e.DepartmentId == hrId || e.DepartmentId == marketingId), 
            "All employees should be in Boston departments");
        Assert.IsTrue(bostonEmployees.Any(e => e.Name == "David"), "Should include David");
        Assert.IsTrue(bostonEmployees.Any(e => e.Name == "Eve"), "Should include Eve");
    }
}

#endregion
