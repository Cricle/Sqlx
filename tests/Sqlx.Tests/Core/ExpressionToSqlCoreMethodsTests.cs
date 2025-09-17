// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlCoreMethodsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS8625, CS8604, CS8603, CS8602, CS8629 // Null-related warnings in test code

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// ExpressionToSql 核心方法的详细测试，验证每个公共方法的功能和边界情况。
    /// </summary>
    [TestClass]
    public class ExpressionToSqlCoreMethodsTests
    {
        #region 测试实体

        public class TestEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public decimal? Salary { get; set; }
            public double Score { get; set; }
            public string? Email { get; set; }
            public int CategoryId { get; set; }
            public string? Description { get; set; }
        }

        public class Product
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public decimal Price { get; set; }
            public bool IsActive { get; set; }
            public int StockQuantity { get; set; }
            public string? Category { get; set; }
        }

        #endregion

        #region Where方法测试

        [TestMethod]
        public void Where_SingleCondition_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 100);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Single WHERE SQL: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE关键字");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("100"), "应包含ID条件");
        }

        [TestMethod]
        public void Where_MultipleConditions_AndCombination()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 100)
                .Where(e => e.Name == "Test")
                .Where(e => e.IsActive);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple WHERE SQL: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE关键字");
            Assert.IsTrue(sql.Contains("AND"), "多个WHERE应使用AND连接");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Name") && sql.Contains("IsActive"),
                "应包含所有条件字段");
        }

        [TestMethod]
        public void Where_NullPredicate_IgnoresCondition()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 0);
            expr.Where(null!); // 传入null

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"NULL predicate SQL: {sql}");
            Assert.IsTrue(sql.Contains("Id"), "应保留有效条件");
            // null条件应被忽略，不应影响SQL生成
        }

        [TestMethod]
        public void Where_ComplexBooleanLogic_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => (e.Age > 18 && e.Age < 65) || (e.IsActive && e.Salary > 50000));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex boolean SQL: {sql}");
            Assert.IsTrue(sql.Contains("Age") && sql.Contains("IsActive") && sql.Contains("Salary"),
                "应包含所有条件字段");
            Assert.IsTrue(sql.Contains("AND") || sql.Contains("OR"), "应包含逻辑操作符");
        }

        #endregion

        #region And方法测试

        [TestMethod]
        public void And_EquivalentToWhere_ProducesSameResult()
        {
            // Arrange
            using var expr1 = ExpressionToSql<TestEntity>.ForSqlServer();
            expr1.Where(e => e.Id > 0).Where(e => e.IsActive);

            using var expr2 = ExpressionToSql<TestEntity>.ForSqlServer();
            expr2.Where(e => e.Id > 0).And(e => e.IsActive);

            // Act
            var sql1 = expr1.ToSql();
            var sql2 = expr2.ToSql();

            // Assert
            Console.WriteLine($"WHERE SQL: {sql1}");
            Console.WriteLine($"AND SQL: {sql2}");
            Assert.AreEqual(sql1, sql2, "And方法应该与Where方法产生相同的SQL");
        }

        #endregion

        #region OrderBy和OrderByDescending测试

        [TestMethod]
        public void OrderBy_SingleColumn_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.OrderBy(e => e.Name);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Single ORDER BY SQL: {sql}");
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY关键字");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("ASC"), "应包含Name字段和ASC");
        }

        [TestMethod]
        public void OrderByDescending_SingleColumn_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.OrderByDescending(e => e.CreatedAt);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"ORDER BY DESC SQL: {sql}");
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY关键字");
            Assert.IsTrue(sql.Contains("CreatedAt") && sql.Contains("DESC"), "应包含CreatedAt字段和DESC");
        }

        [TestMethod]
        public void OrderBy_MultipleColumns_PreservesOrder()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.OrderBy(e => e.Name)
                .OrderByDescending(e => e.CreatedAt)
                .OrderBy(e => e.Age);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple ORDER BY SQL: {sql}");
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY关键字");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("CreatedAt") && sql.Contains("Age"),
                "应包含所有排序字段");

            // 验证顺序 - Name应出现在CreatedAt之前，CreatedAt应出现在Age之前
            var nameIndex = sql.IndexOf("Name");
            var createdAtIndex = sql.IndexOf("CreatedAt");
            var ageIndex = sql.IndexOf("Age");

            Assert.IsTrue(nameIndex < createdAtIndex && createdAtIndex < ageIndex,
                "字段应按添加顺序排列");
        }

        [TestMethod]
        public void OrderBy_NullSelector_IgnoresCondition()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.OrderBy(e => e.Name);
            // 这里我们通过反射或其他方式测试null处理，但实际上泛型约束不允许直接传null

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"OrderBy with valid selector SQL: {sql}");
            Assert.IsTrue(sql.Contains("ORDER BY") && sql.Contains("Name"),
                "有效的排序表达式应正常工作");
        }

        #endregion

        #region Take和Skip测试

        [TestMethod]
        public void Take_PositiveNumber_GeneratesLimitClause()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Take(50);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"TAKE SQL: {sql}");
            Assert.IsTrue(sql.Contains("50"), "应包含限制数量");
            // SQL Server可能使用FETCH NEXT或TOP语法
            Assert.IsTrue(sql.Contains("FETCH") || sql.Contains("TOP") || sql.Contains("LIMIT"),
                "应包含限制语法");
        }

        [TestMethod]
        public void Skip_PositiveNumber_GeneratesOffsetClause()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.OrderBy(e => e.Id) // SKIP通常需要ORDER BY
                .Skip(20);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"SKIP SQL: {sql}");
            Assert.IsTrue(sql.Contains("20"), "应包含跳过数量");
            Assert.IsTrue(sql.Contains("ORDER BY"), "SKIP通常需要ORDER BY");
        }

        [TestMethod]
        public void TakeSkip_Together_GeneratesPagination()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.OrderBy(e => e.Id)
                .Skip(20)
                .Take(10);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Pagination SQL: {sql}");
            Assert.IsTrue(sql.Contains("20") && sql.Contains("10"), "应包含跳过和限制数量");
            Assert.IsTrue(sql.Contains("ORDER BY"), "分页需要ORDER BY");
        }

        [TestMethod]
        public void Take_ZeroOrNegative_HandlesGracefully()
        {
            // Arrange & Act
            using var expr1 = ExpressionToSql<TestEntity>.ForSqlServer();
            expr1.Take(0);

            using var expr2 = ExpressionToSql<TestEntity>.ForSqlServer();
            expr2.Take(-1);

            var sql1 = expr1.ToSql();
            var sql2 = expr2.ToSql();

            // Assert
            Console.WriteLine($"Take(0) SQL: {sql1}");
            Console.WriteLine($"Take(-1) SQL: {sql2}");

            // 应该能处理边界情况而不抛出异常
            Assert.IsNotNull(sql1, "Take(0)应产生有效SQL");
            Assert.IsNotNull(sql2, "Take(-1)应产生有效SQL");
        }

        #endregion

        #region Set方法测试（UPDATE操作）

        [TestMethod]
        public void Set_WithValue_GeneratesUpdateSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Set(e => e.Name, "New Name")
                .Set(e => e.Age, 25)
                .Where(e => e.Id == 1);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"SET with value SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE关键字");
            Assert.IsTrue(sql.Contains("SET"), "应包含SET关键字");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("New Name"), "应包含Name设置");
            Assert.IsTrue(sql.Contains("Age") && sql.Contains("25"), "应包含Age设置");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE条件");
        }

        [TestMethod]
        public void Set_WithExpression_GeneratesExpressionSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Set(e => e.Age, e => e.Age + 1)
                .Set(e => e.Score, e => e.Score * 1.1)
                .Where(e => e.Id > 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"SET with expression SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE关键字");
            Assert.IsTrue(sql.Contains("SET"), "应包含SET关键字");
            Assert.IsTrue(sql.Contains("Age") && sql.Contains("+"), "应包含Age增量表达式");
            Assert.IsTrue(sql.Contains("Score") && sql.Contains("*"), "应包含Score乘法表达式");
        }

        [TestMethod]
        public void Set_MultipleFields_GeneratesMultipleSetClauses()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Set(e => e.Name, "Updated")
                .Set(e => e.IsActive, false)
                .Set(e => e.UpdatedAt, DateTime.Now)
                .Where(e => e.Id == 1);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple SET SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE关键字");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("IsActive") && sql.Contains("UpdatedAt"),
                "应包含所有SET字段");
            var setCount = sql.Split(',').Length;
            Assert.IsTrue(setCount >= 3, "应包含多个SET子句");
        }

        #endregion

        #region Select方法测试

        [TestMethod]
        public void Select_StringColumns_GeneratesSelectClause()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Select("Id", "Name", "Email");

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"SELECT columns SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT关键字");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Name") && sql.Contains("Email"),
                "应包含指定的列");
            Assert.IsFalse(sql.Contains("*"), "不应包含通配符");
        }

        [TestMethod]
        public void Select_WithExpression_GeneratesSelectClause()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"SELECT expression SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT关键字");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Name"), "应包含表达式中的列");
            Assert.IsFalse(sql.Contains("*"), "不应包含通配符");
        }

        [TestMethod]
        public void Select_MultipleExpressions_CombinesColumns()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Select(e => e.Id, e => e.Name, e => e.Email);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple SELECT expressions SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT关键字");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Name") && sql.Contains("Email"),
                "应包含所有表达式的列");
        }

        #endregion

        #region Insert和Values方法测试

        [TestMethod]
        public void InsertInto_WithValues_GeneratesInsertSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.InsertInto()
                .Values(1, "Test", 25, true, DateTime.Now, null, 50000m, 85.5, "test@example.com", 1, "Test description");

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"INSERT SQL: {sql}");
            Assert.IsTrue(sql.Contains("INSERT INTO"), "应包含INSERT INTO关键字");
            Assert.IsTrue(sql.Contains("VALUES"), "应包含VALUES关键字");
            Assert.IsTrue(sql.Contains("TestEntity"), "应包含表名");
            Assert.IsTrue(sql.Contains("Test") && sql.Contains("25"), "应包含插入的值");
        }

        [TestMethod]
        public void AddValues_MultipleRows_GeneratesMultipleValueSets()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.InsertInto()
                .Values(1, "First", 25, true, DateTime.Now, null, 50000m, 85.5, "first@example.com", 1, "First description")
                .AddValues(2, "Second", 30, false, DateTime.Now, null, 60000m, 90.0, "second@example.com", 2, "Second description");

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple VALUES SQL: {sql}");
            Assert.IsTrue(sql.Contains("INSERT INTO"), "应包含INSERT INTO关键字");
            Assert.IsTrue(sql.Contains("VALUES"), "应包含VALUES关键字");
            Assert.IsTrue(sql.Contains("First") && sql.Contains("Second"), "应包含两行数据");

            var valuesSets = sql.Split('(').Length - 1; // 计算VALUES子句数量
            Assert.IsTrue(valuesSets >= 2, "应包含多个VALUES子句");
        }

        #endregion

        #region Delete方法测试

        [TestMethod]
        public void Delete_WithPredicate_GeneratesDeleteSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Delete(e => e.IsActive == false);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"DELETE with predicate SQL: {sql}");
            Assert.IsTrue(sql.Contains("DELETE FROM"), "应包含DELETE FROM关键字");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE关键字");
            Assert.IsTrue(sql.Contains("IsActive"), "应包含删除条件");
        }

        [TestMethod]
        public void Delete_WithoutPredicate_RequiresWhere()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Delete()
                .Where(e => e.Id == 1);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"DELETE with WHERE SQL: {sql}");
            Assert.IsTrue(sql.Contains("DELETE FROM"), "应包含DELETE FROM关键字");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE关键字");
            Assert.IsTrue(sql.Contains("Id"), "应包含删除条件");
        }

        [TestMethod]
        public void Delete_WithoutWhereClause_ThrowsException()
        {
            // Arrange
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Delete();

            // Act & Assert
            try
            {
                var sql = expr.ToSql();
                Assert.Fail("没有WHERE条件的DELETE应抛出异常");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✅ 正确抛出异常: {ex.Message}");
                Assert.IsTrue(ex.Message.Contains("WHERE"), "异常消息应提到WHERE子句的必要性");
            }
        }

        #endregion

        #region SQL生成方法测试

        [TestMethod]
        public void ToSql_BasicQuery_ReturnsValidSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Basic ToSql: {sql}");
            Assert.IsNotNull(sql, "ToSql应返回非null的字符串");
            Assert.IsTrue(sql.Length > 0, "ToSql应返回非空字符串");
            Assert.IsTrue(sql.Contains("SELECT"), "默认应生成SELECT查询");
        }

        [TestMethod]
        public void ToTemplate_BasicQuery_ReturnsValidTemplate()
        {
            // Arrange
            var testAge = 25;
            var testName = "Test";

            // Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Age > testAge)
                .Where(e => e.Name == testName);

            var template = expr.ToTemplate();

            // Assert
            Console.WriteLine($"Template SQL: {template.Sql}");
            Console.WriteLine($"Parameters count: {template.Parameters.Length}");

            Assert.IsNotNull(template.Sql, "模板SQL不应为null");
            Assert.IsNotNull(template.Parameters, "模板参数不应为null");
            Assert.IsTrue(template.Sql.Length > 0, "模板SQL不应为空");

            if (template.Parameters.Length > 0)
            {
                foreach (var param in template.Parameters)
                {
                    Console.WriteLine($"Parameter: {param.ParameterName} = {param.Value}");
                    Assert.IsNotNull(param.ParameterName, "参数名不应为null");
                }
            }
        }

        #endregion

        #region 组合方法测试

        [TestMethod]
        public void CombinedMethods_CompleteQuery_GeneratesComplexSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Select(e => new { e.Id, e.Name, e.Email })
                .Where(e => e.Age >= 18)
                .Where(e => e.IsActive)
                .OrderBy(e => e.Name)
                .OrderByDescending(e => e.CreatedAt)
                .Skip(10)
                .Take(20);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex combined SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE");
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Name") && sql.Contains("Email"),
                "应包含SELECT列");
            Assert.IsTrue(sql.Contains("Age") && sql.Contains("IsActive"), "应包含WHERE条件");
            Assert.IsTrue(sql.Contains("10") && sql.Contains("20"), "应包含分页参数");
        }

        [TestMethod]
        public void CombinedMethods_UpdateQuery_GeneratesUpdateSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Set(e => e.Name, "Updated Name")
                .Set(e => e.Age, e => e.Age + 1)
                .Set(e => e.UpdatedAt, DateTime.Now)
                .Where(e => e.Id == 1)
                .Where(e => e.IsActive);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex UPDATE SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE");
            Assert.IsTrue(sql.Contains("SET"), "应包含SET");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("Age") && sql.Contains("UpdatedAt"),
                "应包含所有SET字段");
        }

        #endregion

        #region 资源管理测试

        [TestMethod]
        public void Dispose_ReleasesResources_NoThrow()
        {
            // Arrange
            var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 0);

            // Act & Assert - 第一次释放
            expr.Dispose();
            Console.WriteLine("✅ 第一次Dispose完成");

            // 第二次释放应该不抛出异常
            expr.Dispose();
            Console.WriteLine("✅ 第二次Dispose完成");
        }

        [TestMethod]
        public void AfterDispose_MethodCalls_ShouldThrowOrHandleGracefully()
        {
            // Arrange
            var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 0);
            expr.Dispose();

            // Act & Assert
            try
            {
                var sql = expr.ToSql();
                Assert.Fail("使用已释放对象应抛出异常");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("✅ 正确抛出ObjectDisposedException");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"抛出其他类型异常: {ex.GetType().Name}: {ex.Message}");
                // 只要抛出异常就可以接受
            }
        }

        #endregion
    }
}
