using Xunit;
using Sqlx;
using System;
using System.Linq;

namespace Sqlx.Tests.Core
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Email { get; set; } = "";
    }

    /// <summary>
    /// Tests for uncovered ExpressionToSql methods
    /// </summary>
    public class ExpressionToSql_UncoveredMethods_Tests
    {
        [Fact]
        public void Select_WithStringArray_GeneratesCorrectSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Select("Id", "Name")
                .ToSql();
            
            Assert.Contains("SELECT Id, Name", sql);
            Assert.Contains("FROM [User]", sql);
        }

        [Fact]
        public void Select_WithEmptyArray_GeneratesSelectAll()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Select(Array.Empty<string>())
                .ToSql();
            
            Assert.Contains("SELECT *", sql);
        }

        [Fact]
        public void Select_WithExpression_GeneratesCorrectSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Select(u => new { u.Id, u.Name })
                .ToSql();
            
            Assert.Contains("SELECT", sql);
            Assert.Contains("FROM [User]", sql);
        }

        [Fact]
        public void Select_WithMultipleExpressions_GeneratesCorrectSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Select(u => (object)u.Id, u => (object)u.Name)
                .ToSql();
            
            Assert.Contains("SELECT", sql);
            Assert.Contains("FROM [User]", sql);
        }

        [Fact]
        public void And_AddsWhereCondition()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Where(u => u.Id > 10)
                .And(u => u.Age < 50)
                .ToSql();
            
            Assert.Contains("WHERE", sql);
            Assert.Contains("AND", sql);
        }

        [Fact]
        public void Insert_WithSelector_GeneratesInsertSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Insert(u => new { u.Name, u.Age })
                .Values("John", 30)
                .ToSql();
            
            Assert.StartsWith("INSERT INTO", sql);
            Assert.Contains("VALUES", sql);
        }

        [Fact]
        public void Insert_WithoutSelector_GeneratesInsertSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Insert()
                .Values("John", 30, "john@example.com")
                .ToSql();
            
            Assert.StartsWith("INSERT INTO", sql);
            Assert.Contains("VALUES", sql);
        }

        [Fact]
        public void InsertAll_GeneratesInsertWithAllColumns()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .InsertAll()
                .Values(1, "John", 30, "john@example.com")
                .ToSql();
            
            Assert.StartsWith("INSERT INTO", sql);
            Assert.Contains("[Id]", sql);
            Assert.Contains("[Name]", sql);
            Assert.Contains("[Age]", sql);
            Assert.Contains("[Email]", sql);
        }

        [Fact]
        public void InsertSelect_GeneratesInsertSelectSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Insert(u => new { u.Name, u.Age })
                .InsertSelect("SELECT Name, Age FROM OtherTable")
                .ToSql();
            
            Assert.StartsWith("INSERT INTO", sql);
            Assert.Contains("SELECT Name, Age FROM OtherTable", sql);
        }

        [Fact]
        public void AddValues_AddsMultipleRows()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Insert(u => new { u.Name, u.Age })
                .AddValues("John", 30)
                .AddValues("Jane", 25)
                .ToSql();
            
            Assert.StartsWith("INSERT INTO", sql);
            Assert.Contains("VALUES", sql);
            Assert.Contains("'John'", sql);
            Assert.Contains("'Jane'", sql);
        }

        [Fact]
        public void Having_AddsHavingClause()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Where(u => u.Age > 18)
                .Having(u => u.Age > 18)
                .ToSql();
            
            Assert.Contains("WHERE", sql);
            Assert.Contains("HAVING", sql);
        }

        [Fact]
        public void Delete_WithoutWhere_ThrowsException()
        {
            var expr = ExpressionToSql<User>.ForSqlServer().Delete();
            
            Assert.Throws<InvalidOperationException>(() => expr.ToSql());
        }

        [Fact]
        public void Delete_WithPredicate_GeneratesDeleteSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Delete(u => u.Id == 1)
                .ToSql();
            
            Assert.StartsWith("DELETE FROM", sql);
            Assert.Contains("WHERE", sql);
        }

        [Fact]
        public void Delete_ThenWhere_GeneratesDeleteSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Delete()
                .Where(u => u.Id == 1)
                .ToSql();
            
            Assert.StartsWith("DELETE FROM", sql);
            Assert.Contains("WHERE", sql);
        }

        [Fact]
        public void Update_GeneratesUpdateSql()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .Update()
                .Set(u => u.Name, "John")
                .Where(u => u.Id == 1)
                .ToSql();
            
            Assert.StartsWith("UPDATE", sql);
            Assert.Contains("SET", sql);
            Assert.Contains("WHERE", sql);
        }

        [Fact]
        public void UseParameterizedQueries_EnablesParameterization()
        {
            var expr = ExpressionToSql<User>.ForSqlServer()
                .UseParameterizedQueries()
                .Where(u => u.Id == 1);
            
            var template = expr.ToTemplate();
            Assert.NotNull(template.Parameters);
        }

        [Fact]
        public void ToAdditionalClause_GeneratesCorrectClauses()
        {
            var expr = ExpressionToSql<User>.ForSqlServer()
                .Where(u => u.Age > 18)
                .Having(u => u.Age > 18)
                .OrderBy(u => u.Name)
                .Take(10)
                .Skip(5);
            
            var clause = expr.ToAdditionalClause();
            
            Assert.Contains("HAVING", clause);
            Assert.Contains("ORDER BY", clause);
            Assert.Contains("OFFSET", clause);
        }

        [Fact]
        public void ForDB2_CreatesDB2Dialect()
        {
            var sql = ExpressionToSql<User>.ForDB2()
                .Where(u => u.Id == 1)
                .ToSql();
            
            Assert.Contains("WHERE", sql);
        }

        [Fact]
        public void AddGroupBy_AddsGroupByColumn()
        {
            var sql = ExpressionToSql<User>.ForSqlServer()
                .AddGroupBy("[Age]")
                .ToSql();
            
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("[Age]", sql);
        }
    }
}
