// -----------------------------------------------------------------------
// <copyright file="MultiDatabaseDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using ComprehensiveExample.Models;
using ComprehensiveExample.Services;

namespace ComprehensiveExample.Demonstrations;

/// <summary>
/// 多数据库方言支持演示类
/// 展示 Sqlx 对不同数据库系统的支持
/// </summary>
public static class MultiDatabaseDemo
{
    public static async Task RunDemonstrationAsync(DbConnection connection)
    {
        Console.WriteLine("\n🌐 多数据库方言支持演示");
        Console.WriteLine("=".PadRight(60, '='));
        
        var multiDbService = new MultiDatabaseService(connection);
        var expressionService = new ExpressionToSqlService(connection);
        
        // 演示不同数据库方言的 SQL 生成
        await DemonstrateSqlDialects();
        
        // 演示 Expression to SQL 在不同数据库中的使用
        await DemonstrateExpressionToSqlDialects(expressionService);
        
        // 演示数据库特定功能
        await DemonstrateDatabaseSpecificFeatures();
        
        // 演示迁移友好性
        await DemonstrateMigrationFriendly();
    }
    
    private static Task DemonstrateSqlDialects()
    {
        Console.WriteLine("\n📝 SQL 方言生成演示");
        
        // 模拟不同数据库的 SQL 生成
        var commonCondition = "活跃用户查询";
        
        Console.WriteLine($"🎯 查询条件: {commonCondition}");
        Console.WriteLine();
        
        // SQL Server 方言
        var sqlServerSql = @"SELECT [id], [name], [email], [created_at], [is_active], [department_id]
FROM [users] 
WHERE [is_active] = @isActive AND [department_id] IS NOT NULL
ORDER BY [created_at] DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        
        Console.WriteLine("🔷 SQL Server 方言:");
        Console.WriteLine($"   {sqlServerSql.Replace(Environment.NewLine, Environment.NewLine + "   ")}");
        Console.WriteLine();
        
        // MySQL 方言
        var mySqlSql = @"SELECT `id`, `name`, `email`, `created_at`, `is_active`, `department_id`
FROM `users` 
WHERE `is_active` = @isActive AND `department_id` IS NOT NULL
ORDER BY `created_at` DESC
LIMIT @skip, @take";
        
        Console.WriteLine("🟢 MySQL 方言:");
        Console.WriteLine($"   {mySqlSql.Replace(Environment.NewLine, Environment.NewLine + "   ")}");
        Console.WriteLine();
        
        // PostgreSQL 方言
        var postgreSql = @"SELECT ""id"", ""name"", ""email"", ""created_at"", ""is_active"", ""department_id""
FROM ""users"" 
WHERE ""is_active"" = @isActive AND ""department_id"" IS NOT NULL
ORDER BY ""created_at"" DESC
LIMIT @take OFFSET @skip";
        
        Console.WriteLine("🐘 PostgreSQL 方言:");
        Console.WriteLine($"   {postgreSql.Replace(Environment.NewLine, Environment.NewLine + "   ")}");
        Console.WriteLine();
        
        // SQLite 方言
        var sqliteSql = @"SELECT id, name, email, created_at, is_active, department_id
FROM users 
WHERE is_active = @isActive AND department_id IS NOT NULL
ORDER BY created_at DESC
LIMIT @take OFFSET @skip";
        
        Console.WriteLine("📊 SQLite 方言:");
        Console.WriteLine($"   {sqliteSql.Replace(Environment.NewLine, Environment.NewLine + "   ")}");
        Console.WriteLine();
        
        Console.WriteLine("✅ 所有方言都表达相同的查询逻辑，但语法适配各自数据库");
        
        return Task.CompletedTask;
    }
    
