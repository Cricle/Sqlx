// -----------------------------------------------------------------------
// <copyright file="DialectTests.cs" company="Cricle">
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
    /// Tests for SqlTemplate generation with different database dialects.
    /// Verifies that parameter prefixes and column wrapping are correctly applied.
    /// </summary>
    [TestClass]
    public class DialectTests : TestBase
    {
        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void SqlServer_Dialect_UsesAtSignParameterPrefix()
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
            generatedCode.Should().Contain("parameters[\"@id\"] = id;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void MySQL_Dialect_UsesAtSignParameterPrefix()
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
            generatedCode.Should().Contain("parameters[\"@id\"] = id;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void PostgreSQL_Dialect_UsesDollarSignParameterPrefix()
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
        [Sqlx(""SELECT * FROM Users WHERE Id = $id"")]
        SqlTemplate GetUserByIdSql(int id);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
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
            generatedCode.Should().Contain("parameters[\"$id\"] = id;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void SQLite_Dialect_UsesAtSignParameterPrefix()
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
            generatedCode.Should().Contain("parameters[\"@id\"] = id;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void BatchInsert_SqlServer_UsesAtSignParameterPrefix()
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
        [Sqlx(""INSERT INTO Users (Id, Name) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertUsersSql(List<User> users);
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
            generatedCode.Should().Contain("@Id_{paramIndex}");
            generatedCode.Should().Contain("@Name_{paramIndex}");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void BatchInsert_PostgreSQL_UsesDollarSignParameterPrefix()
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
        [Sqlx(""INSERT INTO Users (Id, Name) VALUES {{VALUES_PLACEHOLDER}}"")]
        SqlTemplate InsertUsersSql(List<User> users);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
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
            generatedCode.Should().Contain("$Id_{paramIndex}");
            generatedCode.Should().Contain("$Name_{paramIndex}");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void ComplexObject_SqlServer_UsesAtSignParameterPrefix()
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

    public class UserFilter
    {
        public int MinId { get; set; }
        public string NamePattern { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM Users WHERE Id >= @MinId AND Name LIKE @NamePattern"")]
        SqlTemplate SearchUsersSql(UserFilter filter);
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
            generatedCode.Should().Contain("parameters[\"@MinId\"] = filter?.MinId;");
            generatedCode.Should().Contain("parameters[\"@NamePattern\"] = filter?.NamePattern;");
        }

        [TestMethod]
        [TestCategory("SqlTemplateGeneration")]
        [TestCategory("TDD")]
        public void ComplexObject_PostgreSQL_UsesDollarSignParameterPrefix()
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

    public class UserFilter
    {
        public int MinId { get; set; }
        public string NamePattern { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM Users WHERE Id >= $MinId AND Name LIKE $NamePattern"")]
        SqlTemplate SearchUsersSql(UserFilter filter);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
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
            generatedCode.Should().Contain("parameters[\"$MinId\"] = filter?.MinId;");
            generatedCode.Should().Contain("parameters[\"$NamePattern\"] = filter?.NamePattern;");
        }
    }
}
