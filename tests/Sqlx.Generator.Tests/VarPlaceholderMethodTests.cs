// -----------------------------------------------------------------------
// <copyright file="VarPlaceholderMethodTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for methods using {{var}} placeholders with [SqlxVar] methods.
/// </summary>
[TestClass]
public class VarPlaceholderMethodTests
{
    [TestMethod]
    public void VarPlaceholder_WithSqlxVarMethod_GeneratesCorrectly()
    {
        var source = @"
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    [Sqlx]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IUserRepository : ICrudRepository<User, int>
    {
        [SqlTemplate(""SELECT {{var --name a}}"")]
        int AX(int a, double b, string c);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository
    {
        [SqlxVar(""a"")]
        private string GetA() => ""test_value"";
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Should generate without errors
        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("UserRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate UserRepository.Repository.g.cs");

        var code = repositorySource.Source;

        // Should generate cached _dynamicContext field
        Assert.IsTrue(code.Contains("private global::Sqlx.PlaceholderContext? _dynamicContext;"), 
            "Should generate _dynamicContext cache field");
        
        // Should generate GetDynamicContext() method with caching logic
        Assert.IsTrue(code.Contains("GetDynamicContext()"), "Should generate GetDynamicContext() method");
        Assert.IsTrue(code.Contains("if (_dynamicContext != null) return _dynamicContext;"), 
            "Should have cache check in GetDynamicContext()");
        Assert.IsTrue(code.Contains("_dynamicContext = context;"), 
            "Should cache the context in GetDynamicContext()");
        
        // Should generate GetVarValue() method
        Assert.IsTrue(code.Contains("GetVarValue(object instance, string variableName)"), "Should generate GetVarValue() method");
        
        // Should generate AX() method
        Assert.IsTrue(code.Contains("AX(int a, double b, string c)"), "Should generate AX() method");
        
        // Should use dynamic context for template preparation
        Assert.IsTrue(code.Contains("GetDynamicContext()"), "Should use dynamic context for var placeholder");
        
        // Should have instance field for caching prepared template
        Assert.IsTrue(code.Contains("private global::Sqlx.SqlTemplate? _aXTemplate;"), 
            "Should have instance field _aXTemplate for caching prepared template");
        
        // Should prepare template once per instance
        Assert.IsTrue(code.Contains("if (_aXTemplate == null)"), 
            "Should check if template is already prepared");
        Assert.IsTrue(code.Contains("_aXTemplate = global::Sqlx.SqlTemplate.Prepare"), 
            "Should prepare template with GetDynamicContext()");
        
        // Should render template each time
        Assert.IsTrue(code.Contains("_aXTemplate.Render(null)"), 
            "Should render template to resolve {{var}} placeholders");
    }

    [TestMethod]
    public void VarPlaceholder_MultipleVarMethods_GeneratesCorrectly()
    {
        var source = @"
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    [Sqlx]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IProductRepository : ICrudRepository<Product, int>
    {
        [SqlTemplate(""SELECT * FROM {{var --name tableName}} WHERE id = @id"")]
        Product GetFromDynamicTable(int id);
        
        [SqlTemplate(""SELECT {{var --name columns}} FROM products"")]
        string GetDynamicColumns();
    }

    [RepositoryFor(typeof(IProductRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class ProductRepository
    {
        [SqlxVar(""tableName"")]
        private string GetTableName() => ""products_archive"";
        
        [SqlxVar(""columns"")]
        private string GetColumns() => ""id, name, price"";
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("ProductRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate ProductRepository.Repository.g.cs");

        var code = repositorySource.Source;

        // Debug: print generated code
        System.Console.WriteLine("=== Generated Code ===");
        System.Console.WriteLine(code);
        System.Console.WriteLine("=== End Generated Code ===");

        // Should generate both methods
        Assert.IsTrue(code.Contains("GetFromDynamicTable(int id)"), "Should generate GetFromDynamicTable method");
        Assert.IsTrue(code.Contains("GetDynamicColumns()"), "Should generate GetDynamicColumns method");
        
        // Should have GetVarValue with both variables
        Assert.IsTrue(code.Contains("\"tableName\""), "Should handle tableName variable");
        Assert.IsTrue(code.Contains("\"columns\""), "Should handle columns variable");
    }
}
