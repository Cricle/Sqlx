// -----------------------------------------------------------------------
// <copyright file="CrudComprehensiveTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// CRUD操作全面测试，覆盖所有数据库方言
    /// </summary>
    [TestClass]
    public class CrudComprehensiveTests
    {
        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal Salary { get; set; }
        }

        #region CREATE (INSERT) Tests

        [TestMethod]
        public void Create_InsertInto_AllDialects_GeneratesCorrectSql()
        {
            // SQL Server
            using var sqlServer = ExpressionToSql<TestUser>.ForSqlServer();
            sqlServer.InsertInto().Values(1, "John", "john@test.com", 25, true, DateTime.Now, 5000m);
            var sqlServerSql = sqlServer.ToSql();
            Assert.IsTrue(sqlServerSql.StartsWith("INSERT INTO [TestUser]"));
            Assert.IsTrue(sqlServerSql.Contains("([Id], [Name], [Email], [Age], [IsActive], [CreatedAt], [Salary])"));
            Assert.IsTrue(sqlServerSql.Contains("VALUES"));

            // MySQL
            using var mysql = ExpressionToSql<TestUser>.ForMySql();
            mysql.InsertInto().Values(1, "John", "john@test.com", 25, true, DateTime.Now, 5000m);
            var mysqlSql = mysql.ToSql();
            Assert.IsTrue(mysqlSql.StartsWith("INSERT INTO `TestUser`"));
            Assert.IsTrue(mysqlSql.Contains("(`Id`, `Name`, `Email`, `Age`, `IsActive`, `CreatedAt`, `Salary`)"));

            // PostgreSQL
            using var postgres = ExpressionToSql<TestUser>.ForPostgreSQL();
            postgres.InsertInto().Values(1, "John", "john@test.com", 25, true, DateTime.Now, 5000m);
            var postgresSql = postgres.ToSql();
            Assert.IsTrue(postgresSql.StartsWith("INSERT INTO \"TestUser\""));
            Assert.IsTrue(postgresSql.Contains("(\"Id\", \"Name\", \"Email\", \"Age\", \"IsActive\", \"CreatedAt\", \"Salary\")"));

            // Oracle
            using var oracle = ExpressionToSql<TestUser>.ForOracle();
            oracle.InsertInto().Values(1, "John", "john@test.com", 25, true, DateTime.Now, 5000m);
            var oracleSql = oracle.ToSql();
            Assert.IsTrue(oracleSql.StartsWith("INSERT INTO \"TestUser\""));

            // SQLite
            using var sqlite = ExpressionToSql<TestUser>.ForSqlite();
            sqlite.InsertInto().Values(1, "John", "john@test.com", 25, true, DateTime.Now, 5000m);
            var sqliteSql = sqlite.ToSql();
            Assert.IsTrue(sqliteSql.StartsWith("INSERT INTO [TestUser]"));
        }

        [TestMethod]
        public void Create_InsertMultipleValues_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.InsertInto()
                .Values(1, "John", "john@test.com", 25, true, DateTime.Now, 5000m)
                .AddValues(2, "Jane", "jane@test.com", 30, false, DateTime.Now, 6000m);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("VALUES"));
            Assert.IsTrue(sql.Count(c => c == '(') >= 4); // 至少2对括号用于两行数据
        }

        [TestMethod]
        public void Create_InsertSelect_GeneratesCorrectSql()
        {
            using var selectQuery = ExpressionToSql<TestUser>.ForSqlServer();
            selectQuery.Where(u => u.Age > 18);

            using var insertQuery = ExpressionToSql<TestUser>.ForSqlServer();
            insertQuery.InsertInto().InsertSelect(selectQuery);

            var sql = insertQuery.ToSql();
            Assert.IsTrue(sql.StartsWith("INSERT INTO [TestUser]"));
            Assert.IsTrue(sql.Contains("SELECT * FROM [TestUser] WHERE"));
        }

        #endregion

        #region READ (SELECT) Tests

        [TestMethod]
        public void Read_BasicSelect_AllDialects_GeneratesCorrectSql()
        {
            // SQL Server
            using var sqlServer = ExpressionToSql<TestUser>.ForSqlServer();
            var sqlServerSql = sqlServer.ToSql();
            Assert.AreEqual("SELECT * FROM [TestUser]", sqlServerSql);

            // MySQL
            using var mysql = ExpressionToSql<TestUser>.ForMySql();
            var mysqlSql = mysql.ToSql();
            Assert.AreEqual("SELECT * FROM `TestUser`", mysqlSql);

            // PostgreSQL
            using var postgres = ExpressionToSql<TestUser>.ForPostgreSQL();
            var postgresSql = postgres.ToSql();
            Assert.AreEqual("SELECT * FROM \"TestUser\"", postgresSql);

            // Oracle
            using var oracle = ExpressionToSql<TestUser>.ForOracle();
            var oracleSql = oracle.ToSql();
            Assert.AreEqual("SELECT * FROM \"TestUser\"", oracleSql);

            // SQLite
            using var sqlite = ExpressionToSql<TestUser>.ForSqlite();
            var sqliteSql = sqlite.ToSql();
            Assert.AreEqual("SELECT * FROM [TestUser]", sqliteSql);
        }

        [TestMethod]
        public void Read_WhereConditions_AllDataTypes_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            
            // 整数条件
            expr.Where(u => u.Id == 1);
            var sql1 = expr.ToSql();
            Assert.IsTrue(sql1.Contains("WHERE [Id] = 1"));

            // 字符串条件
            using var expr2 = ExpressionToSql<TestUser>.ForSqlServer();
            expr2.Where(u => u.Name == "John");
            var sql2 = expr2.ToSql();
            Assert.IsTrue(sql2.Contains("WHERE [Name] = 'John'"));

            // 布尔条件
            using var expr3 = ExpressionToSql<TestUser>.ForSqlServer();
            expr3.Where(u => u.IsActive);
            var sql3 = expr3.ToSql();
            Assert.IsTrue(sql3.Contains("WHERE [IsActive] = 1"));

            // 日期条件
            using var expr4 = ExpressionToSql<TestUser>.ForSqlServer();
            var testDate = new DateTime(2023, 1, 1);
            expr4.Where(u => u.CreatedAt > testDate);
            var sql4 = expr4.ToSql();
            Assert.IsTrue(sql4.Contains("WHERE [CreatedAt] >"));

            // 小数条件
            using var expr5 = ExpressionToSql<TestUser>.ForSqlServer();
            expr5.Where(u => u.Salary >= 5000m);
            var sql5 = expr5.ToSql();
            Assert.IsTrue(sql5.Contains("WHERE [Salary] >= 5000"));
        }

        [TestMethod]
        public void Read_ComplexWhere_LogicalOperators_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.Age >= 18 && u.Age <= 65)
                .Where(u => u.IsActive)
                .Where(u => u.Name != "Admin");

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("WHERE"));
            Assert.IsTrue(sql.Contains("AND"));
            Assert.IsTrue(sql.Contains("[Age] >= 18"));
            Assert.IsTrue(sql.Contains("[Age] <= 65"));
            Assert.IsTrue(sql.Contains("[IsActive] = 1"));
            Assert.IsTrue(sql.Contains("[Name] <> 'Admin'"));
        }

        [TestMethod]
        public void Read_OrderBy_AllDialects_GeneratesCorrectSql()
        {
            // SQL Server
            using var sqlServer = ExpressionToSql<TestUser>.ForSqlServer();
            sqlServer.OrderBy(u => u.Name).OrderByDescending(u => u.CreatedAt);
            var sqlServerSql = sqlServer.ToSql();
            Assert.IsTrue(sqlServerSql.Contains("ORDER BY [Name] ASC, [CreatedAt] DESC"));

            // MySQL
            using var mysql = ExpressionToSql<TestUser>.ForMySql();
            mysql.OrderBy(u => u.Name).OrderByDescending(u => u.CreatedAt);
            var mysqlSql = mysql.ToSql();
            Assert.IsTrue(mysqlSql.Contains("ORDER BY `Name` ASC, `CreatedAt` DESC"));

            // PostgreSQL
            using var postgres = ExpressionToSql<TestUser>.ForPostgreSQL();
            postgres.OrderBy(u => u.Name).OrderByDescending(u => u.CreatedAt);
            var postgresSql = postgres.ToSql();
            Assert.IsTrue(postgresSql.Contains("ORDER BY \"Name\" ASC, \"CreatedAt\" DESC"));
        }

        [TestMethod]
        public void Read_Pagination_AllDialects_GeneratesCorrectSql()
        {
            // SQL Server - OFFSET/FETCH
            using var sqlServer = ExpressionToSql<TestUser>.ForSqlServer();
            sqlServer.OrderBy(u => u.Id).Skip(10).Take(20);
            var sqlServerSql = sqlServer.ToSql();
            Assert.IsTrue(sqlServerSql.Contains("ORDER BY [Id] ASC"));
            Assert.IsTrue(sqlServerSql.Contains("OFFSET 10 ROWS"));
            Assert.IsTrue(sqlServerSql.Contains("FETCH NEXT 20 ROWS ONLY"));

            // MySQL - LIMIT/OFFSET
            using var mysql = ExpressionToSql<TestUser>.ForMySql();
            mysql.OrderBy(u => u.Id).Skip(10).Take(20);
            var mysqlSql = mysql.ToSql();
            Assert.IsTrue(mysqlSql.Contains("LIMIT 20"));
            Assert.IsTrue(mysqlSql.Contains("OFFSET 10"));

            // PostgreSQL - LIMIT/OFFSET
            using var postgres = ExpressionToSql<TestUser>.ForPostgreSQL();
            postgres.OrderBy(u => u.Id).Skip(10).Take(20);
            var postgresSql = postgres.ToSql();
            Assert.IsTrue(postgresSql.Contains("LIMIT 20"));
            Assert.IsTrue(postgresSql.Contains("OFFSET 10"));

            // SQLite - LIMIT/OFFSET
            using var sqlite = ExpressionToSql<TestUser>.ForSqlite();
            sqlite.OrderBy(u => u.Id).Skip(10).Take(20);
            var sqliteSql = sqlite.ToSql();
            Assert.IsTrue(sqliteSql.Contains("LIMIT 20"));
            Assert.IsTrue(sqliteSql.Contains("OFFSET 10"));
        }

        [TestMethod]
        public void Read_GroupBy_Having_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            var groupedExpr = expr.GroupBy(u => u.Age);
            groupedExpr.Having(g => g.Key > 18);

            var sql = groupedExpr.ToSql();
            Assert.IsTrue(sql.Contains("GROUP BY [Age]"));
            Assert.IsTrue(sql.Contains("HAVING [Age] > 18"));
        }

        #endregion

        #region UPDATE Tests

        [TestMethod]
        public void Update_BasicSet_AllDialects_GeneratesCorrectSql()
        {
            // SQL Server
            using var sqlServer = ExpressionToSql<TestUser>.ForSqlServer();
            sqlServer.Set(u => u.Name, "Updated Name")
                     .Set(u => u.Age, 30)
                     .Where(u => u.Id == 1);
            var sqlServerSql = sqlServer.ToSql();
            Assert.IsTrue(sqlServerSql.StartsWith("UPDATE [TestUser] SET"));
            Assert.IsTrue(sqlServerSql.Contains("[Name] = 'Updated Name'"));
            Assert.IsTrue(sqlServerSql.Contains("[Age] = 30"));
            Assert.IsTrue(sqlServerSql.Contains("WHERE [Id] = 1"));

            // MySQL
            using var mysql = ExpressionToSql<TestUser>.ForMySql();
            mysql.Set(u => u.Name, "Updated Name")
                 .Set(u => u.Age, 30)
                 .Where(u => u.Id == 1);
            var mysqlSql = mysql.ToSql();
            Assert.IsTrue(mysqlSql.StartsWith("UPDATE `TestUser` SET"));
            Assert.IsTrue(mysqlSql.Contains("`Name` = 'Updated Name'"));
            Assert.IsTrue(mysqlSql.Contains("`Age` = 30"));

            // PostgreSQL
            using var postgres = ExpressionToSql<TestUser>.ForPostgreSQL();
            postgres.Set(u => u.Name, "Updated Name")
                    .Set(u => u.Age, 30)
                    .Where(u => u.Id == 1);
            var postgresSql = postgres.ToSql();
            Assert.IsTrue(postgresSql.StartsWith("UPDATE \"TestUser\" SET"));
            Assert.IsTrue(postgresSql.Contains("\"Name\" = 'Updated Name'"));
            Assert.IsTrue(postgresSql.Contains("\"Age\" = 30"));
        }

        [TestMethod]
        public void Update_ExpressionSet_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Set(u => u.Age, u => u.Age + 1)
                .Set(u => u.Salary, u => u.Salary * 1.1m)
                .Where(u => u.IsActive);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("UPDATE [TestUser] SET"));
            Assert.IsTrue(sql.Contains("[Age] = [Age] + 1"));
            Assert.IsTrue(sql.Contains("[Salary] = [Salary] * 1.1"));
            Assert.IsTrue(sql.Contains("WHERE [IsActive] = 1"));
        }

        [TestMethod]
        public void Update_MixedSetTypes_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Set(u => u.Name, "Fixed Name")                // 常量值
                .Set(u => u.Age, u => u.Age + 1)              // 表达式
                .Set(u => u.IsActive, false)                  // 布尔值
                .Where(u => u.Id == 1);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[Name] = 'Fixed Name'"));
            Assert.IsTrue(sql.Contains("[Age] = [Age] + 1"));
            Assert.IsTrue(sql.Contains("[IsActive] = 0"));  // 布尔false应该转换为0
        }

        #endregion

        #region DELETE Tests (通过WHERE条件实现)

        [TestMethod]
        public void Delete_WhereCondition_GeneratesCorrectSql()
        {
            // 注意：这个框架通过WHERE条件实现删除逻辑
            // 实际的DELETE语句需要在应用层处理
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.IsActive == false)
                .Where(u => u.Age < 18);

            var whereClause = expr.ToWhereClause();
            Assert.IsTrue(whereClause.Contains("[IsActive] = 0"));
            Assert.IsTrue(whereClause.Contains("[Age] < 18"));
            Assert.IsTrue(whereClause.Contains("AND"));

            // 可以生成完整的DELETE语句
            var deleteStatement = $"DELETE FROM [TestUser] WHERE {whereClause}";
            Assert.IsTrue(deleteStatement.StartsWith("DELETE FROM [TestUser] WHERE"));
        }

        [TestMethod]
        public void Delete_ComplexConditions_AllDialects_GeneratesCorrectWhere()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<TestUser>.ForSqlServer()),
                ("MySQL", ExpressionToSql<TestUser>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<TestUser>.ForPostgreSQL()),
                ("Oracle", ExpressionToSql<TestUser>.ForOracle()),
                ("SQLite", ExpressionToSql<TestUser>.ForSqlite())
            };

            foreach (var (dialectName, expr) in testCases)
            {
                using (expr)
                {
                    expr.Where(u => u.CreatedAt < DateTime.Now.AddDays(-30))
                        .Where(u => u.IsActive == false);

                    var whereClause = expr.ToWhereClause();
                    Assert.IsTrue(whereClause.Contains("IsActive"), $"{dialectName}: Should contain IsActive condition");
                    Assert.IsTrue(whereClause.Contains("CreatedAt"), $"{dialectName}: Should contain CreatedAt condition");
                    Assert.IsTrue(whereClause.Contains("AND"), $"{dialectName}: Should contain AND operator");
                }
            }
        }

        #endregion

        #region 数据类型和边界值测试

        [TestMethod]
        public void DataTypes_NullValues_HandledCorrectly()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            
            // 测试NULL值比较
            expr.Where(u => u.Name == null);
            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("IS NULL"), "Should use IS NULL for null equality");

            // 测试NOT NULL
            using var expr2 = ExpressionToSql<TestUser>.ForSqlServer();
            expr2.Where(u => u.Name != null);
            var sql2 = expr2.ToSql();
            Assert.IsTrue(sql2.Contains("IS NOT NULL"), "Should use IS NOT NULL for null inequality");
        }

        [TestMethod]
        public void DataTypes_SpecialCharacters_EscapedCorrectly()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Set(u => u.Name, "O'Connor")
                .Set(u => u.Email, "test@example.com")
                .Where(u => u.Id == 1);

            var sql = expr.ToSql();
            // 检查单引号是否被正确转义
            Assert.IsTrue(sql.Contains("'O''Connor'") || sql.Contains("'O\\'Connor'"), 
                "Single quotes should be escaped");
        }

        [TestMethod]
        public void DataTypes_NumericPrecision_HandledCorrectly()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Set(u => u.Salary, 12345.67m)
                .Where(u => u.Age > 25);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("12345.67"), "Decimal values should maintain precision");
        }

        [TestMethod]
        public void DataTypes_DateTime_FormattedCorrectly()
        {
            var testDate = new DateTime(2023, 12, 31, 23, 59, 59);
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.CreatedAt >= testDate);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("CreatedAt"), "Should contain CreatedAt column");
            Assert.IsTrue(sql.Contains(">="), "Should contain comparison operator");
        }

        #endregion

        #region 性能和边界测试

        [TestMethod]
        public void Performance_LargeWhereConditions_HandledEfficiently()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            
            // 添加多个WHERE条件
            for (int i = 0; i < 50; i++)
            {
                expr.Where(u => u.Age > i);
            }

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Length > 100, "Should generate substantial SQL");
            Assert.IsTrue(sql.Count(c => c == 'A' && sql.Substring(sql.IndexOf(c)).StartsWith("AND")) >= 45, 
                "Should contain multiple AND conditions");
        }

        [TestMethod]
        public void EdgeCase_EmptyConditions_GeneratesBasicSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            var sql = expr.ToSql();
            Assert.AreEqual("SELECT * FROM [TestUser]", sql, "Should generate basic SELECT without conditions");
        }

        [TestMethod]
        public void EdgeCase_OnlyTakeWithoutSkip_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Take(10);
            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("FETCH NEXT 10 ROWS ONLY"), "Should contain FETCH NEXT for SQL Server");
        }

        [TestMethod]
        public void EdgeCase_OnlySkipWithoutTake_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Skip(5);
            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("OFFSET 5 ROWS"), "Should contain OFFSET for SQL Server");
        }

        #endregion

        #region 方言特异性测试

        [TestMethod]
        public void Dialect_SqlServer_SpecificFeatures()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlServer();
            expr.Where(u => u.Name.Contains("test"))
                .OrderBy(u => u.Id)
                .Skip(10)
                .Take(20);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("["), "Should use square brackets");
            Assert.IsTrue(sql.Contains("LIKE"), "Should use LIKE for Contains");
            Assert.IsTrue(sql.Contains("OFFSET") && sql.Contains("FETCH"), "Should use OFFSET/FETCH for pagination");
        }

        [TestMethod]
        public void Dialect_MySQL_SpecificFeatures()
        {
            using var expr = ExpressionToSql<TestUser>.ForMySql();
            expr.Where(u => u.Name.Contains("test"))
                .OrderBy(u => u.Id)
                .Skip(10)
                .Take(20);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("`"), "Should use backticks");
            Assert.IsTrue(sql.Contains("CONCAT"), "Should use CONCAT for string operations");
            Assert.IsTrue(sql.Contains("LIMIT") && sql.Contains("OFFSET"), "Should use LIMIT/OFFSET for pagination");
        }

        [TestMethod]
        public void Dialect_PostgreSQL_SpecificFeatures()
        {
            using var expr = ExpressionToSql<TestUser>.ForPostgreSQL();
            expr.Where(u => u.Name.Contains("test"))
                .OrderBy(u => u.Id)
                .Skip(10)
                .Take(20);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("\""), "Should use double quotes");
            Assert.IsTrue(sql.Contains("LIMIT") && sql.Contains("OFFSET"), "Should use LIMIT/OFFSET for pagination");
        }

        [TestMethod]
        public void Dialect_Oracle_SpecificFeatures()
        {
            using var expr = ExpressionToSql<TestUser>.ForOracle();
            expr.Where(u => u.Name.Contains("test"))
                .OrderBy(u => u.Id)
                .Skip(10)
                .Take(20);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("\""), "Should use double quotes");
            Assert.IsTrue(sql.Contains("OFFSET") && sql.Contains("FETCH"), "Should use OFFSET/FETCH for pagination");
        }

        [TestMethod]
        public void Dialect_SQLite_SpecificFeatures()
        {
            using var expr = ExpressionToSql<TestUser>.ForSqlite();
            expr.Where(u => u.Name.Contains("test"))
                .OrderBy(u => u.Id)
                .Skip(10)
                .Take(20);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("["), "Should use square brackets");
            Assert.IsTrue(sql.Contains("LIMIT") && sql.Contains("OFFSET"), "Should use LIMIT/OFFSET for pagination");
        }

        #endregion
    }
}


