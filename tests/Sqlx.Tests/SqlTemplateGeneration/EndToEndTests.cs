// -----------------------------------------------------------------------
// <copyright file="EndToEndTests.cs" company="Cricle">
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
    /// End-to-end integration tests for SqlTemplate generation.
    /// Tests complete scenarios with SqlTemplate and execution methods coexisting.
    /// </summary>
    [TestClass]
    public class EndToEndTests : TestBase
    {
        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("Integration")]
        public void EndToEnd_SqlTemplateAndExecuteMethodsCoexist()
        {
            // Arrange - Repository with both SqlTemplate and execution methods
            var source = @"
using System.Data;
using System.Collections.Generic;
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
        // SqlTemplate methods - for debugging
        [Sqlx(""SELECT * FROM Users WHERE Id = @id"")]
        SqlTemplate GetUserByIdSql(int id);

        [Sqlx(""INSERT INTO Users (Name, Email) VALUES (@name, @email)"")]
        SqlTemplate InsertUserSql(string name, string email);

        // Execution methods - for actual database operations
        [Sqlx(""SELECT * FROM Users WHERE Id = @id"")]
        User? GetUserById(int id);

        [Sqlx(""INSERT INTO Users (Name, Email) VALUES (@name, @email)"")]
        int InsertUser(string name, string email);
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
            
            // Verify SqlTemplate methods exist
            generatedCode.Should().Contain("SqlTemplate GetUserByIdSql(int id)");
            generatedCode.Should().Contain("SqlTemplate InsertUserSql(string name, string email)");
            
            // Verify execution methods exist
            generatedCode.Should().Contain("User? GetUserById(int id)");
            generatedCode.Should().Contain("int InsertUser(string name, string email)");
            
            // Verify SqlTemplate methods don't execute queries
            generatedCode.Should().Contain("new global::Sqlx.SqlTemplate(");
            
            // Verify execution methods do execute queries
            generatedCode.Should().Contain("ExecuteReaderAsync");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("Integration")]
        public void EndToEnd_ComplexScenarioWithMultipleDialects()
        {
            // Arrange - Test with different dialects
            var source = @"
using System.Data;
using System.Collections.Generic;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public interface IProductRepository
    {
        [Sqlx(""SELECT * FROM Products WHERE Price > @minPrice"")]
        SqlTemplate GetExpensiveProductsSql(decimal minPrice);

        [Sqlx(""INSERT INTO Products (Name, Price) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate BulkInsertProductsSql(List<Product> products);
    }

    [RepositoryFor(typeof(IProductRepository))]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
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
            
            // Verify PostgreSQL parameter prefix ($)
            generatedCode.Should().Contain("parameters[\"$minPrice\"] = minPrice;");
            
            // Verify batch insert with PostgreSQL prefix
            generatedCode.Should().Contain("$Id_{paramIndex}");
            generatedCode.Should().Contain("$Name_{paramIndex}");
            generatedCode.Should().Contain("$Price_{paramIndex}");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("Integration")]
        public void EndToEnd_ComplexObjectParameters()
        {
            // Arrange
            var source = @"
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SearchCriteria
    {
        public int MinId { get; set; }
        public int MaxId { get; set; }
        public string NamePattern { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM Users WHERE Id >= @MinId AND Id <= @MaxId AND Name LIKE @NamePattern"")]
        SqlTemplate SearchUsersSql(SearchCriteria criteria);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SqlServer)]
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
            
            // Verify all properties are extracted from complex object
            generatedCode.Should().Contain("parameters[\"@MinId\"] = criteria?.MinId;");
            generatedCode.Should().Contain("parameters[\"@MaxId\"] = criteria?.MaxId;");
            generatedCode.Should().Contain("parameters[\"@NamePattern\"] = criteria?.NamePattern;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("Integration")]
        public void EndToEnd_MixedScalarAndComplexParameters()
        {
            // Arrange
            var source = @"
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Filter
    {
        public string NamePattern { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM Users WHERE Id > @minId AND Name LIKE @NamePattern"")]
        SqlTemplate SearchUsersSql(int minId, Filter filter);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.MySql)]
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
            
            // Verify scalar parameter
            generatedCode.Should().Contain("parameters[\"@minId\"] = minId;");
            
            // Verify complex object property
            generatedCode.Should().Contain("parameters[\"@NamePattern\"] = filter?.NamePattern;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("Integration")]
        public void EndToEnd_VerifyNoSideEffects()
        {
            // Arrange
            var source = @"
using System.Data;
using Sqlx;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM Users WHERE Id = @id"")]
        SqlTemplate GetUserByIdSql(int id);
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
            
            // Verify SqlTemplate method creates dictionary and returns SqlTemplate
            generatedCode.Should().Contain("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
            generatedCode.Should().Contain("__result__ = new global::Sqlx.SqlTemplate(");
            
            // Verify NO database execution code
            var methodStart = generatedCode.IndexOf("SqlTemplate GetUserByIdSql(int id)");
            var methodEnd = generatedCode.IndexOf("SqlTemplate GetUserByIdSql", methodStart + 1);
            if (methodEnd == -1) methodEnd = generatedCode.Length;
            
            var methodBody = generatedCode.Substring(methodStart, methodEnd - methodStart);
            
            // Should NOT contain ExecuteReader, ExecuteScalar, ExecuteNonQuery
            methodBody.Should().NotContain("ExecuteReaderAsync");
            methodBody.Should().NotContain("ExecuteScalarAsync");
            methodBody.Should().NotContain("ExecuteNonQueryAsync");
        }
    }
}
