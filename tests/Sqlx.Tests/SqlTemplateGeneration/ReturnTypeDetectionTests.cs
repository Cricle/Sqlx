// -----------------------------------------------------------------------
// <copyright file="ReturnTypeDetectionTests.cs" company="Cricle">
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
    /// Tests for SqlTemplate return type detection.
    /// TDD Round 1: Return Type Detection
    /// </summary>
    [TestClass]
    public class ReturnTypeDetectionTests : TestBase
    {
        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GetReturnType_WhenReturnsSqlTemplate_ShouldDetectSqlTemplateType()
        {
            // Arrange
            var source = @"
using System.Data;
using System.Threading.Tasks;
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
            // Should not have compilation errors
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.Should().BeEmpty($"Should not have compilation errors, but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
            
            // Get generated code
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
            generatedCode.Should().NotBeNullOrEmpty("Generator should produce output");
            
            // The generated method should return SqlTemplate
            generatedCode.Should().Contain("SqlTemplate GetUserByIdSql(int id)");
            
            // The generated method should NOT contain database execution code
            generatedCode.Should().NotContain("ExecuteReader");
            generatedCode.Should().NotContain("ExecuteScalar");
            generatedCode.Should().NotContain("ExecuteNonQuery");
            
            // Should contain SqlTemplate construction
            generatedCode.Should().Contain("new global::Sqlx.SqlTemplate(");
            generatedCode.Should().Contain("parameters");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GetReturnType_WhenReturnsTaskSqlTemplate_ShouldDetectAsyncSqlTemplateType()
        {
            // Arrange
            var source = @"
using System.Data;
using System.Threading.Tasks;
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
        Task<SqlTemplate> GetUserByIdSqlAsync(int id);
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
            
            // The generated method should return Task<SqlTemplate>
            generatedCode.Should().Match(code => 
                code.Contains("Task<SqlTemplate> GetUserByIdSqlAsync(int id)") ||
                code.Contains("Task<Sqlx.SqlTemplate> GetUserByIdSqlAsync(int id)") ||
                code.Contains("System.Threading.Tasks.Task<Sqlx.SqlTemplate> GetUserByIdSqlAsync(int id)"));
            
            // Should NOT contain database execution code
            generatedCode.Should().NotContain("ExecuteReaderAsync");
            generatedCode.Should().NotContain("ExecuteScalarAsync");
            generatedCode.Should().NotContain("ExecuteNonQueryAsync");
            
            // Should contain SqlTemplate construction
            generatedCode.Should().Contain("new global::Sqlx.SqlTemplate(");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GetReturnType_WhenReturnsUser_ShouldNotAffectExistingBehavior()
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
        User GetUserById(int id);
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
            
            // The generated method should execute query normally
            generatedCode.Should().Contain("User GetUserById(int id)");
            
            // Should contain database execution code
            generatedCode.Should().ContainAny("ExecuteReader", "FromSqlRaw");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GetReturnType_WhenReturnsList_ShouldNotAffectExistingBehavior()
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
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM Users"")]
        List<User> GetAllUsers();
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
            
            // The generated method should execute query normally
            generatedCode.Should().Match(code => 
                code.Contains("List<User> GetAllUsers()") ||
                code.Contains("List<Test.User> GetAllUsers()") ||
                code.Contains("System.Collections.Generic.List<Test.User> GetAllUsers()"));
            
            // Should contain database execution code
            generatedCode.Should().ContainAny("ExecuteReader", "FromSqlRaw", "ToList");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GetReturnType_WhenReturnsScalar_ShouldNotAffectExistingBehavior()
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
        [Sqlx(""SELECT COUNT(*) FROM Users"")]
        int GetUserCount();
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
            
            // The generated method should execute query normally
            generatedCode.Should().Contain("int GetUserCount()");
            
            // Should contain database execution code
            generatedCode.Should().Contain("ExecuteScalar");
        }
    }
}