    private static async Task DemonstrateExpressionToSqlDialects(IExpressionToSqlService service)
    {
        Console.WriteLine("\n🎨 Expression to SQL 方言演示");
        
        // 定义通用查询条件
        var minDate = DateTime.Now.AddDays(-30);
        
        Console.WriteLine("🎯 查询条件: 活跃用户 && 最近30天创建");
        Console.WriteLine();
        
        // SQL Server 方言
        var sqlServerSql = "SELECT [id], [name], [email] FROM [users] WHERE [is_active] = 1 AND [created_at] >= @minDate ORDER BY [name] LIMIT 10";
        
        Console.WriteLine("🔷 SQL Server Expression to SQL:");
        Console.WriteLine($"   生成的 SQL: {sqlServerSql}");
        Console.WriteLine($"   特点: 使用 [方括号] 标识符");
        Console.WriteLine();
        
        // MySQL 方言
        var mySqlSql = "SELECT `id`, `name`, `email` FROM `users` WHERE `is_active` = 1 AND `created_at` >= @minDate ORDER BY `name` LIMIT 10";
        
        Console.WriteLine("🟢 MySQL Expression to SQL:");
        Console.WriteLine($"   生成的 SQL: {mySqlSql}");
        Console.WriteLine($"   特点: 使用 `反引号` 标识符");
        Console.WriteLine();
        
        // PostgreSQL 方言
        var postgreSql = "SELECT \"id\", \"name\", \"email\" FROM \"users\" WHERE \"is_active\" = true AND \"created_at\" >= @minDate ORDER BY \"name\" LIMIT 10";
        
        Console.WriteLine("🐘 PostgreSQL Expression to SQL:");
        Console.WriteLine($"   生成的 SQL: {postgreSql}");
        Console.WriteLine($"   特点: 使用 \"双引号\" 标识符");
        Console.WriteLine();
        
        // SQLite 方言 (实际执行)
        var sqliteSql = "WHERE is_active = 1 AND created_at >= '" + minDate.ToString("yyyy-MM-dd HH:mm:ss") + "' ORDER BY name LIMIT 10";
        
        Console.WriteLine("📊 SQLite Expression to SQL:");
        Console.WriteLine($"   生成的 SQL: {sqliteSql}");
        Console.WriteLine($"   特点: 无标识符引用 (简洁模式)");
        
        // 实际执行 SQLite 查询
        try
        {
            var results = await service.QueryUsersAsync(sqliteSql);
            Console.WriteLine($"   ✅ 实际执行结果: 找到 {results.Count} 个用户");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 执行结果: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    private static Task DemonstrateDatabaseSpecificFeatures()
    {
        Console.WriteLine("\n🔧 数据库特定功能演示");
        
        // SQL Server 特性
        Console.WriteLine("🔷 SQL Server 特性:");
        Console.WriteLine("   - OFFSET/FETCH 分页 (SQL Server 2012+)");
        Console.WriteLine("   - TOP 子句支持");
        Console.WriteLine("   - 窗口函数 (ROW_NUMBER, RANK, DENSE_RANK)");
        Console.WriteLine("   - CTE (公共表表达式) 支持");
        Console.WriteLine("   - MERGE 语句支持");
        Console.WriteLine();
        
        // MySQL 特性
        Console.WriteLine("🟢 MySQL 特性:");
        Console.WriteLine("   - LIMIT offset, count 分页");
        Console.WriteLine("   - AUTO_INCREMENT 自增主键");
        Console.WriteLine("   - 全文索引和搜索");
        Console.WriteLine("   - JSON 数据类型支持 (MySQL 5.7+)");
        Console.WriteLine("   - 分区表支持");
        Console.WriteLine();
        
        // PostgreSQL 特性
        Console.WriteLine("🐘 PostgreSQL 特性:");
        Console.WriteLine("   - LIMIT/OFFSET 分页");
        Console.WriteLine("   - SERIAL/BIGSERIAL 自增类型");
        Console.WriteLine("   - 数组数据类型");
        Console.WriteLine("   - JSONB 高性能 JSON 存储");
        Console.WriteLine("   - 全文搜索 (tsvector, tsquery)");
        Console.WriteLine("   - 自定义数据类型和函数");
        Console.WriteLine();
        
        // SQLite 特性
        Console.WriteLine("📊 SQLite 特性:");
        Console.WriteLine("   - LIMIT/OFFSET 分页");
        Console.WriteLine("   - INTEGER PRIMARY KEY 自增");
        Console.WriteLine("   - 动态类型系统");
        Console.WriteLine("   - 内嵌式数据库 (无服务器)");
        Console.WriteLine("   - 跨平台兼容性");
        Console.WriteLine("   - 事务 ACID 特性");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }
    
    private static async Task DemonstrateMigrationFriendly()
    {
        Console.WriteLine("\n🔄 迁移友好性演示");
        
        Console.WriteLine("🎯 Sqlx 的数据库迁移优势:");
        Console.WriteLine();
        
        Console.WriteLine("1️⃣ 统一的 C# 代码:");
        Console.WriteLine("   - 相同的实体类定义");
        Console.WriteLine("   - 相同的 Repository 接口");
        Console.WriteLine("   - 相同的业务逻辑代码");
        Console.WriteLine();
        
        Console.WriteLine("2️⃣ 自动方言适配:");
        Console.WriteLine("   - 编译时检测数据库类型");
        Console.WriteLine("   - 自动生成适配的 SQL 语句");
        Console.WriteLine("   - 参数化查询防止注入");
        Console.WriteLine();
        
        Console.WriteLine("3️⃣ 表达式查询兼容:");
        Console.WriteLine("   var query = ExpressionToSql<User>.ForSqlServer()  // 或 ForMySql(), ForPostgreSQL(), ForSqlite()");
        Console.WriteLine("       .Where(u => u.IsActive && u.CreatedAt > minDate)");
        Console.WriteLine("       .OrderBy(u => u.Name);");
        Console.WriteLine();
        
        Console.WriteLine("4️⃣ 特性注解统一:");
        Console.WriteLine("   [TableName(\"users\")]           // 所有数据库通用");
        Console.WriteLine("   [ColumnName(\"user_id\")]        // 列名映射");
        Console.WriteLine("   [SqlExecuteType(...)]           // 操作类型");
        Console.WriteLine("   [Sqlx(\"custom sql\")]           // 自定义 SQL");
        Console.WriteLine();
        
        Console.WriteLine("5️⃣ 性能一致性:");
        Console.WriteLine("   - 零反射，编译时生成");
        Console.WriteLine("   - 各数据库都采用最优实现");
        Console.WriteLine("   - 原生 DbBatch 支持");
        Console.WriteLine();
        
        Console.WriteLine("✅ 只需修改连接字符串和 ExpressionToSql 工厂方法，即可完成数据库迁移！");
        
        await Task.CompletedTask;
    }
}
