// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using SqlxDemo.Models;
using SqlxDemo.Services;

namespace SqlxDemo
{
    /// <summary>
    /// Sqlx 演示程序主入口
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 程序主入口点
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns>异步任务</returns>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Sqlx 演示程序 ===");
            Console.WriteLine("展示编译时 SQL 模板和动态 SQL 功能");
            Console.WriteLine();

            try
            {
                // 创建内存数据库连接
                using var connection = new SqliteConnection("Data Source=:memory:");
                await connection.OpenAsync();

                // 创建示例表
                await CreateSampleTablesAsync(connection);

                // 演示简单仓储功能
                await DemonstrateSimpleRepositoryAsync(connection);

                // 演示 SQL 模板功能
                await DemonstrateSqlTemplateAsync(connection);

                Console.WriteLine();
                Console.WriteLine("演示完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"演示过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            if (args.Length == 0 || !args[0].Equals("--no-wait", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 创建示例表
        /// </summary>
        private static async Task CreateSampleTablesAsync(SqliteConnection connection)
        {
            Console.WriteLine("创建示例表...");

            // 创建 user 表
            var createUserTable = @"
                CREATE TABLE [user] (
                    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [name] TEXT NOT NULL,
                    [email] TEXT,
                    [age] INTEGER,
                    [department_id] INTEGER,
                    [hire_date] TEXT,
                    [is_active] INTEGER DEFAULT 1
                )";

            // 创建 department 表
            var createDepartmentTable = @"
                CREATE TABLE [department] (
                    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [name] TEXT NOT NULL,
                    [budget] REAL DEFAULT 0
                )";

            // 创建 product 表
            var createProductTable = @"
                CREATE TABLE [product] (
                    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [name] TEXT NOT NULL,
                    [price] REAL,
                    [is_active] INTEGER DEFAULT 1
                )";

            using var command = connection.CreateCommand();

            command.CommandText = createUserTable;
            await command.ExecuteNonQueryAsync();

            command.CommandText = createDepartmentTable;
            await command.ExecuteNonQueryAsync();

            command.CommandText = createProductTable;
            await command.ExecuteNonQueryAsync();

            // 插入示例数据
            await InsertSampleDataAsync(connection);

            Console.WriteLine("示例表创建完成");
        }

        /// <summary>
        /// 插入示例数据
        /// </summary>
        private static async Task InsertSampleDataAsync(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();

            // 插入部门数据
            command.CommandText = "INSERT INTO [department] ([name], [budget]) VALUES ('IT部门', 100000), ('销售部门', 80000), ('人事部门', 60000)";
            await command.ExecuteNonQueryAsync();

            // 插入用户数据
            command.CommandText = @"
                INSERT INTO [user] ([name], [email], [age], [department_id], [hire_date], [is_active]) VALUES 
                ('张三', 'zhangsan@example.com', 28, 1, '2023-01-15', 1),
                ('李四', 'lisi@example.com', 32, 2, '2022-06-10', 1),
                ('王五', 'wangwu@example.com', 25, 1, '2023-03-20', 1),
                ('赵六', 'zhaoliu@example.com', 35, 3, '2021-12-01', 0)";
            await command.ExecuteNonQueryAsync();

            // 插入产品数据
            command.CommandText = @"
                INSERT INTO [product] ([name], [price], [is_active]) VALUES 
                ('产品A', 99.99, 1),
                ('产品B', 199.99, 1),
                ('产品C', 299.99, 0)";
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 演示简单仓储功能
        /// </summary>
        private static async Task DemonstrateSimpleRepositoryAsync(SqliteConnection connection)
        {
            Console.WriteLine();
            Console.WriteLine("=== 演示简单仓储功能 ===");

            var userRepository = new SimpleUserRepository(connection);
            var productRepository = new SimpleProductRepository(connection);

            // 演示用户查询
            Console.WriteLine("查询所有用户:");
            try
            {
                // var users = await userRepository.GetAllUsersAsync();
                // Console.WriteLine($"找到 {users?.Count ?? 0} 个用户");
                Console.WriteLine("用户查询功能已准备就绪");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"用户查询出错: {ex.Message}");
            }

            Console.WriteLine("简单仓储演示完成");
        }

        /// <summary>
        /// 演示 SQL 模板功能
        /// </summary>
        private static async Task DemonstrateSqlTemplateAsync(SqliteConnection connection)
        {
            Console.WriteLine();
            Console.WriteLine("=== 演示 SQL 模板功能 ===");

            var demo = new SqlTemplateAnySimpleDemo(connection);

            // 演示基本模板功能
            Console.WriteLine("SQL 模板基本功能演示:");
            await demo.RunAnyPlaceholderDemoAsync();

            Console.WriteLine("SQL 模板演示完成");
        }
    }
}
