using System;
using Sqlx;

// Test ExpressionToSql functionality
Console.WriteLine("=== Testing ExpressionToSql ===");

try
{
    // Test 1: Basic query
    Console.WriteLine("1. Basic query test:");
    var query1 = ExpressionToSql<PersonTest>.ForSqlite()
        .Where(p => p.PersonId > 1)
        .Take(10);

    var template1 = query1.ToTemplate();
    Console.WriteLine($"   SQL: {template1.Sql}");
    Console.WriteLine($"   Parameters: {string.Join(", ", template1.Parameters)}");
    query1.Dispose();

    // Test 2: Complex query
    Console.WriteLine("\n2. Complex query test:");
    var query2 = ExpressionToSql<PersonTest>.ForSqlite()
        .Where(p => p.PersonId >= 1 && p.PersonId <= 10)
        .Where(p => p.PersonName!.Contains("Alice"))
        .OrderBy(p => p.PersonName)
        .Skip(2)
        .Take(5);

    var template2 = query2.ToTemplate();
    Console.WriteLine($"   SQL: {template2.Sql}");
    Console.WriteLine($"   Parameters: {string.Join(", ", template2.Parameters)}");
    query2.Dispose();

    // Test 3: Different dialects
    Console.WriteLine("\n3. Different database dialects:");
    
    var mysqlQuery = ExpressionToSql<PersonTest>.ForMySql()
        .Where(p => p.PersonId > 5)
        .Take(10);
    Console.WriteLine($"   MySQL: {mysqlQuery.ToSql()}");
    mysqlQuery.Dispose();

    var sqlServerQuery = ExpressionToSql<PersonTest>.ForSqlServer()
        .Where(p => p.PersonId > 5)
        .Take(10);
    Console.WriteLine($"   SQL Server: {sqlServerQuery.ToSql()}");
    sqlServerQuery.Dispose();

    Console.WriteLine("\n✅ All tests passed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Test failed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

/// <summary>
/// 简单测试 ExpressionToSql 功能的人员类
/// </summary>
public class PersonTest
{
    /// <summary>
    /// 人员ID
    /// </summary>
    public int PersonId { get; set; }
    
    /// <summary>
    /// 人员姓名
    /// </summary>
    public string? PersonName { get; set; }
}
