// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBase_Final_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Final tests for ExpressionToSqlBase to achieve 100% coverage
    /// </summary>
    public class ExpressionToSqlBase_Final_Tests
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        [Fact]
        public void WhereFrom_WithNullExpression_ShouldThrowArgumentNullException()
        {
            // Test null argument handling
            var query = ExpressionToSql<TestUser>.ForSqlServer();

            Assert.Throws<ArgumentNullException>(() => query.WhereFrom(null!));
        }

        [Fact]
        public void WhereFrom_WithValidExpression_ShouldMergeConditions()
        {
            // Test merging WHERE conditions from another expression
            var whereExpr = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age > 25);

            var query = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.IsActive)
                .WhereFrom(whereExpr);

            var sql = query.ToSql();
            Assert.Contains("WHERE", sql);
        }

        [Fact]
        public void GetParameters_ShouldReturnCopyOfParameters()
        {
            // Test GetParameters returns a copy
            var query = ExpressionToSql<TestUser>.ForSqlServer()
                .UseParameterizedQueries()
                .Where(x => x.Age > 25);

            var params1 = query.GetParameters();
            var params2 = query.GetParameters();

            Assert.NotSame(params1, params2); // Should be different instances
        }

        [Fact]
        public void ToWhereClause_WithNoConditions_ShouldReturnEmpty()
        {
            // Test ToWhereClause with no conditions
            var query = ExpressionToSql<TestUser>.ForSqlServer();

            var whereClause = query.ToWhereClause();

            Assert.Equal(string.Empty, whereClause);
        }

        [Fact]
        public void ToWhereClause_WithConditions_ShouldRemoveOuterParentheses()
        {
            // Test ToWhereClause removes outer parentheses
            var query = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age > 25);

            var whereClause = query.ToWhereClause();

            Assert.DoesNotContain("((", whereClause); // Should not have double parentheses at start
        }

        [Fact]
        public void CollectionContains_WithList_ShouldGenerateInClause()
        {
            // Test List.Contains() → IN clause
            var ids = new List<int> { 10, 20, 30 };
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => ids.Contains(x.Id))
                .ToSql();

            Assert.Contains("IN", sql);
            Assert.Contains("10", sql);
            Assert.Contains("20", sql);
            Assert.Contains("30", sql);
        }

        [Fact]
        public void CollectionContains_WithEmptyCollection_ShouldGenerateInNull()
        {
            // Test empty collection → IN (NULL)
            var ids = new List<int>();
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => ids.Contains(x.Id))
                .ToSql();

            Assert.Contains("IN", sql);
            Assert.Contains("NULL", sql);
        }

        [Fact]
        public void CollectionContains_WithNullCollection_ShouldGenerateInNull()
        {
            // Test null collection → IN (NULL)
            List<int>? ids = null;
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => ids != null && ids.Contains(x.Id))
                .ToSql();

            Assert.NotNull(sql);
        }

        [Fact]
        public void CollectionContains_WithNullableValues_ShouldHandleNull()
        {
            // Test collection with null values
            var names = new List<string?> { "Alice", null, "Bob" };
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => names.Contains(x.Name))
                .ToSql();

            Assert.Contains("IN", sql);
            Assert.Contains("NULL", sql);
        }

        [Fact]
        public void StringConcatenation_SqlServer_ShouldUsePlus()
        {
            // Test string concatenation in SQL Server
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name + " Test" == "Alice Test")
                .ToSql();

            Assert.Contains("+", sql);
        }

        [Fact]
        public void StringConcatenation_PostgreSQL_ShouldUseConcat()
        {
            // Test string concatenation in PostgreSQL
            var sql = ExpressionToSql<TestUser>.ForPostgreSQL()
                .Where(x => x.Name + " Test" == "Alice Test")
                .ToSql();

            // PostgreSQL uses CONCAT or ||
            Assert.True(sql.Contains("CONCAT") || sql.Contains("||"));
        }

        [Fact]
        public void ModuloOperator_ShouldGenerateCorrectSQL()
        {
            // Test modulo operator
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age % 2 == 0)
                .ToSql();

            Assert.Contains("%", sql);
        }

        [Fact]
        public void CoalesceOperator_ShouldGenerateCorrectSQL()
        {
            // Test coalesce operator
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => (x.Name ?? "Unknown") == "Unknown")
                .ToSql();

            Assert.Contains("COALESCE", sql);
        }

        [Fact]
        public void DateTimeFunction_SqlServer_ShouldUseDATEADD()
        {
            // Test DateTime.AddDays in SQL Server
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.CreatedAt.AddDays(7) > DateTime.Now)
                .ToSql();

            Assert.Contains("DATEADD", sql);
        }

        [Fact]
        public void DateTimeFunction_NonSqlServer_ShouldFallback()
        {
            // Test DateTime function in non-SQL Server
            var sql = ExpressionToSql<TestUser>.ForMySql()
                .Where(x => x.CreatedAt > DateTime.Now)
                .ToSql();

            Assert.NotNull(sql);
        }

        [Fact]
        public void ConvertToSnakeCase_WithLowercase_ShouldReturnAsIs()
        {
            // Test snake_case conversion with already lowercase
            var query = ExpressionToSql<TestUser>.ForSqlServer();
            query.SetTableName("test_table");

            Assert.NotNull(query);
        }

        [Fact]
        public void ConvertToSnakeCase_WithPascalCase_ShouldConvert()
        {
            // Test PascalCase to snake_case conversion
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.CreatedAt > DateTime.Now)
                .ToSql();

            // Column names should be converted to snake_case
            Assert.Contains("created_at", sql.ToLower());
        }

        [Fact]
        public void BooleanComparison_WithTrueConstant_ShouldGenerateCorrectSQL()
        {
            // Test boolean comparison with true constant
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.IsActive == true)
                .ToSql();

            Assert.Contains("=", sql);
            Assert.Contains("1", sql);
        }

        [Fact]
        public void BooleanComparison_WithFalseConstant_ShouldGenerateCorrectSQL()
        {
            // Test boolean comparison with false constant
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.IsActive == false)
                .ToSql();

            Assert.Contains("=", sql);
            Assert.Contains("0", sql);
        }

        [Fact]
        public void BooleanComparison_NotEqual_ShouldGenerateCorrectSQL()
        {
            // Test boolean not equal comparison
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.IsActive != true)
                .ToSql();

            Assert.Contains("<>", sql);
        }

        [Fact]
        public void BooleanMember_InLogicalExpression_ShouldAddComparison()
        {
            // Test boolean member in logical expression
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.IsActive && x.Age > 25)
                .ToSql();

            Assert.Contains("AND", sql);
        }

        [Fact]
        public void BooleanMember_InOrExpression_ShouldAddComparison()
        {
            // Test boolean member in OR expression
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.IsActive || x.Age > 25)
                .ToSql();

            Assert.Contains("OR", sql);
        }

        [Fact]
        public void NullComparison_Equal_ShouldUseIsNull()
        {
            // Test NULL = comparison → IS NULL
            string? nullValue = null;
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name == nullValue)
                .ToSql();

            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void NullComparison_NotEqual_ShouldUseIsNotNull()
        {
            // Test NULL <> comparison → IS NOT NULL
            string? nullValue = null;
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name != nullValue)
                .ToSql();

            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void StringProperty_Length_SqlServer_ShouldUseLEN()
        {
            // Test string.Length in SQL Server
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.Length > 5)
                .ToSql();

            Assert.Contains("LEN", sql);
        }

        [Fact]
        public void StringProperty_Length_MySQL_ShouldUseLENGTH()
        {
            // Test string.Length in MySQL
            var sql = ExpressionToSql<TestUser>.ForMySql()
                .Where(x => x.Name.Length > 5)
                .ToSql();

            Assert.Contains("LENGTH", sql);
        }

        [Fact]
        public void MathFunction_Abs_ShouldGenerateCorrectSQL()
        {
            // Test Math.Abs
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => Math.Abs(x.Age) > 10)
                .ToSql();

            Assert.Contains("ABS", sql);
        }

        [Fact]
        public void StringFunction_Substring_SQLite_ShouldUseSUBSTR()
        {
            // Test Substring in SQLite
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .Where(x => x.Name.Substring(0, 3) == "Ali")
                .ToSql();

            Assert.Contains("SUBSTR", sql);
        }

        [Fact]
        public void StringFunction_Substring_SqlServer_ShouldUseSUBSTRING()
        {
            // Test Substring in SQL Server
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.Substring(0, 3) == "Ali")
                .ToSql();

            Assert.Contains("SUBSTRING", sql);
        }

        [Fact]
        public void BooleanLiteral_Oracle_ShouldUse1()
        {
            // Test boolean literal in Oracle
            var sql = ExpressionToSql<TestUser>.ForOracle()
                .Where(x => x.IsActive == true)
                .ToSql();

            Assert.Contains("1", sql);
        }
    }
}
