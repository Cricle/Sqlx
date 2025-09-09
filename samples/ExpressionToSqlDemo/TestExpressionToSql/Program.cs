using System;
using Sqlx;

// Test ExpressionToSql functionality
Console.WriteLine("=== Testing ExpressionToSql ===");

try
{
    // Test 1: Basic instantiation
    Console.WriteLine("1. Basic instantiation test:");
    var query1 = ExpressionToSql<PersonTest>.ForSqlite();
    Console.WriteLine($"   ✅ ForSqlite() created successfully: {query1.GetType().Name}");

    // Test 2: Different dialects
    Console.WriteLine("\n2. Different database dialects:");
    
    var mysqlQuery = ExpressionToSql<PersonTest>.ForMySql();
    Console.WriteLine($"   ✅ ForMySql() created successfully: {mysqlQuery.GetType().Name}");

    var sqlServerQuery = ExpressionToSql<PersonTest>.ForSqlServer();
    Console.WriteLine($"   ✅ ForSqlServer() created successfully: {sqlServerQuery.GetType().Name}");
    
    var pgQuery = ExpressionToSql<PersonTest>.ForPostgreSQL();
    Console.WriteLine($"   ✅ ForPostgreSQL() created successfully: {pgQuery.GetType().Name}");
    
    var defaultQuery = ExpressionToSql<PersonTest>.Create();
    Console.WriteLine($"   ✅ Create() created successfully: {defaultQuery.GetType().Name}");

    Console.WriteLine("\n✅ All basic tests passed!");
    Console.WriteLine("   Note: Full ExpressionToSql functionality requires source generation context.");
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
