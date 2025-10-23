using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Collections.Generic;

namespace Sqlx.Tests;

/// <summary>
/// SQL模板引擎安全验证测试
/// 测试目标：确保占位符选项中的 -- 不会被误判为 SQL 注入
/// </summary>
[TestClass]
public class SqlTemplateEngineSecurityTests
{
    private SqlTemplateEngine? _engine;
    private IMethodSymbol? _testMethod;
    private INamedTypeSymbol? _testEntity;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        // 创建测试用的 IMethodSymbol 和 INamedTypeSymbol
        var compilation = CreateTestCompilation();
        var testClass = compilation.GetTypeByMetadataName("TestEntity")!;
        _testEntity = testClass;

        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    [Description("占位符选项中的 -- 不应被误判为 SQL 注入")]
    public void PlaceholderOption_WithDoubleDash_ShouldNotBeDetectedAsSqlInjection()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), "ProcessedSql 不应为空");
        Assert.IsFalse(result.Errors.Any(e => e.Contains("SQL injection")),
            $"不应该有 SQL 注入错误。实际错误: {string.Join("; ", result.Errors)}");
    }

    [TestMethod]
    [Description("生成的 SQL 不应为空字符串")]
    public void ProcessTemplate_WithValidTemplate_ShouldReturnNonEmptySql()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        Assert.IsNotNull(result.ProcessedSql);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), "ProcessedSql 不应为空");
        Assert.AreNotEqual("", result.ProcessedSql);
        StringAssert.Contains(result.ProcessedSql, "SELECT");
        StringAssert.Contains(result.ProcessedSql, "FROM");
    }

    [TestMethod]
    [Description("ORDER BY 占位符应正确生成 DESC 排序")]
    public void OrderByPlaceholder_WithDescOption_ShouldGenerateDescendingOrder()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "ORDER BY");
        StringAssert.Contains(result.ProcessedSql, "DESC");
        StringAssert.Contains(result.ProcessedSql, "created_at");
    }

    [TestMethod]
    [Description("多个占位符选项都不应触发 SQL 注入检测")]
    public void MultipleOptions_WithDoubleDash_ShouldNotTriggerInjectionDetection()
    {
        // Arrange
        var templates = new[]
        {
            "SELECT {{columns --exclude Id}} FROM {{table}}",
            "INSERT INTO {{table}} ({{columns --exclude Id CreatedAt}}) VALUES ({{values}})",
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            "SELECT * FROM {{table}} {{orderby created_at --desc}}",
            "SELECT * FROM {{table}} {{limit --offset 10}}"
        };

        foreach (var template in templates)
        {
            // Act
            var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

            // Assert
            Assert.IsFalse(result.Errors.Any(e => e.Contains("SQL injection")),
                $"模板 '{template}' 不应触发 SQL 注入检测。错误: {string.Join("; ", result.Errors)}");
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"模板 '{template}' 应该生成非空的 SQL");
        }
    }

    [TestMethod]
    [Description("真正的 SQL 注入模式应该被检测")]
    public void RealSqlInjection_ShouldBeDetected()
    {
        // Arrange
        var maliciousTemplates = new[]
        {
            "SELECT * FROM users UNION SELECT * FROM passwords",
            "DROP TABLE users",
            "SELECT * FROM users WHERE id = 1; DROP TABLE users",
            "exec('malicious code')",
            "SELECT * FROM users /* comment */ WHERE id = 1"
        };

        foreach (var template in maliciousTemplates)
        {
            // Act
            var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

            // Assert
            Assert.IsTrue(result.Errors.Any(e => e.Contains("SQL injection") || e.Contains("security")),
                $"恶意模板 '{template}' 应该被检测为 SQL 注入");
        }
    }

    [TestMethod]
    [Description("空模板应返回警告但不应崩溃")]
    public void EmptyTemplate_ShouldReturnWarningNotCrash()
    {
        // Arrange
        var template = "";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), "应该有默认值");
        Assert.IsTrue(result.Warnings.Count > 0, "应该有警告");
    }

    [TestMethod]
    [Description("生成的 SQL 应包含所有必要的列")]
    public void GeneratedSql_ShouldContainAllColumns()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        StringAssert.Contains(lowerSql, "id");
        StringAssert.Contains(lowerSql, "name");
        StringAssert.Contains(lowerSql, "created_at");
    }

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
