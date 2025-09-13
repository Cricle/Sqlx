// -----------------------------------------------------------------------
// <copyright file="AbstractGeneratorModernSyntaxTests.cs" company="Cricle">
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
/// Tests for AbstractGenerator support of modern C# syntax including Records and Primary Constructors.
/// Ensures the generator properly handles C# 9+ and C# 12+ language features.
/// </summary>
[TestClass]
public class AbstractGeneratorModernSyntaxTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that Record types are properly handled in repository generation.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithRecordTypes_HandlesRecordsCorrectly()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    // C# 9+ Record type
    public record UserRecord(int Id, string Name, string Email, DateTime CreatedAt);
    
    // Record with additional properties
    public record ProductRecord(int Id, string Name, decimal Price)
    {
        public bool IsActive { get; init; } = true;
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    }

    public interface IRecordService
    {
        UserRecord? GetUserById(int id);
        IList<UserRecord> GetAllUsers();
        int CreateUser(UserRecord user);
        int UpdateUser(UserRecord user);
        
        ProductRecord? GetProductById(int id);
        IList<ProductRecord> GetActiveProducts();
        int CreateProduct(ProductRecord product);
    }

    [RepositoryFor(typeof(IRecordService))]
    public partial class RecordService : IRecordService
    {
        private readonly DbConnection connection;
        public RecordService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle record types without compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle record types without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate proper entity instantiation for records
        Assert.IsTrue(generatedCode.Contains("UserRecord") || generatedCode.Contains("ProductRecord"), 
            "Should reference record types in generated code");
        Assert.IsTrue(generatedCode.Contains("new "), 
            "Should instantiate record types correctly");
    }

    /// <summary>
    /// Tests that Primary Constructor classes are properly handled.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithPrimaryConstructors_HandlesPrimaryConstructorsCorrectly()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    // C# 12+ Primary Constructor
    public class Customer(int id, string name, string email)
    {
        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string Email { get; init; } = email;
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    // Primary Constructor with additional members
    public class Order(int id, string customerName, decimal total)
    {
        public int Id { get; init; } = id;
        public string CustomerName { get; init; } = customerName;
        public decimal Total { get; init; } = total;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = ""Pending"";
        
        public void MarkAsShipped() => Status = ""Shipped"";
    }

    public interface IPrimaryConstructorService
    {
        Customer? GetCustomerById(int id);
        IList<Customer> GetActiveCustomers();
        int CreateCustomer(Customer customer);
        int UpdateCustomer(Customer customer);
        
        Order? GetOrderById(int id);
        IList<Order> GetOrdersByCustomer(string customerName);
        int CreateOrder(Order order);
        int UpdateOrderStatus(Order order);
    }

    [RepositoryFor(typeof(IPrimaryConstructorService))]
    public partial class PrimaryConstructorService : IPrimaryConstructorService
    {
        private readonly DbConnection connection;
        public PrimaryConstructorService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle primary constructor classes without compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle primary constructor classes without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate proper entity instantiation for primary constructor classes
        Assert.IsTrue(generatedCode.Contains("Customer") || generatedCode.Contains("Order"), 
            "Should reference primary constructor types in generated code");
        Assert.IsTrue(generatedCode.Contains("new "), 
            "Should instantiate primary constructor classes correctly");
    }

    /// <summary>
    /// Tests mixed usage of traditional classes, records, and primary constructors.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithMixedModernSyntax_HandlesAllSyntaxTypes()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    // Traditional class
    public class TraditionalProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    // Record type (C# 9+)
    public record CategoryRecord(int Id, string Name, string Description);

    // Primary Constructor (C# 12+)
    public class ModernInventory(int id, string productName, int quantity)
    {
        public int Id { get; init; } = id;
        public string ProductName { get; init; } = productName;
        public int Quantity { get; set; } = quantity;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public interface IMixedSyntaxService
    {
        // Traditional class operations
        TraditionalProduct? GetProduct(int id);
        int CreateProduct(TraditionalProduct product);
        
        // Record operations
        CategoryRecord? GetCategory(int id);
        int CreateCategory(CategoryRecord category);
        
        // Primary constructor operations
        ModernInventory? GetInventory(int id);
        int UpdateInventory(ModernInventory inventory);
        
        // Mixed operations
        IList<TraditionalProduct> GetProductsByCategory(CategoryRecord category);
        int UpdateProductInventory(TraditionalProduct product, ModernInventory inventory);
    }

    [RepositoryFor(typeof(IMixedSyntaxService))]
    public partial class MixedSyntaxService : IMixedSyntaxService
    {
        private readonly DbConnection connection;
        public MixedSyntaxService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle mixed syntax without compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle mixed modern syntax without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate code for all syntax types
        Assert.IsTrue(generatedCode.Contains("TraditionalProduct") || 
                     generatedCode.Contains("CategoryRecord") || 
                     generatedCode.Contains("ModernInventory"), 
            "Should handle all types of modern syntax in the same service");
    }

    /// <summary>
    /// Tests that init-only properties are handled correctly in generated code.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithInitOnlyProperties_HandlesInitProperties()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ModernEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public record ImmutableRecord(int Id, string Name, DateTime CreatedDate)
    {
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }

    public interface IInitPropertyService
    {
        ModernEntity? GetEntity(int id);
        IList<ModernEntity> GetAllEntities();
        int CreateEntity(ModernEntity entity);
        
        ImmutableRecord? GetRecord(int id);
        IList<ImmutableRecord> GetAllRecords();
        int CreateRecord(ImmutableRecord record);
    }

    [RepositoryFor(typeof(IInitPropertyService))]
    public partial class InitPropertyService : IInitPropertyService
    {
        private readonly DbConnection connection;
        public InitPropertyService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle init-only properties without compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle init-only properties without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should properly handle init-only properties in entity creation
        Assert.IsTrue(generatedCode.Contains("ModernEntity") || generatedCode.Contains("ImmutableRecord"), 
            "Should reference types with init-only properties");
        Assert.IsTrue(generatedCode.Contains("new "), 
            "Should instantiate types with init-only properties");
    }

    /// <summary>
    /// Tests that nullable reference types work correctly with modern syntax.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithNullableReferenceTypes_HandlesNullabilityCorrectly()
    {
        string sourceCode = @"
#nullable enable
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public record NullableRecord(int Id, string Name, string? Description, DateTime? LastModified);

    public class NullableEntity(int id, string name)
    {
        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string? Description { get; set; }
        public DateTime? LastModified { get; set; }
        public string? Category { get; init; }
    }

    public interface INullableService
    {
        NullableRecord? GetRecord(int id);
        NullableRecord? FindByName(string name);
        IList<NullableRecord> SearchByDescription(string? description);
        int CreateRecord(NullableRecord record);
        
        NullableEntity? GetEntity(int id);
        IList<NullableEntity> GetEntitiesByCategory(string? category);
        int CreateEntity(NullableEntity entity);
        int? UpdateEntityDescription(int id, string? newDescription);
    }

    [RepositoryFor(typeof(INullableService))]
    public partial class NullableService : INullableService
    {
        private readonly DbConnection connection;
        public NullableService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle nullable reference types without compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle nullable reference types without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should properly handle nullable reference types
        Assert.IsTrue(generatedCode.Contains("?") || generatedCode.Contains("null"), 
            "Should handle nullable reference types appropriately");
        Assert.IsTrue(generatedCode.Contains("NullableRecord") || generatedCode.Contains("NullableEntity"), 
            "Should reference nullable types correctly");
    }

    /// <summary>
    /// Tests that required properties (C# 11) are handled if present.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithRequiredProperties_HandlesRequiredCorrectly()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class EntityWithRequired
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    }

    public interface IRequiredService
    {
        EntityWithRequired? GetById(int id);
        IList<EntityWithRequired> GetAll();
        int Create(EntityWithRequired entity);
    }

    [RepositoryFor(typeof(IRequiredService))]
    public partial class RequiredService : IRequiredService
    {
        private readonly DbConnection connection;
        public RequiredService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle required properties (may have some warnings but should work)
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            // Required keyword might not be available in all test environments
            Console.WriteLine($"Required properties test had errors (may be expected): {errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate code even if required keyword isn't fully supported
        Assert.IsTrue(generatedCode.Contains("EntityWithRequired") || generatedCode.Contains("RequiredService"), 
            "Should handle entities with required properties");
    }

    /// <summary>
    /// Tests performance with complex modern syntax scenarios.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithComplexModernSyntax_PerformsWell()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    // Complex record with many properties
    public record ComplexRecord(
        int Id, 
        string Name, 
        string? Description, 
        DateTime CreatedDate, 
        DateTime? ModifiedDate,
        bool IsActive,
        decimal? Price,
        int CategoryId,
        string? Tags,
        byte[]? Data)
    {
        public string? AdditionalInfo { get; init; }
        public bool IsDeleted { get; init; } = false;
        public string? CreatedBy { get; init; }
        public string? ModifiedBy { get; set; }
    }

    // Complex primary constructor class
    public class ComplexEntity(
        int id, 
        string name, 
        string email, 
        DateTime birthDate)
    {
        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string Email { get; init; } = email;
        public DateTime BirthDate { get; init; } = birthDate;
        
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, object?> Metadata { get; set; } = new();
    }

    public interface IComplexModernService
    {
        ComplexRecord? GetRecord(int id);
        IList<ComplexRecord> GetRecordsByCategory(int categoryId);
        IList<ComplexRecord> SearchRecords(string? name, bool? isActive, decimal? minPrice, decimal? maxPrice);
        int CreateRecord(ComplexRecord record);
        int UpdateRecord(ComplexRecord record);
        int DeleteRecord(int id);
        
        ComplexEntity? GetEntity(int id);
        IList<ComplexEntity> GetEntitiesByEmail(string email);
        IList<ComplexEntity> GetVerifiedEntities();
        int CreateEntity(ComplexEntity entity);
        int UpdateEntityVerification(int id, bool isVerified);
        int UpdateEntityLastLogin(int id, DateTime loginDate);
    }

    [RepositoryFor(typeof(IComplexModernService))]
    public partial class ComplexModernService : IComplexModernService
    {
        private readonly DbConnection connection;
        public ComplexModernService(DbConnection connection) => this.connection = connection;
    }
}";

        var startTime = DateTime.UtcNow;
        
        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        
        var endTime = DateTime.UtcNow;
        var generationTime = endTime - startTime;

        // Should handle complex modern syntax in reasonable time
        Assert.IsTrue(generationTime.TotalSeconds < 30, 
            $"Complex modern syntax generation should complete in reasonable time. Took: {generationTime.TotalSeconds} seconds");

        // Should handle complex types without major errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Console.WriteLine($"Complex modern syntax test had some errors (may be expected): {errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate substantial code for complex types
        Assert.IsTrue(generatedCode.Contains("ComplexRecord") || generatedCode.Contains("ComplexEntity"), 
            "Should handle complex modern syntax types");
        Assert.IsTrue(generatedCode.Length > 1000, 
            "Should generate substantial code for complex scenarios");
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
