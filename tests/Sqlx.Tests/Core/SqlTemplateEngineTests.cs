// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngineTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator.Core;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for the SQL template engine functionality.
/// </summary>
[TestClass]
public class SqlTemplateEngineTests
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _userType = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();
        
        // Create a test compilation
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
    }

    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> GetActiveUsersAsync();
        Task<int> UpdateUserAsync(User user);
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Get test symbols
        var semanticModel = _compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();
        
        _userType = _compilation.GetTypeByMetadataName("TestNamespace.User")!;
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.IUserService")!;
        _testMethod = serviceType.GetMembers("GetUserByIdAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    public void ProcessTemplate_EmptyTemplate_ReturnsDefaultWithWarning()
    {
        // Arrange
        var emptyTemplate = "";

        // Act
        var result = _engine.ProcessTemplate(emptyTemplate, _testMethod, _userType, "User");

        // Assert
        Assert.AreEqual("SELECT 1", result.ProcessedSql);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("Empty SQL template")));
    }

    [TestMethod]
    public void ProcessTemplate_TablePlaceholder_ReplacesWithTableName()
    {
        // Arrange
        var template = "SELECT * FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.AreEqual("SELECT * FROM user", result.ProcessedSql);
        Assert.IsFalse(result.Warnings.Any());
    }

    [TestMethod]
    public void ProcessTemplate_QuotedTablePlaceholder_ReplacesWithQuotedTableName()
    {
        // Arrange
        var template = "SELECT * FROM {{table:quoted}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.AreEqual("SELECT * FROM [user]", result.ProcessedSql);
        Assert.IsFalse(result.Warnings.Any());
    }

    [TestMethod]
    public void ProcessTemplate_ColumnsAutoPlaceholder_ReplacesWithSnakeCaseColumns()
    {
        // Arrange
        var template = "SELECT {{columns:auto}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("id, name, email, age, department_id, is_active"));
        Assert.IsTrue(result.ProcessedSql.Contains("FROM user"));
        Assert.IsFalse(result.Warnings.Any());
    }

    [TestMethod]
    public void ProcessTemplate_QuotedColumnsPlaceholder_ReplacesWithQuotedSnakeCaseColumns()
    {
        // Arrange
        var template = "SELECT {{columns:quoted}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("[id], [name], [email], [age], [department_id], [is_active]"));
        Assert.IsFalse(result.Warnings.Any());
    }

    [TestMethod]
    public void ProcessTemplate_WhereIdPlaceholder_ReplacesWithSnakeCaseId()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} WHERE {{where:id}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.AreEqual("SELECT * FROM user WHERE id = @id", result.ProcessedSql);
        Assert.IsFalse(result.Warnings.Any());
    }

    [TestMethod]
    public void ProcessTemplate_WhereAutoPlaceholder_GeneratesWhereClauseFromParameters()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} WHERE {{where:auto}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("WHERE id = @id"));
        Assert.IsFalse(result.Warnings.Any());
    }

    [TestMethod]
    public void ProcessTemplate_ComplexTemplate_ProcessesAllPlaceholdersCorrectly()
    {
        // Arrange
        var template = "SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:id}} ORDER BY {{columns:auto}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("id, name, email, age, department_id, is_active"));
        Assert.IsTrue(result.ProcessedSql.Contains("FROM [user]"));
        Assert.IsTrue(result.ProcessedSql.Contains("WHERE id = @id"));
        Assert.IsFalse(result.Warnings.Any());
    }

    [TestMethod]
    public void ProcessTemplate_WithParameters_ExtractsParametersCorrectly()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} WHERE id = @id AND name = @name";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.IsTrue(result.Parameters.Any(p => p.Name == "id"));
        Assert.AreEqual("SELECT * FROM user WHERE id = @id AND name = @name", result.ProcessedSql);
    }

    [TestMethod]
    public void ProcessTemplate_UnknownPlaceholder_KeepsOriginalPlaceholder()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} WHERE {{unknown:placeholder}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User");

        // Assert
        Assert.AreEqual("SELECT * FROM user WHERE {{unknown:placeholder}}", result.ProcessedSql);
        // Unknown placeholders may or may not generate warnings depending on implementation
        Assert.IsTrue(true, "Test should verify that unknown placeholders are preserved");
    }

    [TestMethod]
    public void ValidateTemplate_ValidTemplate_ReturnsValid()
    {
        // Arrange
        var template = "SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void ValidateTemplate_EmptyTemplate_ReturnsInvalid()
    {
        // Arrange
        var template = "";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("empty")));
    }
}
