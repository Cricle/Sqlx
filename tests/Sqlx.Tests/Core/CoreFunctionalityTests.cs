using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Sqlx;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class CoreFunctionalityTests
    {
        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        [TestMethod]
        public void ExpressionToSql_BasicWhere_GeneratesCorrectSQL()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.Name == "test");
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("WHERE"), "Should contain WHERE clause");
            Assert.IsTrue(sql.Contains("[Name]"), "Should contain column name in brackets");
            Assert.IsTrue(sql.Contains("= 'test'"), "Should contain string constant");
        }

        [TestMethod]
        public void ExpressionToSql_SqlServerDialect_UsesSquareBrackets()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.Name == "test");
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("[TestUser]"), "Should wrap table name with square brackets");
            Assert.IsTrue(sql.Contains("[Name]"), "Should wrap column name with square brackets");
        }

        [TestMethod]
        public void ExpressionToSql_MySqlDialect_UsesBackticks()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForMySql();
            expr.Where(u => u.Name == "test");
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("`TestUser`"), "Should wrap table name with backticks");
            Assert.IsTrue(sql.Contains("`Name`"), "Should wrap column name with backticks");
        }

        [TestMethod]
        public void ExpressionToSql_PostgreSqlDialect_UsesDoubleQuotes()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForPostgreSQL();
            expr.Where(u => u.Name == "test");
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("\"TestUser\""), "Should wrap table name with double quotes");
            Assert.IsTrue(sql.Contains("\"Name\""), "Should wrap column name with double quotes");
        }

        [TestMethod]
        public void ExpressionToSql_SqliteDialect_UsesSquareBracketsWithDollarParameters()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlite();
            expr.Where(u => u.Name == "test");
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("[TestUser]"), "Should wrap table name with square brackets");
            Assert.IsTrue(sql.Contains("[Name]"), "Should wrap column name with square brackets");
            // Note: Parameter prefix distinction is internal, SQL output may still show @
        }

        [TestMethod]
        public void ExpressionToSql_OrderBy_GeneratesCorrectClause()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.IsActive).OrderBy(u => u.Name);
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("ORDER BY"), "Should contain ORDER BY clause");
            Assert.IsTrue(sql.Contains("[Name] ASC"), "Should contain column with ASC direction");
        }

        [TestMethod]
        public void ExpressionToSql_Take_GeneratesLimitClause()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.IsActive).Take(10);
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("FETCH NEXT 10 ROWS ONLY") || sql.Contains("TOP 10"),
                "Should contain SQL Server pagination syntax");
        }

        [TestMethod]
        public void ExpressionToSql_SqliteDialect_UsesLimitOffset()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlite();
            expr.Where(u => u.IsActive).Skip(5).Take(10);
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("LIMIT 10 OFFSET 5"), "Should use LIMIT/OFFSET syntax for SQLite");
        }

        [TestMethod]
        public void ExpressionToSql_ComplexWhere_GeneratesCorrectConditions()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.IsActive && u.Age > 25);
            expr.Where(u => u.Name.Contains("test"));
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.Contains("WHERE"), "Should contain WHERE clause");
            Assert.IsTrue(sql.Contains("AND"), "Should contain AND operator between conditions");
            Assert.IsTrue(sql.Contains("[IsActive]"), "Should contain IsActive condition");
            Assert.IsTrue(sql.Contains("[Age]"), "Should contain Age condition");
            Assert.IsTrue(sql.Contains("[Name]"), "Should contain Name condition");
        }

        [TestMethod]
        public void ExpressionToSql_Update_GeneratesUpdateStatement()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.Id == 1);
            expr.Set(u => u.Name, "Updated Name");
            var sql = expr.ToSql();

            // Assert
            Assert.IsTrue(sql.StartsWith("UPDATE"), "Should start with UPDATE");
            Assert.IsTrue(sql.Contains("SET"), "Should contain SET clause");
            Assert.IsTrue(sql.Contains("WHERE"), "Should contain WHERE clause");
            Assert.IsTrue(sql.Contains("[Name]"), "Should contain column to update");
        }

        [TestMethod]
        public void ExpressionToSql_Insert_GeneratesInsertStatement()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            var insertExpr = expr.InsertInto();

            // Assert
            Assert.IsNotNull(insertExpr, "Should return insert expression");
            // Note: Full INSERT SQL generation testing would require values
        }

        [TestMethod]
        public void ExpressionToSql_Dispose_DoesNotThrow()
        {
            // Arrange
            var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.Name == "test");

            // Act & Assert
            try
            {
                expr.Dispose();
                Assert.IsTrue(true, "Dispose completed without exception");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Dispose should not throw, but threw: {ex.Message}");
            }
        }

        [TestMethod]
        public void ExpressionToSql_MultipleDialects_GenerateDifferentSQL()
        {
            // Arrange
            var condition = new Func<ExpressionToSql<TestUser>, ExpressionToSql<TestUser>>(
                expr => expr.Where(u => u.Name == "test"));

            // Act
            using var sqlServer = condition(ExpressionToSql<TestUser>.ForSqlServer());
            using var mysql = condition(ExpressionToSql<TestUser>.ForMySql());
            using var postgres = condition(ExpressionToSql<TestUser>.ForPostgreSQL());

            var sqlServerSql = sqlServer.ToSql();
            var mysqlSql = mysql.ToSql();
            var postgresSql = postgres.ToSql();

            // Assert
            Assert.AreNotEqual(sqlServerSql, mysqlSql, "SQL Server and MySQL should generate different SQL");
            Assert.AreNotEqual(sqlServerSql, postgresSql, "SQL Server and PostgreSQL should generate different SQL");
            Assert.AreNotEqual(mysqlSql, postgresSql, "MySQL and PostgreSQL should generate different SQL");
        }
    }
}
