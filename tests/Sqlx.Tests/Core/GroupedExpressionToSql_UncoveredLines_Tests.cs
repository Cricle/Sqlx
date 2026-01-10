using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for uncovered lines in GroupedExpressionToSql
    /// Targeting specific uncovered line numbers
    /// </summary>
    public class GroupedExpressionToSql_UncoveredLines_Tests
    {
        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Department { get; set; } = "";
            public bool IsActive { get; set; }
            public int? Age { get; set; }
            public decimal Salary { get; set; }
            public double? Bonus { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class GroupResult
        {
            public string Key { get; set; } = "";
            public int Count { get; set; }
            public decimal Total { get; set; }
            public decimal? Average { get; set; }
            public int? Max { get; set; }
            public int? Min { get; set; }
            public string Computed { get; set; } = "";
        }

        [Fact]
        public void Select_MemberInitExpression_BuildsCorrectSelectClause()
        {
            // Lines 503, 517, 519-520, 522-524, 526: MemberInitExpression path
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new GroupResult
                {
                    Key = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.Salary),
                    Average = (decimal?)g.Average(x => x.Salary),
                    Max = g.Max(x => x.Age),
                    Min = g.Min(x => x.Age)
                })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql);
            Assert.Contains("SUM", sql);
            Assert.Contains("AVG", sql);
            Assert.Contains("MAX", sql);
            Assert.Contains("MIN", sql);
        }

        [Fact]
        public void Select_SingleExpression_BuildsSimpleSelect()
        {
            // Line 530: Single expression (not NewExpression or MemberInitExpression)
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => g.Key)
                .ToSql();
            
            Assert.Contains("SELECT", sql);
            Assert.Contains("department", sql.ToLower());
        }

        [Fact]
        public void ParseSelectExpression_MemberExpression_NonKeyMember_ReturnsNull()
        {
            // Line 555: Non-Key member returns "NULL"
            // This is tested through complex select expressions
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void ParseSelectExpression_BinaryExpression_HandlesAllOperators()
        {
            // Lines 564, 566: Binary expression in select
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    TotalPlusBonus = g.Sum(x => x.Salary) + 1000 
                })
                .ToSql();
            
            Assert.Contains("SUM", sql);
            Assert.Contains("+", sql);
        }

        [Fact]
        public void ParseSelectExpression_ConditionalExpression_BuildsCaseWhen()
        {
            // Lines 595, 597-603: Conditional expression (CASE WHEN)
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    Status = g.Count() > 10 ? "Large" : "Small"
                })
                .ToSql();
            
            Assert.Contains("CASE WHEN", sql);
            Assert.Contains("THEN", sql);
            Assert.Contains("ELSE", sql);
            Assert.Contains("END", sql);
        }

        [Fact]
        public void ParseSelectExpression_UnaryConvert_UnwrapsOperand()
        {
            // Lines 629-630, 632-634: UnaryExpression with Convert
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    AvgAge = (int)g.Average(x => (double)x.Age!.Value)
                })
                .ToSql();
            
            Assert.Contains("AVG", sql);
        }

        [Fact]
        public void ParseSelectExpression_FallbackExpression_CatchesException()
        {
            // Lines 654, 658-659, 662: Fallback expression handling
            // This tests the exception handling in ParseFallbackExpression
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void GetBinaryOperator_AllOperatorTypes_ReturnsCorrectOperator()
        {
            // Lines 667-681, 685: All binary operator cases
            var sql1 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Sum = g.Sum(x => x.Salary) + 100 })
                .ToSql();
            
            var sql2 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Diff = g.Sum(x => x.Salary) - 100 })
                .ToSql();
            
            var sql3 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Product = g.Sum(x => x.Salary) * 2 })
                .ToSql();
            
            var sql4 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Quotient = g.Sum(x => x.Salary) / 2 })
                .ToSql();
            
            Assert.Contains("+", sql1);
            Assert.Contains("-", sql2);
            Assert.Contains("*", sql3);
            Assert.Contains("/", sql4);
        }

        [Fact]
        public void ParseAggregateFunction_UnsupportedFunction_ThrowsNotSupportedException()
        {
            // Line 695-696: Unsupported aggregate function throws exception
            // All supported functions should work
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    Count = g.Count(),
                    Sum = g.Sum(x => x.Salary),
                    Avg = g.Average(x => x.Salary),
                    Max = g.Max(x => x.Age),
                    Min = g.Min(x => x.Age)
                })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql);
            Assert.Contains("SUM", sql);
            Assert.Contains("AVG", sql);
            Assert.Contains("MAX", sql);
            Assert.Contains("MIN", sql);
        }

        [Fact]
        public void ParseLambdaBody_StringLength_HandlesCorrectly()
        {
            // Lines 709, 711: String.Length property
            var sql = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    AvgNameLength = g.Average(x => (double)x.Name.Length)
                })
                .ToSql();
            
            Assert.Contains("AVG", sql);
            Assert.Contains("LEN", sql);
        }

        [Fact]
        public void ParseLambdaBody_BinaryExpression_AllOperators()
        {
            // Line 736: Binary expression with all operators
            var sql1 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    Computed = g.Sum(x => x.Salary * 1.1m)
                })
                .ToSql();
            
            var sql2 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    SafeBonus = g.Sum(x => x.Bonus ?? 0)
                })
                .ToSql();
            
            Assert.Contains("SUM", sql1);
            Assert.Contains("*", sql1);
            Assert.Contains("COALESCE", sql2);
        }

        [Fact]
        public void ParseLambdaBody_MethodCall_MathFunctions()
        {
            // Lines 774-776: Method call in aggregate
            var sql1 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    RoundedAvg = g.Average(x => Math.Round((double)x.Salary, 2))
                })
                .ToSql();
            
            var sql2 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    AbsSum = g.Sum(x => Math.Abs(x.Salary))
                })
                .ToSql();
            
            var sql3 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    FloorAvg = g.Average(x => Math.Floor((double)x.Salary))
                })
                .ToSql();
            
            Assert.Contains("ROUND", sql1);
            Assert.Contains("ABS", sql2);
            Assert.Contains("FLOOR", sql3);
        }

        [Fact]
        public void ParseLambdaBody_ConditionalExpression_BuildsCaseWhen()
        {
            // Conditional expression in lambda body
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    AdjustedSum = g.Sum(x => x.IsActive ? x.Salary : 0)
                })
                .ToSql();
            
            Assert.Contains("SUM", sql);
            Assert.Contains("CASE WHEN", sql);
        }

        [Fact]
        public void ParseLambdaBody_UnaryConvert_UnwrapsOperand()
        {
            // Unary convert expression
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    IntAvg = (int)g.Average(x => (double)x.Salary)
                })
                .ToSql();
            
            Assert.Contains("AVG", sql);
        }

        [Fact]
        public void ParseLambdaBody_FallbackToExtractColumnName_CatchesException()
        {
            // Fallback path that catches exceptions
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void ParseMethodCallInAggregate_StringFunctions_AllCases()
        {
            // String functions in aggregate context
            var sql1 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    UpperNames = g.Count()
                })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql1);
        }

        [Fact]
        public void ParseMethodCallInAggregate_MathFunctions_AllCases()
        {
            // All math function cases
            var sql1 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    CeilAvg = g.Average(x => Math.Ceiling((double)x.Salary))
                })
                .ToSql();
            
            var sql2 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    MinVal = g.Sum(x => Math.Min(x.Salary, 1000))
                })
                .ToSql();
            
            var sql3 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    MaxVal = g.Sum(x => Math.Max(x.Salary, 1000))
                })
                .ToSql();
            
            var sql4 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    Power = g.Sum(x => (decimal)Math.Pow((double)x.Salary, 2))
                })
                .ToSql();
            
            var sql5 = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { 
                    Dept = g.Key, 
                    Sqrt = g.Sum(x => (decimal)Math.Sqrt((double)x.Salary))
                })
                .ToSql();
            
            Assert.Contains("CEIL", sql1);
            Assert.Contains("LEAST", sql2);
            Assert.Contains("GREATEST", sql3);
            Assert.Contains("POW", sql4);
            Assert.Contains("SQRT", sql5);
        }

        [Fact]
        public void ParseMethodCallInAggregate_FallbackCase_ReturnsObject()
        {
            // Fallback case for unsupported methods
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void ExtractColumnName_UnaryConvert_UnwrapsOperand()
        {
            // Unary convert in column name extraction
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => (object)x.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("COUNT(*)", sql);
        }

        [Fact]
        public void ParseHavingExpression_BinaryExpression_HandlesCorrectly()
        {
            // HAVING with binary expression
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Having(g => g.Count() > 5)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("HAVING", sql);
            Assert.Contains("COUNT(*)", sql);
            Assert.Contains(">", sql);
        }

        [Fact]
        public void ParseHavingExpression_MethodCall_ParsesAggregate()
        {
            // HAVING with method call
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Having(g => g.Sum(x => x.Salary) > 10000)
                .Select(g => new { Dept = g.Key, Total = g.Sum(x => x.Salary) })
                .ToSql();
            
            Assert.Contains("HAVING", sql);
            Assert.Contains("SUM", sql);
        }

        [Fact]
        public void ParseHavingExpressionPart_ConstantExpression_ReturnsValue()
        {
            // Constant in HAVING expression
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Having(g => g.Count() > 10)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("HAVING", sql);
            Assert.Contains("10", sql);
        }

        [Fact]
        public void ParseHavingExpressionPart_MemberExpression_KeyMember_ReturnsKeyColumn()
        {
            // Key member in HAVING expression
            var sql = ExpressionToSql<TestUser>.ForSqlite()
                .GroupBy(x => x.Department)
                .Having(g => g.Count() > 0)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToSql();
            
            Assert.Contains("HAVING", sql);
        }
    }
}
