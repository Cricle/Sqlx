// -----------------------------------------------------------------------
// <copyright file="BatchInsertTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.SqlTemplateGeneration
{
    /// <summary>
    /// Tests for batch INSERT operations in SqlTemplate generation.
    /// TDD Round 4: Batch Operations Support
    /// </summary>
    [TestClass]
    public class BatchInsertTests : TestBase
    {
        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WithValuesPlaceholder_ShouldDetectBatchInsert()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""INSERT INTO Users (Name, Email) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertUsersSql(List<User> users);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""Users"")]
    public partial class UserRepository : IUserRepository { }
}";

            // Act
            var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

            // Assert
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.Should().BeEmpty();
            
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
            generatedCode.Should().NotBeNullOrEmpty();
            
            // Should detect VALUES_PLACEHOLDER and use batch insert logic
            generatedCode.Should().Contain("sqlBuilder");
            generatedCode.Should().Contain("foreach");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_BatchInsert_ShouldGenerateCorrectSql()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""INSERT INTO Users (Name, Email) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertUsersSql(List<User> users);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""Users"")]
    public partial class UserRepository : IUserRepository { }
}";

            // Act
            var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

            // Assert
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.Should().BeEmpty();
            
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
            generatedCode.Should().NotBeNullOrEmpty();
            
            // Should build SQL with StringBuilder
            generatedCode.Should().Contain("var sqlBuilder = new global::System.Text.StringBuilder");
            
            // Should append VALUES clauses
            generatedCode.Should().Contain("sqlBuilder.Append(\"(\")");
            generatedCode.Should().Contain("sqlBuilder.Append(\")\")");
            
            // Should handle comma separation
            generatedCode.Should().Contain("isFirst");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_BatchInsert_ShouldGenerateParameterDictionary()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""INSERT INTO Users (Name, Email) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertUsersSql(List<User> users);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""Users"")]
    public partial class UserRepository : IUserRepository { }
}";

            // Act
            var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

            // Assert
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.Should().BeEmpty();
            
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
            generatedCode.Should().NotBeNullOrEmpty();
            
            // Should create parameters dictionary
            generatedCode.Should().Contain("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
            
            // Should use paramIndex for unique parameter names
            generatedCode.Should().Contain("paramIndex");
            
            // Should add parameters for each item
            generatedCode.Should().Match(code => 
                code.Contains("parameters[") && code.Contains("item."));
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_BatchInsert_ShouldHandleMultipleProperties()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; }
    }

    public interface IProductRepository
    {
        [Sqlx(""INSERT INTO Products (Name, Price, Stock, Category) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertProductsSql(List<Product> products);
    }

    [RepositoryFor(typeof(IProductRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""Products"")]
    public partial class ProductRepository : IProductRepository { }
}";

            // Act
            var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

            // Assert
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.Should().BeEmpty();
            
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "ProductRepository");
            generatedCode.Should().NotBeNullOrEmpty();
            
            // Should handle all properties
            generatedCode.Should().Contain("item.Name");
            generatedCode.Should().Contain("item.Price");
            generatedCode.Should().Contain("item.Stock");
            generatedCode.Should().Contain("item.Category");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_BatchInsert_ShouldReturnSqlTemplate()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""INSERT INTO Users (Name, Email) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertUsersSql(List<User> users);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""Users"")]
    public partial class UserRepository : IUserRepository { }
}";

            // Act
            var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

            // Assert
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.Should().BeEmpty();
            
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
            generatedCode.Should().NotBeNullOrEmpty();
            
            // Should return SqlTemplate with generated SQL and parameters
            generatedCode.Should().Match(code => 
                code.Contains("new global::Sqlx.SqlTemplate(") &&
                (code.Contains("return new global::Sqlx.SqlTemplate(") || code.Contains("__result__ = new global::Sqlx.SqlTemplate(")));
            generatedCode.Should().Contain("sqlBuilder.ToString()");
            generatedCode.Should().Contain(", parameters)");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_BatchInsert_WithEmptyCollection_ShouldHandleGracefully()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""INSERT INTO Users (Name, Email) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertUsersSql(List<User> users);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""Users"")]
    public partial class UserRepository : IUserRepository { }
}";

            // Act
            var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

            // Assert
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.Should().BeEmpty();
            
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
            generatedCode.Should().NotBeNullOrEmpty();
            
            // Should check for null or empty collection
            generatedCode.Should().Match(code => 
                code.Contains("== null") || code.Contains("!") && code.Contains(".Any()"));
        }
    }
}
