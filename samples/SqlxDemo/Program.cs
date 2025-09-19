// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using SqlxDemo.Models;

namespace SqlxDemo
{
    /// <summary>
    /// Sqlx 完整功能演示程序 - 使用 SQLite 数据库
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 程序主入口点
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 === Sqlx 3.0 完整功能演示 === 🚀");
            Console.WriteLine("使用 SQLite 数据库展示所有核心功能");
            Console.WriteLine();

            try
            {
                // 创建内存数据库连接
                using var connection = new SqliteConnection("Data Source=:memory:");
                await connection.OpenAsync();

                // 创建示例表和数据
                await SetupDatabaseAsync(connection);

                // 演示所有功能
                await DemonstrateAllFeaturesAsync(connection);

                Console.WriteLine();
                Console.WriteLine("✅ 所有功能演示完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 演示过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            if (args.Length == 0 || !args[0].Equals("--no-wait", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\n按任意键退出...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 设置数据库和示例数据
        /// </summary>
        private static async Task SetupDatabaseAsync(SqliteConnection connection)
        {
            Console.WriteLine("🔧 设置 SQLite 数据库...");

            // 创建表
            var createTables = @"
                CREATE TABLE [user] (
                    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [name] TEXT NOT NULL,
                    [email] TEXT,
                    [age] INTEGER,
                    [salary] REAL DEFAULT 0,
                    [department_id] INTEGER,
                    [is_active] INTEGER DEFAULT 1,
                    [hire_date] TEXT,
                    [bonus] REAL,
                    [performance_rating] REAL DEFAULT 0
                );

                CREATE TABLE [product] (
                    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [name] TEXT NOT NULL,
                    [price] REAL DEFAULT 0,
                    [is_active] INTEGER DEFAULT 1
                );

                INSERT INTO [user] ([name], [email], [age], [salary], [department_id], [hire_date], [bonus], [performance_rating]) VALUES 
                ('张三', 'zhang@example.com', 28, 8000, 1, '2023-01-15', 1000, 4.5),
                ('李四', 'li@example.com', 32, 12000, 2, '2022-06-10', 2000, 4.8),
                ('王五', 'wang@example.com', 25, 6000, 1, '2023-03-20', 500, 4.2),
                ('赵六', 'zhao@example.com', 35, 15000, 3, '2021-12-01', 3000, 4.9);

                INSERT INTO [product] ([name], [price], [is_active]) VALUES 
                ('笔记本电脑', 5999.99, 1),
                ('无线鼠标', 199.99, 1),
                ('机械键盘', 899.99, 1),
                ('显示器', 2999.99, 0);
            ";

            using var command = connection.CreateCommand();
            command.CommandText = createTables;
            await command.ExecuteNonQueryAsync();

            Console.WriteLine("✅ 数据库设置完成\n");
        }

        /// <summary>
        /// 演示所有 Sqlx 功能
        /// </summary>
        private static async Task DemonstrateAllFeaturesAsync(SqliteConnection connection)
        {
            // 1. 直接执行 - 最简单的使用方式
            DemonstrateParameterizedSqlAsync();

            // 2. SqlTemplate 静态模板演示
            await DemonstrateSqlTemplateAsync(connection);

            // 3. ExpressionToSql 动态查询演示  
            await DemonstrateExpressionToSqlAsync(connection);

            // 4. 源代码生成演示
            DemonstrateSourceGenerationAsync();

            // 5. INSERT 操作演示
            await DemonstrateInsertOperationsAsync(connection);

            // 6. UPDATE 操作演示
            await DemonstrateUpdateOperationsAsync(connection);

            // 7. DELETE 操作演示
            await DemonstrateDeleteOperationsAsync(connection);

            // 8. 复杂查询演示
            await DemonstrateComplexQueriesAsync(connection);
        }

        /// <summary>
        /// 演示 ParameterizedSql 直接执行功能
        /// </summary>
        private static void DemonstrateParameterizedSqlAsync()
        {
            Console.WriteLine("🚀 === ParameterizedSql 直接执行演示 ===");

            // 基本直接执行
            var sql1 = ParameterizedSql.Create(
                "SELECT * FROM [user] WHERE [age] > @age AND [is_active] = @active", 
                new Dictionary<string, object?> { ["age"] = 25, ["active"] = true });

            Console.WriteLine($"📝 基本直接执行:");
            Console.WriteLine($"   SQL: {sql1.Sql}");
            Console.WriteLine($"   参数: age={sql1.Parameters.GetValueOrDefault("age")}, active={sql1.Parameters.GetValueOrDefault("active")}");

            // 复杂查询直接执行
            var sql2 = ParameterizedSql.Create(
                "SELECT [name], [email], [salary] FROM [user] WHERE [salary] BETWEEN @minSalary AND @maxSalary ORDER BY [salary] DESC",
                new Dictionary<string, object?> { ["minSalary"] = 5000, ["maxSalary"] = 15000 });

            Console.WriteLine($"💼 薪资范围查询:");
            Console.WriteLine($"   SQL: {sql2.Sql}");
            Console.WriteLine($"   参数数量: {sql2.Parameters.Count}");

            // 渲染最终SQL（用于调试）
            Console.WriteLine($"🔍 渲染后的SQL示例: {sql1.Render()}");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示源代码生成功能
        /// </summary>
        private static void DemonstrateSourceGenerationAsync()
        {
            Console.WriteLine("⚙️ === 源代码生成演示 ===");

            Console.WriteLine($"🔧 Sqlx 支持基于特性的源代码生成:");
            Console.WriteLine($"   • [Sqlx] - 标记需要生成实现的方法");
            Console.WriteLine($"   • [SqlExecuteType] - 指定SQL操作类型");
            Console.WriteLine($"   • [RepositoryFor] - 自动生成Repository实现");
            Console.WriteLine($"   • [ExpressionToSql] - 表达式转SQL方法");

            Console.WriteLine($"📋 生成器特性:");
            Console.WriteLine($"   • 编译时代码生成，零运行时反射");
            Console.WriteLine($"   • AOT 原生支持，最佳性能");
            Console.WriteLine($"   • 强类型安全，编译时验证");
            Console.WriteLine($"   • 智能SQL生成，支持多数据库方言");

            Console.WriteLine($"🎯 示例用法:");
            Console.WriteLine($"   [RepositoryFor(typeof(IUserService))]");
            Console.WriteLine($"   public partial class UserService : IUserService");
            Console.WriteLine($"   {{");
            Console.WriteLine($"       // 源生成器自动生成实现代码");
            Console.WriteLine($"   }}");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示 SqlTemplate 静态模板功能
        /// </summary>
        private static async Task DemonstrateSqlTemplateAsync(SqliteConnection connection)
        {
            Console.WriteLine("📝 === SqlTemplate 静态模板演示 ===");

            // 基本模板
            var template = SqlTemplate.Parse("SELECT * FROM [user] WHERE [age] > @age AND [is_active] = 1");
            var sql = template.Execute(new { age = 25 });
            
            Console.WriteLine($"🔍 查询年龄大于25的活跃用户:");
            Console.WriteLine($"   SQL: {sql.Sql}");
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM [user] WHERE [age] > 25 AND [is_active] = 1";
            var count = await command.ExecuteScalarAsync();
            Console.WriteLine($"   结果: 找到 {count} 个用户");

            // 参数化模板
            var paramTemplate = SqlTemplate.Parse("SELECT [name], [email] FROM [user] WHERE [department_id] = @deptId");
            var paramSql = paramTemplate.Bind().Param("deptId", 1).Build();
            Console.WriteLine($"🏢 查询部门ID为1的用户:");
            Console.WriteLine($"   SQL: {paramSql.Sql}");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示 ExpressionToSql 动态查询功能
        /// </summary>
        private static async Task DemonstrateExpressionToSqlAsync(SqliteConnection connection)
        {
            Console.WriteLine("🔧 === ExpressionToSql 动态查询演示 ===");

            // 基本查询
            var query1 = ExpressionToSql<User>.ForSqlite()
                .Select(u => new { u.Name, u.Email, u.Age })
                .Where(u => u.Age > 25 && u.IsActive)
                .OrderBy(u => u.Age)
                .Take(10);

            Console.WriteLine($"🎯 类型安全的动态查询:");
            Console.WriteLine($"   SQL: {query1.ToSql()}");

            // 复杂条件查询
            var query2 = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Salary > 8000)
                .And(u => u.PerformanceRating >= 4.5)
                .OrderByDescending(u => u.Salary);

            Console.WriteLine($"💰 高薪高绩效员工查询:");
            Console.WriteLine($"   SQL: {query2.ToSql()}");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示 INSERT 操作
        /// </summary>
        private static async Task DemonstrateInsertOperationsAsync(SqliteConnection connection)
        {
            Console.WriteLine("➕ === INSERT 操作演示 ===");

            // 使用新的 As 方法
            var insertQuery = ExpressionToSql<User>.ForSqlite()
                .AsInsert(u => new { u.Name, u.Email, u.Age, u.Salary })
                .Values("新员工", "new@example.com", 26, 7000);

            Console.WriteLine($"👤 插入新用户 (As方法):");
            Console.WriteLine($"   SQL: {insertQuery.ToSql()}");

            // 插入所有列
            var insertAllQuery = ExpressionToSql<Product>.ForSqlite()
                .AsInsertIntoAll()
                .Values(null, "新产品", 1299.99, 1);

            Console.WriteLine($"🛍️ 插入新产品 (所有列):");
            Console.WriteLine($"   SQL: {insertAllQuery.ToSql()}");

            // INSERT SELECT
            var insertSelectQuery = ExpressionToSql<User>.ForSqlite()
                .AsInsertSelect("SELECT [name], [email], 30, 8000, 1, 1, datetime('now'), 0, 4.0 FROM [user] WHERE [id] = 1");

            Console.WriteLine($"📋 INSERT SELECT 示例:");
            Console.WriteLine($"   SQL: {insertSelectQuery.ToSql()}");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示 UPDATE 操作
        /// </summary>
        private static async Task DemonstrateUpdateOperationsAsync(SqliteConnection connection)
        {
            Console.WriteLine("✏️ === UPDATE 操作演示 ===");

            // 基本更新
            var updateQuery = ExpressionToSql<User>.ForSqlite()
                .Update()
                .Set(u => u.Salary, 9000)
                .Set(u => u.Bonus, 1500)
                .Where(u => u.Id == 1);

            Console.WriteLine($"💵 更新员工薪资和奖金:");
            Console.WriteLine($"   SQL: {updateQuery.ToSql()}");

            // 条件更新
            var conditionalUpdate = ExpressionToSql<Product>.ForSqlite()
                .Update()
                .Set(p => p.Price, 1999.99m)
                .Where(p => p.Name.Contains("鼠标"));

            Console.WriteLine($"🖱️ 更新鼠标价格:");
            Console.WriteLine($"   SQL: {conditionalUpdate.ToSql()}");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示 DELETE 操作
        /// </summary>
        private static async Task DemonstrateDeleteOperationsAsync(SqliteConnection connection)
        {
            Console.WriteLine("🗑️ === DELETE 操作演示 ===");

            // 条件删除
            var deleteQuery = ExpressionToSql<User>.ForSqlite()
                .Delete(u => !u.IsActive);

            Console.WriteLine($"🚫 删除非活跃用户:");
            Console.WriteLine($"   SQL: {deleteQuery.ToSql()}");

            // 复杂条件删除
            var complexDelete = ExpressionToSql<Product>.ForSqlite()
                .Delete()
                .Where(p => p.Price < 100 && !p.Is_active);

            Console.WriteLine($"💸 删除低价非活跃产品:");
            Console.WriteLine($"   SQL: {complexDelete.ToSql()}");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示复杂查询功能
        /// </summary>
        private static async Task DemonstrateComplexQueriesAsync(SqliteConnection connection)
        {
            Console.WriteLine("🧩 === 复杂查询演示 ===");

            // 分页查询
            var pagedQuery = ExpressionToSql<User>.ForSqlite()
                .Select(u => new { u.Name, u.Email, u.Salary })
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(0)
                .Take(2);

            Console.WriteLine($"📄 分页查询 (前2条记录):");
            Console.WriteLine($"   SQL: {pagedQuery.ToSql()}");

            // 聚合查询
            var avgQuery = ExpressionToSql<User>.ForSqlite()
                .Select("AVG([salary]) as AvgSalary, COUNT(*) as UserCount")
                .Where(u => u.IsActive);

            Console.WriteLine($"📊 聚合查询 (平均薪资):");
            Console.WriteLine($"   SQL: {avgQuery.ToSql()}");

            // 使用 Any 占位符
            var anyQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Age > Any.Int("minAge") && u.Salary > Any.Value<decimal>("minSalary"));

            Console.WriteLine($"🎲 Any 占位符查询:");
            Console.WriteLine($"   SQL: {anyQuery.ToSql()}");

            Console.WriteLine($"💡 提示: Any 占位符在实际使用时会被参数化处理");
            Console.WriteLine();
        }
    }
}
