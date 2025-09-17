// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlAdvancedCrudTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// ExpressionToSql 高级 CRUD 功能单元测试
    /// 验证所有新增功能的正确性和边界情况
    /// </summary>
    [TestClass]
    public class ExpressionToSqlAdvancedCrudTests
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal? Salary { get; set; }
            public string? Department { get; set; }
        }

        #region SELECT Expression Tests - 验证SELECT表达式功能

        [TestMethod]
        public void Select_AnonymousObjectExpression_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Select(u => new { u.Id, u.Name, u.Email })
                .Where(u => u.IsActive);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("SELECT [Id], [Name], [Email]"));
            Assert.IsTrue(sql.Contains("FROM [User]"));
            Assert.IsTrue(sql.Contains("WHERE [IsActive] = 1"));

            Console.WriteLine($"✅ SELECT 匿名对象: {sql}");
        }

        [TestMethod]
        public void Select_SinglePropertyExpression_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Select(u => u.Name)
                .Where(u => u.Age > 18);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("SELECT [Name]"));
            Assert.IsTrue(sql.Contains("WHERE [Age] > 18"));

            Console.WriteLine($"✅ SELECT 单属性: {sql}");
        }

        [TestMethod]
        public void Select_MultipleExpressions_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Select(u => u.Id, u => u.Name, u => u.Email)
                .OrderBy(u => u.Name);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("SELECT [Id], [Name], [Email]"));
            Assert.IsTrue(sql.Contains("ORDER BY [Name] ASC"));

            Console.WriteLine($"✅ SELECT 多表达式: {sql}");
        }

        [TestMethod]
        public void Select_WithNullExpression_HandlesGracefully()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer();
            Expression<Func<User, object>>? nullExpression = null;
            query.Select(nullExpression!);

            var sql = query.ToSql();

            // Assert - 应该生成默认的SELECT *
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("SELECT"));

            Console.WriteLine($"✅ SELECT NULL表达式处理: {sql}");
        }

        #endregion

        #region DELETE Operation Tests - 验证DELETE操作功能

        [TestMethod]
        public void Delete_WithoutWhere_ThrowsException()
        {
            // Arrange
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Delete();

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => query.ToSql());
            Assert.IsTrue(exception.Message.Contains("DELETE"));
            Assert.IsTrue(exception.Message.Contains("WHERE"));

            Console.WriteLine($"✅ DELETE 安全检查: {exception.Message}");
        }

        [TestMethod]
        public void Delete_WithWhereCondition_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Delete()
                .Where(u => u.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("DELETE FROM [User]"));
            Assert.IsTrue(sql.Contains("WHERE [Id] = 1"));

            Console.WriteLine($"✅ DELETE 带条件: {sql}");
        }

        [TestMethod]
        public void Delete_WithPredicateDirectly_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Delete(u => u.Age < 18 && !u.IsActive);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("DELETE FROM [User]"));
            Assert.IsTrue(sql.Contains("WHERE"));
            Assert.IsTrue(sql.Contains("[Age] < 18"));
            Assert.IsTrue(sql.Contains("[IsActive] = 0"));

            Console.WriteLine($"✅ DELETE 直接条件: {sql}");
        }

        [TestMethod]
        public void Delete_WithComplexWhere_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Delete()
                .Where(u => u.Department == "IT" && u.Salary > 50000);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("DELETE FROM [User]"));
            Assert.IsTrue(sql.Contains("WHERE"));
            Assert.IsTrue(sql.Contains("[Department] = 'IT'"));
            Assert.IsTrue(sql.Contains("[Salary] > 50000"));

            Console.WriteLine($"✅ DELETE 复杂条件: {sql}");
        }

        #endregion

        #region UPDATE Operation Tests - 验证UPDATE操作功能

        [TestMethod]
        public void Update_AutoModeSwitch_WhenSetCalled()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.Name, "新名字")
                .Where(u => u.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("UPDATE [User] SET"), $"Expected UPDATE statement, got: {sql}");
            Assert.IsTrue(sql.Contains("[Name] = '新名字'"), $"Expected Name assignment, got: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), $"Expected WHERE clause, got: {sql}");

            Console.WriteLine($"✅ UPDATE 自动模式切换: {sql}");
        }

        [TestMethod]
        public void Update_ExpressionSet_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.Age, u => u.Age + 1)
                .Where(u => u.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("UPDATE [User] SET"), $"Expected UPDATE statement, got: {sql}");
            Assert.IsTrue(sql.Contains("[Age] = [Age] + 1"), $"Expected Age expression, got: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), $"Expected WHERE clause, got: {sql}");

            Console.WriteLine($"✅ UPDATE 表达式SET: {sql}");
        }

        [TestMethod]
        public void Update_MultipleSetClauses_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.Name, "更新的名字")
                .Set(u => u.Age, u => u.Age + 1)
                .Set(u => u.IsActive, true)
                .Where(u => u.Department == "HR");

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("UPDATE [User] SET"), $"Expected UPDATE statement, got: {sql}");
            Assert.IsTrue(sql.Contains("[Name] = '更新的名字'"), $"Expected Name assignment, got: {sql}");
            Assert.IsTrue(sql.Contains("[Age] = [Age] + 1"), $"Expected Age expression, got: {sql}");
            Assert.IsTrue(sql.Contains("[IsActive] = 1"), $"Expected IsActive assignment, got: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), $"Expected WHERE clause, got: {sql}");

            Console.WriteLine($"✅ UPDATE 多SET子句: {sql}");
        }

        [TestMethod]
        public void Update_ExplicitUpdateCall_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Update()
                .Set(u => u.Email, "new@example.com")
                .Where(u => u.Id == 5);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("UPDATE [User] SET"));
            Assert.IsTrue(sql.Contains("[Email] = 'new@example.com'"));

            Console.WriteLine($"✅ UPDATE 显式调用: {sql}");
        }

        #endregion

        #region INSERT Operation Tests - 验证INSERT操作功能

        [TestMethod]
        public void Insert_WithColumns_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Insert(u => new { u.Name, u.Email, u.Age })
                .Values("张三", "zhang@example.com", 25);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("INSERT INTO [User]"));
            Assert.IsTrue(sql.Contains("([Name], [Email], [Age])"));
            Assert.IsTrue(sql.Contains("VALUES ('张三', 'zhang@example.com', 25)"));

            Console.WriteLine($"✅ INSERT 指定列: {sql}");
        }

        [TestMethod]
        public void InsertInto_AllColumns_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .InsertInto()
                .Values(1, "李四", "li@example.com", 30, true, DateTime.Now, 60000m, "IT");

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("INSERT INTO [User]"));
            Assert.IsTrue(sql.Contains("VALUES"));

            Console.WriteLine($"✅ INSERT INTO 所有列: {sql}");
        }

        [TestMethod]
        public void Insert_MultipleValues_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Insert(u => new { u.Name, u.Age })
                .Values("用户1", 25)
                .Values("用户2", 30);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("INSERT INTO [User]"));
            Assert.IsTrue(sql.Contains("([Name], [Age])"));
            Assert.IsTrue(sql.Contains("VALUES ('用户1', 25), ('用户2', 30)"));

            Console.WriteLine($"✅ INSERT 多行数据: {sql}");
        }

        #endregion

        #region Database Dialect Tests - 验证多数据库方言支持

        [TestMethod]
        public void CRUD_SqlServer_GeneratesCorrectSyntax()
        {
            // SELECT
            using var select = ExpressionToSql<User>.ForSqlServer()
                .Select(u => new { u.Id, u.Name })
                .Where(u => u.IsActive);
            Assert.IsTrue(select.ToSql().Contains("[Id], [Name]"));

            // UPDATE
            using var update = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.Name, "测试")
                .Where(u => u.Id == 1);
            Assert.IsTrue(update.ToSql().Contains("UPDATE [User] SET [Name] = '测试'"));

            // DELETE
            using var delete = ExpressionToSql<User>.ForSqlServer()
                .Delete(u => u.Id == 1);
            Assert.IsTrue(delete.ToSql().Contains("DELETE FROM [User]"));

            Console.WriteLine("✅ SQL Server 方言测试通过");
        }

        [TestMethod]
        public void CRUD_MySQL_GeneratesCorrectSyntax()
        {
            // SELECT
            using var select = ExpressionToSql<User>.ForMySql()
                .Select(u => new { u.Id, u.Name })
                .Where(u => u.IsActive);
            var selectSql = select.ToSql();
            Assert.IsTrue(selectSql.Contains("`Id`, `Name`") || selectSql.Contains("Id, Name"));

            // UPDATE
            using var update = ExpressionToSql<User>.ForMySql()
                .Set(u => u.Name, "测试")
                .Where(u => u.Id == 1);
            Assert.IsTrue(update.ToSql().Contains("UPDATE"));

            Console.WriteLine("✅ MySQL 方言测试通过");
        }

        [TestMethod]
        public void CRUD_PostgreSQL_GeneratesCorrectSyntax()
        {
            // SELECT
            using var select = ExpressionToSql<User>.ForPostgreSQL()
                .Select(u => new { u.Id, u.Name })
                .Where(u => u.IsActive);
            Assert.IsTrue(select.ToSql().Contains("SELECT"));

            // UPDATE
            using var update = ExpressionToSql<User>.ForPostgreSQL()
                .Set(u => u.Name, "测试")
                .Where(u => u.Id == 1);
            Assert.IsTrue(update.ToSql().Contains("UPDATE"));

            Console.WriteLine("✅ PostgreSQL 方言测试通过");
        }

        #endregion

        #region Edge Cases and Error Handling - 边界情况和错误处理

        [TestMethod]
        public void EdgeCase_NullValues_HandledCorrectly()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.Department, (string?)null)
                .Where(u => u.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("[Department] = NULL"));

            Console.WriteLine($"✅ NULL值处理: {sql}");
        }

        [TestMethod]
        public void EdgeCase_BooleanValues_ConvertedCorrectly()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.IsActive, false)
                .Where(u => u.IsActive == true);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("[IsActive] = 0"));
            Assert.IsTrue(sql.Contains("[IsActive] = 1"));

            Console.WriteLine($"✅ 布尔值转换: {sql}");
        }

        [TestMethod]
        public void EdgeCase_DateTimeValues_FormattedCorrectly()
        {
            // Arrange & Act
            var testDate = new DateTime(2024, 1, 1, 12, 0, 0);
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.CreatedAt, testDate)
                .Where(u => u.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("2024"));

            Console.WriteLine($"✅ 日期时间格式化: {sql}");
        }

        [TestMethod]
        public void EdgeCase_DecimalValues_FormattedCorrectly()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.Salary, 12345.67m)
                .Where(u => u.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("12345.67"));

            Console.WriteLine($"✅ 小数格式化: {sql}");
        }

        [TestMethod]
        public void EdgeCase_StringEscaping_HandledCorrectly()
        {
            // Arrange & Act
            using var query = ExpressionToSql<User>.ForSqlServer()
                .Set(u => u.Name, "O'Connor")
                .Where(u => u.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("'O''Connor'") || sql.Contains("'O\\'Connor'"));

            Console.WriteLine($"✅ 字符串转义: {sql}");
        }

        #endregion

        #region Performance and Memory Tests - 性能和内存测试

        [TestMethod]
        public void Performance_ComplexQuery_ExecutesQuickly()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Arrange & Act
            for (int i = 0; i < 1000; i++)
            {
                using var query = ExpressionToSql<User>.ForSqlServer()
                    .Select(u => new { u.Id, u.Name, u.Email })
                    .Where(u => u.Age > 18 && u.IsActive)
                    .OrderBy(u => u.Name)
                    .Skip(i * 10)
                    .Take(10);

                var sql = query.ToSql();
                Assert.IsNotNull(sql);
            }

            stopwatch.Stop();

            // Assert - 1000次查询应该在1秒内完成
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000,
                $"性能测试失败: {stopwatch.ElapsedMilliseconds}ms > 1000ms");

            Console.WriteLine($"✅ 性能测试: 1000次复杂查询耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void Memory_DisposablePattern_WorksCorrectly()
        {
            // Arrange & Act & Assert
            // 验证using语句正常工作
            using (var query = ExpressionToSql<User>.ForSqlServer().Select(u => u.Id))
            {
                Assert.IsNotNull(query.ToSql());
            }

            // 验证手动Dispose正常工作
            var query2 = ExpressionToSql<User>.ForSqlServer().Select(u => u.Name);
            Assert.IsNotNull(query2.ToSql());
            query2.Dispose();

            Console.WriteLine("✅ 内存管理测试通过");
        }

        #endregion
    }
}
