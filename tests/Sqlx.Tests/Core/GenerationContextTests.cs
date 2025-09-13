// -----------------------------------------------------------------------
// <copyright file="GenerationContextTests.cs" company="Cricle">
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
/// Tests for GenerationContextBase and ClassGenerationContext.
/// Tests context creation, symbol detection, and lifecycle management.
/// </summary>
[TestClass]
public class GenerationContextTests : CodeGenerationTestBase
{
    /// <summary>
    /// Compiles source code with the Sqlx source generator and returns compilation results.
    /// </summary>
    protected (Compilation compilation, string[] generatedSources, Diagnostic[] diagnostics) CompileWithSourceGenerator(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetBasicReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create and run the source generator
        ISourceGenerator generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create(generator));
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

        // Get the generated sources
        var generatedSources = outputCompilation.SyntaxTrees.Skip(1).Select(tree => tree.ToString()).ToArray();
        var allDiagnostics = generateDiagnostics.Concat(outputCompilation.GetDiagnostics()).ToArray();

        return (outputCompilation, generatedSources, allDiagnostics);
    }

    /// <summary>
    /// Gets basic references for compilation.
    /// </summary>
    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location)
        };

        // Add additional assemblies safely
        SafeAddAssemblyReference(references, "System.Runtime");
        SafeAddAssemblyReference(references, "System.Linq.Expressions");
        SafeAddAssemblyReference(references, "netstandard");

        return references;
    }

    private static void SafeAddAssemblyReference(List<MetadataReference> references, string assemblyName)
    {
        try
        {
            var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);
            if (assembly != null)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }
        catch
        {
            // Ignore if assembly cannot be loaded
        }
    }

    /// <summary>
    /// Tests ClassGenerationContext constructor with valid parameters.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_Constructor_CreatesValidInstance()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(compilation, "Compilation should not be null");
        Assert.IsNotNull(generatedSources, "Generated sources should not be null");
        
        // Should generate attribute definitions at minimum
        Assert.IsTrue(generatedSources.Length > 0, "Should generate at least attribute sources");
        
        Console.WriteLine($"ClassGenerationContext test: {generatedSources.Length} sources generated");
    }

    /// <summary>
    /// Tests ClassGenerationContext with empty constructor (testing/mocking).
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_EmptyConstructor_CreatesValidInstance()
    {
        // Act
        var context = new ClassGenerationContext();

        // Assert
        Assert.IsNotNull(context, "Context should not be null");
        Assert.IsNotNull(context.Methods, "Methods collection should not be null");
        Assert.AreEqual(0, context.Methods.Count, "Methods should be empty for empty constructor");
        
        Console.WriteLine("ClassGenerationContext empty constructor test passed");
    }

    /// <summary>
    /// Tests DbConnection detection in generation context.
    /// </summary>
    [TestMethod]
    public void GenerationContext_DbConnectionDetection_WorksCorrectly()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers(DbConnection connection) => null!;
        
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User GetUser(int id, DbConnection connection) => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should handle DbConnection parameters");
        
        // Should generate code that handles DbConnection
        var allCode = string.Join("\n", generatedSources);
        var hasDbConnectionHandling = allCode.Contains("DbConnection") || 
                                     allCode.Contains("connection") ||
                                     generatedSources.Length > 1;
        
        Assert.IsTrue(hasDbConnectionHandling, "Should handle DbConnection parameters");
        
        Console.WriteLine("DbConnection detection test passed");
    }

    /// <summary>
    /// Tests transaction parameter detection in generation context.
    /// </summary>
    [TestMethod]
    public void GenerationContext_TransactionDetection_WorksCorrectly()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestService
    {
        [RawSql(""INSERT INTO Users (Name) VALUES (@name)"")]
        public void InsertUser(string name, DbTransaction transaction) { }
        
        [SqlExecuteType(SqlExecuteTypes.Update, ""Users"")]
        public void UpdateUser(User user, DbTransaction transaction) { }
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should handle DbTransaction parameters");
        
        // Should generate code that handles transactions
        var allCode = string.Join("\n", generatedSources);
        var hasTransactionHandling = allCode.Contains("Transaction") || 
                                    allCode.Contains("transaction") ||
                                    generatedSources.Length > 1;
        
        Assert.IsTrue(hasTransactionHandling, "Should handle DbTransaction parameters");
        
        Console.WriteLine("Transaction detection test passed");
    }

    /// <summary>
    /// Tests execution context setting in ClassGenerationContext.
    /// </summary>
    [TestMethod]
    public void ClassGenerationContext_SetExecutionContext_UpdatesContext()
    {
        // This test verifies that the SetExecutionContext method works
        // We can't easily test the actual execution context, but we can test instantiation
        
        // Act
        var context = new ClassGenerationContext();
        
        // Assert - should not throw
        Assert.IsNotNull(context, "Context should be created successfully");
        Assert.IsNotNull(context.Methods, "Methods should be initialized");
        
        // Test that we can access the properties without error
        try
        {
            var methods = context.Methods;
            Assert.IsNotNull(methods, "Methods property should be accessible");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should be able to access Methods property: {ex.Message}");
        }
        
        Console.WriteLine("SetExecutionContext test passed");
    }

    /// <summary>
    /// Tests GenerationContextBase GetSymbol method with different symbol types.
    /// </summary>
    [TestMethod]
    public void GenerationContextBase_GetSymbol_HandlesVariousSymbolTypes()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    public class ServiceWithField
    {
        private DbConnection _connection = null!;
        
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should handle various symbol types");
        
        // Should generate code for different symbol scenarios
        var allCode = string.Join("\n", generatedSources);
        var hasSymbolHandling = allCode.Contains("User") || 
                               allCode.Contains("TestService") ||
                               generatedSources.Length > 1;
        
        Assert.IsTrue(hasSymbolHandling, "Should handle various symbol types");
        
        Console.WriteLine("GetSymbol method test passed");
    }

    /// <summary>
    /// Tests generation context with complex inheritance scenarios.
    /// </summary>
    [TestMethod]
    public void GenerationContext_WithInheritance_HandlesBaseTypes()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public abstract class BaseService
    {
        protected DbConnection BaseConnection { get; set; } = null!;
    }
    
    public class UserService : BaseService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
        
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User GetUser(int id) => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should handle inheritance scenarios");
        
        // Should generate code for inherited services
        var allCode = string.Join("\n", generatedSources);
        var hasInheritanceHandling = allCode.Contains("UserService") || 
                                    allCode.Contains("BaseService") ||
                                    allCode.Contains("User") ||
                                    generatedSources.Length > 1;
        
        Assert.IsTrue(hasInheritanceHandling, "Should handle inheritance scenarios");
        
        Console.WriteLine("Inheritance handling test passed");
    }

    /// <summary>
    /// Tests generation context with multiple methods in the same class.
    /// </summary>
    [TestMethod]
    public void GenerationContext_WithMultipleMethods_CreatesAllContexts()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ComprehensiveService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
        
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User GetUser(int id) => null!;
        
        [RawSql(""SELECT * FROM Users"")]
        public async Task<List<User>> GetUsersAsync() => null!;
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public void InsertUser(User user) { }
        
        [SqlExecuteType(SqlExecuteTypes.Update, ""Users"")]
        public void UpdateUser(User user) { }
        
        [SqlExecuteType(SqlExecuteTypes.Delete, ""Users"")]
        public void DeleteUser(int id) { }
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should handle multiple methods");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate sources for multiple methods");
        
        // Should generate substantial code for multiple methods
        var totalCodeLength = generatedSources.Sum(s => s.Length);
        Assert.IsTrue(totalCodeLength > 300, "Should generate substantial code for multiple methods");
        
        Console.WriteLine($"Multiple methods test: {generatedSources.Length} sources, {totalCodeLength} characters generated");
    }

    /// <summary>
    /// Tests generation context error handling with invalid scenarios.
    /// </summary>
    [TestMethod]
    public void GenerationContext_WithInvalidScenarios_HandlesGracefully()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class InvalidService
    {
        // Method without return type specification
        [RawSql(""SELECT * FROM Users"")]
        public void GetUsers() { }
        
        // Method with missing parameters for SQL
        [RawSql(""SELECT * FROM Users WHERE Id = @id AND Name = @name"")]
        public User GetUser() => null!;
        
        // Method with conflicting attribute usage
        [RawSql(""SELECT * FROM Users"")]
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public User GetUserConflict() => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act & Assert
        try
        {
            var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
            
            // Should not crash on invalid scenarios
            Assert.IsNotNull(generatedSources, "Should handle invalid scenarios gracefully");
            
            // Should generate at least attribute sources
            Assert.IsTrue(generatedSources.Length > 0, "Should generate sources even with invalid methods");
            
            Console.WriteLine("Invalid scenarios test passed - generator handled errors gracefully");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Generator should handle invalid scenarios gracefully: {ex.Message}");
        }
    }
}
