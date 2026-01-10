using Xunit;
using Sqlx;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    public class ProductItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Advanced tests for ExpressionToSqlBase parsing methods
    /// </summary>
    public class ExpressionToSqlBase_Advanced_Tests
    {
        [Fact]
        public void CollectionContains_GeneratesInClause()
        {
            var ids = new List<int> { 1, 2, 3 };
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => ids.Contains(p.Id))
                .ToSql();
            
            Assert.Contains("IN", sql);
            Assert.Contains("1", sql);
            Assert.Contains("2", sql);
            Assert.Contains("3", sql);
        }

        [Fact]
        public void CollectionContains_EmptyList_GeneratesInNull()
        {
            var ids = new List<int>();
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => ids.Contains(p.Id))
                .ToSql();
            
            Assert.Contains("IN (NULL)", sql);
        }

        [Fact]
        public void ConditionalExpression_GeneratesCaseWhen()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => (p.Price > 100 ? p.IsActive : !p.IsActive))
                .ToSql();
            
            Assert.Contains("CASE WHEN", sql);
            Assert.Contains("THEN", sql);
            Assert.Contains("ELSE", sql);
            Assert.Contains("END", sql);
        }

        [Fact]
        public void MathFunctions_GeneratesCorrectSql()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => Math.Abs(p.Price) > 100)
                .ToSql();
            
            Assert.Contains("ABS", sql);
        }

        [Fact]
        public void StringToUpper_GeneratesUpper()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.Name.ToUpper() == "TEST")
                .ToSql();
            
            Assert.Contains("UPPER", sql);
        }

        [Fact]
        public void StringToLower_GeneratesLower()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.Name.ToLower() == "test")
                .ToSql();
            
            Assert.Contains("LOWER", sql);
        }

        [Fact]
        public void StringTrim_GeneratesTrim()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.Name.Trim() == "test")
                .ToSql();
            
            Assert.Contains("TRIM", sql);
        }

        [Fact]
        public void StringLength_GeneratesLen()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.Name.Length > 10)
                .ToSql();
            
            Assert.Contains("LEN", sql);
        }

        [Fact]
        public void NullComparison_GeneratesIsNull()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.Name == null)
                .ToSql();
            
            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void NullNotEqual_GeneratesIsNotNull()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.Name != null)
                .ToSql();
            
            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void BooleanTrue_GeneratesCorrectComparison()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.IsActive == true)
                .ToSql();
            
            Assert.Contains("= 1", sql);
        }

        [Fact]
        public void BooleanFalse_GeneratesCorrectComparison()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.IsActive == false)
                .ToSql();
            
            Assert.Contains("= 0", sql);
        }

        [Fact]
        public void BooleanProperty_GeneratesCorrectComparison()
        {
            var sql = ExpressionToSql<ProductItem>.ForSqlServer()
                .Where(p => p.IsActive)
                .ToSql();
            
            Assert.Contains("= 1", sql);
        }

        [Fact]
        public void SetTableName_ChangesTableName()
        {
            var expr = ExpressionToSql<ProductItem>.ForSqlServer();
            expr.SetTableName("CustomProducts");
            var sql = expr.ToSql();
            
            Assert.Contains("CustomProducts", sql);
        }

        [Fact]
        public void AddGroupBy_AddsGroupByClause()
        {
            var expr = ExpressionToSql<ProductItem>.ForSqlServer();
            expr.AddGroupBy("[Price]");
            var sql = expr.ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("[Price]", sql);
        }

        [Fact]
        public void AddGroupBy_DuplicateColumn_DoesNotAddTwice()
        {
            var expr = ExpressionToSql<ProductItem>.ForSqlServer();
            expr.AddGroupBy("[Price]");
            expr.AddGroupBy("[Price]");
            var sql = expr.ToSql();
            
            var count = sql.Split(new[] { "[Price]" }, StringSplitOptions.None).Length - 1;
            Assert.Equal(1, count);
        }

        [Fact]
        public void MySql_UsesBackticks()
        {
            var sql = ExpressionToSql<ProductItem>.ForMySql()
                .Where(p => p.Id == 1)
                .ToSql();
            
            Assert.Contains("`", sql);
        }

        [Fact]
        public void PostgreSQL_UsesDoubleQuotes()
        {
            var sql = ExpressionToSql<ProductItem>.ForPostgreSQL()
                .Where(p => p.Id == 1)
                .ToSql();
            
            Assert.Contains("\"", sql);
        }

        [Fact]
        public void Oracle_UsesDoubleQuotes()
        {
            var sql = ExpressionToSql<ProductItem>.ForOracle()
                .Where(p => p.Id == 1)
                .ToSql();
            
            Assert.Contains("\"", sql);
        }
    }
}
