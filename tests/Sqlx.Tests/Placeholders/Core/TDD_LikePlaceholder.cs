// -----------------------------------------------------------------------
// <copyright file="TDD_LikePlaceholder.cs" company="Cricle">
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
/// TDD tests for {{like}} placeholder - ensures dialect-specific string concatenation
/// Bug: SQLite doesn't support CONCAT() function, must use || operator
/// </summary>
[TestClass]
public class TDD_LikePlaceholder
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
                Task<List<Product>> SearchAsync(string pattern);
                Task<List<Product>> SearchWithModeAsync(string searchTerm);
                Task<List<Product>> SearchStartsWithAsync(string prefix);
            }";

        _compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

        _productType = _compilation.GetTypeByMetadataName("Product") as INamedTypeSymbol;
        _searchMethod = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("SearchAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    public void LikePlaceholder_WithParameter_SQLite_ShouldNotUseCONCAT()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE name {{like @pattern}} AND is_deleted = {{bool_false}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _searchMethod, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("name LIKE @pattern"), 
            $"应该包含 'name LIKE @pattern'。实际SQL: {result.ProcessedSql}");
        Assert.IsFalse(result.ProcessedSql.Contains("CONCAT"), 
            $"SQLite不应该使用CONCAT函数。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void LikePlaceholder_ContainsMode_SQLite_ShouldUsePipeOperator()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE name {{like --mode contains --column name --pattern searchTerm}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("SearchWithModeAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("'%' || @searchTerm || '%'"), 
            $"SQLite应该使用||操作符。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void LikePlaceholder_ContainsMode_MySQL_ShouldUseCONCAT()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE name {{like --mode contains --column name --pattern searchTerm}}";
        var dialect = Sqlx.Generator.SqlDefine.MySql;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("SearchWithModeAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("CONCAT('%', @searchTerm, '%')"), 
            $"MySQL应该使用CONCAT函数。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void LikePlaceholder_ContainsMode_PostgreSQL_ShouldUsePipeOperator()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE name {{like --mode contains --column name --pattern searchTerm}}";
        var dialect = Sqlx.Generator.SqlDefine.PostgreSql;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("SearchWithModeAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("'%' || @searchTerm || '%'"), 
            $"PostgreSQL应该使用||操作符和@参数前缀 (Npgsql supports @ format)。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void LikePlaceholder_ContainsMode_SqlServer_ShouldUsePlusOperator()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE name {{like --mode contains --column name --pattern searchTerm}}";
        var dialect = Sqlx.Generator.SqlDefine.SqlServer;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("SearchWithModeAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("'%' + @searchTerm + '%'"), 
            $"SQL Server应该使用+操作符。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void LikePlaceholder_StartsWithMode_SQLite_ShouldUsePipeOperator()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE name {{like --mode starts --column name --pattern prefix}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("SearchStartsWithAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("@prefix || '%'"), 
            $"SQLite应该使用||操作符。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void LikePlaceholder_StartsWithMode_MySQL_ShouldUseCONCAT()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} WHERE name {{like --mode starts --column name --pattern prefix}}";
        var dialect = Sqlx.Generator.SqlDefine.MySql;
        var method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("SearchStartsWithAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, method, _productType, "products", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("CONCAT(@prefix, '%')"), 
            $"MySQL应该使用CONCAT函数。实际SQL: {result.ProcessedSql}");
    }
}
