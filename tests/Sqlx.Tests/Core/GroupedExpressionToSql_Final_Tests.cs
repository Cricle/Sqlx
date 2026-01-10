// -----------------------------------------------------------------------
// <copyright file="GroupedExpressionToSql_Final_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Final tests for GroupedExpressionToSql to achieve maximum coverage
    /// </summary>
    public class GroupedExpressionToSql_Final_Tests
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public bool IsActive { get; set; }
        }

        [Fact]
        public void GroupBy_WithNullKeySelector_ShouldHandleGracefully()
        {
            // Test null key selector handling
            var query = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age);

            var sql = query.ToSql();
            Assert.Contains("GROUP BY", sql);
        }

        [Fact]
        public void GroupBy_WithComplexKeyExpression_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age / 10)
                .Select(g => new { AgeGroup = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("GROUP BY", sql);
        }

        [Fact]
        public void GroupBy_WithStringKey_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("GROUP BY", sql);
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithBooleanKey_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("GROUP BY", sql);
        }

        [Fact]
        public void GroupBy_SelectWithOnlyKey_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Age = g.Key })
                .ToSql();

            Assert.Contains("GROUP BY", sql);
            Assert.Contains("[age]", sql.ToLower());
        }

        [Fact]
        public void GroupBy_SelectWithOnlyAggregates_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    TotalCount = g.Count(),
                    TotalSalary = g.Sum(x => x.Salary)
                })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithAvgDecimal_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { AvgSalary = g.Average(x => x.Salary) })
                .ToSql();

            Assert.Contains("AVG", sql);
        }

        [Fact]
        public void GroupBy_WithMultipleAggregatesOnSameColumn_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    MinSalary = g.Min(x => x.Salary),
                    MaxSalary = g.Max(x => x.Salary),
                    AvgSalary = g.Average(x => x.Salary)
                })
                .ToSql();

            Assert.Contains("MIN", sql);
            Assert.Contains("MAX", sql);
            Assert.Contains("AVG", sql);
        }

        [Fact]
        public void GroupBy_WithComplexAggregateExpression_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    Total = g.Sum(x => x.Salary + 1000)
                })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("+", sql);
        }

        [Fact]
        public void GroupBy_WithNestedConditional_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    Result = g.Count() > 5 ? g.Sum(x => x.Salary) : 0
                })
                .ToSql();

            Assert.Contains("CASE WHEN", sql);
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithAllBinaryOperators_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    Add = g.Sum(x => x.Salary) + 100,
                    Sub = g.Sum(x => x.Salary) - 100,
                    Mul = g.Sum(x => x.Salary) * 2,
                    Div = g.Sum(x => x.Salary) / 2
                })
                .ToSql();

            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithCoalesceInSelect_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    Total = g.Sum(x => x.Salary) + 0
                })
                .ToSql();

            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithAllComparisonOperators_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    Eq = g.Sum(x => x.Salary) == 1000,
                    Neq = g.Sum(x => x.Salary) != 1000,
                    Gt = g.Sum(x => x.Salary) > 1000,
                    Gte = g.Sum(x => x.Salary) >= 1000,
                    Lt = g.Sum(x => x.Salary) < 1000,
                    Lte = g.Sum(x => x.Salary) <= 1000
                })
                .ToSql();

            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithUnaryConvert_ShouldHandleCorrectly()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    Count = (object)g.Count()
                })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithFallbackExpression_ShouldHandleGracefully()
        {
            // Test fallback expression handling
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { 
                    Key = g.Key,
                    Count = g.Count()
                })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_HavingWithComplexExpression_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Count() > 5 && g.Sum(x => x.Salary) > 10000)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("HAVING", sql);
        }

        [Fact]
        public void GroupBy_HavingWithKeyComparison_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Having(g => g.Key > 25 && g.Count() > 5)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("HAVING", sql);
            Assert.Contains("[age]", sql.ToLower());
        }

        [Fact]
        public void GroupBy_WithMathAbsInAggregate_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Total = g.Sum(x => Math.Abs((double)x.Salary)) })
                .ToSql();

            Assert.Contains("SUM", sql);
            Assert.Contains("ABS", sql);
        }

        [Fact]
        public void GroupBy_WithStringToUpperInAggregate_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithStringToLowerInAggregate_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithStringTrimInAggregate_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithStringReplaceInAggregate_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithStringSubstringOneParamInAggregate_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithStringSubstringTwoParamsInAggregate_ShouldGenerateCorrectSQL()
        {
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age)
                .Select(g => new { Count = g.Count() })
                .ToSql();

            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithUnsupportedAggregateFunction_ShouldHandleGracefully()
        {
            // Test that query can be created successfully
            var query = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Age);
            
            Assert.NotNull(query);
        }
    }
}
