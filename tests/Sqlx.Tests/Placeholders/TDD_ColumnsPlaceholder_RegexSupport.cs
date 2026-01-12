// -----------------------------------------------------------------------
// <copyright file="TDD_ColumnsPlaceholder_RegexSupport.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// TDD tests for {{columns --regex pattern}} support.
/// </summary>
[TestClass]
public class TDD_ColumnsPlaceholder_RegexSupport
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _testEntity = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        var sourceCode = @"
using System.Threading.Tasks;

public class UserProfile
{
    public long Id { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public int UserAge { get; set; }
    public string ProfileBio { get; set; }
    public string ProfileAvatar { get; set; }
}

public interface ITestRepository
{
    Task<UserProfile> TestMethod();
}
";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        _compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var semanticModel = _compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        _testMethod = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol
            ?? throw new InvalidOperationException("Could not get method symbol");

        _testEntity = _compilation.GetTypeByMetadataName("UserProfile")
            ?? throw new InvalidOperationException("Could not find UserProfile");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("Columns")]
    [TestCategory("Regex")]
    public void Columns_WithRegex_ShouldMatchUserFields()
    {
        // Arrange
        var template = "SELECT {{columns --regex ^User.*}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "user_profile", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        Assert.AreEqual(0, result.Errors.Count);
        
        // Should include User* fields
        Assert.IsTrue(result.ProcessedSql.Contains("user_name"), "Should include user_name");
        Assert.IsTrue(result.ProcessedSql.Contains("user_email"), "Should include user_email");
        Assert.IsTrue(result.ProcessedSql.Contains("user_age"), "Should include user_age");
        
        // Should NOT include Profile* or Id fields
        Assert.IsFalse(result.ProcessedSql.Contains("profile_bio"), "Should NOT include profile_bio");
        Assert.IsFalse(result.ProcessedSql.Contains("profile_avatar"), "Should NOT include profile_avatar");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("Columns")]
    [TestCategory("Regex")]
    public void Columns_WithRegex_ShouldMatchProfileFields()
    {
        // Arrange
        var template = "SELECT {{columns --regex ^Profile.*}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "user_profile", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        
        // Should include Profile* fields
        Assert.IsTrue(result.ProcessedSql.Contains("profile_bio"), "Should include profile_bio");
        Assert.IsTrue(result.ProcessedSql.Contains("profile_avatar"), "Should include profile_avatar");
        
        // Should NOT include User* fields
        Assert.IsFalse(result.ProcessedSql.Contains("user_name"), "Should NOT include user_name");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("Columns")]
    [TestCategory("Regex")]
    public void Columns_WithRegexAndExclude_ShouldCombine()
    {
        // Arrange
        var template = "SELECT {{columns --regex ^User.* --exclude UserAge}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "user_profile", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        
        // Should include User* except UserAge
        Assert.IsTrue(result.ProcessedSql.Contains("user_name"));
        Assert.IsTrue(result.ProcessedSql.Contains("user_email"));
        Assert.IsFalse(result.ProcessedSql.Contains("user_age"), "Should exclude user_age");
    }
}
