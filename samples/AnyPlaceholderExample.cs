// <copyright file="AnyPlaceholderExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Linq.Expressions;
using Sqlx;
using Sqlx.Expressions;

namespace Sqlx.Samples;

/// <summary>
/// Demonstrates the use of Any.Value&lt;T&gt;() placeholders in ExpressionBlockResult.
/// This feature allows you to create reusable expression templates with named placeholders
/// that can be filled with actual values at runtime.
/// </summary>
public class AnyPlaceholderExample
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public bool IsActive { get; set; }
    }

    public static void Run()
    {
        Console.WriteLine("=== Any Placeholder Examples ===\n");

        // Example 1: Simple placeholder
        SimplePlaceholder();

        // Example 2: Multiple placeholders
        MultiplePlaceholders();

        // Example 3: Reusable expression template
        ReusableTemplate();

        // Example 4: UPDATE with placeholders
        UpdateWithPlaceholders();

        // Example 5: Complex query with mixed placeholders and constants
        ComplexQuery();
    }

    /// <summary>
    /// Example 1: Simple placeholder usage.
    /// </summary>
    private static void SimplePlaceholder()
    {
        Console.WriteLine("Example 1: Simple Placeholder");
        Console.WriteLine("------------------------------");

        // Define expression template with placeholder
        Expression<Func<User, bool>> template = u => u.Age > Any.Value<int>("minAge");

        // Parse and fill placeholder
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 18);

        Console.WriteLine($"SQL: {result.Sql}");
        Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        Console.WriteLine();
    }

    /// <summary>
    /// Example 2: Multiple placeholders.
    /// </summary>
    private static void MultiplePlaceholders()
    {
        Console.WriteLine("Example 2: Multiple Placeholders");
        Console.WriteLine("--------------------------------");

        // Define expression template with multiple placeholders
        Expression<Func<User, bool>> template = u => 
            u.Age >= Any.Value<int>("minAge") && 
            u.Age <= Any.Value<int>("maxAge") &&
            u.Department == Any.Value<string>("dept");

        // Parse expression
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite);

        Console.WriteLine($"Placeholders: {string.Join(", ", result.GetPlaceholderNames())}");
        Console.WriteLine($"All filled: {result.AreAllPlaceholdersFilled()}");

        // Fill placeholders
        result.WithParameter("minAge", 25)
              .WithParameter("maxAge", 45)
              .WithParameter("dept", "Engineering");

        Console.WriteLine($"SQL: {result.Sql}");
        Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        Console.WriteLine($"All filled: {result.AreAllPlaceholdersFilled()}");
        Console.WriteLine();
    }

    /// <summary>
    /// Example 3: Reusable expression template.
    /// </summary>
    private static void ReusableTemplate()
    {
        Console.WriteLine("Example 3: Reusable Template");
        Console.WriteLine("----------------------------");

        // Define a reusable template
        Expression<Func<User, bool>> ageRangeTemplate = u => 
            u.Age >= Any.Value<int>("minAge") && u.Age <= Any.Value<int>("maxAge");

        // Use template with different values
        Console.WriteLine("Query 1: Young employees (18-30)");
        var query1 = ExpressionBlockResult.Parse(ageRangeTemplate.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 18)
            .WithParameter("maxAge", 30);
        Console.WriteLine($"SQL: {query1.Sql}");
        Console.WriteLine($"Parameters: {string.Join(", ", query1.Parameters.Select(p => $"{p.Key}={p.Value}"))}");

        Console.WriteLine("\nQuery 2: Senior employees (50-65)");
        var query2 = ExpressionBlockResult.Parse(ageRangeTemplate.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 50)
            .WithParameter("maxAge", 65);
        Console.WriteLine($"SQL: {query2.Sql}");
        Console.WriteLine($"Parameters: {string.Join(", ", query2.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        Console.WriteLine();
    }

    /// <summary>
    /// Example 4: UPDATE with placeholders.
    /// </summary>
    private static void UpdateWithPlaceholders()
    {
        Console.WriteLine("Example 4: UPDATE with Placeholders");
        Console.WriteLine("-----------------------------------");

        // Define UPDATE template with placeholders
        Expression<Func<User, User>> updateTemplate = u => new User
        {
            Salary = Any.Value<decimal>("newSalary"),
            Department = Any.Value<string>("newDept")
        };

        // Parse and fill placeholders
        var result = ExpressionBlockResult.ParseUpdate(updateTemplate, SqlDefine.SQLite)
            .WithParameter("newSalary", 75000m)
            .WithParameter("newDept", "Management");

        Console.WriteLine($"SQL: {result.Sql}");
        Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        Console.WriteLine();
    }

    /// <summary>
    /// Example 5: Complex query with mixed placeholders and constants.
    /// </summary>
    private static void ComplexQuery()
    {
        Console.WriteLine("Example 5: Complex Query with Mixed Placeholders");
        Console.WriteLine("-----------------------------------------------");

        // Mix placeholders with constants
        var minSalary = 50000m; // Constant
        Expression<Func<User, bool>> template = u => 
            (u.Age >= Any.Value<int>("minAge") && u.Salary >= minSalary) ||
            u.Department == Any.Value<string>("dept");

        // Parse and fill placeholders
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 30)
            .WithParameter("dept", "Sales");

        Console.WriteLine($"SQL: {result.Sql}");
        Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        Console.WriteLine("\nNote: minSalary (50000) is a constant, so it's parameterized as @p0");
        Console.WriteLine("      while minAge and dept are named placeholders");
        Console.WriteLine();
    }

    /// <summary>
    /// Example 6: Different SQL dialects.
    /// </summary>
    private static void DifferentDialects()
    {
        Console.WriteLine("Example 6: Different SQL Dialects");
        Console.WriteLine("---------------------------------");

        Expression<Func<User, bool>> template = u => u.Age > Any.Value<int>("minAge");

        // SQLite
        var sqlite = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 18);
        Console.WriteLine($"SQLite:     {sqlite.Sql}");

        // PostgreSQL
        var postgres = ExpressionBlockResult.Parse(template.Body, SqlDefine.PostgreSql)
            .WithParameter("minAge", 18);
        Console.WriteLine($"PostgreSQL: {postgres.Sql}");

        // MySQL
        var mysql = ExpressionBlockResult.Parse(template.Body, SqlDefine.MySql)
            .WithParameter("minAge", 18);
        Console.WriteLine($"MySQL:      {mysql.Sql}");

        // Oracle
        var oracle = ExpressionBlockResult.Parse(template.Body, SqlDefine.Oracle)
            .WithParameter("minAge", 18);
        Console.WriteLine($"Oracle:     {oracle.Sql}");
        Console.WriteLine();
    }
}

