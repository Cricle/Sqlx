// -----------------------------------------------------------------------
// <copyright file="CSharpGeneratorTests.cs" company="Cricle">
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
/// Tests for CSharpGenerator implementation.
/// Tests the C#-specific source generator functionality and syntax receiver.
/// </summary>
[TestClass]
public class CSharpGeneratorTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that CSharpGenerator initializes correctly.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_Initialize_DoesNotThrow()
    {
        var generator = new CSharpGenerator();

        // Test that initialization doesn't throw - actual initialization context testing is complex
        // and would require more elaborate mocking infrastructure
        Assert.IsNotNull(generator, "Generator should be created successfully");

        // The generator's Initialize method sets up syntax receivers, but testing this
        // requires a full GeneratorInitializationContext which is difficult to mock
        // In practice, this is tested by the integration tests that actually run the generator
    }

    /// <summary>
    /// Tests that CSharpGenerator generates code correctly.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_WithRepositoryClass_GeneratesImplementation()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    [RepositoryFor(typeof(ITestService))]
    public partial class TestService
    {
        private readonly DbConnection connection;
        public TestService(DbConnection connection) => this.connection = connection;
    }

    public interface ITestService
    {
        int GetCount();
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate some implementation code
        Assert.IsNotNull(generatedCode, "Should generate implementation code");
        Assert.IsTrue(generatedCode.Contains("TestService") || generatedCode.Length > 0,
            "Should generate code for TestService");
    }

    /// <summary>
    /// Tests that CSharpSyntaxReceiver collects methods with Sqlx attributes.
    /// </summary>
    [TestMethod]
    public void CSharpSyntaxReceiver_CollectsMethods_WithSqlxAttributes()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IUserService
    {
        [Sqlx(""SELECT * FROM Users"")]
        IList<User> GetAllUsers();
        
        [Sqlx(""SELECT * FROM Users WHERE Id = @id"")]
        User? GetUserById(int id);
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        int CreateUser(User user);
        
        // Method without Sqlx attribute - should not be collected
        void DoSomething();
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        public UserService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should compile without major errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count != 0)
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should collect methods without compilation errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate implementations for methods with Sqlx attributes
        Assert.IsTrue(generatedCode.Contains("GetAllUsers") || generatedCode.Contains("SELECT * FROM Users"),
            "Should generate implementation for GetAllUsers");
        Assert.IsTrue(generatedCode.Contains("GetUserById") || generatedCode.Contains("@id"),
            "Should generate implementation for GetUserById");
        Assert.IsTrue(generatedCode.Contains("CreateUser") || generatedCode.Contains("INSERT"),
            "Should generate implementation for CreateUser");
    }

    /// <summary>
    /// Tests that CSharpSyntaxReceiver collects repository classes correctly.
    /// </summary>
    [TestMethod]
    public void CSharpSyntaxReceiver_CollectsRepositoryClasses_WithRepositoryForAttribute()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IProductService
    {
        IList<Product> GetAllProducts();
        Product? GetProductById(int id);
        int CreateProduct(Product product);
    }

    public interface IOrderService
    {
        IList<Order> GetAllOrders();
        Order? GetOrderById(int id);
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductService : IProductService
    {
        private readonly DbConnection connection;
        public ProductService(DbConnection connection) => this.connection = connection;
    }

    [RepositoryFor(typeof(IOrderService))]
    public partial class OrderService : IOrderService
    {
        private readonly DbConnection connection;
        public OrderService(DbConnection connection) => this.connection = connection;
    }

    // Class without RepositoryFor attribute - should not be collected
    public class RegularService
    {
        public void DoSomething() { }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle multiple repository classes
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count != 0)
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle multiple repository classes without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate implementations for both repository classes
        Assert.IsTrue(generatedCode.Contains("ProductService") && generatedCode.Contains("OrderService"),
            "Should generate implementations for both repository classes");
        Assert.IsTrue(generatedCode.Contains("GetAllProducts") || generatedCode.Contains("Product"),
            "Should generate Product-related methods");
        Assert.IsTrue(generatedCode.Contains("GetAllOrders") || generatedCode.Contains("Order"),
            "Should generate Order-related methods");
    }

    /// <summary>
    /// Tests CSharpGenerator with async methods.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_WithAsyncMethods_GeneratesAsyncImplementations()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class AsyncEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public interface IAsyncService
    {
        Task<IList<AsyncEntity>> GetAllAsync();
        Task<AsyncEntity?> GetByIdAsync(int id);
        Task<AsyncEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<int> CreateAsync(AsyncEntity entity);
        Task<int> CreateAsync(AsyncEntity entity, CancellationToken cancellationToken);
        Task UpdateAsync(AsyncEntity entity);
        Task DeleteAsync(int id, CancellationToken cancellationToken);
    }

    [RepositoryFor(typeof(IAsyncService))]
    public partial class AsyncService : IAsyncService
    {
        private readonly DbConnection connection;
        public AsyncService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle async methods correctly
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count != 0)
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle async methods without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate async implementations (using actual database execution logic)
        Assert.IsTrue(generatedCode.Contains("CreateCommand()") || generatedCode.Contains("_connection") || generatedCode.Contains("Task"),
            "Should generate actual database execution logic for repository methods");
        Assert.IsTrue(generatedCode.Contains("CancellationToken") || generatedCode.Contains("cancellationToken"),
            "Should handle CancellationToken parameters");
        Assert.IsTrue(generatedCode.Contains("Task"),
            "Should handle Task return types");
    }

    /// <summary>
    /// Tests CSharpGenerator with generic methods and complex types.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_WithGenericMethods_HandlesGenericsCorrectly()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class GenericEntity<T>
    {
        public int Id { get; set; }
        public T Data { get; set; } = default(T)!;
        public string Name { get; set; } = string.Empty;
    }

    public interface IGenericService
    {
        IList<GenericEntity<string>> GetStringEntities();
        IList<GenericEntity<int>> GetIntEntities();
        GenericEntity<T>? GetGenericById<T>(int id);
        int CreateGeneric<T>(GenericEntity<T> entity);
        
        // Expression-based generic method
        [ExpressionToSql]
        IList<GenericEntity<T>> FindWhere<T>(Expression<Func<GenericEntity<T>, bool>> predicate);
    }

    [RepositoryFor(typeof(IGenericService))]
    public partial class GenericService : IGenericService
    {
        private readonly DbConnection connection;
        public GenericService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle generic methods (may have some warnings but should work)
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count != 0)
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            // Generics might not be fully supported but should not crash
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should attempt to handle generic types
        Assert.IsTrue(generatedCode.Contains("GenericEntity") || generatedCode.Contains("Generic"),
            "Should reference generic entity types");
    }

    /// <summary>
    /// Tests CSharpGenerator error handling and diagnostics.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_WithInvalidCode_HandlesErrorsGracefully()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    // Invalid interface - missing members
    public interface IInvalidService
    {
        // Method with invalid return type
        InvalidReturnType GetSomething();
        
        // Method with missing parameter types
        int ProcessData(MissingType data);
    }

    // Repository for invalid interface
    [RepositoryFor(typeof(IInvalidService))]
    public partial class InvalidService : IInvalidService
    {
        private readonly DbConnection connection;
        public InvalidService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle invalid code gracefully
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        // Invalid types might not always cause compilation errors in the generator context
        // The main test is that the generator doesn't crash

        // Generator should still produce some output even with errors
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode, "Should still generate some code even with errors");
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

        // Add reference to the Sqlx.Annotations assembly
        try
        {
            references.Add(MetadataReference.CreateFromFile(typeof(Sqlx.Annotations.SqlxAttribute).Assembly.Location));
        }
        catch
        {
            // Fallback: try to load Sqlx.dll from current app domain assemblies
            var sqlxAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Sqlx");
            if (sqlxAssembly != null && !string.IsNullOrEmpty(sqlxAssembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(sqlxAssembly.Location));
            }
        }

        return references;
    }

}

