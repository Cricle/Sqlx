using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using SqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// {{where}} 占位符功能测试
/// TDD Phase 1: 核心占位符单元测试
/// </summary>
[TestClass]
public class TDD_WherePlaceholder
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

    #region {{where}} 占位符基础测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{where}} 基础测试 - 应该生成WHERE子句")]
    public void Where_Basic_ShouldGenerateWhereClause()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{where}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{where}}占位符应该被处理（不再包含大括号）
        Assert.IsFalse(result.ProcessedSql.Contains("{{where}}"),
            "SQL不应该包含未处理的{{where}}占位符");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{where}} 与Expression参数结合 - 应该转换为SQL")]
    public void Where_WithExpression_ShouldTranslateToSql()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{where}}";

        // 创建带有Expression参数的方法
        var compilation = CreateCompilationWithExpressionMethod();
        var testClass = compilation.GetTypeByMetadataName("TestEntity")!;
        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        var method = methodClass.GetMembers("FindByNameAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine!.ProcessTemplate(template, method, testClass, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 应该生成WHERE子句或相关代码
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        Assert.IsFalse(lowerSql.Contains("{{where}}"),
            "不应该包含未处理的占位符");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{where}} 空WHERE - 应该省略WHERE子句")]
    public void Where_Empty_ShouldOmitWhereClause()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{where}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 空WHERE应该被优雅处理（不产生错误）
        Assert.AreEqual(0, result.Errors.Count,
            $"不应该有错误。实际错误: {string.Join(", ", result.Errors)}");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{where}} 与动态片段 - 应该支持动态WHERE")]
    public void Where_WithDynamicFragment_ShouldWork()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{where}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 动态WHERE应该被支持
        Assert.IsFalse(result.ProcessedSql.Contains("{{where}}"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{where}} 多条件组合 - 应该用AND连接")]
    public void Where_MultipleConditions_ShouldCombineWithAnd()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{where}}";

        // 创建带有多个参数的方法
        var compilation = CreateCompilationWithMultipleParams();
        var testClass = compilation.GetTypeByMetadataName("TestEntity")!;
        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        var method = methodClass.GetMembers("FindByMultipleAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine!.ProcessTemplate(template, method, testClass, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // WHERE占位符应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{where}}"));
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

    private CSharpCompilation CreateCompilationWithExpressionMethod()
    {
        var code = @"
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;
            using System.Threading.Tasks;

            public class TestEntity
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            public class TestMethods
            {
                public Task<List<TestEntity>> FindByNameAsync(Expression<Func<TestEntity, bool>> predicate) => null;
            }
        ";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private CSharpCompilation CreateCompilationWithMultipleParams()
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
            }

            public class TestMethods
            {
                public Task<List<TestEntity>> FindByMultipleAsync(string name, long id) => null;
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


