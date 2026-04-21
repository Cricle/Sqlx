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
using Sqlx;
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
        Assert.IsTrue(code.Contains("var result = cmd.ExecuteScalar();"),
            "SELECT scalar methods should use ExecuteScalar");
        Assert.IsTrue(code.Contains("global::Sqlx.TypeConverter.Convert<int>(result)"),
            "SELECT scalar methods should convert scalar result through TypeConverter");
        
        // Should generate GetCountAsync() method
        Assert.IsTrue(code.Contains("public async System.Threading.Tasks.Task<int> GetCountAsync()") ||
                     code.Contains("public async Task<int> GetCountAsync()"), 
                     "Should generate GetCountAsync() method");
        Assert.IsTrue(code.Contains("_getCountAsyncTemplate"), "Should generate _getCountAsyncTemplate field");
        Assert.IsTrue(code.Contains("var result = await cmd.ExecuteScalarAsync") || code.Contains("var result = cmd.ExecuteScalar();"),
            "Simple scalar count method should use ExecuteScalar");
        Assert.IsTrue(code.Contains("global::Sqlx.TypeConverter.Convert<int>(result)"),
            "Simple scalar count method should convert scalar result through TypeConverter");
        
        // Should generate EchoAsync() method
        Assert.IsTrue(code.Contains("public async System.Threading.Tasks.Task<string> EchoAsync(string value)") ||
                     code.Contains("public async Task<string> EchoAsync(string value)"), 
                     "Should generate EchoAsync() method");
        Assert.IsTrue(code.Contains("_echoAsyncTemplate"), "Should generate _echoAsyncTemplate field");
        Assert.IsTrue(code.Contains("global::Sqlx.TypeConverter.Convert<string>(result)"),
            "Simple string scalar method should convert scalar result through TypeConverter");
        
        // Should use simple parameter binding (not entity-specific)
        Assert.IsTrue(code.Contains("p.ParameterName = _paramPrefix + \"value\"") || 
                     code.Contains("p.ParameterName = _param_value"), 
                     "Should use simple parameter binding for scalar parameters");
    }

    [TestMethod]
    public void SimpleScalarMethod_WithNoParameters_GeneratesCorrectly()
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
        [SqlTemplate(""SELECT 42"")]
        int GetMagicNumber();
        
        [SqlTemplate(""SELECT 'Hello World'"")]
        string GetGreeting();
    }

    [RepositoryFor(typeof(IUserRepository))]
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
using Sqlx;
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
        Assert.IsTrue(code.Contains("CountExpensiveAsync(decimal minPrice)"), 
            "Should generate CountExpensiveAsync() method");
        Assert.IsTrue(code.Contains("GetExpensiveAsync(decimal minPrice)"), 
            "Should generate GetExpensiveAsync() method");
        Assert.IsTrue(code.Contains("GetAveragePriceAsync()"), 
            "Should generate GetAveragePriceAsync() method");
        
        // Scalar methods should use simple binding
        // Entity method should use ProductParameterBinder (but we're not checking that here)
    }

    [TestMethod]
    public void SimpleScalarMethod_WithUnsignedScalar_InfersDbTypeAndGeneratesCorrectly()
    {
        var source = @"
using Sqlx;
using Sqlx.Annotations;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Sqlx]
    public class CounterEntity
    {
        public int Id { get; set; }
        public uint Count { get; set; }
    }

    public interface ICounterRepository : ICrudRepository<CounterEntity, int>
    {
        [SqlTemplate(""SELECT @minValue"")]
        Task<uint> EchoUnsignedAsync(uint minValue);
    }

    [RepositoryFor(typeof(ICounterRepository))]
    public partial class CounterRepository
    {
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("CounterRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate CounterRepository.Repository.g.cs");

        var code = repositorySource.Source;

        Assert.IsTrue(code.Contains("EchoUnsignedAsync(uint minValue)"), "Should generate unsigned scalar method");
        Assert.IsTrue(code.Contains("p.DbType = System.Data.DbType.UInt32;"), "Should infer DbType.UInt32 for uint parameter");
        Assert.IsTrue(code.Contains("global::Sqlx.TypeConverter.Convert<uint>(result)"),
            "Unsigned scalar method should convert ExecuteScalar result through TypeConverter");
    }

    [TestMethod]
    public void SimpleScalarMethod_WithOutputParameterWrapper_UsesReaderOutputPath()
    {
        var source = @"
using Sqlx;
using Sqlx.Annotations;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Sqlx]
    public class AuditEntity
    {
        public int Id { get; set; }
    }

    public interface IAuditRepository : ICrudRepository<AuditEntity, int>
    {
        [SqlTemplate(""EXEC test_output"")]
        Task<int> RunAsync(OutputParameter<int> outputValue);
    }

    [RepositoryFor(typeof(IAuditRepository))]
    public partial class AuditRepository
    {
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("AuditRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate AuditRepository.Repository.g.cs");

        var code = repositorySource.Source;

        Assert.IsTrue(code.Contains("using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, default).ConfigureAwait(false);") &&
                      code.Contains("outputValue.Value = global::Sqlx.TypeConverter.Convert<int>(reader.GetValue(__outputIndex));") &&
                      code.Contains("outputValue.HasValue = true;"),
            "OutputParameter<T> should be populated from the result reader path.");
    }

    [TestMethod]
    public void SimpleScalarMethod_WithIntDml_ReturnsAffectedRowsViaExecuteNonQuery()
    {
        var source = @"
using Sqlx;
using Sqlx.Annotations;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Sqlx]
    public class CounterEntity
    {
        public int Id { get; set; }
        public uint Count { get; set; }
    }

    public interface ICounterMutationRepository : ICrudRepository<CounterEntity, int>
    {
        [SqlTemplate(""UPDATE counters SET count = @count WHERE id = @id"")]
        Task<int> UpdateCountAsync(int id, uint count);
    }

    [RepositoryFor(typeof(ICounterMutationRepository))]
    public partial class CounterMutationRepository
    {
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("CounterMutationRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate CounterMutationRepository.Repository.g.cs");

        var code = repositorySource.Source;

        Assert.IsTrue(code.Contains("UpdateCountAsync(int id, uint count)"), "Should generate DML int method");
        Assert.IsTrue(code.Contains("await cmd.ExecuteNonQueryAsync"), "DML int methods should use ExecuteNonQueryAsync");
        Assert.IsFalse(code.Contains("global::Sqlx.TypeConverter.Convert<int>(result)"), "DML int methods should not be treated as scalar ExecuteScalar queries");
    }

    [TestMethod]
    public void SimpleScalarMethod_WithNullableReferenceScalar_UsesScalarPathAndNullBinding()
    {
        var source = @"
using Sqlx;
using Sqlx.Annotations;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Sqlx]
    public class MessageEntity
    {
        public int Id { get; set; }
    }

    public interface IMessageRepository : ICrudRepository<MessageEntity, int>
    {
        [SqlTemplate(""SELECT @prefix"")]
        Task<string?> EchoNullableAsync(string? prefix);
    }

    [RepositoryFor(typeof(IMessageRepository))]
    public partial class MessageRepository
    {
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("MessageRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate MessageRepository.Repository.g.cs");

        var code = repositorySource.Source;

        Assert.IsTrue(code.Contains("EchoNullableAsync(string? prefix)"), "Should generate nullable string scalar method");
        Assert.IsTrue(code.Contains("p.DbType = System.Data.DbType.String;"), "Should infer DbType.String for nullable string parameter");
        Assert.IsTrue(code.Contains("p.Value = prefix ?? (object)DBNull.Value;"), "Nullable string parameter should bind DBNull when null");
        Assert.IsTrue(code.Contains("await cmd.ExecuteScalarAsync"), "Nullable string scalar method should use ExecuteScalarAsync");
        Assert.IsTrue(code.Contains("global::Sqlx.TypeConverter.Convert<string?>(result)"), "Nullable string scalar method should convert scalar result through TypeConverter");
    }
}
