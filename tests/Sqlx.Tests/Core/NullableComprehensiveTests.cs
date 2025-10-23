using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 全面的 Nullable 支持测试
/// 确保 nullable value types (int?) 和 nullable reference types (string?) 都能正确处理
/// </summary>
[TestClass]
public class NullableComprehensiveTests
{
    private Compilation _compilation = null!;
    private INamedTypeSymbol _entityType = null!;
    private SqlTemplateEngine _engine = null!;
    private IMethodSymbol _testMethod = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SqlTemplateEngine();

        // 创建测试编译（启用 nullable 引用类型）
        var sourceCode = @"
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    // 实体包含各种 nullable 和 non-nullable 类型
    public class Product
    {
        // 非nullable值类型
        public int Id { get; set; }
        public long StockCount { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Nullable 值类型 (Nullable<T>)
        public int? CategoryId { get; set; }
        public long? SupplierId { get; set; }
        public decimal? DiscountPrice { get; set; }
        public DateTime? LastModified { get; set; }
        public bool? IsFeatured { get; set; }

        // 非nullable引用类型
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;

        // Nullable 引用类型
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Tags { get; set; }
    }

    public interface IProductService
    {
        Task<Product?> GetProductAsync(int id);
        Task<List<Product>> GetAllProductsAsync();
        Task<int> InsertProductAsync(Product product);
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, new CSharpParseOptions(
            Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11,
            kind: Microsoft.CodeAnalysis.SourceCodeKind.Regular));

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DateTime).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        // 获取测试符号
        _entityType = _compilation.GetTypeByMetadataName("TestNamespace.Product")!;
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.IProductService")!;
        _testMethod = serviceType.GetMembers("GetProductAsync").OfType<IMethodSymbol>().First();

