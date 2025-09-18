// -----------------------------------------------------------------------
// <copyright file="SqlTemplateWithPartialMethodsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS0618 // Type or member is obsolete - Testing obsolete API for compatibility

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// SqlTemplate 与部分方法结合使用的单元测试
    /// </summary>
    [TestClass]
    public class SqlTemplateWithPartialMethodsTests
    {
        private SqliteConnection _connection = null!;

        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public int DepartmentId { get; set; }
            public decimal Salary { get; set; }
            public DateTime HireDate { get; set; }
        }

        // 测试用的服务接口
        public interface IUserTemplateService
        {
            Task<IList<TestUser>> QueryUsersAsync(SqlTemplate template);
            Task<TestUser?> GetUserByIdAsync(SqlTemplate template);
            Task<int> ExecuteTemplateAsync(SqlTemplate template);
            Task<int> GetUserCountAsync(SqlTemplate template);
        }

        // 手动实现的测试服务（模拟源代码生成器的输出）
        public class UserTemplateService : IUserTemplateService
        {
            private readonly DbConnection _connection;

            public UserTemplateService(DbConnection connection)
            {
                _connection = connection;
            }

            public async Task<IList<TestUser>> QueryUsersAsync(SqlTemplate template)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = template.Sql;

                foreach (var param in template.Parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }

                var results = new List<TestUser>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new TestUser
                    {
                        Id = reader.GetInt32("Id"),
                        Name = reader.GetString("Name"),
                        Email = reader.GetString("Email"),
                        Age = reader.GetInt32("Age"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DepartmentId = reader.GetInt32("DepartmentId"),
                        Salary = reader.GetDecimal("Salary"),
                        HireDate = reader.GetDateTime("HireDate")
                    });
                }

                return results;
            }

            public async Task<TestUser?> GetUserByIdAsync(SqlTemplate template)
            {
                var users = await QueryUsersAsync(template);
                return users.Count > 0 ? users[0] : null;
            }

            public async Task<int> ExecuteTemplateAsync(SqlTemplate template)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = template.Sql;

                foreach (var param in template.Parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }

                return await command.ExecuteNonQueryAsync();
            }

            public async Task<int> GetUserCountAsync(SqlTemplate template)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = template.Sql;

                foreach (var param in template.Parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        [TestInitialize]
        public async Task Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            await _connection.OpenAsync();

            // 创建测试表
            var createTableSql = @"
                CREATE TABLE TestUser (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Age INTEGER NOT NULL,
                    IsActive BOOLEAN NOT NULL,
                    DepartmentId INTEGER NOT NULL,
                    Salary DECIMAL NOT NULL,
                    HireDate DATETIME NOT NULL
                )";

            using var command = _connection.CreateCommand();
            command.CommandText = createTableSql;
            await command.ExecuteNonQueryAsync();

            // 插入测试数据
            await InsertTestData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _connection?.Dispose();
        }

        private async Task InsertTestData()
        {
            var insertSql = @"
                INSERT INTO TestUser (Name, Email, Age, IsActive, DepartmentId, Salary, HireDate) VALUES
                ('张三', 'zhangsan@example.com', 28, 1, 1, 8000.00, '2020-01-15'),
                ('李四', 'lisi@example.com', 32, 1, 2, 12000.00, '2019-03-10'),
                ('王五', 'wangwu@example.com', 25, 0, 1, 6000.00, '2021-06-20'),
                ('赵六', 'zhaoliu@example.com', 35, 1, 3, 15000.00, '2018-12-05'),
                ('钱七', 'qianqi@example.com', 29, 1, 2, 9500.00, '2020-08-18')";

            using var command = _connection.CreateCommand();
            command.CommandText = insertSql;
            await command.ExecuteNonQueryAsync();
        }

        [TestMethod]
        public async Task SqlTemplate_WithExpressionToSql_WorksCorrectly()
        {
            // 测试：使用 ExpressionToSql 生成 SqlTemplate
            var service = new UserTemplateService(_connection);

            using var query = ExpressionToSql<TestUser>.ForSqlite()
                .Where(u => u.Age > Any.Int("minAge") && u.IsActive == Any.Bool("isActive"))
                .OrderBy(u => u.Name);

            var template = query.ToTemplate();

            // 设置实际参数值
            var parameters = new Dictionary<string, object?>
            {
                ["@minAge"] = 25,
                ["@isActive"] = true
            };

            var actualTemplate = new SqlTemplate(template.Sql, parameters);
            var users = await service.QueryUsersAsync(actualTemplate);

            Assert.IsTrue(users.Count >= 2, "应该返回至少2个用户");
            
            foreach (var user in users)
            {
                Assert.IsTrue(user.Age > 25, $"用户 {user.Name} 的年龄应该大于25");
                Assert.IsTrue(user.IsActive, $"用户 {user.Name} 应该是活跃状态");
            }

            Console.WriteLine($"✅ ExpressionToSql + SqlTemplate 测试成功");
            Console.WriteLine($"   查询SQL: {actualTemplate.Sql}");
            Console.WriteLine($"   返回用户数: {users.Count}");
        }

        [TestMethod]
        public async Task SqlTemplate_DirectCreation_WorksCorrectly()
        {
            // 测试：直接创建 SqlTemplate
            var service = new UserTemplateService(_connection);

            var template = SqlTemplate.Create(@"
                SELECT * FROM TestUser 
                WHERE DepartmentId = @deptId AND Salary >= @minSalary
                ORDER BY Salary DESC",
                new { deptId = 2, minSalary = 10000 });

            var users = await service.QueryUsersAsync(template);

            Assert.IsTrue(users.Count >= 1, "应该返回至少1个用户");
            
            foreach (var user in users)
            {
                Assert.AreEqual(2, user.DepartmentId, $"用户 {user.Name} 应该属于部门2");
                Assert.IsTrue(user.Salary >= 10000, $"用户 {user.Name} 的薪资应该>=10000");
            }

            Console.WriteLine($"✅ 直接创建 SqlTemplate 测试成功");
            Console.WriteLine($"   查询SQL: {template.Sql}");
            Console.WriteLine($"   返回用户数: {users.Count}");
        }

        [TestMethod]
        public async Task SqlTemplate_ComplexQuery_WorksCorrectly()
        {
            // 测试：复杂查询 SqlTemplate
            var service = new UserTemplateService(_connection);

            var complexSql = @"
                SELECT u.*, 
                       CASE 
                           WHEN u.Salary > @highSalaryThreshold THEN '高薪'
                           WHEN u.Salary > @mediumSalaryThreshold THEN '中薪'
                           ELSE '普通'
                       END as SalaryLevel
                FROM TestUser u
                WHERE u.IsActive = @isActive 
                AND u.Age BETWEEN @minAge AND @maxAge
                AND u.HireDate >= @hireDate
                ORDER BY u.Salary DESC";

            var template = SqlTemplate.Create(complexSql, new
            {
                highSalaryThreshold = 12000,
                mediumSalaryThreshold = 8000,
                isActive = true,
                minAge = 25,
                maxAge = 35,
                hireDate = new DateTime(2019, 1, 1)
            });

            var users = await service.QueryUsersAsync(template);

            Assert.IsTrue(users.Count >= 1, "应该返回至少1个用户");
            
            foreach (var user in users)
            {
                Assert.IsTrue(user.IsActive, $"用户 {user.Name} 应该是活跃状态");
                Assert.IsTrue(user.Age >= 25 && user.Age <= 35, $"用户 {user.Name} 年龄应该在25-35之间");
                Assert.IsTrue(user.HireDate >= new DateTime(2019, 1, 1), $"用户 {user.Name} 入职时间应该>=2019-01-01");
            }

            Console.WriteLine($"✅ 复杂查询 SqlTemplate 测试成功");
            Console.WriteLine($"   查询SQL: {template.Sql}");
            Console.WriteLine($"   返回用户数: {users.Count}");
        }

        [TestMethod]
        public async Task SqlTemplate_DynamicQuery_WorksCorrectly()
        {
            // 测试：动态构建 SqlTemplate
            var service = new UserTemplateService(_connection);

            var dynamicTemplate = BuildDynamicUserQuery(
                includeInactive: false,
                minAge: 30,
                departmentIds: new List<int> { 1, 2 });

            var users = await service.QueryUsersAsync(dynamicTemplate);

            Assert.IsTrue(users.Count >= 1, "应该返回至少1个用户");
            
            foreach (var user in users)
            {
                Assert.IsTrue(user.IsActive, $"用户 {user.Name} 应该是活跃状态");
                Assert.IsTrue(user.Age >= 30, $"用户 {user.Name} 年龄应该>=30");
                Assert.IsTrue(user.DepartmentId == 1 || user.DepartmentId == 2, 
                    $"用户 {user.Name} 应该属于部门1或2");
            }

            Console.WriteLine($"✅ 动态构建 SqlTemplate 测试成功");
            Console.WriteLine($"   查询SQL: {dynamicTemplate.Sql}");
            Console.WriteLine($"   返回用户数: {users.Count}");
        }

        [TestMethod]
        public async Task SqlTemplate_ExecuteNonQuery_WorksCorrectly()
        {
            // 测试：执行非查询 SqlTemplate
            var service = new UserTemplateService(_connection);

            // 插入新用户
            var insertTemplate = SqlTemplate.Create(@"
                INSERT INTO TestUser (Name, Email, Age, IsActive, DepartmentId, Salary, HireDate)
                VALUES (@name, @email, @age, @isActive, @deptId, @salary, @hireDate)",
                new
                {
                    name = "新用户",
                    email = "newuser@example.com",
                    age = 26,
                    isActive = true,
                    deptId = 1,
                    salary = 7500.00m,
                    hireDate = DateTime.Now
                });

            var insertResult = await service.ExecuteTemplateAsync(insertTemplate);
            Assert.AreEqual(1, insertResult, "应该插入1条记录");

            // 更新用户
            var updateTemplate = SqlTemplate.Create(@"
                UPDATE TestUser 
                SET Salary = @newSalary 
                WHERE Name = @name",
                new { newSalary = 8000.00m, name = "新用户" });

            var updateResult = await service.ExecuteTemplateAsync(updateTemplate);
            Assert.AreEqual(1, updateResult, "应该更新1条记录");

            // 验证更新结果
            var queryTemplate = SqlTemplate.Create(
                "SELECT COUNT(*) FROM TestUser WHERE Name = @name AND Salary = @salary",
                new { name = "新用户", salary = 8000.00m });

            var count = await service.GetUserCountAsync(queryTemplate);
            Assert.AreEqual(1, count, "应该有1个用户的薪资被更新为8000");

            Console.WriteLine($"✅ 执行非查询 SqlTemplate 测试成功");
            Console.WriteLine($"   插入结果: {insertResult}条记录");
            Console.WriteLine($"   更新结果: {updateResult}条记录");
        }

        [TestMethod]
        public async Task SqlTemplate_WithAnyPlaceholders_WorksCorrectly()
        {
            // 测试：使用 Any 占位符的 SqlTemplate
            var service = new UserTemplateService(_connection);

            using var query = ExpressionToSql<TestUser>.ForSqlite()
                .Where(u => u.Age >= Any.Int("minAge") && 
                           u.Salary > Any.Value<decimal>("minSalary") &&
                           u.IsActive == Any.Bool("status"))
                .OrderBy(u => u.Salary);

            var template = query.ToTemplate();

            // 设置不同的参数值进行多次查询
            var testCases = new[]
            {
                new { minAge = 25, minSalary = 7000m, status = true, expectedMinCount = 2 },
                new { minAge = 30, minSalary = 10000m, status = true, expectedMinCount = 1 },
                new { minAge = 20, minSalary = 5000m, status = false, expectedMinCount = 1 }
            };

            foreach (var testCase in testCases)
            {
                var parameters = new Dictionary<string, object?>
                {
                    ["@minAge"] = testCase.minAge,
                    ["@minSalary"] = testCase.minSalary,
                    ["@status"] = testCase.status
                };

                var actualTemplate = new SqlTemplate(template.Sql, parameters);
                var users = await service.QueryUsersAsync(actualTemplate);

                Assert.IsTrue(users.Count >= testCase.expectedMinCount, 
                    $"测试用例 minAge={testCase.minAge}, minSalary={testCase.minSalary}, status={testCase.status} 应该返回至少{testCase.expectedMinCount}个用户，实际返回{users.Count}个");

                Console.WriteLine($"   测试用例: minAge={testCase.minAge}, minSalary={testCase.minSalary}, status={testCase.status} -> 返回{users.Count}个用户");
            }

            Console.WriteLine($"✅ Any占位符 SqlTemplate 测试成功");
        }

        /// <summary>
        /// 动态构建查询模板的辅助方法
        /// </summary>
        private SqlTemplate BuildDynamicUserQuery(
            bool includeInactive = false,
            int? minAge = null,
            List<int>? departmentIds = null)
        {
            var sqlBuilder = new System.Text.StringBuilder("SELECT * FROM TestUser WHERE 1=1");
            var parameters = new Dictionary<string, object?>();

            if (!includeInactive)
            {
                sqlBuilder.Append(" AND IsActive = @isActive");
                parameters["@isActive"] = true;
            }

            if (minAge.HasValue)
            {
                sqlBuilder.Append(" AND Age >= @minAge");
                parameters["@minAge"] = minAge.Value;
            }

            if (departmentIds?.Count > 0)
            {
                var placeholders = new List<string>();
                for (int i = 0; i < departmentIds.Count; i++)
                {
                    var paramName = $"@dept{i}";
                    placeholders.Add(paramName);
                    parameters[paramName] = departmentIds[i];
                }
                sqlBuilder.Append($" AND DepartmentId IN ({string.Join(",", placeholders)})");
            }

            sqlBuilder.Append(" ORDER BY Name");

            return new SqlTemplate(sqlBuilder.ToString(), parameters);
        }
    }
}
