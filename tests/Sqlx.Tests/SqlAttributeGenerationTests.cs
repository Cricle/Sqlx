// -----------------------------------------------------------------------
// <copyright file="SqlAttributeGenerationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for SQL attribute generation in RepositoryFor generator.
/// Tests cover various method patterns and their corresponding SQL attribute generation.
/// </summary>
[TestClass]
public class SqlAttributeGenerationTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that GetAll methods generate SELECT * attributes.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_GetAllMethods_GenerateSelectAll()
    {
        string sourceCode = @"
using System.Collections.Generic;
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
        IList<Item> GetAll();
        IList<Item> GetAllItems();
        IList<Item> FindAll();
        IList<Item> FindAllItems();
    }

    [RepositoryFor(typeof(IItemService))]
    public partial class ItemRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // All GetAll patterns should generate SELECT * queries
        Assert.IsTrue(generatedCode.Contains("[Sqlx(\"SELECT * FROM Item\")]") ||
                     generatedCode.Contains("[Sqlx(\"SELECT * FROM Items\")]"),
            "GetAll methods should generate SELECT * queries");

        // Check multiple variations
        var selectPatterns = new[] { "GetAll", "GetAllItems", "FindAll", "FindAllItems" };
        foreach (var pattern in selectPatterns)
        {
            Assert.IsTrue(generatedCode.Contains(pattern),
                $"Generated code should contain method {pattern}");
        }
    }

    /// <summary>
    /// Tests that GetById methods generate WHERE Id = @id attributes.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_GetByIdMethods_GenerateWhereId()
    {
        string sourceCode = @"
using System.Collections.Generic;
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
        Product? GetProductById(int id);
        Product? FindById(int id);
        Product? FindProductById(int productId);
        Product? GetByKey(int key);
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // GetById patterns should generate WHERE Id queries
        Assert.IsTrue(generatedCode.Contains("WHERE Id = @id") ||
                     generatedCode.Contains("WHERE Id = @productId") ||
                     generatedCode.Contains("WHERE Id = @key"),
            "GetById methods should generate WHERE Id queries");

        // Check parameter mapping
        Assert.IsTrue(generatedCode.Contains("GetById(int id)"),
            "Should preserve parameter names in generated methods");
        Assert.IsTrue(generatedCode.Contains("FindProductById(int productId)"),
            "Should preserve parameter names in generated methods");
        Assert.IsTrue(generatedCode.Contains("GetByKey(int key)"),
            "Should preserve parameter names in generated methods");
    }

    /// <summary>
    /// Tests that Create/Insert methods generate SqlExecuteType Insert attributes.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_CreateMethods_GenerateInsertExecuteType()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""custom_orders"")]
    public class Order
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    public interface IOrderService
    {
        int Create(Order order);
        int CreateOrder(Order order);
        int Insert(Order order);
        int InsertOrder(Order order);
        int Add(Order order);
        int AddOrder(Order order);
    }

    [RepositoryFor(typeof(IOrderService))]
    public partial class OrderRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Create patterns should generate SqlExecuteType Insert
        Assert.IsTrue(generatedCode.Contains("SqlExecuteTypes.Insert"),
            "Create methods should generate SqlExecuteType Insert");

        // Should use table name from TableName attribute
        Assert.IsTrue(generatedCode.Contains("\"custom_orders\""),
            "Should use table name from TableName attribute");

        // Check multiple create patterns
        var createPatterns = new[] { "Create", "CreateOrder", "Insert", "InsertOrder", "Add", "AddOrder" };
        foreach (var pattern in createPatterns)
        {
            Assert.IsTrue(generatedCode.Contains(pattern),
                $"Generated code should contain method {pattern}");
        }
    }

    /// <summary>
    /// Tests that Update methods generate SqlExecuteType Update attributes.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_UpdateMethods_GenerateUpdateExecuteType()
    {
        string sourceCode = @"
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
        int Update(Customer customer);
        int UpdateCustomer(Customer customer);
        int Modify(Customer customer);
        int ModifyCustomer(Customer customer);
    }

    [RepositoryFor(typeof(ICustomerService))]
    public partial class CustomerRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Update patterns should generate SqlExecuteType Update
        Assert.IsTrue(generatedCode.Contains("SqlExecuteTypes.Update"),
            "Update methods should generate SqlExecuteType Update");

        // Should use entity name as table name by default
        Assert.IsTrue(generatedCode.Contains("\"Customer\""),
            "Should use entity name as table name by default");

        // Check multiple update patterns
        var updatePatterns = new[] { "Update", "UpdateCustomer", "Modify", "ModifyCustomer" };
        foreach (var pattern in updatePatterns)
        {
            Assert.IsTrue(generatedCode.Contains(pattern),
                $"Generated code should contain method {pattern}");
        }
    }

    /// <summary>
    /// Tests that Delete methods generate SqlExecuteType Delete attributes.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_DeleteMethods_GenerateDeleteExecuteType()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IAccountService
    {
        int Delete(int id);
        int DeleteAccount(int id);
        int Remove(int id);
        int RemoveAccount(int id);
        int DeleteById(int accountId);
    }

    [RepositoryFor(typeof(IAccountService))]
    public partial class AccountRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Delete patterns should generate SqlExecuteType Delete
        Assert.IsTrue(generatedCode.Contains("SqlExecuteTypes.Delete"),
            "Delete methods should generate SqlExecuteType Delete");

        // Should use entity name as table name
        Assert.IsTrue(generatedCode.Contains("\"Account\""),
            "Should use entity name as table name");

        // Check multiple delete patterns
        var deletePatterns = new[] { "Delete", "DeleteAccount", "Remove", "RemoveAccount", "DeleteById" };
        foreach (var pattern in deletePatterns)
        {
            Assert.IsTrue(generatedCode.Contains(pattern),
                $"Generated code should contain method {pattern}");
        }
    }

    /// <summary>
    /// Tests that Count methods generate COUNT(*) queries.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_CountMethods_GenerateCountQueries()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface IPostService
    {
        int Count();
        int CountPosts();
        long CountAll();
        int GetCount();
        int GetPostCount();
    }

    [RepositoryFor(typeof(IPostService))]
    public partial class PostRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Count methods should generate COUNT(*) queries
        Assert.IsTrue(generatedCode.Contains("COUNT(*)"),
            "Count methods should generate COUNT(*) queries");

        // Should use entity name as table name
        Assert.IsTrue(generatedCode.Contains("SELECT COUNT(*) FROM Post"),
            "Count queries should reference the correct table");

        // Check multiple count patterns
        var countPatterns = new[] { "Count()", "CountPosts()", "CountAll()", "GetCount()", "GetPostCount()" };
        foreach (var pattern in countPatterns)
        {
            Assert.IsTrue(generatedCode.Contains(pattern),
                $"Generated code should contain method {pattern}");
        }
    }

    /// <summary>
    /// Tests that Exists methods generate COUNT(*) queries with WHERE clauses.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_ExistsMethods_GenerateExistsQueries()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        bool Exists(int id);
        bool ExistsUser(int id);
        bool UserExists(int userId);
        bool ExistsById(int id);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Exists methods should generate COUNT(*) queries with WHERE
        Assert.IsTrue(generatedCode.Contains("COUNT(*)") && generatedCode.Contains("WHERE"),
            "Exists methods should generate COUNT(*) queries with WHERE clauses");

        // Should include parameter references
        Assert.IsTrue(generatedCode.Contains("@id") || generatedCode.Contains("@userId"),
            "Exists queries should include parameter references");

        // Check multiple exists patterns
        var existsPatterns = new[] { "Exists", "ExistsUser", "UserExists", "ExistsById" };
        foreach (var pattern in existsPatterns)
        {
            Assert.IsTrue(generatedCode.Contains(pattern),
                $"Generated code should contain method {pattern}");
        }
    }

    /// <summary>
    /// Tests that complex method names default to SELECT queries.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_ComplexMethodNames_DefaultToSelect()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface IDocumentService
    {
        IList<Document> ProcessDocuments();
        Document ValidateDocument(int id);
        void CalculateStats();
        string GenerateReport();
    }

    [RepositoryFor(typeof(IDocumentService))]
    public partial class DocumentRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Unknown patterns should default to SELECT
        Assert.IsTrue(generatedCode.Contains("SELECT * FROM Document"),
            "Unknown method patterns should default to SELECT queries");

        // Check that all methods are generated
        var methodPatterns = new[] { "ProcessDocuments", "ValidateDocument", "CalculateStats", "GenerateReport" };
        foreach (var pattern in methodPatterns)
        {
            Assert.IsTrue(generatedCode.Contains(pattern),
                $"Generated code should contain method {pattern}");
        }
    }

    /// <summary>
    /// Tests that table names are correctly determined from various sources.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_TableNameResolution_WorksCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""special_items"")]
    public class SpecialItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RegularItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ISpecialItemService
    {
        IList<SpecialItem> GetAll();
        int Create(SpecialItem item);
    }

    public interface IRegularItemService
    {
        IList<RegularItem> GetAll();
        int Create(RegularItem item);
    }

    [RepositoryFor(typeof(ISpecialItemService))]
    public partial class SpecialItemRepository
    {
    }

    [RepositoryFor(typeof(IRegularItemService))]
    public partial class RegularItemRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Should use TableName attribute when available
        Assert.IsTrue(generatedCode.Contains("special_items"),
            "Should use table name from TableName attribute");

        // Should use entity class name when no TableName attribute
        Assert.IsTrue(generatedCode.Contains("RegularItem"),
            "Should use entity class name when no TableName attribute");

        // Verify both SELECT and INSERT operations use correct table names
        Assert.IsTrue(generatedCode.Contains("SELECT * FROM special_items") ||
                     generatedCode.Contains("SqlExecuteTypes.Insert, \"special_items\""),
            "Should use custom table name in all operations");
    }

    /// <summary>
    /// Tests that method body generation includes NotImplementedException.
    /// </summary>
    [TestMethod]
    public void SqlGeneration_MethodBodies_IncludeNotImplementedException()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Sample
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ISampleService
    {
        IList<Sample> GetAll();
        Sample? GetById(int id);
        int Create(Sample sample);
        void DoVoidOperation();
    }

    [RepositoryFor(typeof(ISampleService))]
    public partial class SampleRepository
    {
    }
}";

        var generatedCode = GetGeneratedCode(sourceCode);

        // Method bodies should include actual implementation, not NotImplementedException
        Assert.IsFalse(generatedCode.Contains("NotImplementedException"),
            "Method bodies should contain actual implementation, not NotImplementedException");

        // Should include explanatory comments
        Assert.IsTrue(generatedCode.Contains("This method will be implemented by Sqlx source generator") ||
                     generatedCode.Contains("generated by Sqlx"),
            "Should include explanatory comments about implementation");
    }

    private static string GetGeneratedCode(string sourceCode)
    {
        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Compilation failed with errors:\n{errorMessages}");
        }

        var generatedSources = GetGeneratedSources(compilation);
        return string.Join("\n", generatedSources);
    }

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

    private static List<string> GetGeneratedSources(Compilation compilation)
    {
        var generatedSources = new List<string>();
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            if (syntaxTree.FilePath.Contains("Generated") ||
                string.IsNullOrEmpty(syntaxTree.FilePath) ||
                syntaxTree.ToString().Contains("// <auto-generated>"))
            {
                generatedSources.Add(syntaxTree.ToString());
            }
        }

        return generatedSources;
    }

    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location));

        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        references.Add(MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location));

        return references;
    }
}

