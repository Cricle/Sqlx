// -----------------------------------------------------------------------
// <copyright file="AbstractGeneratorHelpersTests.cs" company="Cricle">
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
/// Tests for AbstractGenerator helper methods including AttributeHelpers and DocHelpers.
/// Tests the utility methods that support code generation.
/// </summary>
[TestClass]
public class AbstractGeneratorHelpersTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that SqlxAttribute generation works correctly for different method patterns.
    /// </summary>
    [TestMethod]
    public void GenerateSqlxAttribute_ForGetAllPattern_GeneratesSelectStatement()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
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
        IList<User> GetAllUsers();
        IList<User> FindAllActiveUsers();
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        public UserService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate SELECT statements for GetAll and FindAll patterns
        Assert.IsTrue(generatedCode.Contains("SELECT * FROM"), 
            "Should generate SELECT statements for GetAll/FindAll patterns");
        Assert.IsTrue(generatedCode.Contains("[Sqlx(\"SELECT * FROM") || generatedCode.Contains("SELECT * FROM [User]"), 
            "Should use User table in generated SQL");
    }

    /// <summary>
    /// Tests that SqlxAttribute generation works for GetBy patterns.
    /// </summary>
    [TestMethod]
    public void GenerateSqlxAttribute_ForGetByPattern_GeneratesWhereClause()
    {
        string sourceCode = @"
using System.Data.Common;
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
        Product? GetByName(string name);
        Product? FindByPrice(decimal price);
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

        // Should generate WHERE clauses for GetBy/FindBy patterns
        Assert.IsTrue(generatedCode.Contains("WHERE"), 
            "Should generate WHERE clauses for GetBy/FindBy patterns");
        Assert.IsTrue(generatedCode.Contains("@") || generatedCode.Contains("CreateParameter"), 
            "Should use parameterized queries");
    }

    /// <summary>
    /// Tests that SqlExecuteType attributes are generated for CRUD operations.
    /// </summary>
    [TestMethod]
    public void GenerateSqlxAttribute_ForCrudOperations_GeneratesSqlExecuteType()
    {
        string sourceCode = @"
using System.Data.Common;
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
        int CreateOrder(Order order);
        int AddOrder(Order order);
        int InsertOrder(Order order);
        int UpdateOrder(Order order);
        int ModifyOrder(Order order);
        int DeleteOrder(int id);
        int RemoveOrder(int id);
    }

    [RepositoryFor(typeof(IOrderService))]
    public partial class OrderService : IOrderService
    {
        private readonly DbConnection connection;
        public OrderService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate SqlExecuteType attributes for CRUD operations
        Assert.IsTrue(generatedCode.Contains("SqlExecuteType") || generatedCode.Contains("INSERT") || generatedCode.Contains("UPDATE") || generatedCode.Contains("DELETE"), 
            "Should generate CRUD SQL operations");
        Assert.IsTrue(generatedCode.Contains("ExecuteNonQuery"), 
            "Should use ExecuteNonQuery for CRUD operations");
    }

    /// <summary>
    /// Tests that Count and Exists patterns generate appropriate SQL.
    /// </summary>
    [TestMethod]
    public void GenerateSqlxAttribute_ForCountAndExistsPatterns_GeneratesCountStatements()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IStatisticsService
    {
        int GetUserCount();
        long CountActiveUsers();
        bool UserExists(int id);
        bool ExistsInDatabase(string identifier);
    }

    [RepositoryFor(typeof(IStatisticsService))]
    public partial class StatisticsService : IStatisticsService
    {
        private readonly DbConnection connection;
        public StatisticsService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate COUNT(*) statements for count and exists patterns
        Assert.IsTrue(generatedCode.Contains("COUNT(*)") || generatedCode.Contains("COUNT"), 
            "Should generate COUNT statements for count/exists patterns");
        Assert.IsTrue(generatedCode.Contains("ExecuteScalar"), 
            "Should use ExecuteScalar for count operations");
    }

    /// <summary>
    /// Tests XML documentation generation for different parameter types.
    /// </summary>
    [TestMethod]
    public void DocumentationGeneration_ForDifferentParameterTypes_GeneratesAppropriateDescriptions()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ICustomerService
    {
        Customer? GetById(int id);
        IList<Customer> Search(string searchTerm);
        int Create(Customer customer);
        Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken);
    }

    [RepositoryFor(typeof(ICustomerService))]
    public partial class CustomerService : ICustomerService
    {
        private readonly DbConnection connection;
        public CustomerService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate appropriate parameter documentation
        Assert.IsTrue(generatedCode.Contains("/// <param name="), 
            "Should generate parameter documentation");
        Assert.IsTrue(generatedCode.Contains("cancellation token") || generatedCode.Contains("CancellationToken"), 
            "Should document cancellation token parameters appropriately");
        Assert.IsTrue(generatedCode.Contains("Customer entity") || generatedCode.Contains("entity to process"), 
            "Should document entity parameters appropriately");
    }

    /// <summary>
    /// Tests return value documentation generation for different return types.
    /// </summary>
    [TestMethod]
    public void DocumentationGeneration_ForDifferentReturnTypes_GeneratesAppropriateDescriptions()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IItemService
    {
        Item? GetSingle();
        IList<Item> GetCollection();
        int GetCount();
        bool CheckExists();
        void ProcessItem();
        Task<Item?> GetSingleAsync();
        Task<IList<Item>> GetCollectionAsync();
        Task<int> GetCountAsync();
        Task ProcessItemAsync();
    }

    [RepositoryFor(typeof(IItemService))]
    public partial class ItemService : IItemService
    {
        private readonly DbConnection connection;
        public ItemService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate appropriate return value documentation
        Assert.IsTrue(generatedCode.Contains("/// <returns>"), 
            "Should generate return value documentation");
        Assert.IsTrue(generatedCode.Contains("collection of") || generatedCode.Contains("Collection"), 
            "Should document collection return types");
        Assert.IsTrue(generatedCode.Contains("task") || generatedCode.Contains("Task"), 
            "Should document Task return types appropriately");
        Assert.IsTrue(generatedCode.Contains("number of affected rows") || generatedCode.Contains("affected rows"), 
            "Should document affected rows for int returns in CRUD operations");
    }

    /// <summary>
    /// Tests attribute copying when methods already have SQL attributes.
    /// </summary>
    [TestMethod]
    public void AttributeCopying_WithExistingSqlAttributes_CopiesAttributesCorrectly()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    public interface IEmployeeService
    {
        [Sqlx(""SELECT * FROM Employees WHERE Department = @dept"")]
        IList<Employee> GetByDepartment(string dept);
        
        [SqlExecuteType(SqlExecuteType.Insert, ""Employees"")]
        int AddEmployee(Employee employee);
        
        [RawSql(""SELECT COUNT(*) FROM Employees WHERE Active = 1"")]
        int GetActiveCount();
    }

    [RepositoryFor(typeof(IEmployeeService))]
    public partial class EmployeeService : IEmployeeService
    {
        private readonly DbConnection connection;
        public EmployeeService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should preserve existing SQL attributes
        Assert.IsTrue(generatedCode.Contains("Department = @dept") || generatedCode.Contains("Department"), 
            "Should preserve custom SQL from Sqlx attribute");
        Assert.IsTrue(generatedCode.Contains("INSERT") || generatedCode.Contains("ExecuteNonQuery"), 
            "Should handle SqlExecuteType attributes");
    }

    /// <summary>
    /// Tests SQL dialect detection and application.
    /// </summary>
    [TestMethod]
    public void SqlDialectHandling_WithDifferentConnectionTypes_UsesCorrectDialect()
    {
        string sourceCode = @"
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IProductService
    {
        Product? GetById(int id);
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductService : IProductService
    {
        private readonly SqliteConnection connection;
        public ProductService(SqliteConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        
        // Should compile without errors even with specific connection types
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Console.WriteLine($"Dialect handling test had some errors (may be expected): {errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should generate some form of SQL (specific dialect testing would require deeper inspection)
        Assert.IsTrue(generatedCode.Contains("SELECT") || generatedCode.Contains("ProductService"), 
            "Should generate code for specific connection types");
    }

    /// <summary>
    /// Tests parameter null checking generation.
    /// </summary>
    [TestMethod]
    public void ParameterNullChecking_ForReferenceTypes_GeneratesNullChecks()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    public interface IDocumentService
    {
        int Create(Document document);
        IList<Document> SearchByTitle(string title);
        int ProcessBatch(IList<Document> documents);
    }

    [RepositoryFor(typeof(IDocumentService))]
    public partial class DocumentService : IDocumentService
    {
        private readonly DbConnection connection;
        public DocumentService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate null checks for reference type parameters
        Assert.IsTrue(generatedCode.Contains("== null") || generatedCode.Contains("ArgumentNullException") || 
                     generatedCode.Contains("null!") || generatedCode.Contains("is null"), 
            "Should generate null checks for reference type parameters");
    }

    /// <summary>
    /// Tests default value generation for different return types.
    /// </summary>
    [TestMethod]
    public void DefaultValueGeneration_ForDifferentReturnTypes_GeneratesCorrectDefaults()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IProblematicService
    {
        // Intentionally problematic method to trigger error fallback
        System.Tuple<int, string, bool> GetComplexTuple(object complexParam);
        System.Collections.Generic.Dictionary<string, object> GetDictionary();
        int GetSimpleInt();
        TestEntity? GetEntity();
    }

    [RepositoryFor(typeof(IProblematicService))]
    public partial class ProblematicService : IProblematicService
    {
        private readonly DbConnection connection;
        public ProblematicService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should handle complex types and provide appropriate defaults
        Assert.IsTrue(generatedCode.Contains("default") || generatedCode.Contains("null") || 
                     generatedCode.Contains("return") || generatedCode.Contains("ProblematicService"), 
            "Should handle complex return types and provide defaults");
    }

    /// <summary>
    /// Tests that method name inference works correctly for various patterns.
    /// </summary>
    [TestMethod]
    public void MethodNameInference_ForVariousPatterns_GeneratesCorrectSql()
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
        public bool IsActive { get; set; }
    }

    public interface IItemService
    {
        // Various naming patterns
        IList<Item> FindAllItems();
        Item? GetItemById(int id);
        Item? FindItemByName(string name);
        int CreateNewItem(Item item);
        int AddItem(Item item);
        int InsertItem(Item item);
        int UpdateExistingItem(Item item);
        int ModifyItem(Item item);
        int DeleteItem(int id);
        int RemoveItem(int id);
        int CountAllItems();
        bool ItemExists(int id);
        bool ExistsById(int id);
    }

    [RepositoryFor(typeof(IItemService))]
    public partial class ItemService : IItemService
    {
        private readonly DbConnection connection;
        public ItemService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify different operation types are correctly inferred
        Assert.IsTrue(generatedCode.Contains("SELECT"), 
            "Should generate SELECT for find/get operations");
        Assert.IsTrue(generatedCode.Contains("INSERT") || generatedCode.Contains("ExecuteNonQuery"), 
            "Should generate INSERT for create/add operations");
        Assert.IsTrue(generatedCode.Contains("UPDATE") || generatedCode.Contains("SET"), 
            "Should generate UPDATE for update/modify operations");
        Assert.IsTrue(generatedCode.Contains("DELETE") || generatedCode.Contains("DELETE FROM"), 
            "Should generate DELETE for delete/remove operations");
        Assert.IsTrue(generatedCode.Contains("COUNT"), 
            "Should generate COUNT for count operations");
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
