// -----------------------------------------------------------------------
// <copyright file="AbstractGeneratorCoreMethodsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for AbstractGenerator core method generation and execution logic.
/// Focuses on testing the main code generation pipelines and edge cases.
/// </summary>
[TestClass]
public class AbstractGeneratorCoreMethodsTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that Execute method handles null syntax receiver gracefully.
    /// </summary>
    [TestMethod]
    public void Execute_WithNullSyntaxReceiver_ExitsGracefully()
    {
        string sourceCode = @"
using System.Data.Common;

namespace TestNamespace
{
    public class TestClass
    {
        public void RegularMethod() { }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should not have any compilation errors - generator should exit gracefully with null receiver
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.AreEqual(0, errors.Count, "Generator should handle null syntax receiver gracefully");
    }

    /// <summary>
    /// Tests that method parameter generation includes default values correctly.
    /// </summary>
    [TestMethod]
    public void GenerateRepositoryMethod_WithDefaultParameters_IncludesDefaults()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        User? GetUser(int id, string status = ""active"", bool includeDeleted = false);
        Task<User?> GetUserAsync(int id, CancellationToken cancellationToken = default);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        public UserService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code should compile without errors. Errors:\n{errorMessages}");
        }

        // Get generated code and verify default parameters are included
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Check that default values are preserved in generated methods
        Assert.IsTrue(generatedCode.Contains("string status = \"active\""), 
            "Generated method should preserve string default parameter value");
        Assert.IsTrue(generatedCode.Contains("bool includeDeleted = false"), 
            "Generated method should preserve boolean default parameter value");
        Assert.IsTrue(generatedCode.Contains("CancellationToken cancellationToken = default"), 
            "Generated method should include default for CancellationToken");
    }

    /// <summary>
    /// Tests XML documentation generation for repository methods.
    /// </summary>
    [TestMethod]
    public void GenerateRepositoryMethod_GeneratesXmlDocumentation()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public interface IProductService
    {
        Product? GetById(int id);
        IList<Product> GetByPriceRange(decimal minPrice, decimal maxPrice);
        int Create(Product product);
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductService : IProductService
    {
        private readonly DbConnection connection;
        public ProductService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify XML documentation is generated
        Assert.IsTrue(generatedCode.Contains("/// <summary>"), 
            "Generated code should contain XML documentation summaries");
        Assert.IsTrue(generatedCode.Contains("/// Generated implementation of"), 
            "Generated code should contain method implementation documentation");
        Assert.IsTrue(generatedCode.Contains("/// <param name="), 
            "Generated code should contain parameter documentation");
        Assert.IsTrue(generatedCode.Contains("/// <returns>"), 
            "Generated code should contain return value documentation");
        Assert.IsTrue(generatedCode.Contains("/// This method was automatically generated"), 
            "Generated code should indicate it was auto-generated");
    }

    /// <summary>
    /// Tests method generation error handling and fallback generation.
    /// </summary>
    [TestMethod]
    public void GenerateRepositoryMethod_WithInvalidReturnType_GeneratesFallback()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    // Intentionally problematic interface to test error handling
    public interface IProblematicService
    {
        // Method with complex generic return type that might cause issues
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object?>> GetComplexData();
    }

    [RepositoryFor(typeof(IProblematicService))]
    public partial class ProblematicService : IProblematicService
    {
        private readonly DbConnection connection;
        public ProblematicService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // The generator should handle complex types gracefully, either by generating valid code or fallbacks
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should either have valid implementation or fallback error comment
        Assert.IsTrue(
            generatedCode.Contains("GetComplexData") || generatedCode.Contains("Error generating method"),
            "Generator should either implement method or provide error fallback");
    }

    /// <summary>
    /// Tests SqlExecuteType attribute parsing and operation routing.
    /// </summary>
    [TestMethod]
    public void GenerateOptimizedRepositoryMethodBody_WithSqlExecuteTypeAttribute_RoutesCorrectly()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public interface IOrderService
    {
        [SqlExecuteType(SqlExecuteType.Insert, ""Orders"")]
        int CreateOrder(Order order);
        
        [SqlExecuteType(SqlExecuteType.Update, ""Orders"")]
        int UpdateOrder(Order order);
        
        [SqlExecuteType(SqlExecuteType.Delete, ""Orders"")]
        int DeleteOrder(int id);
        
        [SqlExecuteType(SqlExecuteType.Select, ""Orders"")]
        IList<Order> GetAllOrders();
    }

    [RepositoryFor(typeof(IOrderService))]
    public partial class OrderService : IOrderService
    {
        private readonly DbConnection connection;
        public OrderService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify different operations are generated
        Assert.IsTrue(generatedCode.Contains("INSERT INTO"), 
            "Generated code should contain INSERT statement for Insert operation");
        Assert.IsTrue(generatedCode.Contains("UPDATE"), 
            "Generated code should contain UPDATE statement for Update operation");
        Assert.IsTrue(generatedCode.Contains("DELETE FROM"), 
            "Generated code should contain DELETE statement for Delete operation");
        Assert.IsTrue(generatedCode.Contains("SELECT"), 
            "Generated code should contain SELECT statement for Select operation");
    }

    /// <summary>
    /// Tests batch operation generation with different types.
    /// </summary>
    [TestMethod]
    public void GenerateOptimizedRepositoryMethodBody_WithBatchOperations_GeneratesBatchCode()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public interface IItemService
    {
        [SqlExecuteType(SqlExecuteType.BatchInsert, ""Items"")]
        int CreateItems(IList<Item> items);
        
        [SqlExecuteType(SqlExecuteType.BatchUpdate, ""Items"")]
        int UpdateItems(IList<Item> items);
        
        [SqlExecuteType(SqlExecuteType.BatchDelete, ""Items"")]
        int DeleteItems(IList<Item> items);
    }

    [RepositoryFor(typeof(IItemService))]
    public partial class ItemService : IItemService
    {
        private readonly DbConnection connection;
        public ItemService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Batch operations should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify batch operation patterns
        Assert.IsTrue(generatedCode.Contains("DbBatch") || generatedCode.Contains("batch"), 
            "Generated code should use batch operations for batch methods");
        Assert.IsTrue(generatedCode.Contains("foreach") || generatedCode.Contains("Count"), 
            "Generated code should iterate over collection items");
    }

    /// <summary>
    /// Tests async method generation patterns.
    /// </summary>
    [TestMethod]
    public void GenerateRepositoryMethod_WithAsyncMethods_GeneratesAsyncPatterns()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public interface ICustomerService
    {
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<IList<Customer>> GetAllAsync();
        Task<int> CreateAsync(Customer customer);
        Task<int> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    }

    [RepositoryFor(typeof(ICustomerService))]
    public partial class CustomerService : ICustomerService
    {
        private readonly DbConnection connection;
        public CustomerService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Async methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify async patterns
        Assert.IsTrue(generatedCode.Contains("async Task"), 
            "Generated code should contain async Task methods");
        Assert.IsTrue(generatedCode.Contains("await "), 
            "Generated code should use await for async operations");
        Assert.IsTrue(generatedCode.Contains("Async("), 
            "Generated code should call async database methods");
        Assert.IsTrue(generatedCode.Contains("CancellationToken"), 
            "Generated code should handle cancellation tokens");
    }

    /// <summary>
    /// Tests scalar return type detection and handling.
    /// </summary>
    [TestMethod]
    public void GenerateRepositoryMethod_WithScalarReturnTypes_GeneratesScalarOperations()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IStatisticsService
    {
        int GetUserCount();
        long GetTotalRevenue();
        decimal GetAverageOrderValue();
        bool UserExists(int id);
        string GetUserName(int id);
        DateTime? GetLastLoginDate(int userId);
        Task<int> GetActiveUsersCountAsync();
        Task<bool> IsUserActiveAsync(int id);
    }

    [RepositoryFor(typeof(IStatisticsService))]
    public partial class StatisticsService : IStatisticsService
    {
        private readonly DbConnection connection;
        public StatisticsService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Scalar methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify scalar operation patterns
        Assert.IsTrue(generatedCode.Contains("ExecuteScalar"), 
            "Generated code should use ExecuteScalar for scalar operations");
        Assert.IsTrue(generatedCode.Contains("Convert.") || generatedCode.Contains("(int)") || generatedCode.Contains("(long)"), 
            "Generated code should handle type conversions for scalar results");
    }

    /// <summary>
    /// Tests void method generation and handling.
    /// </summary>
    [TestMethod]
    public void GenerateRepositoryMethod_WithVoidMethods_GeneratesVoidOperations()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public interface IAuditService
    {
        void LogAction(string action);
        void ProcessAuditLog(AuditLog log);
        Task LogActionAsync(string action);
        Task ProcessAuditLogAsync(AuditLog log);
    }

    [RepositoryFor(typeof(IAuditService))]
    public partial class AuditService : IAuditService
    {
        private readonly DbConnection connection;
        public AuditService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Void methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify void operation patterns
        Assert.IsTrue(generatedCode.Contains("public void ") || generatedCode.Contains("public async Task "), 
            "Generated code should contain void or Task void methods");
        // Void methods should not have return statements for values
        var voidMethodLines = generatedCode.Split('\n')
            .Where(line => line.Contains("public void") || (line.Contains("public async Task") && !line.Contains("Task<")))
            .ToList();
        Assert.IsTrue(voidMethodLines.Any(), "Should have void methods generated");
    }

    /// <summary>
    /// Helper method to compile source code with the Sqlx generator.
    /// </summary>
    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) CompileWithSourceGenerator(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetBasicReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        return (newCompilation, diagnostics);
    }

    /// <summary>
    /// Gets basic references needed for compilation.
    /// </summary>
    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location));

        var runtimeAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        // Add reference to the Sqlx assembly
        references.Add(MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location));

        return references;
    }
}
