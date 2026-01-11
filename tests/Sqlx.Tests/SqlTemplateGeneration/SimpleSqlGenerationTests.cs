// -----------------------------------------------------------------------
// <copyright file="SimpleSqlGenerationTests.cs" company="Cricle">
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
    /// Tests for simple SQL generation without parameters.
    /// TDD Round 2: Simple SQL Generation
    /// </summary>
    [TestClass]
    public class SimpleSqlGenerationTests : TestBase
    {
        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WhenNoParameters_ShouldReturnCorrectSql()
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
        [Sqlx(""SELECT * FROM Users"")]
        SqlTemplate GetAllUsersSql();
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
            
            // Should contain the SQL string
            generatedCode.Should().Contain("SELECT * FROM Users");
            
            // Should create SqlTemplate with the SQL
            generatedCode.Should().Contain("new global::Sqlx.SqlTemplate(");
            
            // Should have parameters dictionary (even if empty)
            generatedCode.Should().Contain("parameters");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WhenComplexQuery_ShouldReturnCorrectSql()
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
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT Id, Name, Email FROM Users WHERE Active = 1 ORDER BY Name"")]
        SqlTemplate GetActiveUsersSql();
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
            
            // Should contain the complete SQL string
            generatedCode.Should().Contain("SELECT Id, Name, Email FROM Users WHERE Active = 1 ORDER BY Name");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WhenMultilineQuery_ShouldReturnCorrectSql()
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
        [Sqlx(@""
            SELECT 
                Id, 
                Name 
            FROM Users 
            WHERE Active = 1
        "")]
        SqlTemplate GetActiveUsersSql();
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
            
            // Should contain the SQL (whitespace may vary)
            generatedCode.Should().Match(code => 
                code.Contains("SELECT") && 
                code.Contains("FROM Users") && 
                code.Contains("WHERE Active = 1"));
        }
    }
}
