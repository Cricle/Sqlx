// -----------------------------------------------------------------------
// <copyright file="TDD_InPlaceholder.cs" company="Cricle">
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
/// TDD tests for {{in}} placeholder - ensures correct parameter handling
/// Bug: {{in @ids}} generates @values instead of @ids
/// </summary>
[TestClass]
public class TDD_InPlaceholder
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
                public bool IsDeleted { get; set; }
            }
            
            public interface IRepo 
            {
                Task<List<Product>> GetByIdsAsync(long[] ids);
                Task<List<Product>> GetByStatusesAsync(string[] statuses);
            }";

        _compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

        _productType = _compilation.GetTypeByMetadataName("Product") as INamedTypeSymbol;
        _searchMethod = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("GetByIdsAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    public void InPlaceholder_WithParameterReference_ShouldUseCorrectParameterName()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE id {{in @ids}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("IN (@ids)"), 
            $"应该使用 'IN (@ids)'。实际SQL: {result.ProcessedSql}");
        Assert.IsFalse(result.ProcessedSql.Contains("@values"), 
            $"不应该使用默认的 @values。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void InPlaceholder_WithCommandLineOptions_ShouldUseSpecifiedParameter()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE {{in --column status --values statuses}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("GetByStatusesAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        // SQLite wraps columns with brackets for safety
        Assert.IsTrue(result.ProcessedSql.Contains("[status] IN (@statuses)"), 
            $"应该使用 '[status] IN (@statuses)'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void NotInPlaceholder_WithParameterReference_ShouldUseCorrectParameterName()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE id {{not_in @ids}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("NOT IN (@ids)"), 
            $"应该使用 'NOT IN (@ids)'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void InPlaceholder_PostgreSQL_ShouldUseDollarSign()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE id {{in @ids}}";
        var dialect = Sqlx.Generator.SqlDefine.PostgreSql;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("IN ($ids)"), 
            $"PostgreSQL应该使用 'IN ($ids)'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void InPlaceholder_Oracle_ShouldUseColon()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE id {{in @ids}}";
        var dialect = Sqlx.Generator.SqlDefine.Oracle;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("IN (:ids)"), 
            $"Oracle应该使用 'IN (:ids)'。实际SQL: {result.ProcessedSql}");
    }
}
