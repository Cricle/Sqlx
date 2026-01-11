// -----------------------------------------------------------------------
// <copyright file="ParameterDictionaryTests.cs" company="Cricle">
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
    /// Tests for parameter dictionary construction in SqlTemplate generation.
    /// TDD Round 3: Parameter Dictionary Building
    /// </summary>
    [TestClass]
    public class ParameterDictionaryTests : TestBase
    {
        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WithScalarParameter_ShouldAddToParameterDictionary()
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
            
            // Should create parameters dictionary
            generatedCode.Should().Contain("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
            
            // Should add the parameter to dictionary
            generatedCode.Should().Contain("parameters[\"@id\"] = id;");
            
            // Should pass parameters to SqlTemplate constructor
            generatedCode.Should().Contain("new global::Sqlx.SqlTemplate(");
            generatedCode.Should().Contain(", parameters)");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WithMultipleScalarParameters_ShouldAddAllToParameterDictionary()
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
        [Sqlx(""SELECT * FROM Users WHERE Name = @name AND Email = @email"")]
        SqlTemplate FindUserSql(string name, string email);
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
            
            // Should add both parameters to dictionary
            generatedCode.Should().Contain("parameters[\"@name\"] = name;");
            generatedCode.Should().Contain("parameters[\"@email\"] = email;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WithComplexObjectParameter_ShouldExpandProperties()
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
        [Sqlx(""INSERT INTO Users (Name, Email) VALUES (@Name, @Email)"")]
        SqlTemplate InsertUserSql(User user);
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
            
            // Should expand object properties into parameters
            // The parameter names should include the property names
            generatedCode.Should().Match(code => 
                code.Contains("parameters[\"@Name\"]") || 
                code.Contains("parameters[\"@name\"]"));
            
            generatedCode.Should().Match(code => 
                code.Contains("parameters[\"@Email\"]") || 
                code.Contains("parameters[\"@email\"]"));
            
            // Should access properties from the object
            generatedCode.Should().Contain("user");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WithMixedParameters_ShouldHandleBothScalarAndComplex()
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
        [Sqlx(""UPDATE Users SET Name = @Name, Email = @Email WHERE Id = @id"")]
        SqlTemplate UpdateUserSql(int id, User user);
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
            
            // Should handle scalar parameter
            generatedCode.Should().Contain("parameters[\"@id\"] = id;");
            
            // Should handle complex object properties
            generatedCode.Should().Match(code => 
                code.Contains("user.Name") || code.Contains("user?.Name"));
            generatedCode.Should().Match(code => 
                code.Contains("user.Email") || code.Contains("user?.Email"));
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void GenerateSqlTemplate_WithDifferentParameterTypes_ShouldHandleAllTypes()
        {
            // Arrange
            var source = @"
using System;
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
        [Sqlx(""SELECT * FROM Users WHERE Id = @id AND Name = @name AND Active = @active AND CreatedDate > @date"")]
        SqlTemplate FindUsersSql(int id, string name, bool active, DateTime date);
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
            
            // Should handle all parameter types
            generatedCode.Should().Contain("parameters[\"@id\"] = id;");
            generatedCode.Should().Contain("parameters[\"@name\"] = name;");
            generatedCode.Should().Contain("parameters[\"@active\"] = active;");
            generatedCode.Should().Contain("parameters[\"@date\"] = date;");
        }
    }
}
