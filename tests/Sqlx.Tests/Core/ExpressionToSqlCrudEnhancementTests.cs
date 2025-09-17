// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlCrudEnhancementTests.cs" company="Cricle">
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
    /// ExpressionToSql CRUD 增强功能单元测试
    /// </summary>
    [TestClass]
    public class ExpressionToSqlCrudEnhancementTests
    {
        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal? Salary { get; set; }
        }

        #region SELECT Expression Tests

        [TestMethod]
        public void Select_WithSingleExpression_GeneratesCorrectColumns()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Select(e => new { e.Id, e.Name, e.Email });

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("SELECT [Id], [Name], [Email] FROM [TestEntity]"));
        }

        [TestMethod]
        public void Select_WithMultipleExpressions_GeneratesCorrectColumns()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Select(e => e.Id, e => e.Name)
                .Where(e => e.IsActive);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("SELECT [Id], [Name] FROM [TestEntity]"));
            Assert.IsTrue(sql.Contains("WHERE [IsActive] = 1"));
        }

        [TestMethod]
        public void Select_WithStringColumns_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Select("Id", "Name", "Email")
                .Where(e => e.Age > 18);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("SELECT Id, Name, Email FROM [TestEntity]"));
            Assert.IsTrue(sql.Contains("WHERE [Age] > 18"));
        }

        #endregion

        #region INSERT Tests

        [TestMethod]
        public void InsertInto_AutoInferAllColumns_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .InsertInto()
                .Values(1, "张三", "zhang@test.com", 25, true, DateTime.Now, 5000m);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("INSERT INTO [TestEntity]"));
            Assert.IsTrue(sql.Contains("([Id], [Name], [Email], [Age], [IsActive], [CreatedAt], [Salary])"));
            Assert.IsTrue(sql.Contains("VALUES"));
        }

        [TestMethod]
        public void Insert_WithSpecificColumns_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Insert(e => new { e.Name, e.Email, e.Age })
                .Values("李四", "li@test.com", 30);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("INSERT INTO [TestEntity]"));
            Assert.IsTrue(sql.Contains("([Name], [Email], [Age])"));
            Assert.IsTrue(sql.Contains("VALUES ('李四', 'li@test.com', 30)"));
        }

        [TestMethod]
        public void Insert_MultipleValues_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .InsertInto()
                .Values(1, "用户1", "user1@test.com", 25, true, DateTime.Now, 5000m)
                .AddValues(2, "用户2", "user2@test.com", 30, false, DateTime.Now, 6000m);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("INSERT INTO [TestEntity]"));
            Assert.IsTrue(sql.Contains("VALUES"));
            // 应该包含两组VALUES
            var valuesCount = sql.Split("(").Length - 1; // 计算括号数量来判断VALUES组数
            Assert.IsTrue(valuesCount >= 2, "Should contain multiple value sets");
        }

        #endregion

        #region UPDATE Tests

        [TestMethod]
        public void Set_AutoSwitchesToUpdateMode_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Set(e => e.Name, "新名称")
                .Set(e => e.Age, 35)
                .Where(e => e.Id == 1);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("UPDATE [TestEntity] SET"));
            Assert.IsTrue(sql.Contains("[Name] = '新名称'"));
            Assert.IsTrue(sql.Contains("[Age] = 35"));
            Assert.IsTrue(sql.Contains("WHERE ([Id] = 1)"));
        }

        [TestMethod]
        public void Set_WithExpression_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Set(e => e.Age, e => e.Age + 1)
                .Where(e => e.IsActive);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("UPDATE [TestEntity] SET"));
            Assert.IsTrue(sql.Contains("[Age] = [Age] + 1"));
            Assert.IsTrue(sql.Contains("WHERE ([IsActive] = 1)"));
        }

        [TestMethod]
        public void Update_ExplicitMode_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Update()
                .Set(e => e.Name, "更新名称")
                .Where(e => e.Id == 5);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("UPDATE [TestEntity] SET"));
            Assert.IsTrue(sql.Contains("[Name] = '更新名称'"));
            Assert.IsTrue(sql.Contains("WHERE ([Id] = 5)"));
        }

        #endregion

        #region DELETE Tests

        [TestMethod]
        public void Delete_WithCondition_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Delete(e => e.IsActive == false);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("DELETE FROM [TestEntity]"));
            Assert.IsTrue(sql.Contains("WHERE [IsActive] = 0"));
        }

        [TestMethod]
        public void Delete_WithExplicitWhere_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Delete()
                .Where(e => e.Age < 18);

            var sql = query.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("DELETE FROM [TestEntity]"));
            Assert.IsTrue(sql.Contains("WHERE [Age] < 18"));
        }

        [TestMethod]
        public void Delete_WithoutWhere_ThrowsException()
        {
            // Arrange
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Delete();

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => query.ToSql());
            Assert.IsTrue(exception.Message.Contains("DELETE operation requires WHERE clause for safety"));
        }

        #endregion

        #region Multi-Database Dialect Tests

        [TestMethod]
        public void CrudOperations_AllDialects_GenerateCorrectSyntax()
        {
            // SQL Server
            using var sqlServer = ExpressionToSql<TestEntity>.ForSqlServer()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);
            Assert.IsTrue(sqlServer.ToSql().Contains("[Id], [Name]"));

            // MySQL
            using var mysql = ExpressionToSql<TestEntity>.ForMySql()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);
            Assert.IsTrue(mysql.ToSql().Contains("`Id`, `Name`"));

            // PostgreSQL
            using var postgres = ExpressionToSql<TestEntity>.ForPostgreSQL()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);
            Assert.IsTrue(postgres.ToSql().Contains("\"Id\", \"Name\""));

            // SQLite
            using var sqlite = ExpressionToSql<TestEntity>.ForSqlite()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);
            var sqliteSql = sqlite.ToSql();
            Assert.IsTrue(sqliteSql.Contains("Id, Name") || sqliteSql.Contains("[Id], [Name]"), $"SQLite SQL: {sqliteSql}");
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public void Select_WithNullExpression_HandlesGracefully()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Select((Expression<Func<TestEntity, object>>)null!)
                .Where(e => e.IsActive);

            var sql = query.ToSql();

            // Assert - Should fall back to SELECT * 
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("SELECT * FROM [TestEntity]"));
        }

        [TestMethod]
        public void Set_WithNullSelector_HandlesGracefully()
        {
            // Arrange & Act
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Set((Expression<Func<TestEntity, string>>)null!, "test")
                .Where(e => e.Id == 1);

            var sql = query.ToSql();

            // Assert - Should generate UPDATE without the null SET clause
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("UPDATE [TestEntity]"));
        }

        #endregion
    }
}
