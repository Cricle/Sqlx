// <copyright file="ExpressionBlockResultExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Linq.Expressions;
using Sqlx;
using Sqlx.Expressions;

namespace Sqlx.Samples;

/// <summary>
/// 演示使用 ExpressionBlockResult 统一解析 WHERE 和 UPDATE 表达式。
/// ExpressionBlockResult 提供高效的表达式解析，避免重复解析，支持 AOT。
/// </summary>
public class ExpressionBlockResultExample
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? Email { get; set; }
        public int Version { get; set; }
    }

    public static void Main()
    {
        Console.WriteLine("=== ExpressionBlockResult 示例 ===\n");
        Console.WriteLine("1. WHERE 表达式解析\n-------------------");
        var minAge = 18;
        Expression<Func<User, bool>> whereExpr1 = u => u.Age > minAge;
        var result1 = ExpressionBlockResult.Parse(whereExpr1.Body, SqlDefine.SQLite);
        Console.WriteLine($"表达式: u => u.Age > {minAge}\nSQL: {result1.Sql}\n参数: {string.Join(", ", result1.Parameters.Select(p => $"{p.Key}={p.Value}"))}\n");
        var name = "John";
        Expression<Func<User, bool>> whereExpr2 = u => u.Age > minAge && u.Name == name && u.IsActive;
        var result2 = ExpressionBlockResult.Parse(whereExpr2.Body, SqlDefine.SQLite);
        Console.WriteLine($"表达式: u => u.Age > {minAge} && u.Name == \"{name}\" && u.IsActive\nSQL: {result2.Sql}\n参数: {string.Join(", ", result2.Parameters.Select(p => $"{p.Key}={p.Value}"))}\n");

        Console.WriteLine("2. UPDATE 表达式解析\n--------------------");
        Expression<Func<User, User>> updateExpr1 = u => new User { Name = "Jane", Age = 25 };
        var result3 = ExpressionBlockResult.ParseUpdate(updateExpr1, SqlDefine.SQLite);
        Console.WriteLine($"表达式: u => new User {{ Name = \"Jane\", Age = 25 }}\nSQL: {result3.Sql}\n参数: {string.Join(", ", result3.Parameters.Select(p => $"{p.Key}={p.Value}"))}\n");
        Expression<Func<User, User>> updateExpr2 = u => new User { Age = u.Age + 1, Version = u.Version + 1 };
        var result4 = ExpressionBlockResult.ParseUpdate(updateExpr2, SqlDefine.SQLite);
        Console.WriteLine($"表达式: u => new User {{ Age = u.Age + 1, Version = u.Version + 1 }}\nSQL: {result4.Sql}\n参数: {string.Join(", ", result4.Parameters.Select(p => $"{p.Key}={p.Value}"))}\n");
        Expression<Func<User, User>> updateExpr3 = u => new User { Name = u.Name.Trim().ToLower(), Email = u.Email != null ? u.Email.ToUpper() : null };
        var result5 = ExpressionBlockResult.ParseUpdate(updateExpr3, SqlDefine.SQLite);
        Console.WriteLine($"表达式: u => new User {{ Name = u.Name.Trim().ToLower(), Email = ... }}\nSQL: {result5.Sql}\n参数: {string.Join(", ", result5.Parameters.Select(p => $"{p.Key}={p.Value ?? "null"}"))}\n");

        Console.WriteLine("3. 多数据库方言支持\n-------------------");
        Expression<Func<User, bool>> dialectExpr = u => u.Age > 18;
        var sqliteResult = ExpressionBlockResult.Parse(dialectExpr.Body, SqlDefine.SQLite);
        var pgResult = ExpressionBlockResult.Parse(dialectExpr.Body, SqlDefine.PostgreSql);
        var mysqlResult = ExpressionBlockResult.Parse(dialectExpr.Body, SqlDefine.MySql);
        var sqlServerResult = ExpressionBlockResult.Parse(dialectExpr.Body, SqlDefine.SqlServer);
        Console.WriteLine($"SQLite:     {sqliteResult.Sql}\nPostgreSQL: {pgResult.Sql}\nMySQL:      {mysqlResult.Sql}\nSQL Server: {sqlServerResult.Sql}\n");

        Console.WriteLine("4. 实际使用场景\n---------------");
        var updateCondition = 18;
        var updateName = "UpdatedName";
        Expression<Func<User, User>> updateExpr = u => new User { Name = updateName, Age = u.Age + 1 };
        Expression<Func<User, bool>> whereExpr = u => u.Age > updateCondition;
        var updateResult = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);
        var whereResult = ExpressionBlockResult.Parse(whereExpr.Body, SqlDefine.SQLite);
        var allParameters = new System.Collections.Generic.Dictionary<string, object?>(updateResult.Parameters);
        foreach (var param in whereResult.Parameters) allParameters[param.Key] = param.Value;
        var fullSql = $"UPDATE [users] SET {updateResult.Sql} WHERE {whereResult.Sql}";
        Console.WriteLine($"完整 SQL: {fullSql}\n所有参数: {string.Join(", ", allParameters.Select(p => $"{p.Key}={p.Value}"))}\n");
        Console.WriteLine("5. 性能优势\n-----------\n✓ 一次解析，同时获取 SQL 和参数\n✓ 避免重复遍历表达式树\n✓ 支持 AOT 编译\n✓ 零反射，纯表达式树解析\n✓ 线程安全，无共享状态");
    }
}
