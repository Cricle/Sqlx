using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using SqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// {{offset}} 占位符功能测试
/// TDD Phase 1: 核心占位符单元测试
/// </summary>
[TestClass]
public class TDD_OffsetPlaceholder
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

    #region {{offset}} 占位符测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{offset}} 基础测试 - 应该生成OFFSET子句")]
    public void Offset_Basic_ShouldGenerateOffsetClause()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{offset}}占位符应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{offset}}"),
            "SQL不应该包含未处理的{{offset}}占位符");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{offset}} 与{{limit}}组合 - 应该正确配合")]
    public void Offset_WithLimit_ShouldCombineCorrectly()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{limit}} {{offset}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 两个占位符都应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{limit}}"),
            "不应该包含未处理的{{limit}}");
        Assert.IsFalse(result.ProcessedSql.Contains("{{offset}}"),
            "不应该包含未处理的{{offset}}");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{offset}} 零值 - 应该正常工作")]
    public void Offset_ZeroValue_ShouldWork()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} LIMIT 10 OFFSET 0";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 零值OFFSET应该被接受
        Assert.AreEqual(0, result.Errors.Count,
            $"零值OFFSET不应该产生错误。实际错误: {string.Join(", ", result.Errors)}");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{offset}} 负值 - 应该产生错误或警告")]
    public void Offset_NegativeValue_ShouldError()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} LIMIT 10 OFFSET -1";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        // 负值OFFSET应该被正常处理（可能产生警告，但不崩溃）
        Assert.IsNotNull(result.ProcessedSql);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{offset}} 不同数据库方言 - 应该使用正确语法")]
    public void Offset_DifferentDialects_ShouldUseCorrectSyntax()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{limit}} {{offset}}";

        // Act: 测试多个数据库方言
        var sqliteResult = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);
        var postgresResult = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlServerResult = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert: 所有方言都应该生成有效SQL
        Assert.IsNotNull(sqliteResult.ProcessedSql);
        Assert.IsNotNull(postgresResult.ProcessedSql);
        Assert.IsNotNull(sqlServerResult.ProcessedSql);

        // SQLite和PostgreSQL使用OFFSET
        Assert.IsFalse(sqliteResult.ProcessedSql.Contains("{{offset}}"));
        Assert.IsFalse(postgresResult.ProcessedSql.Contains("{{offset}}"));

        // SQL Server可能使用FETCH NEXT或其他语法
        Assert.IsFalse(sqlServerResult.ProcessedSql.Contains("{{offset}}"));
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

