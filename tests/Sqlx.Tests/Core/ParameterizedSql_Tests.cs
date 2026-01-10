using Xunit;
using Sqlx;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for ParameterizedSql class
    /// </summary>
    public class ParameterizedSql_Tests
    {
        [Fact]
        public void Create_WithSqlOnly_CreatesInstance()
        {
            var sql = "SELECT * FROM Users";
            var paramSql = ParameterizedSql.Create(sql);
            
            Assert.Equal(sql, paramSql.Sql);
            Assert.Null(paramSql.Parameters);
        }

        [Fact]
        public void Create_WithParameters_CreatesInstance()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", 123 } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            
            Assert.Equal(sql, paramSql.Sql);
            Assert.NotNull(paramSql.Parameters);
            Assert.Equal(123, paramSql.Parameters["@id"]);
        }

        [Fact]
        public void Render_WithoutParameters_ReturnsSql()
        {
            var sql = "SELECT * FROM Users";
            var paramSql = ParameterizedSql.Create(sql);
            var rendered = paramSql.Render();
            
            Assert.Equal(sql, rendered);
        }

        [Fact]
        public void Render_WithNullParameters_ReturnsSql()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var paramSql = ParameterizedSql.Create(sql, null);
            var rendered = paramSql.Render();
            
            Assert.Equal(sql, rendered);
        }

        [Fact]
        public void Render_WithEmptyParameters_ReturnsSql()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var paramSql = ParameterizedSql.Create(sql, new Dictionary<string, object?>());
            var rendered = paramSql.Render();
            
            Assert.Equal(sql, rendered);
        }

        [Fact]
        public void Render_WithIntParameter_ReplacesParameter()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", 123 } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE Id = 123", rendered);
        }

        [Fact]
        public void Render_WithStringParameter_ReplacesWithQuotedString()
        {
            var sql = "SELECT * FROM Users WHERE Name = @name";
            var parameters = new Dictionary<string, object?> { { "@name", "John" } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE Name = 'John'", rendered);
        }

        [Fact]
        public void Render_WithStringContainingQuote_EscapesQuote()
        {
            var sql = "SELECT * FROM Users WHERE Name = @name";
            var parameters = new Dictionary<string, object?> { { "@name", "O'Brien" } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE Name = 'O''Brien'", rendered);
        }

        [Fact]
        public void Render_WithNullParameter_ReplacesWithNull()
        {
            var sql = "SELECT * FROM Users WHERE Name = @name";
            var parameters = new Dictionary<string, object?> { { "@name", null } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE Name = NULL", rendered);
        }

        [Fact]
        public void Render_WithBoolTrueParameter_ReplacesWithOne()
        {
            var sql = "SELECT * FROM Users WHERE IsActive = @active";
            var parameters = new Dictionary<string, object?> { { "@active", true } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE IsActive = 1", rendered);
        }

        [Fact]
        public void Render_WithBoolFalseParameter_ReplacesWithZero()
        {
            var sql = "SELECT * FROM Users WHERE IsActive = @active";
            var parameters = new Dictionary<string, object?> { { "@active", false } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE IsActive = 0", rendered);
        }

        [Fact]
        public void Render_WithDateTimeParameter_ReplacesWithFormattedDate()
        {
            var sql = "SELECT * FROM Orders WHERE OrderDate = @date";
            var date = new DateTime(2024, 12, 25, 10, 30, 45);
            var parameters = new Dictionary<string, object?> { { "@date", date } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Orders WHERE OrderDate = '2024-12-25 10:30:45'", rendered);
        }

        [Fact]
        public void Render_WithGuidParameter_ReplacesWithQuotedGuid()
        {
            var sql = "SELECT * FROM Users WHERE UserId = @id";
            var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
            var parameters = new Dictionary<string, object?> { { "@id", guid } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE UserId = '12345678-1234-1234-1234-123456789012'", rendered);
        }

        [Fact]
        public void Render_WithDecimalParameter_ReplacesWithNumber()
        {
            var sql = "SELECT * FROM Products WHERE Price = @price";
            var parameters = new Dictionary<string, object?> { { "@price", 99.99m } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Contains("99.99", rendered);
        }

        [Fact]
        public void Render_WithDoubleParameter_ReplacesWithNumber()
        {
            var sql = "SELECT * FROM Products WHERE Price = @price";
            var parameters = new Dictionary<string, object?> { { "@price", 99.99 } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Contains("99.99", rendered);
        }

        [Fact]
        public void Render_WithFloatParameter_ReplacesWithNumber()
        {
            var sql = "SELECT * FROM Products WHERE Price = @price";
            var parameters = new Dictionary<string, object?> { { "@price", 99.99f } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Contains("99.99", rendered);
        }

        [Fact]
        public void Render_WithMultipleParameters_ReplacesAll()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id AND Name = @name AND IsActive = @active";
            var parameters = new Dictionary<string, object?> 
            { 
                { "@id", 123 },
                { "@name", "John" },
                { "@active", true }
            };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE Id = 123 AND Name = 'John' AND IsActive = 1", rendered);
        }

        [Fact]
        public void Render_WithCustomObjectParameter_UsesToString()
        {
            var sql = "SELECT * FROM Users WHERE Status = @status";
            var parameters = new Dictionary<string, object?> { { "@status", 42 } };
            var paramSql = ParameterizedSql.Create(sql, parameters);
            var rendered = paramSql.Render();
            
            Assert.Equal("SELECT * FROM Users WHERE Status = 42", rendered);
        }
    }
}
