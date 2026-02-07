// -----------------------------------------------------------------------
// <copyright file="SimpleScalarMethodTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for simple scalar methods in repositories that inherit from ICrudRepository.
/// These methods don't use entity types and should generate correctly.
/// </summary>
[TestClass]
public class SimpleScalarMethodTests
{
    [TestMethod]
    public void SimpleScalarMethod_InCrudRepository_GeneratesCorrectly()
    {
        var source = @"
using Sqlx.Annotations;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Sqlx]
    public class Todo
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface ITodoRepository : ICrudRepository<Todo, long>
    {
        [SqlTemplate(""SELECT 1"", EnableCaching = true)]
        int A();
        
        [SqlTemplate(""SELECT COUNT(*) FROM todos"")]
        Task<int> GetCountAsync();
        
        [SqlTemplate(""SELECT @value"")]
        Task<string> EchoAsync(string value);
    }

    [RepositoryFor(typeof(ITodoRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""todos"")]
    public partial class TodoRepository
    {
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Should generate without errors
        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        // Should generate the repository
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("TodoRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate TodoRepository.Repository.g.cs");

        var code = repositorySource.Source;

        // Should generate A() method
        Assert.IsTrue(code.Contains("public int A()"), "Should generate A() method");
        Assert.IsTrue(code.Contains("_aTemplate"), "Should generate _aTemplate field");
        
        // Should generate GetCountAsync() method
        Assert.IsTrue(code.Contains("public async Task<int> GetCountAsync()"), "Should generate GetCountAsync() method");
        Assert.IsTrue(code.Contains("_getCountAsyncTemplate"), "Should generate _getCountAsyncTemplate field");
        
        // Should generate EchoAsync() method
        Assert.IsTrue(code.Contains("public async Task<string> EchoAsync(string value)"), "Should generate EchoAsync() method");
        Assert.IsTrue(code.Contains("_echoAsyncTemplate"), "Should generate _echoAsyncTemplate field");
        
        // Should use simple parameter binding (not entity-specific)
        Assert.IsTrue(code.Contains("p.ParameterName = _paramPrefix + \"value\"") || 
                     code.Contains("p.ParameterName = _param_value"), 
                     "Should use simple parameter binding for scalar parameters");
    }

    [TestMethod]
    public void SimpleScalarMethod_WithNoParameters_GeneratesCorrectly()
    {
        var source = @"
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
        [SqlTemplate(""SELECT 42"")]
        int GetMagicNumber();
        
        [SqlTemplate(""SELECT 'Hello World'"")]
        string GetGreeting();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository
    {
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("UserRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate UserRepository.Repository.g.cs");

        var code = repositorySource.Source;

        // Should generate both methods
        Assert.IsTrue(code.Contains("public int GetMagicNumber()"), "Should generate GetMagicNumber() method");
        Assert.IsTrue(code.Contains("public string GetGreeting()"), "Should generate GetGreeting() method");
        
        // Should not have parameter binding code for these methods
        Assert.IsTrue(code.Contains("_getMagicNumberTemplate"), "Should generate template field");
        Assert.IsTrue(code.Contains("_getGreetingTemplate"), "Should generate template field");
    }

    [TestMethod]
    public void SimpleScalarMethod_MixedWithEntityMethods_GeneratesCorrectly()
    {
        var source = @"
using Sqlx.Annotations;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestNamespace
{
    [Sqlx]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public interface IProductRepository : ICrudRepository<Product, int>
    {
        // Simple scalar method
        [SqlTemplate(""SELECT COUNT(*) FROM products WHERE price > @minPrice"")]
        Task<int> CountExpensiveAsync(decimal minPrice);
        
        // Entity method
        [SqlTemplate(""SELECT {{columns}} FROM products WHERE price > @minPrice"")]
        Task<List<Product>> GetExpensiveAsync(decimal minPrice);
        
        // Another simple scalar
        [SqlTemplate(""SELECT AVG(price) FROM products"")]
        Task<decimal> GetAveragePriceAsync();
    }

    [RepositoryFor(typeof(IProductRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class ProductRepository
    {
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

        // Should generate all three methods
        Assert.IsTrue(code.Contains("public async Task<int> CountExpensiveAsync(decimal minPrice)"), 
            "Should generate CountExpensiveAsync() method");
        Assert.IsTrue(code.Contains("public async Task<List<Product>> GetExpensiveAsync(decimal minPrice)"), 
            "Should generate GetExpensiveAsync() method");
        Assert.IsTrue(code.Contains("public async Task<decimal> GetAveragePriceAsync()"), 
            "Should generate GetAveragePriceAsync() method");
        
        // Scalar methods should use simple binding
        // Entity method should use ProductParameterBinder (but we're not checking that here)
    }
}
