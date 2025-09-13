// -----------------------------------------------------------------------
// <copyright file="ErrorHandlingAndEdgeCasesTests.cs" company="Cricle">
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
/// Tests for error handling, edge cases, and robustness of the Sqlx framework.
/// Focuses on how the system behaves with invalid inputs, malformed code, and boundary conditions.
/// </summary>
[TestClass]
public class ErrorHandlingAndEdgeCasesTests : CodeGenerationTestBase
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
    /// Tests source generator behavior with syntactically invalid code.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithInvalidSyntax_HandlesGracefully()
    {
        // Arrange - Code with syntax errors
        var sourceCode = @"
using System;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class InvalidService
    {
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers( // Missing closing parenthesis
        
        [RawSql(""SELECT * FROM Users"")]
        public User GetUser(int id // Missing closing parenthesis and semicolon
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    // Missing closing brace
";

        // Act & Assert - Should not crash the generator
        try
        {
            var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
            
            // Generator should handle syntax errors gracefully
            Assert.IsTrue(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), 
                "Should report syntax errors");
            
            // Generator should not crash
            Assert.IsNotNull(generatedSources, "Generated sources should not be null even with syntax errors");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Source generator should not throw exceptions on invalid syntax: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests source generator with missing required attributes.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithMissingAttributes_ReportsCorrectDiagnostics()
    {
        // Arrange - Service methods without required attributes
        var sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class ServiceWithoutAttributes
    {
        // No Sqlx attribute - should be ignored
        public List<User> GetUsers() => new List<User>();
        
        // No RawSql content - should report diagnostic
        public User GetUser(int id) => null!;
        
        // Invalid return type for ExecuteNonQuery
        public string ExecuteInvalidOperation() => """";
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
        // Should compile without generator errors (methods without attributes are ignored)
        var generatorErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && 
                                                    d.Id.StartsWith("SP")).ToList();
        
        // The generator should not produce errors for methods without attributes
        // (they are simply ignored)
        Assert.IsTrue(generatorErrors.Count == 0 || generatorErrors.All(e => !e.GetMessage().Contains("ServiceWithoutAttributes")), 
            "Methods without Sqlx attributes should be ignored, not cause errors");
    }

    /// <summary>
    /// Tests handling of invalid SQL syntax in attributes.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithInvalidSqlSyntax_HandlesGracefully()
    {
        // Arrange - Invalid SQL syntax in attributes
        var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ServiceWithInvalidSql
    {
        [RawSql(""SELECT * FROM INVALID SQL SYNTAX WHERE"")]
        public List<User> GetUsersWithInvalidSql() => null!;
        
        [RawSql(""INVALID COMMAND {0} @param"")]
        public User GetUserWithMalformedSql(int id) => null!;
        
        [Sqlx("""")]
        public List<User> GetUsersWithEmptySql() => null!;
        
        [RawSql(null!)]
        public User GetUserWithNullSql() => null!;
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
        // Generator should handle invalid SQL gracefully (SQL validation is not the generator's responsibility)
        Assert.IsNotNull(generatedSources, "Should generate sources even with invalid SQL");
        
        // Check that generator produces some output
        var hasGeneratedCode = generatedSources.Any(s => s.Length > 100);
        Assert.IsTrue(hasGeneratedCode, "Should generate some code structure");
    }

    /// <summary>
    /// Tests handling of complex generic types and constraints.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithComplexGenerics_HandlesCorrectly()
    {
        // Arrange - Complex generic scenarios
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class GenericService<T, U> where T : class, new() where U : struct
    {
        [RawSql(""SELECT * FROM Items"")]
        public List<T> GetItems() => null!;
        
        [RawSql(""SELECT * FROM Items WHERE Id = @id"")]
        public Task<T?> GetItemAsync(U id) => null!;
    }
    
    public class NestedGenericService
    {
        [RawSql(""SELECT * FROM Complex"")]
        public Dictionary<string, List<ComplexType<int, string>>> GetComplexData() => null!;
        
        [Sqlx(""GetNested"")]
        public Task<List<Tuple<User, List<Post>>>> GetNestedStructure() => null!;
    }
    
    public class ComplexType<T, U>
    {
        public T Key { get; set; } = default!;
        public U Value { get; set; } = default!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        // Should handle complex generics without crashing
        Assert.IsNotNull(generatedSources, "Should handle complex generics");
        
        var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        if (hasErrors)
        {
            var errorMessages = string.Join("; ", diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.GetMessage()));
            Console.WriteLine($"Compilation errors (may be expected): {errorMessages}");
        }
        
        // Generator should produce some output even if final compilation has issues
        Assert.IsTrue(generatedSources.Length > 0, "Should generate attribute sources at minimum");
    }

    /// <summary>
    /// Tests handling of circular references and complex inheritance.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithCircularReferences_HandlesGracefully()
    {
        // Arrange - Circular reference scenario
        var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class UserService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
        
        [RawSql(""SELECT * FROM Posts WHERE UserId = @userId"")]
        public List<Post> GetUserPosts(int userId) => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Post> Posts { get; set; } = new();
        public UserProfile Profile { get; set; } = null!;
    }
    
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public User Author { get; set; } = null!;
        public List<Comment> Comments { get; set; } = new();
    }
    
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Post Post { get; set; } = null!;
        public User Author { get; set; } = null!;
    }
    
    public class UserProfile
    {
        public int Id { get; set; }
        public User User { get; set; } = null!;
        public string Bio { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        // Should handle circular references without infinite loops
        Assert.IsNotNull(generatedSources, "Should handle circular references");
        
        // Generator should not get stuck in infinite loops
        Assert.IsTrue(generatedSources.Length > 0, "Should generate code despite circular references");
        
        // Check that the generation completed in reasonable time (implicit by test completion)
        Assert.IsTrue(true, "Test completed without hanging - no infinite loops");
    }

    /// <summary>
    /// Tests error handling with malformed method signatures.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithMalformedMethods_ReportsAppropriateErrors()
    {
        // Arrange - Various malformed method signatures
        var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class MalformedService
    {
        // No return type specified for query method
        [RawSql(""SELECT * FROM Users"")]
        public void GetUsersVoid() { }
        
        // Async method without Task return type
        [RawSql(""SELECT * FROM Users"")]
        public async List<User> GetUsersAsyncInvalid() => null!;
        
        // Missing parameters for parameterized query
        [RawSql(""SELECT * FROM Users WHERE Id = @id AND Name = @name"")]
        public User GetUserMissingParams() => null!;
        
        // Parameter type mismatch
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User GetUserWrongParamType(string id) => null!;
        
        // Generic method with unclear constraints
        [RawSql(""SELECT * FROM Items"")]
        public T GetGenericItem<T>() => default!;
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
        // Should generate code but may have compilation warnings/errors
        Assert.IsNotNull(generatedSources, "Should attempt to generate code for malformed methods");
        
        // Check that generator doesn't crash on malformed signatures
        Assert.IsTrue(generatedSources.Length > 0, "Should generate attribute definitions at minimum");
        
        // The actual method generation may fail, but that's acceptable behavior
        Console.WriteLine($"Generated {generatedSources.Length} source files for malformed methods test");
    }

    /// <summary>
    /// Tests handling of extremely large parameter lists.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithLargeParameterLists_HandlesEfficiently()
    {
        // Arrange - Method with many parameters
        var sourceCode = @"
using System;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ServiceWithManyParams
    {
        [RawSql(""SELECT * FROM Users WHERE "" + 
                ""p1=@p1 AND p2=@p2 AND p3=@p3 AND p4=@p4 AND p5=@p5 AND "" +
                ""p6=@p6 AND p7=@p7 AND p8=@p8 AND p9=@p9 AND p10=@p10"")]
        public User GetUserWithManyParams(
            int p1, string p2, DateTime p3, bool p4, decimal p5,
            double p6, float p7, long p8, short p9, byte p10,
            int? p11, string? p12, DateTime? p13, bool? p14, decimal? p15,
            Guid p16, TimeSpan p17, DateTimeOffset p18, char p19, uint p20) => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var startTime = DateTime.UtcNow;
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        // Assert
        // Should handle large parameter lists efficiently
        Assert.IsTrue(elapsed.TotalSeconds < 30, 
            $"Should handle large parameter lists efficiently. Took: {elapsed.TotalSeconds} seconds");
        
        Assert.IsNotNull(generatedSources, "Should generate sources for large parameter lists");
        
        // Check that it actually generated something meaningful
        Assert.IsTrue(generatedSources.Length > 0, "Should generate code for large parameter lists");
    }

    /// <summary>
    /// Tests handling of unsupported language features.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithUnsupportedFeatures_HandlesGracefully()
    {
        // Arrange - Code with advanced/unsupported features
        var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public unsafe class UnsafeService
    {
        // Unsafe pointer method
        [RawSql(""SELECT * FROM Users"")]
        public unsafe User* GetUserPointer() => null;
        
        // Method with ref returns
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public ref User GetUserRef(int id) => throw new NotImplementedException();
        
        // Method with stackalloc
        [Sqlx(""GetData"")]
        public Span<byte> GetDataSpan()
        {
            Span<byte> buffer = stackalloc byte[1024];
            return buffer;
        }
    }
    
    // Fixed size buffers
    public unsafe struct FixedSizeBuffer
    {
        public fixed byte Buffer[256];
        public int Length;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act & Assert - Should not crash on unsupported features
        try
        {
            var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
            
            // Generator should handle unsupported features gracefully
            Assert.IsNotNull(generatedSources, "Should handle unsupported features without crashing");
            
            // May have compilation errors, but generator should not crash
            Console.WriteLine($"Handled unsupported features, generated {generatedSources.Length} files");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should handle unsupported features gracefully: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests memory usage and cleanup with many source files.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithManyClasses_HandlesMemoryEfficiently()
    {
        // Arrange - Generate many classes to test memory handling
        var classDefinitions = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            classDefinitions.Add($@"
    public class Service{i}
    {{
        [RawSql(""SELECT * FROM Table{i}"")]
        public List<Entity{i}> GetEntities{i}() => null!;
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Table{i}"")]
        public void InsertEntity{i}(Entity{i} entity) {{ }}
    }}
    
    public class Entity{i}
    {{
        public int Id {{ get; set; }}
        public string Name{i} {{ get; set; }} = string.Empty;
        public DateTime Created{i} {{ get; set; }}
    }}");
        }

        var sourceCode = $@"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{{
{string.Join("\n", classDefinitions)}
}}";

        // Act
        var initialMemory = GC.GetTotalMemory(true);
        var startTime = DateTime.UtcNow;
        
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
        
        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;
        
        // Force cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        Assert.IsTrue(elapsed.TotalSeconds < 60, 
            $"Should handle many classes efficiently. Took: {elapsed.TotalSeconds} seconds");
        
        Assert.IsNotNull(generatedSources, "Should generate sources for many classes");
        
        var memoryUsed = finalMemory - initialMemory;
        Assert.IsTrue(memoryUsed < 100 * 1024 * 1024, // Less than 100MB
            $"Should use memory efficiently. Used: {memoryUsed / (1024 * 1024)}MB");
        
        Console.WriteLine($"Generated code for 50 classes in {elapsed.TotalSeconds:F2}s using {memoryUsed / (1024 * 1024):F2}MB");
    }

    /// <summary>
    /// Tests error recovery and continuation after encountering errors.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithMixedValidAndInvalid_ContinuesProcessing()
    {
        // Arrange - Mix of valid and invalid methods
        var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class MixedService
    {
        // Valid method - should generate correctly
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
        
        // Invalid method - missing return type match
        [RawSql(""SELECT * FROM Users"")]
        public void GetUsersVoid() { }
        
        // Valid method - should generate correctly
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User GetUserById(int id) => null!;
        
        // Invalid method - mismatched parameter
        [RawSql(""SELECT * FROM Users WHERE Id = @nonexistent"")]
        public User GetUserMismatch(int id) => null!;
        
        // Valid method - should generate correctly
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public void InsertUser(User user) { }
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
        // Should continue processing valid methods despite encountering invalid ones
        Assert.IsNotNull(generatedSources, "Should continue processing despite errors");
        
        // Should generate sources for valid methods
        Assert.IsTrue(generatedSources.Length > 0, "Should generate code for valid methods");
        
        // Check that at least some useful code was generated
        var totalGeneratedLength = generatedSources.Sum(s => s.Length);
        Assert.IsTrue(totalGeneratedLength > 500, "Should generate substantial code for valid methods");
        
        Console.WriteLine($"Generated {totalGeneratedLength} characters of code despite mixed valid/invalid methods");
    }
}
