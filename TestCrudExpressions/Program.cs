using System;
using Sqlx;

public class MinimalEntity
{
    public int Id { get; set; }
}

// 测试实体类
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("🔍 调试 MinimalEntity UPDATE 问题");
        
        using var expression = ExpressionToSql<MinimalEntity>.ForSqlServer()
            .Set(e => e.Id, 999)
            .Where(e => e.Id == 1);

        var sql = expression.ToSql();
        
        Console.WriteLine($"生成的SQL: {sql}");
        Console.WriteLine($"是否以UPDATE开头: {sql.StartsWith("UPDATE [MinimalEntity] SET")}");
        Console.WriteLine($"是否包含SET子句: {sql.Contains("[Id] = 999")}");
        Console.WriteLine($"是否包含WHERE子句: {sql.Contains("WHERE [Id] = 1")}");
        Console.WriteLine($"是否包含WHERE: {sql.Contains("WHERE")}");
        
        Console.WriteLine("\n🚀 测试 ExpressionToSql 增强的 CRUD 功能\n");

        // 测试 SELECT 表达式
        TestSelectExpressions();
        
        // 测试 INSERT 操作
        TestInsertOperations();
        
        // 测试 UPDATE 操作
        TestUpdateOperations();
        
        // 测试 DELETE 操作
        TestDeleteOperations();
        
        Console.WriteLine("\n✅ 所有测试完成！");
    }

    static void TestSelectExpressions()
    {
        Console.WriteLine("📋 测试 SELECT 表达式功能:");
        
        // 1. 使用表达式选择特定列
        using var query1 = ExpressionToSql<User>.ForSqlServer()
            .Select(u => new { u.Id, u.Name, u.Email })
            .Where(u => u.IsActive);
            
        Console.WriteLine($"   SELECT 表达式: {query1.ToSql()}");
        
        // 2. 使用多个表达式
        using var query2 = ExpressionToSql<User>.ForSqlServer()
            .Select(u => u.Id, u => u.Name)
            .Where(u => u.Age > 18);
            
        Console.WriteLine($"   多表达式 SELECT: {query2.ToSql()}");
        
        // 3. 传统字符串选择
        using var query3 = ExpressionToSql<User>.ForSqlServer()
            .Select("Id", "Name", "Email")
            .Where(u => u.IsActive);
            
        Console.WriteLine($"   字符串 SELECT: {query3.ToSql()}");
        Console.WriteLine();
    }

    static void TestInsertOperations()
    {
        Console.WriteLine("➕ 测试 INSERT 操作:");
        
        // 1. INSERT INTO 所有列
        using var insert1 = ExpressionToSql<User>.ForSqlServer()
            .InsertInto()
            .Values(1, "张三", "zhang@example.com", 25, true, DateTime.Now);
            
        Console.WriteLine($"   INSERT 所有列: {insert1.ToSql()}");
        
        // 2. INSERT 指定列
        using var insert2 = ExpressionToSql<User>.ForSqlServer()
            .Insert(u => new { u.Name, u.Email, u.Age })
            .Values("李四", "li@example.com", 30);
            
        Console.WriteLine($"   INSERT 指定列: {insert2.ToSql()}");
        
        // 3. INSERT 多行
        using var insert3 = ExpressionToSql<User>.ForSqlServer()
            .InsertInto()
            .Values(1, "用户1", "user1@example.com", 25, true, DateTime.Now)
            .AddValues(2, "用户2", "user2@example.com", 30, false, DateTime.Now);
            
        Console.WriteLine($"   INSERT 多行: {insert3.ToSql()}");
        Console.WriteLine();
    }

    static void TestUpdateOperations()
    {
        Console.WriteLine("✏️ 测试 UPDATE 操作:");
        
        // 1. 基本 UPDATE
        using var update1 = ExpressionToSql<User>.ForSqlServer()
            .Update()
            .Set(u => u.Name, "新名称")
            .Set(u => u.Age, 35)
            .Where(u => u.Id == 1);
            
        Console.WriteLine($"   基本 UPDATE: {update1.ToSql()}");
        
        // 2. 表达式 UPDATE
        using var update2 = ExpressionToSql<User>.ForSqlServer()
            .Update()
            .Set(u => u.Age, u => u.Age + 1)
            .Where(u => u.IsActive);
            
        Console.WriteLine($"   表达式 UPDATE: {update2.ToSql()}");
        Console.WriteLine();
    }

    static void TestDeleteOperations()
    {
        Console.WriteLine("🗑️ 测试 DELETE 操作:");
        
        // 1. DELETE 带条件
        using var delete1 = ExpressionToSql<User>.ForSqlServer()
            .Delete(u => u.IsActive == false);
            
        Console.WriteLine($"   DELETE 带条件: {delete1.ToSql()}");
        
        // 2. DELETE 先设置条件
        using var delete2 = ExpressionToSql<User>.ForSqlServer()
            .Delete()
            .Where(u => u.Age < 18);
            
        Console.WriteLine($"   DELETE 先设条件: {delete2.ToSql()}");
        
        // 3. 测试安全检查 - 无WHERE的DELETE应该抛出异常
        try
        {
            using var delete3 = ExpressionToSql<User>.ForSqlServer()
                .Delete();
            var dangerousSql = delete3.ToSql(); // 这应该抛出异常
            Console.WriteLine($"   ❌ 危险: {dangerousSql}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"   ✅ 安全检查通过: {ex.Message}");
        }
        Console.WriteLine();
    }
}
