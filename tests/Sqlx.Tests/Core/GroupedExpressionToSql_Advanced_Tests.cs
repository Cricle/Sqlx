using Xunit;
using Sqlx;
using System;
using System.Linq;

namespace Sqlx.Tests.Core
{
    public class OrderEntity
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "";
    }

    /// <summary>
    /// Advanced tests for GroupedExpressionToSql aggregate functions
    /// </summary>
    public class GroupedExpressionToSql_Advanced_Tests
    {
        [Fact]
        public void GroupBy_WithSelect_GeneratesGroupedQuery()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, TotalOrders = g.Count() })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GroupBy_WithSum_GeneratesSumAggregate()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, TotalAmount = g.Sum(o => o.Amount) })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void GroupBy_WithAverage_GeneratesAvgAggregate()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, AvgAmount = g.Average(o => o.Amount) })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("AVG", sql);
        }

        [Fact]
        public void GroupBy_WithMax_GeneratesMaxAggregate()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, MaxAmount = g.Max(o => o.Amount) })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("MAX", sql);
        }

        [Fact]
        public void GroupBy_WithMin_GeneratesMinAggregate()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, MinAmount = g.Min(o => o.Amount) })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("MIN", sql);
        }

        [Fact]
        public void GroupBy_WithMultipleAggregates_GeneratesCorrectSql()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new 
                { 
                    CustomerId = g.Key, 
                    Count = g.Count(),
                    Total = g.Sum(o => o.Amount),
                    Avg = g.Average(o => o.Amount)
                })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("COUNT(*)", sql);
            Assert.Contains("SUM", sql);
            Assert.Contains("AVG", sql);
        }

        [Fact]
        public void GroupBy_WithWhere_GeneratesWhereBeforeGroupBy()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .Where(o => o.Amount > 100)
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("WHERE", sql);
            Assert.Contains("GROUP BY", sql);
            var whereIndex = sql.IndexOf("WHERE");
            var groupByIndex = sql.IndexOf("GROUP BY");
            Assert.True(whereIndex < groupByIndex);
        }

        [Fact]
        public void GroupBy_WithHaving_GeneratesHavingClause()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Having(g => g.Sum(o => o.Amount) > 1000)
                .Select(g => new { CustomerId = g.Key, Total = g.Sum(o => o.Amount) })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("HAVING", sql);
        }

        [Fact]
        public void GroupBy_WithOrderBy_GeneratesOrderByClause()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .OrderBy(r => r.Count)
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("ORDER BY", sql);
        }

        [Fact]
        public void GroupBy_WithTakeSkip_GeneratesPagination()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .Take(10)
                .Skip(5)
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("OFFSET", sql);
            Assert.Contains("FETCH NEXT", sql);
        }

        [Fact]
        public void GroupBy_MySql_UsesLimitOffset()
        {
            var sql = ExpressionToSql<OrderEntity>.ForMySql()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .Take(10)
                .Skip(5)
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("LIMIT", sql);
            Assert.Contains("OFFSET", sql);
        }

        [Fact]
        public void GroupBy_PostgreSQL_GeneratesCorrectSyntax()
        {
            var sql = ExpressionToSql<OrderEntity>.ForPostgreSQL()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("\"", sql); // PostgreSQL uses double quotes
        }

        [Fact]
        public void GroupBy_ComplexExpression_GeneratesCorrectSql()
        {
            var sql = ExpressionToSql<OrderEntity>.ForSqlServer()
                .Where(o => o.Status == "Completed")
                .GroupBy(o => o.CustomerId)
                .Having(g => g.Count() > 5)
                .Select(g => new 
                { 
                    CustomerId = g.Key,
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.Amount),
                    AvgAmount = g.Average(o => o.Amount),
                    MaxAmount = g.Max(o => o.Amount),
                    MinAmount = g.Min(o => o.Amount)
                })
                .OrderBy(r => r.TotalAmount)
                .Take(20)
                .ToSql();
            
            Assert.Contains("WHERE", sql);
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("HAVING", sql);
            Assert.Contains("COUNT(*)", sql);
            Assert.Contains("SUM", sql);
            Assert.Contains("AVG", sql);
            Assert.Contains("MAX", sql);
            Assert.Contains("MIN", sql);
            Assert.Contains("ORDER BY", sql);
            Assert.Contains("FETCH NEXT", sql);
        }
    }
}
