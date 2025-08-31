// ExpressionToSql独立功能测试
using System;
using System.Linq;
using Sqlx;

// 测试实体
class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== ExpressionToSql 独立功能测试 ===\n");
        
        // 1. 基本查询测试
        Console.WriteLine("1. 基本WHERE查询:");
        var basicQuery = ExpressionToSql<TestUser>.ForSqlite()
            .Where(u => u.Age > 18);
        Console.WriteLine($"   SQL: {basicQuery.ToSql()}");
        var template = basicQuery.ToTemplate();
        Console.WriteLine($"   参数: {string.Join(", ", template.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        basicQuery.Dispose();
        
        // 2. 复杂条件测试
        Console.WriteLine("\n2. 复杂条件查询:");
        var complexQuery = ExpressionToSql<TestUser>.ForSqlite()
            .Where(u => u.Age >= 18 && u.Age <= 65)
            .Where(u => u.IsActive)
            .Where(u => u.Name.Contains("John"));
        Console.WriteLine($"   SQL: {complexQuery.ToSql()}");
        var complexTemplate = complexQuery.ToTemplate();
        Console.WriteLine($"   参数: {string.Join(", ", complexTemplate.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        complexQuery.Dispose();
        
        // 3. 排序和分页测试
        Console.WriteLine("\n3. 排序和分页:");
        var pagedQuery = ExpressionToSql<TestUser>.ForSqlite()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .OrderByDescending(u => u.Age)
            .Skip(10)
            .Take(5);
        Console.WriteLine($"   SQL: {pagedQuery.ToSql()}");
        pagedQuery.Dispose();
        
        // 4. 不同数据库方言测试
        Console.WriteLine("\n4. 不同数据库方言:");
        TestDialects();
        
        // 5. 字符串操作测试
        Console.WriteLine("\n5. 字符串操作:");
        var stringQuery = ExpressionToSql<TestUser>.ForSqlite()
            .Where(u => u.Name.StartsWith("A"))
            .Where(u => u.Name.EndsWith("son"))
            .Where(u => u.Name.ToUpper() == "JOHN");
        Console.WriteLine($"   SQL: {stringQuery.ToSql()}");
        stringQuery.Dispose();
        
        // 6. IN查询测试
        Console.WriteLine("\n6. IN查询:");
        var ids = new[] { 1, 2, 3, 4, 5 };
        var inQuery = ExpressionToSql<TestUser>.ForSqlite()
            .Where(u => ids.Contains(u.Id));
        Console.WriteLine($"   SQL: {inQuery.ToSql()}");
        var inTemplate = inQuery.ToTemplate();
        Console.WriteLine($"   参数: {string.Join(", ", inTemplate.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        inQuery.Dispose();
        
        Console.WriteLine("\n✅ ExpressionToSql独立功能测试完成!");
    }
    
    static void TestDialects()
    {
        // MySQL
        var mysqlQuery = ExpressionToSql<TestUser>.ForMySql()
            .Where(u => u.Age > 25)
            .Take(10);
        Console.WriteLine($"   MySQL:      {mysqlQuery.ToSql()}");
        mysqlQuery.Dispose();
        
        // SQL Server
        var sqlServerQuery = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Age > 25)
            .Skip(5)
            .Take(10);
        Console.WriteLine($"   SQL Server: {sqlServerQuery.ToSql()}");
        sqlServerQuery.Dispose();
        
        // PostgreSQL
        var pgQuery = ExpressionToSql<TestUser>.ForPostgreSQL()
            .Where(u => u.Age > 25)
            .Take(10);
        Console.WriteLine($"   PostgreSQL: {pgQuery.ToSql()}");
        pgQuery.Dispose();
    }
}