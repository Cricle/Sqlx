using Xunit;
using Sqlx;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for SqlTemplate and SqlTemplateBuilder
    /// </summary>
    public class SqlTemplate_Tests
    {
        [Fact]
        public void Parse_ValidSql_CreatesSqlTemplate()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var template = SqlTemplate.Parse(sql);
            
            Assert.Equal(sql, template.Sql);
            Assert.NotNull(template.Parameters);
            Assert.Empty(template.Parameters);
        }

        [Fact]
        public void Parse_NullSql_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SqlTemplate.Parse(null!));
        }

        [Fact]
        public void Execute_WithoutParameters_UsesDefaultParameters()
        {
            var sql = "SELECT * FROM Users";
            var template = SqlTemplate.Parse(sql);
            var result = template.Execute();
            
            Assert.Equal(sql, result.Sql);
            Assert.NotNull(result.Parameters);
        }

        [Fact]
        public void Execute_WithParameters_UsesProvidedParameters()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var template = SqlTemplate.Parse(sql);
            var parameters = new Dictionary<string, object?> { { "id", 123 } };
            var result = template.Execute(parameters);
            
            Assert.Equal(sql, result.Sql);
            Assert.Equal(123, result.Parameters["id"]);
        }

        [Fact]
        public void Bind_CreatesBuilder()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var template = SqlTemplate.Parse(sql);
            var builder = template.Bind();
            
            Assert.NotNull(builder);
        }

        [Fact]
        public void SqlTemplateBuilder_Param_AddsParameter()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var template = SqlTemplate.Parse(sql);
            var result = template.Bind()
                .Param("id", 123)
                .Build();
            
            Assert.Equal(sql, result.Sql);
            Assert.Equal(123, result.Parameters["id"]);
        }

        [Fact]
        public void SqlTemplateBuilder_MultipleParams_AddsAllParameters()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id AND Name = @name";
            var template = SqlTemplate.Parse(sql);
            var result = template.Bind()
                .Param("id", 123)
                .Param("name", "John")
                .Build();
            
            Assert.Equal(sql, result.Sql);
            Assert.Equal(123, result.Parameters["id"]);
            Assert.Equal("John", result.Parameters["name"]);
        }

        [Fact]
        public void SqlTemplateBuilder_OverwriteParam_UpdatesParameter()
        {
            var sql = "SELECT * FROM Users WHERE Id = @id";
            var template = SqlTemplate.Parse(sql);
            var result = template.Bind()
                .Param("id", 123)
                .Param("id", 456)
                .Build();
            
            Assert.Equal(456, result.Parameters["id"]);
        }
    }
}
