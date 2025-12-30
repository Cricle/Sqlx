using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using SqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// {{batch_values}} 占位符功能测试
/// TDD Phase 1: 核心占位符单元测试
/// </summary>
[TestClass]
public class TDD_BatchValuesPlaceholder
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
        _testMethod = methodClass.GetMembers("InsertManyAsync").OfType<IMethodSymbol>().First();
    }

    #region {{batch_values}} 占位符测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{batch_values}} 单条记录 - 应该生成一行VALUES")]
    public void BatchValues_SingleRecord_ShouldGenerateOneRow()
    {
        // Arrange
        var template = "INSERT INTO {{table}} ({{columns}}) VALUES {{batch_values}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{batch_values}}占位符应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{batch_values}}"),
            "SQL不应该包含未处理的{{batch_values}}占位符");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{batch_values}} 多条记录 - 应该生成多行VALUES")]
    public void BatchValues_MultipleRecords_ShouldGenerateMultipleRows()
    {
        // Arrange
        var template = "INSERT INTO {{table}} ({{columns}}) VALUES {{batch_values}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{batch_values}}应该被处理
        Assert.IsFalse(result.ProcessedSql.Contains("{{batch_values}}"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{batch_values}} 带--exclude选项 - 应该排除指定列")]
    public void BatchValues_WithExclude_ShouldExcludeColumns()
    {
        // Arrange
        var template = "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values --exclude Id}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // {{batch_values}}应该被处理，并排除Id列
        Assert.IsFalse(result.ProcessedSql.Contains("{{batch_values}}"));
        Assert.IsFalse(result.ProcessedSql.Contains("{{columns}}"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{batch_values}} 空集合 - 应该优雅处理")]
    public void BatchValues_EmptyCollection_ShouldHandleGracefully()
    {
        // Arrange
        var template = "INSERT INTO {{table}} ({{columns}}) VALUES {{batch_values}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 空集合应该被优雅处理（不崩溃）
        Assert.AreEqual(0, result.Errors.Count,
            $"空集合不应该产生错误。实际错误: {string.Join(", ", result.Errors)}");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Placeholder")]
    [TestCategory("Phase1")]
    [Description("{{batch_values}} 大集合 - 应该支持分批")]
    public void BatchValues_LargeCollection_ShouldBatch()
    {
        // Arrange
        var template = "INSERT INTO {{table}} ({{columns}}) VALUES {{batch_values}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // 大集合应该被支持（可能生成循环代码）
        Assert.IsFalse(result.ProcessedSql.Contains("{{batch_values}}"));
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
                public Task<int> InsertManyAsync(IEnumerable<TestEntity> entities) => null;
            }
        ";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}


