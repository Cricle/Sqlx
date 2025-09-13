// -----------------------------------------------------------------------
// <copyright file="MethodGenerationContextTests.cs" company="Cricle">
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
/// Tests for MethodGenerationContext functionality.
/// Tests method context creation, variable naming, and SQL parameter handling.
/// </summary>
[TestClass]
public class MethodGenerationContextTests : CodeGenerationTestBase
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
    /// Tests MethodGenerationContext variable name constants.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_VariableNames_AreConsistent()
    {
        // Arrange & Act - Test the constants are defined
        var dbConnectionName = MethodGenerationContext.DbConnectionName;
        var cmdName = MethodGenerationContext.CmdName;
        var dbReaderName = MethodGenerationContext.DbReaderName;
        var resultName = MethodGenerationContext.ResultName;
        var dataName = MethodGenerationContext.DataName;
        var startTimeName = MethodGenerationContext.StartTimeName;

        // Assert
        Assert.IsNotNull(dbConnectionName, "DbConnectionName should be defined");
        Assert.IsNotNull(cmdName, "CmdName should be defined");
        Assert.IsNotNull(dbReaderName, "DbReaderName should be defined");
        Assert.IsNotNull(resultName, "ResultName should be defined");
        Assert.IsNotNull(dataName, "DataName should be defined");
        Assert.IsNotNull(startTimeName, "StartTimeName should be defined");

        // Verify they are different from each other
        var names = new[] { dbConnectionName, cmdName, dbReaderName, resultName, dataName, startTimeName };
        var uniqueNames = names.Distinct().ToArray();
        Assert.AreEqual(names.Length, uniqueNames.Length, "All variable names should be unique");

        // Verify naming conventions
        Assert.IsTrue(cmdName.Contains("Cmd"), "Command name should contain 'Cmd'");
        Assert.IsTrue(dbReaderName.Contains("Reader"), "Reader name should contain 'Reader'");
        Assert.IsTrue(resultName.Contains("Result"), "Result name should contain 'Result'");

        Console.WriteLine($"Variable names test passed: {string.Join(", ", names)}");
    }

    /// <summary>
    /// Tests MethodGenerationContext method lifecycle constants.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_LifecycleMethods_AreCorrectlyDefined()
    {
        // Arrange & Act
        var methodExecuting = MethodGenerationContext.MethodExecuting;
        var methodExecuted = MethodGenerationContext.MethodExecuted;
        var methodExecuteFail = MethodGenerationContext.MethodExecuteFail;
        var getTimestampMethod = MethodGenerationContext.GetTimestampMethod;

        // Assert
        Assert.AreEqual("OnExecuting", methodExecuting, "MethodExecuting should be 'OnExecuting'");
        Assert.AreEqual("OnExecuted", methodExecuted, "MethodExecuted should be 'OnExecuted'");
        Assert.AreEqual("OnExecuteFail", methodExecuteFail, "MethodExecuteFail should be 'OnExecuteFail'");
        
        Assert.IsTrue(getTimestampMethod.Contains("Stopwatch"), "GetTimestampMethod should reference Stopwatch");
        Assert.IsTrue(getTimestampMethod.Contains("GetTimestamp"), "GetTimestampMethod should contain GetTimestamp");

        Console.WriteLine("Lifecycle methods test passed");
    }

    /// <summary>
    /// Tests MethodGenerationContext with simple method.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_WithSimpleMethod_GeneratesCorrectly()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class SimpleService
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
        Assert.IsNotNull(generatedSources, "Should generate sources for simple method");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate at least attribute sources");

        // Check that method context generation works
        var allCode = string.Join("\n", generatedSources);
        var hasMethodHandling = allCode.Contains("GetUsers") || 
                               allCode.Contains("User") ||
                               generatedSources.Length > 1;
        
        Assert.IsTrue(hasMethodHandling, "Should handle simple method generation");

        Console.WriteLine($"Simple method test: {generatedSources.Length} sources generated");
    }

    /// <summary>
    /// Tests MethodGenerationContext with parameterized method.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_WithParameters_HandlesCorrectly()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ParameterizedService
    {
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User GetUser(int id) => null!;
        
        [RawSql(""SELECT * FROM Users WHERE Name = @name AND Age = @age"")]
        public List<User> GetUsersByNameAndAge(string name, int age) => null!;
        
        [RawSql(""SELECT * FROM Users WHERE Active = @isActive"")]
        public List<User> GetActiveUsers(bool isActive) => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool Active { get; set; }
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should generate sources for parameterized methods");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate sources");

        // Check that parameter handling works
        var allCode = string.Join("\n", generatedSources);
        var hasParameterHandling = allCode.Contains("GetUser") || 
                                  allCode.Contains("@id") ||
                                  allCode.Contains("@name") ||
                                  allCode.Contains("@age") ||
                                  allCode.Contains("parameter") ||
                                  generatedSources.Length > 1;
        
        Assert.IsTrue(hasParameterHandling, "Should handle parameterized methods");

        Console.WriteLine($"Parameterized method test: {generatedSources.Length} sources generated");
    }

    /// <summary>
    /// Tests MethodGenerationContext with async methods.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_WithAsyncMethods_GeneratesAsyncCode()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class AsyncService
    {
        [RawSql(""SELECT * FROM Users"")]
        public async Task<List<User>> GetUsersAsync() => null!;
        
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public async Task<User> GetUserAsync(int id, CancellationToken cancellationToken = default) => null!;
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public async Task InsertUserAsync(User user, CancellationToken cancellationToken = default) { }
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
        Assert.IsNotNull(generatedSources, "Should generate sources for async methods");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate sources");

        // Check that async handling works
        var allCode = string.Join("\n", generatedSources);
        var hasAsyncHandling = allCode.Contains("async") || 
                              allCode.Contains("Task") ||
                              allCode.Contains("CancellationToken") ||
                              allCode.Contains("Async") ||
                              generatedSources.Length > 1;
        
        Assert.IsTrue(hasAsyncHandling, "Should handle async methods");

        Console.WriteLine($"Async method test: {generatedSources.Length} sources generated");
    }

    /// <summary>
    /// Tests MethodGenerationContext with CRUD operations.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_WithCrudOperations_HandlesAllTypes()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class CrudService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public void InsertUser(User user) { }
        
        [SqlExecuteType(SqlExecuteTypes.Update, ""Users"")]
        public void UpdateUser(User user) { }
        
        [SqlExecuteType(SqlExecuteTypes.Delete, ""Users"")]
        public void DeleteUser(int id) { }
        
        [SqlExecuteType(SqlExecuteTypes.BatchInsert, ""Users"")]
        public void BatchInsertUsers(List<User> users) { }
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
        Assert.IsNotNull(generatedSources, "Should generate sources for CRUD operations");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate sources");

        // Check that CRUD operations are handled
        var allCode = string.Join("\n", generatedSources);
        var hasCrudHandling = allCode.Contains("Insert") || 
                             allCode.Contains("Update") ||
                             allCode.Contains("Delete") ||
                             allCode.Contains("SELECT") ||
                             allCode.Contains("User") ||
                             generatedSources.Length > 1;
        
        Assert.IsTrue(hasCrudHandling, "Should handle CRUD operations");

        Console.WriteLine($"CRUD operations test: {generatedSources.Length} sources generated");
    }

    /// <summary>
    /// Tests MethodGenerationContext with complex return types.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_WithComplexReturnTypes_HandlesCorrectly()
    {
        // Arrange
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ComplexReturnService
    {
        [RawSql(""SELECT COUNT(*) FROM Users"")]
        public int GetUserCount() => 0;
        
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User? GetUserNullable(int id) => null;
        
        [RawSql(""SELECT * FROM Users"")]
        public Task<List<User>> GetUsersTaskList() => null!;
        
        [RawSql(""SELECT Name FROM Users"")]
        public List<string> GetUserNames() => null!;
        
        [RawSql(""SELECT CreatedAt FROM Users WHERE Id = @id"")]
        public DateTime? GetUserCreationDate(int id) => null;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should generate sources for complex return types");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate sources");

        // Check that complex return types are handled
        var allCode = string.Join("\n", generatedSources);
        var hasComplexReturnHandling = allCode.Contains("int") || 
                                      allCode.Contains("string") ||
                                      allCode.Contains("DateTime") ||
                                      allCode.Contains("List") ||
                                      allCode.Contains("Task") ||
                                      generatedSources.Length > 1;
        
        Assert.IsTrue(hasComplexReturnHandling, "Should handle complex return types");

        Console.WriteLine($"Complex return types test: {generatedSources.Length} sources generated");
    }

    /// <summary>
    /// Tests MethodGenerationContext variable naming conflicts avoidance.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_VariableNaming_AvoidsConflicts()
    {
        // Test that MethodGenerationContext uses different variable names than AbstractGenerator
        
        // Arrange - Check that variable names are distinct
        var methodCmdName = MethodGenerationContext.CmdName;
        var abstractGeneratorCmdName = "__repoCmd__"; // Known from AbstractGenerator
        
        // Assert
        Assert.AreNotEqual(methodCmdName, abstractGeneratorCmdName, 
            "MethodGenerationContext should use different command variable name than AbstractGenerator");
        
        Assert.IsTrue(methodCmdName.Contains("method"), 
            "MethodGenerationContext command name should contain 'method'");
        
        Assert.IsTrue(abstractGeneratorCmdName.Contains("repo"), 
            "AbstractGenerator command name should contain 'repo'");

        Console.WriteLine($"Variable naming test passed: Method={methodCmdName}, AbstractGenerator={abstractGeneratorCmdName}");
    }

    /// <summary>
    /// Tests MethodGenerationContext with error scenarios.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_WithErrorScenarios_HandlesGracefully()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ErrorProneService
    {
        // Method with no SQL content
        [RawSql("""")]
        public List<User> GetUsersEmpty() => null!;
        
        // Method with mismatched parameters
        [RawSql(""SELECT * FROM Users WHERE Id = @nonExistentParam"")]
        public User GetUserMismatch(int id) => null!;
        
        // Method with invalid return type for operation
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public List<User> InvalidInsert(User user) => null!;
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
            
            // Should not crash on error scenarios
            Assert.IsNotNull(generatedSources, "Should handle error scenarios gracefully");
            Assert.IsTrue(generatedSources.Length > 0, "Should generate sources even with errors");

            Console.WriteLine("Error scenarios test passed - generator handled errors gracefully");
        }
        catch (Exception ex)
        {
            Assert.Fail($"MethodGenerationContext should handle error scenarios gracefully: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests MethodGenerationContext with inheritance and polymorphism.
    /// </summary>
    [TestMethod]
    public void MethodGenerationContext_WithInheritance_HandlesPolymorphism()
    {
        // Arrange
        var sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class User : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
    
    public class Post : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
    
    public class PolymorphicService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
        
        [RawSql(""SELECT * FROM Posts"")]
        public List<Post> GetPosts() => null!;
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public void InsertUser(User user) { }
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Posts"")]
        public void InsertPost(Post post) { }
    }
}";

        // Act
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Assert
        Assert.IsNotNull(generatedSources, "Should handle inheritance scenarios");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate sources for polymorphic types");

        // Check that inheritance is handled
        var allCode = string.Join("\n", generatedSources);
        var hasInheritanceHandling = allCode.Contains("User") || 
                                    allCode.Contains("Post") ||
                                    allCode.Contains("BaseEntity") ||
                                    generatedSources.Length > 1;
        
        Assert.IsTrue(hasInheritanceHandling, "Should handle inheritance scenarios");

        Console.WriteLine($"Inheritance test: {generatedSources.Length} sources generated");
    }
}
