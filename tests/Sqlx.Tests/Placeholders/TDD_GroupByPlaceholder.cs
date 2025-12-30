using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using SqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// {{groupby}} 占位符功能测试
/// TDD Phase 1: 核心占位符单元测试
/// </summary>
[TestClass]
public class TDD_GroupByPlaceholder
{
    private SqlTemplateEngine? _engine;
    private IMethodSymbol? _testMethod;
    private INamedTypeSymbol? _testEntity;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        var compilation = CreateTestCompilation();
        var testClass = compilation.GetTypeByMetadataName("TestEntity")!;
        _testEntity = testClass;

        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    #region {{groupby}} 占位符测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{groupby}} 单列分组 - 应该生成GROUP BY子句")]
    public void GroupBy_SingleColumn_ShouldGenerate()
    {
        // Arrange
        var template = "SELECT name, COUNT(*) FROM {{table}} {{groupby name}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{groupby}}占位符应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{groupby}}"),
            "SQL不应该包含未处理的{{groupby}}占位符");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{groupby}} 多列分组 - 应该生成多列GROUP BY")]
    public void GroupBy_MultipleColumns_ShouldGenerate()
    {
        // Arrange
        var template = "SELECT name, created_at, COUNT(*) FROM {{table}} {{groupby name created_at}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{groupby}}应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{groupby}}"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{groupby}} 与HAVING组合 - 应该配合工作")]
    public void GroupBy_WithHaving_ShouldCombine()
    {
        // Arrange
        var template = "SELECT name, COUNT(*) as count FROM {{table}} {{groupby name}} HAVING count > 1";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{groupby}}应该被处理，且不干扰HAVING
        Assert.IsFalse(result.ProcessedSql.Contains("{{groupby}}"));
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "HAVING",
            "应该保留HAVING子句");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{groupby}} 与ORDER BY组合 - 应该配合工作")]
    public void GroupBy_WithOrderBy_ShouldCombine()
    {
        // Arrange
        var template = "SELECT name, COUNT(*) as count FROM {{table}} {{groupby name}} {{orderby count --desc}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 两个占位符都应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{groupby}}"));
        Assert.IsFalse(result.ProcessedSql.Contains("{{orderby}}"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{groupby}} 空值 - 应该省略GROUP BY")]
    public void GroupBy_Empty_ShouldOmit()
    {
        // Arrange
        var template = "SELECT name, COUNT(*) FROM {{table}} {{groupby}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 空{{groupby}}应该被优雅处理
        Assert.AreEqual(0, result.Errors.Count,
            $"空GROUP BY不应该产生错误。实际错误: {string.Join(", ", result.Errors)}");
    }

    #endregion

    // 辅助方法：创建测试用的编译环境
    private CSharpCompilation CreateTestCompilation()
    {
        var code = @"
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class TestEntity
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public DateTime CreatedAt { get; set; }
                public DateTime UpdatedAt { get; set; }
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


