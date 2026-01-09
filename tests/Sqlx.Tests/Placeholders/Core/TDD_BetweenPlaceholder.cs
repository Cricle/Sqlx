// -----------------------------------------------------------------------
// <copyright file="TDD_BetweenPlaceholder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Sqlx.Generator;

namespace Sqlx.Tests.Placeholders.Core;

/// <summary>
/// TDD tests for {{between}} placeholder - ensures correct parameter handling
/// Bug: {{between @minPrice, @maxPrice}} generates @minValue, @maxValue instead
/// </summary>
[TestClass]
public class TDD_BetweenPlaceholder
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _searchMethod = null!;
    private INamedTypeSymbol _productType = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        // Create compilation with Product entity and search method
        var code = @"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            
            public class Product 
            { 
                public long Id { get; set; } 
                public string Name { get; set; } 
                public decimal Price { get; set; }
                public bool IsDeleted { get; set; }
            }
            
            public interface IRepo 
            {
                Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
                Task<List<Product>> GetByAgeRangeAsync(int minAge, int maxAge);
            }";

        _compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

        _productType = _compilation.GetTypeByMetadataName("Product") as INamedTypeSymbol;
        _searchMethod = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("GetByPriceRangeAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    public void BetweenPlaceholder_WithParameterReferences_ShouldUseCorrectParameterNames()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE price {{between @minPrice, @maxPrice}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("BETWEEN @minPrice AND @maxPrice"), 
            $"应该使用 'BETWEEN @minPrice AND @maxPrice'。实际SQL: {result.ProcessedSql}");
        Assert.IsFalse(result.ProcessedSql.Contains("@minValue"), 
            $"不应该使用默认的 @minValue。实际SQL: {result.ProcessedSql}");
        Assert.IsFalse(result.ProcessedSql.Contains("@maxValue"), 
            $"不应该使用默认的 @maxValue。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void BetweenPlaceholder_WithCommandLineOptions_ShouldUseSpecifiedParameters()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE {{between --column price --min minPrice --max maxPrice}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        // SQLite wraps columns with brackets for safety
        Assert.IsTrue(result.ProcessedSql.Contains("[price] BETWEEN @minPrice AND @maxPrice"), 
            $"应该使用 '[price] BETWEEN @minPrice AND @maxPrice'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void BetweenPlaceholder_PostgreSQL_ShouldUseDollarSign()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE price {{between @minPrice, @maxPrice}}";
        var dialect = Sqlx.Generator.SqlDefine.PostgreSql;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("BETWEEN @minPrice AND @maxPrice"), 
            $"PostgreSQL应该使用 'BETWEEN @minPrice AND @maxPrice'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void BetweenPlaceholder_Oracle_ShouldUseColon()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE price {{between @minPrice, @maxPrice}}";
        var dialect = Sqlx.Generator.SqlDefine.Oracle;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("BETWEEN :minPrice AND :maxPrice"), 
            $"Oracle应该使用 'BETWEEN :minPrice AND :maxPrice'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void BetweenPlaceholder_WithDifferentParameterNames_ShouldWork()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE age {{between @minAge, @maxAge}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("GetByAgeRangeAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("BETWEEN @minAge AND @maxAge"), 
            $"应该使用 'BETWEEN @minAge AND @maxAge'。实际SQL: {result.ProcessedSql}");
    }
}
