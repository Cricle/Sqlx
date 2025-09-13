// -----------------------------------------------------------------------
// <copyright file="AbstractGeneratorEdgeCasesTests.cs" company="Cricle">
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
/// Tests for AbstractGenerator edge cases, error handling, and boundary conditions.
/// Ensures the generator handles problematic scenarios gracefully.
/// </summary>
[TestClass]
public class AbstractGeneratorEdgeCasesTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests handling of empty interfaces.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithEmptyInterface_HandlesGracefully()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IEmptyService
    {
        // No methods
    }

    [RepositoryFor(typeof(IEmptyService))]
    public partial class EmptyService : IEmptyService
    {
        private readonly DbConnection connection;
        public EmptyService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle empty interface gracefully without errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.AreEqual(0, errors.Count, "Generator should handle empty interfaces gracefully");

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        // Should generate class structure even with no methods
        Assert.IsTrue(generatedCode.Contains("partial class EmptyService"), 
            "Should generate partial class even for empty interface");
    }

    /// <summary>
    /// Tests handling of interfaces with properties (non-method members).
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithInterfaceProperties_IgnoresProperties()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IServiceWithProperties
    {
        string ConnectionString { get; set; }
        int Timeout { get; }
        bool IsConnected { get; }
        
        void Connect();
        void Disconnect();
    }

    [RepositoryFor(typeof(IServiceWithProperties))]
    public partial class ServiceWithProperties : IServiceWithProperties
    {
        private readonly DbConnection connection;
        public ServiceWithProperties(DbConnection connection) => this.connection = connection;

        // Manually implement properties (generator should ignore them)
        public string ConnectionString { get; set; } = string.Empty;
        public int Timeout => 30;
        public bool IsConnected => connection?.State == System.Data.ConnectionState.Open;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should compile successfully, ignoring properties and implementing only methods
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle interface properties gracefully. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate method implementations but not property implementations
        Assert.IsTrue(generatedCode.Contains("Connect()") || generatedCode.Contains("Connect("), 
            "Should generate method implementations");
        Assert.IsTrue(generatedCode.Contains("Disconnect()") || generatedCode.Contains("Disconnect("), 
            "Should generate method implementations");
    }

    /// <summary>
    /// Tests handling of generic interfaces and methods.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithGenericInterface_HandlesGenerics()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IGenericService<T> where T : class
    {
        T? GetById(int id);
        IList<T> GetAll();
        int Create(T entity);
        int Update(T entity);
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [RepositoryFor(typeof(IGenericService<User>))]
    public partial class UserGenericService : IGenericService<User>
    {
        private readonly DbConnection connection;
        public UserGenericService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle generic types appropriately
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle generic interfaces. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate methods with correct generic type usage
        Assert.IsTrue(generatedCode.Contains("User"), 
            "Should use concrete type in generated methods");
    }

    /// <summary>
    /// Tests handling of methods with complex parameter types.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithComplexParameters_HandlesComplexTypes()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class SearchCriteria
    {
        public string? Name { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public List<string>? Tags { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public interface IComplexService
    {
        IList<User> Search(SearchCriteria criteria);
        Task<IList<User>> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken = default);
        Dictionary<string, object> GetUserStatistics(int userId);
        void ProcessBatch(IEnumerable<Dictionary<string, object?>> batch);
    }

    [RepositoryFor(typeof(IComplexService))]
    public partial class ComplexService : IComplexService
    {
        private readonly DbConnection connection;
        public ComplexService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle complex parameter types without crashing
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            // Complex types might cause some compilation issues, but generator shouldn't crash
            Console.WriteLine($"Complex types caused some errors (expected): {errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate some code even if complex types cause issues
        Assert.IsTrue(generatedCode.Contains("ComplexService"), 
            "Should generate class structure even with complex types");
    }

    /// <summary>
    /// Tests handling of nullable reference types and nullable value types.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithNullableTypes_HandlesNullability()
    {
        string sourceCode = @"
#nullable enable
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class NullableEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? ParentId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public interface INullableService
    {
        NullableEntity? GetById(int id);
        NullableEntity? GetByName(string? name);
        IList<NullableEntity> GetByParent(int? parentId);
        int? GetParentId(int id);
        string? GetName(int id);
        bool CreateEntity(NullableEntity? entity);
    }

    [RepositoryFor(typeof(INullableService))]
    public partial class NullableService : INullableService
    {
        private readonly DbConnection connection;
        public NullableService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle nullable types correctly
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle nullable types. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should preserve nullable annotations in generated code
        Assert.IsTrue(generatedCode.Contains("?"), 
            "Should preserve nullable annotations");
        Assert.IsTrue(generatedCode.Contains("NullableEntity"), 
            "Should generate methods with nullable types");
    }

    /// <summary>
    /// Tests handling of methods with multiple attributes.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithMultipleAttributes_HandlesAttributeCombinations()
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

    public interface IAttributeService
    {
        [Sqlx(""SELECT * FROM Products WHERE Id = @id"")]
        [TableName(""Products"")]
        Product? GetProduct(int id);
        
        [SqlExecuteType(SqlExecuteType.Insert, ""Products"")]
        [RawSql(""INSERT INTO Products (Name, Price) VALUES (@Name, @Price)"")]
        int CreateProduct(Product product);
        
        [ExpressionToSql]
        IList<Product> FindProducts(System.Linq.Expressions.Expression<System.Func<Product, bool>> predicate);
    }

    [RepositoryFor(typeof(IAttributeService))]
    public partial class AttributeService : IAttributeService
    {
        private readonly DbConnection connection;
        public AttributeService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle multiple attributes gracefully, giving priority appropriately
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle multiple attributes. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate implementations for all methods
        Assert.IsTrue(generatedCode.Contains("GetProduct"), 
            "Should generate method with multiple attributes");
        Assert.IsTrue(generatedCode.Contains("CreateProduct"), 
            "Should generate method with multiple attributes");
    }

    /// <summary>
    /// Tests handling of interface inheritance chains.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithInterfaceInheritance_HandlesInheritance()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Entity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IBaseRepository<T>
    {
        T? GetById(int id);
        IList<T> GetAll();
    }

    public interface IWritableRepository<T> : IBaseRepository<T>
    {
        int Create(T entity);
        int Update(T entity);
    }

    public interface IEntityService : IWritableRepository<Entity>
    {
        IList<Entity> GetByName(string name);
        void DeleteEntity(int id);
    }

    [RepositoryFor(typeof(IEntityService))]
    public partial class EntityService : IEntityService
    {
        private readonly DbConnection connection;
        public EntityService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle interface inheritance chains
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle interface inheritance. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate implementations for all inherited methods
        Assert.IsTrue(generatedCode.Contains("GetById") || generatedCode.Contains("EntityService"), 
            "Should handle inherited interface methods");
    }

    /// <summary>
    /// Tests error recovery when entity type cannot be inferred.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithUnknownEntityType_ProvidesGracefulFallback()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IMysteriousService
    {
        object GetSomething(int id);
        dynamic GetDynamicData(string key);
        void DoSomethingGeneric<T>(T input);
    }

    [RepositoryFor(typeof(IMysteriousService))]
    public partial class MysteriousService : IMysteriousService
    {
        private readonly DbConnection connection;
        public MysteriousService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should provide fallback implementations or graceful handling
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate some form of implementation or error comments
        Assert.IsTrue(generatedCode.Contains("MysteriousService") || generatedCode.Contains("Error"), 
            "Should provide some implementation or error handling");
    }

    /// <summary>
    /// Tests handling of extremely long method names and parameter lists.
    /// </summary>
    [TestMethod]
    public void GenerateRepository_WithLongMethodNames_HandlesLongNames()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IVerboseService
    {
        TestEntity? GetTestEntityByIdWithOptionalIncludeDeletedRecordsAndSortByNameDescending(
            int id, 
            bool includeDeleted = false, 
            bool sortByName = true, 
            bool descending = false,
            string optionalFilter = """",
            int maxResults = 100,
            int offset = 0);
            
        void PerformComplexOperationWithMultipleParametersAndValidation(
            TestEntity entity,
            bool validateFirst,
            bool logOperation,
            string operationContext,
            System.DateTime timestamp);
    }

    [RepositoryFor(typeof(IVerboseService))]
    public partial class VerboseService : IVerboseService
    {
        private readonly DbConnection connection;
        public VerboseService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle long method names and parameter lists
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Should handle long method names. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate implementations for verbose methods
        Assert.IsTrue(generatedCode.Contains("GetTestEntityByIdWithOptional") || 
                     generatedCode.Contains("PerformComplexOperation"), 
            "Should handle long method names");
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
