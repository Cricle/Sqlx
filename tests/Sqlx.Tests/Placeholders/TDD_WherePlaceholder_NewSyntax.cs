// -----------------------------------------------------------------------
// <copyright file="TDD_WherePlaceholder_NewSyntax.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// TDD tests for new {{where --param paramName}} syntax.
/// </summary>
[TestClass]
public class TDD_WherePlaceholder_NewSyntax
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
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sqlx.Annotations
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ExpressionToSqlAttribute : Attribute { }
}

public class TestEntity
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

public interface ITestRepository
{
    Task<TestEntity> TestMethodWithExpression([Sqlx.Annotations.ExpressionToSql] Expression<Func<TestEntity, bool>> predicate);
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

        _testEntity = _compilation.GetTypeByMetadataName("TestEntity")
            ?? throw new InvalidOperationException("Could not find TestEntity");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("Where")]
    public void Where_NewSyntax_WithParam_ShouldWork()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} {{where --param predicate}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_entity", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        Assert.AreEqual(0, result.Errors.Count);
        
        // Should contain runtime marker for expression parameter
        Assert.IsTrue(
            result.ProcessedSql.Contains("{{RUNTIME_WHERE_EXPR_predicate}}") || 
            result.ProcessedSql.Contains("{{runtime_where_expr_predicate}}"),
            $"Should contain runtime marker for predicate parameter. Got: {result.ProcessedSql}");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("Where")]
    public void Where_OldSyntax_ShouldStillWork()
    {
        // Arrange - backward compatibility
        var template = "SELECT {{columns}} FROM {{table}} {{where}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_entity", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // Old syntax should auto-detect ExpressionToSql parameter
        Assert.IsTrue(
            result.ProcessedSql.Contains("{{RUNTIME_WHERE_EXPR_predicate}}") || 
            result.ProcessedSql.Contains("{{runtime_where_expr_predicate}}"),
            $"Old syntax should still work with auto-detection. Got: {result.ProcessedSql}");
    }
}
