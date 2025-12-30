// -----------------------------------------------------------------------
// <copyright file="AggregateNullHandlingTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using GenSqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Core;

/// <summary>
/// Unit tests for aggregate function NULL handling with COALESCE.
/// **Validates: Requirements 1.5**
/// </summary>
public class AggregateNullHandlingTests
{
    private readonly SqlTemplateEngine _engine;
    private readonly IMethodSymbol _testMethod;
    private readonly INamedTypeSymbol _testEntity;

    public AggregateNullHandlingTests()
    {
        _engine = new SqlTemplateEngine();
        var compilation = CreateTestCompilation();
        _testEntity = compilation.GetTypeByMetadataName("TestEntity")!;
        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    /// <summary>
    /// Test that COUNT with coalesce option returns 0 for empty tables.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Fact]
    public void Count_WithCoalesceOption_ShouldWrapWithCoalesce()
    {
        // Arrange
        var template = "SELECT {{count:*|coalesce=true}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "users", GenSqlDefine.SQLite);

        // Assert
        Assert.Contains("COALESCE(COUNT(*), 0)", result.ProcessedSql);
    }

    /// <summary>
    /// Test that SUM with default option returns 0 for empty tables.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Fact]
    public void Sum_WithDefaultOption_ShouldWrapWithCoalesce()
    {
        // Arrange
        var template = "SELECT {{sum:balance|default=0}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "users", GenSqlDefine.SQLite);

        // Assert
        Assert.Contains("COALESCE(SUM", result.ProcessedSql);
        Assert.Contains(", 0)", result.ProcessedSql);
    }

    /// <summary>
    /// Test that AVG with coalesce option returns 0 for empty tables.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Fact]
    public void Avg_WithCoalesceOption_ShouldWrapWithCoalesce()
    {
        // Arrange
        var template = "SELECT {{avg:balance|coalesce=true}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "users", GenSqlDefine.SQLite);

        // Assert
        Assert.Contains("COALESCE(AVG", result.ProcessedSql);
        Assert.Contains(", 0)", result.ProcessedSql);
    }

    /// <summary>
    /// Test that aggregate functions without coalesce option don't wrap with COALESCE.
    /// </summary>
    [Fact]
    public void Count_WithoutCoalesceOption_ShouldNotWrapWithCoalesce()
    {
        // Arrange
        var template = "SELECT {{count:*}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "users", GenSqlDefine.SQLite);

        // Assert
        Assert.DoesNotContain("COALESCE", result.ProcessedSql);
        Assert.Contains("COUNT(*)", result.ProcessedSql);
    }

    /// <summary>
    /// Test that custom default value is used when specified.
    /// </summary>
    [Fact]
    public void Sum_WithCustomDefaultValue_ShouldUseCustomValue()
    {
        // Arrange
        var template = "SELECT {{sum:balance|default=100}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "users", GenSqlDefine.SQLite);

        // Assert
        Assert.Contains("COALESCE(SUM", result.ProcessedSql);
        Assert.Contains(", 100)", result.ProcessedSql);
    }

    /// <summary>
    /// Test that COALESCE works with column-specific aggregates.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Fact]
    public void Aggregate_WithColumnAndCoalesce_ShouldWrapCorrectly()
    {
        // Arrange
        var template = "SELECT {{count:id|coalesce=true}}, {{sum:amount|default=0}}, {{avg:price|coalesce=true}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "orders", GenSqlDefine.SQLite);

        // Assert
        Assert.Contains("COALESCE(COUNT", result.ProcessedSql);
        Assert.Contains("COALESCE(SUM", result.ProcessedSql);
        Assert.Contains("COALESCE(AVG", result.ProcessedSql);
    }

    private static Compilation CreateTestCompilation()
    {
        var code = @"
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class TestEntity
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public decimal Balance { get; set; }
            }

            public class TestMethods
            {
                public Task<List<TestEntity>> GetAllAsync() => null;
            }
        ";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
