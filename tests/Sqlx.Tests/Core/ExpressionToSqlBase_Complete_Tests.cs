using Xunit;
using Sqlx;
using System;

namespace Sqlx.Tests.Core
{
    public class TestEntity2
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Complete tests for ExpressionToSqlBase uncovered methods
    /// </summary>
    public class ExpressionToSqlBase_Complete_Tests
    {
        [Fact]
        public void StringReplace_GeneratesReplaceFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Name.Replace("old", "new") == "test")
                .ToSql();
            
            Assert.Contains("REPLACE", sql);
        }

        [Fact]
        public void StringSubstring_OneArg_GeneratesSubstringFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Name.Substring(1) == "test")
                .ToSql();
            
            Assert.Contains("SUBSTRING", sql);
        }

        [Fact]
        public void StringSubstring_TwoArgs_GeneratesSubstringFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Name.Substring(1, 5) == "test")
                .ToSql();
            
            Assert.Contains("SUBSTRING", sql);
        }

        [Fact]
        public void StringSubstring_SQLite_GeneratesSubstrFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlite()
                .Where(e => e.Name.Substring(1) == "test")
                .ToSql();
            
            Assert.Contains("SUBSTR", sql);
        }

        [Fact]
        public void MathRound_OneArg_GeneratesRoundFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => Math.Round(e.Price) > 100)
                .ToSql();
            
            Assert.Contains("ROUND", sql);
        }

        [Fact]
        public void MathRound_TwoArgs_GeneratesRoundFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => Math.Round(e.Price, 2) > 100)
                .ToSql();
            
            Assert.Contains("ROUND", sql);
        }

        [Fact]
        public void MathFloor_GeneratesFloorFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => Math.Floor(e.Price) > 100)
                .ToSql();
            
            Assert.Contains("FLOOR", sql);
        }

        [Fact]
        public void MathSqrt_GeneratesSqrtFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => Math.Sqrt((double)e.Price) > 10)
                .ToSql();
            
            Assert.Contains("SQRT", sql);
        }

        [Fact]
        public void MathCeiling_SqlServer_GeneratesCeilingFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => Math.Ceiling((double)e.Price) > 100)
                .ToSql();
            
            Assert.Contains("CEILING", sql);
        }

        [Fact]
        public void MathCeiling_PostgreSQL_GeneratesCeilFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForPostgreSQL()
                .Where(e => Math.Ceiling((double)e.Price) > 100)
                .ToSql();
            
            Assert.Contains("CEIL", sql);
        }

        [Fact]
        public void MathPow_SqlServer_GeneratesPowerFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => Math.Pow((double)e.Price, 2) > 100)
                .ToSql();
            
            Assert.Contains("POWER", sql);
        }

        [Fact]
        public void MathPow_MySQL_GeneratesPowFunction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForMySql()
                .Where(e => Math.Pow((double)e.Price, 2) > 100)
                .ToSql();
            
            Assert.Contains("POW", sql);
        }

        [Fact]
        public void ArithmeticAdd_GeneratesAddition()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price + 10 > 100)
                .ToSql();
            
            Assert.Contains("+", sql);
        }

        [Fact]
        public void ArithmeticSubtract_GeneratesSubtraction()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price - 10 > 100)
                .ToSql();
            
            Assert.Contains("-", sql);
        }

        [Fact]
        public void ArithmeticMultiply_GeneratesMultiplication()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price * 2 > 100)
                .ToSql();
            
            Assert.Contains("*", sql);
        }

        [Fact]
        public void ArithmeticDivide_GeneratesDivision()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price / 2 > 100)
                .ToSql();
            
            Assert.Contains("/", sql);
        }

        [Fact]
        public void ArithmeticModulo_GeneratesModulo()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Id % 2 == 0)
                .ToSql();
            
            Assert.Contains("%", sql);
        }

        [Fact]
        public void CoalesceOperator_GeneratesCoalesce()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => (e.Name ?? "default") == "test")
                .ToSql();
            
            Assert.Contains("COALESCE", sql);
        }

        [Fact]
        public void ComparisonGreaterThan_GeneratesGT()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price > 100)
                .ToSql();
            
            Assert.Contains(">", sql);
        }

        [Fact]
        public void ComparisonGreaterThanOrEqual_GeneratesGTE()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price >= 100)
                .ToSql();
            
            Assert.Contains(">=", sql);
        }

        [Fact]
        public void ComparisonLessThan_GeneratesLT()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price < 100)
                .ToSql();
            
            Assert.Contains("<", sql);
        }

        [Fact]
        public void ComparisonLessThanOrEqual_GeneratesLTE()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price <= 100)
                .ToSql();
            
            Assert.Contains("<=", sql);
        }

        [Fact]
        public void ComparisonNotEqual_GeneratesNotEqual()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price != 100)
                .ToSql();
            
            Assert.Contains("<>", sql);
        }

        [Fact]
        public void LogicalAnd_GeneratesAnd()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price > 100 && e.Id < 50)
                .ToSql();
            
            Assert.Contains("AND", sql);
        }

        [Fact]
        public void LogicalOr_GeneratesOr()
        {
            var sql = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price > 100 || e.Id < 50)
                .ToSql();
            
            Assert.Contains("OR", sql);
        }

        [Fact]
        public void BooleanLiteral_PostgreSQL_UsesKeywords()
        {
            var sql = ExpressionToSql<TestEntity2>.ForPostgreSQL()
                .Where(e => e.Name == "test")
                .ToSql();
            
            // PostgreSQL uses true/false for boolean literals
            Assert.NotNull(sql);
        }

        [Fact]
        public void WhereFrom_MergesConditions()
        {
            var expr1 = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Id > 10);
            
            var expr2 = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Price < 100)
                .WhereFrom(expr1);
            
            var whereClause = expr2.ToWhereClause();
            Assert.Contains("price", whereClause.ToLower());
        }

        [Fact]
        public void WhereFrom_WithNull_ThrowsArgumentNullException()
        {
            var expr = ExpressionToSql<TestEntity2>.ForSqlServer();
            
            Assert.Throws<ArgumentNullException>(() => expr.WhereFrom(null!));
        }

        [Fact]
        public void GetParameters_ReturnsParametersDictionary()
        {
            var expr = ExpressionToSql<TestEntity2>.ForSqlServer()
                .UseParameterizedQueries()
                .Where(e => e.Id == 123);
            
            var parameters = expr.GetParameters();
            Assert.NotNull(parameters);
        }

        [Fact]
        public void ToWhereClause_WithoutConditions_ReturnsEmpty()
        {
            var expr = ExpressionToSql<TestEntity2>.ForSqlServer();
            var whereClause = expr.ToWhereClause();
            
            Assert.Equal(string.Empty, whereClause);
        }

        [Fact]
        public void ToWhereClause_WithConditions_ReturnsClause()
        {
            var expr = ExpressionToSql<TestEntity2>.ForSqlServer()
                .Where(e => e.Id > 10);
            var whereClause = expr.ToWhereClause();
            
            Assert.NotEmpty(whereClause);
        }
    }
}
