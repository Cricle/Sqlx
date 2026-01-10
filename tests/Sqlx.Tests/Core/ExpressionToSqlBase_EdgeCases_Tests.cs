// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBase_EdgeCases_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Edge case tests for ExpressionToSqlBase to achieve 100% coverage
    /// </summary>
    public class ExpressionToSqlBase_EdgeCases_Tests
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal Salary { get; set; }
        }

        [Fact]
        public void StringFunction_Replace_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.Replace("old", "new") == "test")
                .ToSql();

            Assert.Contains("REPLACE", sql);
        }

        [Fact]
        public void StringFunction_Substring_OneParameter_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.Substring(5) == "test")
                .ToSql();

            Assert.Contains("SUBSTRING", sql);
        }

        [Fact]
        public void StringFunction_Substring_TwoParameters_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.Substring(0, 5) == "test")
                .ToSql();

            Assert.Contains("SUBSTRING", sql);
        }

        [Fact]
        public void MathFunction_Round_OneParameter_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => Math.Round((double)x.Salary) > 1000)
                .ToSql();

            Assert.Contains("ROUND", sql);
        }

        [Fact]
        public void MathFunction_Round_TwoParameters_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => Math.Round((double)x.Salary, 2) > 1000)
                .ToSql();

            Assert.Contains("ROUND", sql);
        }

        [Fact]
        public void MathFunction_Floor_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => Math.Floor((double)x.Salary) > 1000)
                .ToSql();

            Assert.Contains("FLOOR", sql);
        }

        [Fact]
        public void MathFunction_Sqrt_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => Math.Sqrt((double)x.Age) > 5)
                .ToSql();

            Assert.Contains("SQRT", sql);
        }

        [Fact]
        public void MathFunction_Ceiling_PostgreSQL_ShouldUseCEIL()
        {
            var sql = ExpressionToSql<TestUser>.ForPostgreSQL()
                .Where(x => Math.Ceiling((double)x.Salary) > 1000)
                .ToSql();

            Assert.Contains("CEIL", sql);
        }

        [Fact]
        public void MathFunction_Ceiling_SqlServer_ShouldUseCEILING()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => Math.Ceiling((double)x.Salary) > 1000)
                .ToSql();

            Assert.Contains("CEILING", sql);
        }

        [Fact]
        public void MathFunction_Pow_MySQL_ShouldUsePOW()
        {
            var sql = ExpressionToSql<TestUser>.ForMySql()
                .Where(x => Math.Pow((double)x.Age, 2) > 100)
                .ToSql();

            Assert.Contains("POW", sql);
        }

        [Fact]
        public void MathFunction_Pow_SqlServer_ShouldUsePOWER()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => Math.Pow((double)x.Age, 2) > 100)
                .ToSql();

            Assert.Contains("POWER", sql);
        }

        [Fact]
        public void StringFunction_Contains_ShouldGenerateLIKE()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.Contains("test"))
                .ToSql();

            Assert.Contains("LIKE", sql);
        }

        [Fact]
        public void StringFunction_StartsWith_ShouldGenerateLIKE()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.StartsWith("test"))
                .ToSql();

            Assert.Contains("LIKE", sql);
        }

        [Fact]
        public void StringFunction_EndsWith_ShouldGenerateLIKE()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.EndsWith("test"))
                .ToSql();

            Assert.Contains("LIKE", sql);
        }

        [Fact]
        public void StringFunction_ToUpper_ShouldGenerateUPPER()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.ToUpper() == "TEST")
                .ToSql();

            Assert.Contains("UPPER", sql);
        }

        [Fact]
        public void StringFunction_ToLower_ShouldGenerateLOWER()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.ToLower() == "test")
                .ToSql();

            Assert.Contains("LOWER", sql);
        }

        [Fact]
        public void StringFunction_Trim_ShouldGenerateTRIM()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Name.Trim() == "test")
                .ToSql();

            Assert.Contains("TRIM", sql);
        }

        [Fact]
        public void DateTimeFunction_AddDays_SqlServer_ShouldUseDATEADD()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.CreatedAt.AddDays(7) > DateTime.Now)
                .ToSql();

            Assert.Contains("DATEADD", sql);
            Assert.Contains("DAYS", sql.ToUpper());
        }

        [Fact]
        public void DateTimeFunction_AddMonths_SqlServer_ShouldUseDATEADD()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.CreatedAt.AddMonths(1) > DateTime.Now)
                .ToSql();

            Assert.Contains("DATEADD", sql);
            Assert.Contains("MONTHS", sql.ToUpper());
        }

        [Fact]
        public void DateTimeFunction_AddYears_SqlServer_ShouldUseDATEADD()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.CreatedAt.AddYears(1) > DateTime.Now)
                .ToSql();

            Assert.Contains("DATEADD", sql);
            Assert.Contains("YEARS", sql.ToUpper());
        }

        [Fact]
        public void ConditionalExpression_ShouldGenerateCASE()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => (x.Age > 18 ? "Adult" : "Minor") == "Adult")
                .ToSql();

            Assert.Contains("CASE WHEN", sql);
        }

        [Fact]
        public void BinaryExpression_Subtract_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age - 10 > 18)
                .ToSql();

            Assert.Contains("-", sql);
        }

        [Fact]
        public void BinaryExpression_Multiply_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age * 2 > 50)
                .ToSql();

            Assert.Contains("*", sql);
        }

        [Fact]
        public void BinaryExpression_Divide_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age / 2 > 10)
                .ToSql();

            Assert.Contains("/", sql);
        }

        [Fact]
        public void BinaryExpression_Modulo_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age % 2 == 0)
                .ToSql();

            Assert.Contains("%", sql);
        }

        [Fact]
        public void BinaryExpression_GreaterThan_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age > 18)
                .ToSql();

            Assert.Contains(">", sql);
        }

        [Fact]
        public void BinaryExpression_GreaterThanOrEqual_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age >= 18)
                .ToSql();

            Assert.Contains(">=", sql);
        }

        [Fact]
        public void BinaryExpression_LessThan_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age < 65)
                .ToSql();

            Assert.Contains("<", sql);
        }

        [Fact]
        public void BinaryExpression_LessThanOrEqual_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age <= 65)
                .ToSql();

            Assert.Contains("<=", sql);
        }

        [Fact]
        public void BinaryExpression_NotEqual_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age != 0)
                .ToSql();

            Assert.Contains("<>", sql);
        }

        [Fact]
        public void BinaryExpression_AndAlso_ShouldGenerateAND()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age > 18 && x.IsActive)
                .ToSql();

            Assert.Contains("AND", sql);
        }

        [Fact]
        public void BinaryExpression_OrElse_ShouldGenerateOR()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age < 18 || x.Age > 65)
                .ToSql();

            Assert.Contains("OR", sql);
        }

        [Fact]
        public void UnaryExpression_Not_WithBooleanMember_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => !x.IsActive)
                .ToSql();

            Assert.Contains("=", sql);
            Assert.Contains("0", sql);
        }

        [Fact]
        public void UnaryExpression_Not_WithExpression_ShouldGenerateNOT()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => !(x.Age > 18))
                .ToSql();

            Assert.Contains("NOT", sql);
        }

        [Fact]
        public void GetMergedWhereConditions_WithExternalExpression_ShouldMerge()
        {
            var whereExpr = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.Age > 25);

            var query = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(x => x.IsActive)
                .WhereFrom(whereExpr);

            var sql = query.ToSql();
            Assert.Contains("WHERE", sql);
        }

        [Fact]
        public void GetMergedParameters_WithExternalExpression_ShouldMerge()
        {
            var whereExpr = ExpressionToSql<TestUser>.ForSqlServer()
                .UseParameterizedQueries()
                .Where(x => x.Age > 25);

            var query = ExpressionToSql<TestUser>.ForSqlServer()
                .UseParameterizedQueries()
                .Where(x => x.IsActive)
                .WhereFrom(whereExpr);

            var sql = query.ToSql();
            // Just verify it doesn't throw and generates SQL
            Assert.NotNull(sql);
            Assert.Contains("WHERE", sql);
        }
    }
}