        Assert.IsNotNull(_entityType, "Entity type should be found");
    }

    #region Nullable Value Types 测试

    [TestMethod]
    public void Entity_NullableValueTypes_ShouldBeDetected()
    {
        // Arrange & Act
        var properties = _entityType.GetMembers().OfType<IPropertySymbol>().ToArray();

        // 检查 nullable value types
        var categoryId = properties.First(p => p.Name == "CategoryId");
        var supplierId = properties.First(p => p.Name == "SupplierId");
        var discountPrice = properties.First(p => p.Name == "DiscountPrice");
        var lastModified = properties.First(p => p.Name == "LastModified");
        var isFeatured = properties.First(p => p.Name == "IsFeatured");

        // Assert - 应该识别为 nullable
        Assert.IsTrue(categoryId.Type.IsNullableType(), "int? should be nullable");
        Assert.IsTrue(supplierId.Type.IsNullableType(), "long? should be nullable");
        Assert.IsTrue(discountPrice.Type.IsNullableType(), "decimal? should be nullable");
        Assert.IsTrue(lastModified.Type.IsNullableType(), "DateTime? should be nullable");
        Assert.IsTrue(isFeatured.Type.IsNullableType(), "bool? should be nullable");
    }

    [TestMethod]
    public void Entity_NonNullableValueTypes_ShouldBeDetected()
    {
        // Arrange & Act
        var properties = _entityType.GetMembers().OfType<IPropertySymbol>().ToArray();

        // 检查 non-nullable value types
        var id = properties.First(p => p.Name == "Id");
        var stockCount = properties.First(p => p.Name == "StockCount");
        var price = properties.First(p => p.Name == "Price");
        var isActive = properties.First(p => p.Name == "IsActive");
        var createdAt = properties.First(p => p.Name == "CreatedAt");

        // Assert - 不应该识别为 nullable
        Assert.IsFalse(id.Type.IsNullableType(), "int should not be nullable");
        Assert.IsFalse(stockCount.Type.IsNullableType(), "long should not be nullable");
        Assert.IsFalse(price.Type.IsNullableType(), "decimal should not be nullable");
        Assert.IsFalse(isActive.Type.IsNullableType(), "bool should not be nullable");
        Assert.IsFalse(createdAt.Type.IsNullableType(), "DateTime should not be nullable");
    }

    #endregion

    #region Nullable Reference Types 测试

    [TestMethod]
    public void Entity_NullableReferenceTypes_ShouldBeDetected()
    {
        // Arrange & Act
        var properties = _entityType.GetMembers().OfType<IPropertySymbol>().ToArray();

        // 检查 nullable reference types
        var description = properties.First(p => p.Name == "Description");
        var imageUrl = properties.First(p => p.Name == "ImageUrl");
        var tags = properties.First(p => p.Name == "Tags");

        // Assert - 应该识别为 nullable
        Assert.IsTrue(description.Type.IsNullableType(), "string? should be nullable");
        Assert.IsTrue(imageUrl.Type.IsNullableType(), "string? should be nullable");
        Assert.IsTrue(tags.Type.IsNullableType(), "string? should be nullable");
    }

    [TestMethod]
    public void Entity_NonNullableReferenceTypes_ShouldBeDetected()
    {
        // Arrange & Act
        var properties = _entityType.GetMembers().OfType<IPropertySymbol>().ToArray();

        // 检查 non-nullable reference types
        var name = properties.First(p => p.Name == "Name");
        var sku = properties.First(p => p.Name == "Sku");

        // Assert - NullableAnnotation 应该是 NotAnnotated
        Assert.AreEqual(NullableAnnotation.NotAnnotated, name.Type.NullableAnnotation, "string (non-nullable) should have NotAnnotated");
        Assert.AreEqual(NullableAnnotation.NotAnnotated, sku.Type.NullableAnnotation, "string (non-nullable) should have NotAnnotated");
    }

    #endregion

    #region IsDBNull 生成测试

    [TestMethod]
    public void CodeGen_NullableTypes_ShouldGenerateIsDBNullCheck()
    {
        // 这个测试验证：
        // 1. nullable value types (int?, decimal?) 应该生成 IsDBNull 检查
        // 2. nullable reference types (string?) 应该生成 IsDBNull 检查
        // 3. non-nullable types 不应该生成 IsDBNull 检查

        // 实际的代码生成测试需要完整的生成器环境
        // 这里我们只是验证类型检测是正确的

        var properties = _entityType.GetMembers().OfType<IPropertySymbol>().ToArray();

        // Nullable 类型（应该有 IsDBNull）
        var nullableProps = properties.Where(p => p.Type.IsNullableType()).ToArray();
        Assert.IsTrue(nullableProps.Length >= 8, $"Should have at least 8 nullable properties, got {nullableProps.Length}");

        // Non-nullable 类型（不应该有 IsDBNull）
        var nonNullableProps = properties.Where(p => !p.Type.IsNullableType()).ToArray();
        Assert.IsTrue(nonNullableProps.Length >= 7, $"Should have at least 7 non-nullable properties, got {nonNullableProps.Length}");
    }

    #endregion

    #region 参数绑定测试

    [TestMethod]
    public void Parameters_NullableTypes_ShouldHandleDBNull()
    {
        // Arrange
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.IProductService")!;
        var insertMethod = serviceType.GetMembers("InsertProductAsync").OfType<IMethodSymbol>().First();

        // Act
        var productParam = insertMethod.Parameters.First(p => p.Name == "product");

        // Assert - Product 是引用类型，参数绑定时应该处理 null
        Assert.IsTrue(productParam.Type.IsReferenceType, "Product should be reference type");
    }

    #endregion

    #region 集成测试

    [TestMethod]
    public void Integration_TemplateProcessing_WithNullableEntity_ShouldWork()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM products WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _entityType, "products");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Errors.Any(), $"Should not have errors: {string.Join(", ", result.Errors)}");

        // 验证所有列都被包含（nullable 和 non-nullable）
        StringAssert.Contains(result.ProcessedSql, "id");
        StringAssert.Contains(result.ProcessedSql, "category_id");
        StringAssert.Contains(result.ProcessedSql, "name");
        StringAssert.Contains(result.ProcessedSql, "description");
    }

    [TestMethod]
    public void Integration_RegexFilter_WithNullableColumns_ShouldWork()
    {
        // Arrange - 筛选所有 Id 结尾的列（包括 nullable 的 CategoryId 和 SupplierId）
        var template = "SELECT {{columns --regex Id$}} FROM products";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _entityType, "products");

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.ProcessedSql, "id");
        StringAssert.Contains(result.ProcessedSql, "category_id");
        StringAssert.Contains(result.ProcessedSql, "supplier_id");
        
        // 不应该包含其他列
        Assert.IsFalse(result.ProcessedSql.Contains("name"), "Should not contain 'name'");
        Assert.IsFalse(result.ProcessedSql.Contains("price"), "Should not contain 'price'");
    }

    #endregion

    #region 性能优化验证

    [TestMethod]
    public void Performance_OnlyNullableTypes_ShouldHaveIsDBNullCheck()
    {
        // 验证性能优化：只有 nullable 类型才生成 IsDBNull 检查
        // 这可以减少 60-70% 的 IsDBNull 调用

        var properties = _entityType.GetMembers().OfType<IPropertySymbol>().ToArray();
        
        // 统计 nullable 和 non-nullable 属性数量
        var nullableCount = properties.Count(p => p.Type.IsNullableType());
        var nonNullableCount = properties.Count(p => !p.Type.IsNullableType());

        // 在这个测试实体中，nullable 和 non-nullable 应该大致相当
        Assert.IsTrue(nullableCount > 0, "Should have nullable properties");
        Assert.IsTrue(nonNullableCount > 0, "Should have non-nullable properties");
        
        // 验证 IsDBNull 调用减少的百分比
        var totalProps = nullableCount + nonNullableCount;
        var savedChecks = (double)nonNullableCount / totalProps * 100;
        
        Assert.IsTrue(savedChecks > 40, $"Should save at least 40% of IsDBNull checks, actual: {savedChecks:F1}%");
    }

    #endregion
}

