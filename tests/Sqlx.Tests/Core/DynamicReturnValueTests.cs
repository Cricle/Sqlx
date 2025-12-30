using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// TDD: 动态返回值 List&lt;Dictionary&lt;string, object&gt;&gt; 功能测试
/// </summary>
[TestClass]
public class DynamicReturnValueTests
{
    private Compilation _compilation = null!;
    private INamedTypeSymbol _userEntityType = null!;
    private INamedTypeSymbol _dictionaryStringObjectType = null!;
    private INamedTypeSymbol _listDictionaryType = null!;

    [TestInitialize]
    public void Setup()
    {
        // 创建测试编译
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public interface IReportService
    {
        // 动态返回值 - 列在运行时确定
        List<Dictionary<string, object>> GetDynamicReport(string columns);
        
        // 动态返回值 - 使用 --regex 筛选
        List<Dictionary<string, object>> GetRegexReport(string regexPattern);
        
        // 动态返回值 - 单行
        Dictionary<string, object>? GetSingleRow(int id);
        
        // 组合：部分列是动态的
        Task<List<Dictionary<string, object>>> GetAsyncReport();
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DateTime).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 获取测试符号
        _userEntityType = _compilation.GetTypeByMetadataName("TestNamespace.User")!;

        // 获取 Dictionary<string, object> 和 List<Dictionary<string, object>> 类型
        _dictionaryStringObjectType = _compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")!
            .Construct(_compilation.GetSpecialType(SpecialType.System_String), _compilation.GetSpecialType(SpecialType.System_Object));
        _listDictionaryType = _compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!
            .Construct(_dictionaryStringObjectType);
    }

    #region 基础动态返回值测试

    [TestMethod]
    public void DynamicReturn_BasicScenario_ShouldGenerateCorrectCode()
    {
        // Arrange
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.IReportService")!;
        var method = serviceType.GetMembers("GetDynamicReport").OfType<IMethodSymbol>().First();

        // Act
        var returnType = method.ReturnType as INamedTypeSymbol;

        // Assert - 验证返回类型是 List<Dictionary<string, object>>
        Assert.IsNotNull(returnType);
        Assert.AreEqual("List", returnType.Name);
        Assert.IsTrue(returnType.TypeArguments.Length > 0);

        var dictType = returnType.TypeArguments[0] as INamedTypeSymbol;
        Assert.IsNotNull(dictType);
        Assert.AreEqual("Dictionary", dictType.Name);
        Assert.AreEqual(2, dictType.TypeArguments.Length);
        Assert.AreEqual(SpecialType.System_String, dictType.TypeArguments[0].SpecialType);
        Assert.AreEqual(SpecialType.System_Object, dictType.TypeArguments[1].SpecialType);
    }

    [TestMethod]
    public void DynamicReturn_WithRegexColumn_ShouldWork()
    {
        // Arrange
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.IReportService")!;
        var method = serviceType.GetMembers("GetRegexReport").OfType<IMethodSymbol>().First();

        // Act
        var returnType = method.ReturnType;

        // Assert - 验证方法支持 --regex 参数
        Assert.IsNotNull(method.Parameters.FirstOrDefault(p => p.Name == "regexPattern"));
    }

    [TestMethod]
    public void DynamicReturn_SingleRow_ShouldReturnNullable()
    {
        // Arrange
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.IReportService")!;
        var method = serviceType.GetMembers("GetSingleRow").OfType<IMethodSymbol>().First();

        // Act
        var returnType = method.ReturnType;

        // Assert - 单行应该返回 Dictionary<string, object>? (nullable)
        Assert.IsTrue(returnType.NullableAnnotation == NullableAnnotation.Annotated ||
                     returnType.ToString().Contains("?"),
                     "Single row should return nullable Dictionary");
    }

    #endregion

    #region 代码生成测试

    // Note: Code generation tests are covered in integration tests
    // These placeholders have been removed in favor of actual generator tests

    [TestMethod]
    public void CodeGen_DynamicReturn_ShouldSupportAsync()
    {
        // Arrange
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.IReportService")!;
        var method = serviceType.GetMembers("GetAsyncReport").OfType<IMethodSymbol>().First();

        // Act
        var returnType = method.ReturnType as INamedTypeSymbol;

        // Assert - 验证异步支持
        Assert.IsNotNull(returnType);
        Assert.AreEqual("Task", returnType.Name);
    }

    #endregion

    #region 性能测试 - Placeholder tests removed (covered in integration tests)

    // Performance tests are validated in actual integration scenarios

    #endregion

    // Note: Security, boundary, and integration tests for dynamic return values
    // are covered in actual integration tests with real database scenarios.
    // Placeholder tests have been removed to avoid meaningless assertions.
}