/// <summary>
/// Example output:
/// 
/// === Any Placeholder Examples ===
/// 
/// Example 1: Simple Placeholder
/// ------------------------------
/// SQL: [age] > @minAge
/// Parameters: @minAge=18
/// 
/// Example 2: Multiple Placeholders
/// --------------------------------
/// Placeholders: minAge, maxAge, dept
/// All filled: False
/// SQL: ([age] >= @minAge AND [age] <= @maxAge AND [department] = @dept)
/// Parameters: @minAge=25, @maxAge=45, @dept=Engineering
/// All filled: True
/// 
/// Example 3: Reusable Template
/// ----------------------------
/// Query 1: Young employees (18-30)
/// SQL: ([age] >= @minAge AND [age] <= @maxAge)
/// Parameters: @minAge=18, @maxAge=30
/// 
/// Query 2: Senior employees (50-65)
/// SQL: ([age] >= @minAge AND [age] <= @maxAge)
/// Parameters: @minAge=50, @maxAge=65
/// 
/// Example 4: UPDATE with Placeholders
/// -----------------------------------
/// SQL: [salary] = @newSalary, [department] = @newDept
/// Parameters: @newSalary=75000, @newDept=Management
/// 
/// Example 5: Complex Query with Mixed Placeholders
/// -----------------------------------------------
/// SQL: (([age] >= @minAge AND [salary] >= @p0) OR [department] = @dept)
/// Parameters: @minAge=30, @p0=50000, @dept=Sales
/// 
/// Note: minSalary (50000) is a constant, so it's parameterized as @p0
///       while minAge and dept are named placeholders
/// </summary>
