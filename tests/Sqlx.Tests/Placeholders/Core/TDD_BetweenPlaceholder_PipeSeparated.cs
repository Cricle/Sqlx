// Test for pipe-separated between placeholder format
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Sqlx.Generator;

namespace Sqlx.Tests.Placeholders.Core;

[TestClass]
public class TDD_BetweenPlaceholder_PipeSeparated
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _searchMethod = null!;
    private INamedTypeSymbol _productType = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        var code = @"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            
            public class Product 
            { 
                public long Id { get; set; } 
                public string Name { get; set; } 
                public decimal Price { get; set; }
            }
            
            public interface IRepo 
            {
                Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
            }";

        _compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

        _productType = _compilation.GetTypeByMetadataName("Product") as INamedTypeSymbol;
        _searchMethod = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("GetByPriceRangeAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    public void BetweenPlaceholder_PipeSeparatedFormat_ShouldWork()
    {
        // Arrange
        var template = "SELECT * FROM products WHERE price {{between|min=@minPrice|max=@maxPrice}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Console.WriteLine($"SQL: {result.ProcessedSql}");
        Console.WriteLine($"Errors: {string.Join(", ", result.Errors)}");
        Console.WriteLine($"Warnings: {string.Join(", ", result.Warnings)}");
        
        Assert.IsFalse(result.ProcessedSql.Contains("[min=@minPrice]"), 
            $"Placeholder should be processed, not left as [min=@minPrice]. SQL: {result.ProcessedSql}");
        Assert.IsTrue(result.ProcessedSql.Contains("BETWEEN") && result.ProcessedSql.Contains("AND"), 
            $"Should generate BETWEEN...AND syntax. SQL: {result.ProcessedSql}");
    }
}
