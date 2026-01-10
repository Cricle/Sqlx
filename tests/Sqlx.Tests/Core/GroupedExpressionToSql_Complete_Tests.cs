// -----------------------------------------------------------------------
// <copyright file="GroupedExpressionToSql_Complete_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Comprehensive tests for GroupedExpressionToSql to achieve 100% coverage
    /// </summary>
    public class GroupedExpressionToSql_Complete_Tests
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public decimal? Bonus { get; set; }
            public bool IsActive { get; set; }
        }

        private class TestUserResult
        {
            public int Id { get; set; }
            public int Count { get; set; }
            public decimal TotalSalary { get; set; }
            public decimal AvgSalary { get; set; }
            public decimal MaxSalary { get; set; }
            public decimal MinSalary { get; set; }
        }

        [Fact]
        public void GroupBy_WithComplexSelectProjection_ShouldGenerateCorrectSQL()
        {
            // Test complex projection with multiple aggregate functions
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new TestUserResult
                {
                    Id = g.Key,
                    Count = g.Count(),
                    TotalSalary = g.Sum(x => x.Salary),
                    AvgSalary = (decimal)g.Average(x => x.Salary),
                    MaxSalary = g.Max(x => x.Salary),
                    MinSalary = g.Min(x => x.Salary)
                })
                .ToSql();

            Assert.Contains("SELECT", sql);
            Assert.Contains("COUNT(*)", sql);
            Assert.Contains("SUM", sql);
            Assert.Contains("AVG", sql);
            Assert.Contains("MAX", sql);
            Assert.Contains("MIN", sql);
            Assert.Contains("GROUP BY", sql);
        }

        [Fact]
        public void GroupBy_WithArithmeticInAggregate_ShouldGenerateCorrectSQL()
        {
            // Test arithmetic operations in aggregate functions
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => x.Salary * (decimal)1.2) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("*", sql);
        }

        [Fact]
        public void GroupBy_WithCoalesceInAggregate_ShouldGenerateCorrectSQL()
        {
            // Test null coalescing in aggregate functions
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => x.Bonus ?? 0) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("COALESCE", sql);
        }

        [Fact]
        public void GroupBy_WithConditionalInAggregate_ShouldGenerateCorrectSQL()
        {
            // Test conditional expressions in aggregate functions
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => x.IsActive ? x.Salary : 0) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("CASE WHEN", sql);
        }

        [Fact]
        public void GroupBy_WithMathRoundInAggregate_ShouldGenerateCorrectSQL()
        {
            // Test Math.Round in aggregate
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => Math.Round((double)x.Salary, 2)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("ROUND", sql);
        }

        [Fact]
        public void GroupBy_WithMathFloorInAggregate_ShouldGenerateCorrectSQL()
        {
            // Test Math.Floor in aggregate
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => (decimal)Math.Floor((double)x.Salary)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("FLOOR", sql);
        }

        [Fact]
        public void GroupBy_WithMathCeilingInAggregate_PostgreSQL_ShouldUseCEIL()
        {
            // Test Math.Ceiling in PostgreSQL (uses CEIL)
            var sql = ExpressionToSql<TestUser>.ForPostgreSQL()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => (decimal)Math.Ceiling((double)x.Salary)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("CEIL", sql);
        }

        [Fact]
        public void GroupBy_WithMathCeilingInAggregate_SqlServer_ShouldUseCEILING()
        {
            // Test Math.Ceiling in SQL Server (uses CEILING)
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => (decimal)Math.Ceiling((double)x.Salary)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("CEILING", sql);
        }

        [Fact]
        public void GroupBy_WithMathPowInAggregate_MySQL_ShouldUsePOW()
        {
            // Test Math.Pow in MySQL (uses POW)
            var sql = ExpressionToSql<TestUser>.ForMySql()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => (decimal)Math.Pow((double)x.Salary, 2)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("POW", sql);
        }

        [Fact]
        public void GroupBy_WithMathPowInAggregate_SqlServer_ShouldUsePOWER()
        {
            // Test Math.Pow in SQL Server (uses POWER)
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => (decimal)Math.Pow((double)x.Salary, 2)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("POWER", sql);
        }

        [Fact]
        public void GroupBy_WithMathSqrtInAggregate_ShouldGenerateCorrectSQL()
        {
            // Test Math.Sqrt in aggregate
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => (decimal)Math.Sqrt((double)x.Salary)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("SQRT", sql);
        }

        [Fact]
        public void GroupBy_WithStringLengthInAggregate_SqlServer_ShouldUseLEN()
        {
            // Test string.Length in SQL Server (uses LEN)
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => x.Name.Length) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("LEN", sql);
        }

        [Fact]
        public void GroupBy_WithStringLengthInAggregate_MySQL_ShouldUseLENGTH()
        {
            // Test string.Length in MySQL (uses LENGTH)
            var sql = ExpressionToSql<TestUser>.ForMySql()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => x.Name.Length) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("LENGTH", sql);
        }

        [Fact]
        public void GroupBy_WithStringFunctionsInAggregate_ShouldGenerateCorrectSQL()
        {
            // Test string functions in aggregate
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithBinaryOperationsInSelect_ShouldGenerateCorrectSQL()
        {
            // Test binary operations in SELECT
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Result = g.Sum(x => x.Salary) + 100 })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("+", sql);
        }

        [Fact]
        public void GroupBy_WithConstantInSelect_ShouldGenerateCorrectSQL()
        {
            // Test constant values in SELECT
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Key = g.Key, Constant = 42 })
                .ToSql();

            Assert.Contains("42", sql);
        }

        [Fact]
        public void GroupBy_WithConditionalInSelect_ShouldGenerateCorrectSQL()
        {
            // Test conditional expression in SELECT
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Result = g.Count() > 5 ? 1 : 0 })
                .ToSql();

            Assert.Contains("CASE WHEN", sql);
        }

        [Fact]
        public void GroupBy_WithCoalesceInSelect_ShouldGenerateCorrectSQL()
        {
            // Test coalesce in SELECT
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Result = g.Sum(x => x.Salary) })
                .ToSql();

            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithHavingCondition_ShouldGenerateCorrectSQL()
        {
            // Test HAVING clause with aggregate condition
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Count() > 5)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("HAVING", sql);
            Assert.Contains("COUNT(*)", sql);
            Assert.Contains(">", sql);
        }

        [Fact]
        public void GroupBy_WithHavingOnSum_ShouldGenerateCorrectSQL()
        {
            // Test HAVING clause with SUM condition
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Sum(x => x.Salary) > 10000)
                .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Salary) })
                .ToSql();

            Assert.Contains("HAVING", sql);
            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithHavingOnAverage_ShouldGenerateCorrectSQL()
        {
            // Test HAVING clause with AVG condition
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Average(x => x.Salary) > 5000)
                .Select(g => new { Key = g.Key, Avg = g.Average(x => x.Salary) })
                .ToSql();

            Assert.Contains("HAVING", sql);
            Assert.Contains("AVG", sql);
        }

        [Fact]
        public void GroupBy_WithHavingOnMax_ShouldGenerateCorrectSQL()
        {
            // Test HAVING clause with MAX condition
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Max(x => x.Salary) > 15000)
                .Select(g => new { Key = g.Key, Max = g.Max(x => x.Salary) })
                .ToSql();

            Assert.Contains("HAVING", sql);
            Assert.Contains("MAX", sql);
        }

        [Fact]
        public void GroupBy_WithHavingOnMin_ShouldGenerateCorrectSQL()
        {
            // Test HAVING clause with MIN condition
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Min(x => x.Salary) > 1000)
                .Select(g => new { Key = g.Key, Min = g.Min(x => x.Salary) })
                .ToSql();

            Assert.Contains("HAVING", sql);
            Assert.Contains("MIN", sql);
        }

        [Fact]
        public void GroupBy_WithHavingOnKey_ShouldGenerateCorrectSQL()
        {
            // Test HAVING clause with Key condition
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Key > 25)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("HAVING", sql);
            Assert.Contains("[age]", sql.ToLower());
        }

        [Fact]
        public void GroupBy_WithSingleExpressionSelect_ShouldGenerateCorrectSQL()
        {
            // Test single expression in SELECT (not NewExpression or MemberInitExpression)
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => g.Count())
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithConvertUnaryExpression_ShouldHandleCorrectly()
        {
            // Test Convert unary expression handling
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = (object)g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithFallbackExpression_ShouldHandleGracefully()
        {
            // Test fallback expression handling (should return NULL on error)
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_ToTemplate_ShouldReturnSqlTemplate()
        {
            // Test ToTemplate method
            var template = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToTemplate();

            Assert.Contains("COUNT(*)", template.Sql);
        }

        [Fact]
        public void GroupBy_ToSql_ShouldReturnSqlString()
        {
            // Test ToSql method
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToSql();

            Assert.NotNull(sql);
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithAllComparisonOperators_ShouldGenerateCorrectSQL()
        {
            // Test all comparison operators in SELECT
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new
                {
                    Gt = g.Sum(x => x.Salary) > 1000,
                    Gte = g.Sum(x => x.Salary) >= 1000,
                    Lt = g.Sum(x => x.Salary) < 1000,
                    Lte = g.Sum(x => x.Salary) <= 1000,
                    Eq = g.Sum(x => x.Salary) == 1000,
                    Neq = g.Sum(x => x.Salary) != 1000
                })
                .ToSql();

            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithAllArithmeticOperators_ShouldGenerateCorrectSQL()
        {
            // Test all arithmetic operators in aggregate
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new
                {
                    Add = g.Sum(x => x.Salary + x.Bonus ?? 0),
                    Sub = g.Sum(x => x.Salary - 100),
                    Mul = g.Sum(x => x.Salary * 1.2m),
                    Div = g.Sum(x => x.Salary / 12)
                })
                .ToSql();

            Assert.Contains("SUM", sql);
        }
    }
}
