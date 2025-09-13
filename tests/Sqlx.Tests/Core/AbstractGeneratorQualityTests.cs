// -----------------------------------------------------------------------
// <copyright file="AbstractGeneratorQualityTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for AbstractGenerator code quality, performance characteristics, and output validation.
/// Ensures generated code meets quality standards and best practices.
/// </summary>
[TestClass]
public class AbstractGeneratorQualityTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that generated code follows proper formatting and indentation.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_HasProperFormatting()
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
        public string Email { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        User? GetById(int id);
        IList<User> GetAll();
        int Create(User user);
        int Update(User user);
        int Delete(int id);
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

        // Check for proper indentation
        var lines = generatedCode.Split('\n');
        var properlyIndentedLines = lines.Where(line => 
            string.IsNullOrWhiteSpace(line) || 
            line.StartsWith("    ") || 
            line.StartsWith("        ") || 
            line.StartsWith("            ") ||
            !line.StartsWith(" ")).Count();
        
        var totalNonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).Count();
        
        Assert.IsTrue(properlyIndentedLines >= totalNonEmptyLines * 0.8, 
            "Most lines should be properly indented (using 4-space indentation)");

        // Check for proper brace placement
        Assert.IsTrue(generatedCode.Contains("{\n") || generatedCode.Contains("{\r\n"), 
            "Opening braces should be followed by newlines");
        
        // Check for consistent spacing
        Assert.IsFalse(generatedCode.Contains("  ,") || generatedCode.Contains(" ,"), 
            "Should not have spaces before commas");
        Assert.IsFalse(generatedCode.Contains("if(") || generatedCode.Contains("while("), 
            "Should have spaces after control keywords");
    }

    /// <summary>
    /// Tests that generated code includes proper error handling patterns.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_IncludesErrorHandling()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
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
        Order? GetById(int id);
        Task<Order?> GetByIdAsync(int id);
        int Create(Order order);
        Task<int> CreateAsync(Order order);
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

        // Check for try-catch blocks
        Assert.IsTrue(generatedCode.Contains("try") && generatedCode.Contains("catch"), 
            "Generated code should include error handling with try-catch blocks");

        // Check for finally blocks or proper resource disposal
        Assert.IsTrue(generatedCode.Contains("finally") || generatedCode.Contains("?.Dispose()") || generatedCode.Contains("using"), 
            "Generated code should properly dispose resources");

        // Check for connection state handling
        Assert.IsTrue(generatedCode.Contains("connection.State") || generatedCode.Contains("Open"), 
            "Generated code should handle connection state properly");
    }

    /// <summary>
    /// Tests that generated code includes proper parameter validation.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_IncludesParameterValidation()
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
        IList<Product> SearchByName(string name);
        int Create(Product product);
        int UpdatePrice(Product product, decimal newPrice);
        int ProcessBatch(IList<Product> products);
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

        // Check for null parameter validation
        Assert.IsTrue(generatedCode.Contains("== null") || generatedCode.Contains("is null") || 
                     generatedCode.Contains("ArgumentNullException") || generatedCode.Contains("null!"), 
            "Generated code should include null parameter validation");

        // Check for collection parameter validation
        Assert.IsTrue(generatedCode.Contains("Count") || generatedCode.Contains("Any()") || 
                     generatedCode.Contains("Length") || generatedCode.Contains("?."), 
            "Generated code should validate collection parameters");
    }

    /// <summary>
    /// Tests that generated code uses efficient SQL patterns.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_UsesEfficientSqlPatterns()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public interface ICustomerService
    {
        Customer? GetById(int id);
        IList<Customer> GetAll();
        IList<Customer> GetByEmailDomain(string domain);
        int GetTotalCount();
        bool Exists(int id);
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

        // Check for parameterized queries (security)
        Assert.IsTrue(generatedCode.Contains("@") || generatedCode.Contains("CreateParameter"), 
            "Generated code should use parameterized queries for security");

        // Check for appropriate SQL command types
        Assert.IsTrue(generatedCode.Contains("ExecuteReader") || generatedCode.Contains("ExecuteScalar") || 
                     generatedCode.Contains("ExecuteNonQuery"), 
            "Generated code should use appropriate SQL execution methods");

        // Check for efficient data reading patterns
        Assert.IsTrue(generatedCode.Contains("GetOrdinal") || generatedCode.Contains("GetInt32") || 
                     generatedCode.Contains("GetString") || generatedCode.Contains("Read()"), 
            "Generated code should use efficient data reading patterns");
    }

    /// <summary>
    /// Tests that generated code includes proper async/await patterns.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_UsesProperAsyncPatterns()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        Task<Document?> GetByIdAsync(int id);
        Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<IList<Document>> GetAllAsync();
        Task<int> CreateAsync(Document document);
        Task<int> CreateAsync(Document document, CancellationToken cancellationToken);
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

        // Check for proper async method signatures
        Assert.IsTrue(generatedCode.Contains("async Task"), 
            "Generated code should have async method signatures");

        // Check for await usage
        Assert.IsTrue(generatedCode.Contains("await "), 
            "Generated code should use await for async operations");

        // Check for async database operations
        Assert.IsTrue(generatedCode.Contains("OpenAsync") || generatedCode.Contains("ExecuteReaderAsync") || 
                     generatedCode.Contains("ExecuteScalarAsync") || generatedCode.Contains("ReadAsync"), 
            "Generated code should use async database operations");

        // Check for CancellationToken usage
        Assert.IsTrue(generatedCode.Contains("CancellationToken"), 
            "Generated code should handle cancellation tokens");
    }

    /// <summary>
    /// Tests that generated code doesn't contain common anti-patterns.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_AvoidsAntiPatterns()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        User? Login(string username, string password);
        Task<User?> LoginAsync(string username, string password);
        IList<User> GetAllUsers();
        int CreateUser(User user);
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

        // Check against SQL injection patterns
        Assert.IsFalse(generatedCode.Contains("\" + ") && generatedCode.Contains("password"), 
            "Generated code should not use string concatenation for SQL with sensitive data");

        // Check against inefficient patterns
        Assert.IsFalse(generatedCode.Contains("SELECT *") && generatedCode.Contains("WHERE 1=1"), 
            "Generated code should avoid inefficient SQL patterns");

        // Check against resource leak patterns
        Assert.IsFalse(Regex.IsMatch(generatedCode, @"new\s+SqlConnection.*(?!using|Dispose)"), 
            "Generated code should properly dispose database connections");

        // Check against synchronous blocking in async context
        Assert.IsFalse(generatedCode.Contains("async") && generatedCode.Contains(".Result"), 
            "Generated code should not block on async operations");
    }

    /// <summary>
    /// Tests the performance characteristics of code generation.
    /// </summary>
    [TestMethod]
    public void CodeGeneration_HasAcceptablePerformance()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class LargeEntity
    {
        public int Id { get; set; }
        public string Field1 { get; set; } = string.Empty;
        public string Field2 { get; set; } = string.Empty;
        public string Field3 { get; set; } = string.Empty;
        public string Field4 { get; set; } = string.Empty;
        public string Field5 { get; set; } = string.Empty;
        public int IntField1 { get; set; }
        public int IntField2 { get; set; }
        public int IntField3 { get; set; }
        public DateTime DateField1 { get; set; }
        public DateTime DateField2 { get; set; }
        public decimal DecimalField1 { get; set; }
        public decimal DecimalField2 { get; set; }
        public bool BoolField1 { get; set; }
        public bool BoolField2 { get; set; }
    }

    public interface ILargeService
    {
        Task<LargeEntity?> GetByIdAsync(int id);
        Task<IList<LargeEntity>> GetAllAsync();
        Task<IList<LargeEntity>> SearchAsync(string field1, string field2, string field3);
        Task<int> CreateAsync(LargeEntity entity);
        Task<int> UpdateAsync(LargeEntity entity);
        Task<int> DeleteAsync(int id);
        Task<IList<LargeEntity>> GetByDateRangeAsync(DateTime start, DateTime end);
        Task<IList<LargeEntity>> GetByMultipleFieldsAsync(int intField1, decimal decimalField1, bool boolField1);
        Task<long> GetCountAsync();
        Task<decimal> GetTotalDecimalAsync();
    }

    [RepositoryFor(typeof(ILargeService))]
    public partial class LargeService : ILargeService
    {
        private readonly DbConnection connection;
        public LargeService(DbConnection connection) => this.connection = connection;
    }
}";

        var startTime = DateTime.UtcNow;
        
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        
        var endTime = DateTime.UtcNow;
        var generationTime = endTime - startTime;

        Assert.IsNotNull(generatedCode);
        
        // Generation should complete in reasonable time (adjust threshold as needed)
        Assert.IsTrue(generationTime.TotalSeconds < 30, 
            $"Code generation should complete in reasonable time. Took: {generationTime.TotalSeconds} seconds");

        // Generated code should not be excessively long
        var lines = generatedCode.Split('\n').Length;
        Assert.IsTrue(lines < 10000, 
            $"Generated code should be reasonably sized. Generated {lines} lines");

        // Should generate all expected methods
        var methodCount = sourceCode.Split(';').Where(s => s.Contains("Task<") || s.Contains("int ") || s.Contains("long ")).Count();
        Assert.IsTrue(generatedCode.Split("public").Length >= methodCount, 
            "Should generate implementations for all interface methods");
    }

    /// <summary>
    /// Tests that generated code includes proper documentation.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_IncludesComprehensiveDocumentation()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
    }

    public interface IBookService
    {
        Book? GetById(int id);
        IList<Book> GetByAuthor(string author);
        IList<Book> SearchByTitle(string titlePattern);
        int Create(Book book);
        int Update(Book book);
        bool Exists(string isbn);
    }

    [RepositoryFor(typeof(IBookService))]
    public partial class BookService : IBookService
    {
        private readonly DbConnection connection;
        public BookService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Count documentation elements
        var summaryCount = Regex.Matches(generatedCode, @"/// <summary>").Count;
        var paramCount = Regex.Matches(generatedCode, @"/// <param name=").Count;
        var returnsCount = Regex.Matches(generatedCode, @"/// <returns>").Count;

        // Should have documentation for most generated methods
        Assert.IsTrue(summaryCount >= 5, 
            $"Generated code should have summary documentation. Found {summaryCount} summaries");
        
        // Should document parameters
        Assert.IsTrue(paramCount >= 8, 
            $"Generated code should document parameters. Found {paramCount} parameter docs");
        
        // Should document return values for non-void methods
        Assert.IsTrue(returnsCount >= 4, 
            $"Generated code should document return values. Found {returnsCount} return docs");

        // Should include auto-generation notice
        Assert.IsTrue(generatedCode.Contains("automatically generated") || 
                     generatedCode.Contains("auto-generated") ||
                     generatedCode.Contains("Generated implementation"),
            "Generated code should indicate it was automatically generated");
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
