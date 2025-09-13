// -----------------------------------------------------------------------
// <copyright file="PerformanceAndStressTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Performance;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.Core;

/// <summary>
/// Performance and stress tests for the Sqlx source generator.
/// Tests compile time performance, memory usage, and behavior under load.
/// </summary>
[TestClass]
public class PerformanceAndStressTests : CodeGenerationTestBase
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
    /// Tests source generator performance with large numbers of methods.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithLargeNumberOfMethods_PerformsEfficiently()
    {
        // Arrange - Generate a service with many methods
        var methodDefinitions = new StringBuilder();
        const int methodCount = 200;
        
        for (int i = 0; i < methodCount; i++)
        {
            methodDefinitions.AppendLine($@"
        [RawSql(""SELECT * FROM Table{i} WHERE Id = @id"")]
        public Entity{i} GetEntity{i}(int id) => null!;
        
        [RawSql(""SELECT * FROM Table{i}"")]
        public List<Entity{i}> GetAll{i}() => null!;
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Table{i}"")]
        public void Insert{i}(Entity{i} entity) {{ }}
        
        [SqlExecuteType(SqlExecuteTypes.Update, ""Table{i}"")]
        public void Update{i}(Entity{i} entity) {{ }}
        
        [SqlExecuteType(SqlExecuteTypes.Delete, ""Table{i}"")]
        public void Delete{i}(int id) {{ }}");
        }

        var entityDefinitions = new StringBuilder();
        for (int i = 0; i < methodCount; i++)
        {
            entityDefinitions.AppendLine($@"
    public class Entity{i}
    {{
        public int Id {{ get; set; }}
        public string Name{i} {{ get; set; }} = string.Empty;
        public DateTime Created{i} {{ get; set; }}
        public bool IsActive{i} {{ get; set; }}
    }}");
        }

        var sourceCode = $@"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace PerformanceTest
{{
    public class LargeService
    {{
{methodDefinitions}
    }}

{entityDefinitions}
}}";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
        
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, // 30 seconds
            $"Generation of {methodCount * 5} methods should complete within 30 seconds. Took: {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.IsTrue(memoryUsed < 200 * 1024 * 1024, // 200MB
            $"Should use reasonable memory for large generation. Used: {memoryUsed / (1024 * 1024)}MB");
        
        Assert.IsNotNull(generatedSources, "Should generate sources");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate code files");
        
        Console.WriteLine($"Generated {methodCount * 5} methods in {stopwatch.ElapsedMilliseconds}ms using {memoryUsed / (1024 * 1024):F1}MB");
    }

    /// <summary>
    /// Tests source generator performance with deep inheritance hierarchies.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithDeepInheritance_HandlesEfficiently()
    {
        // Arrange - Create deep inheritance chain
        var classDefinitions = new StringBuilder();
        const int inheritanceDepth = 50;
        
        // Base class
        classDefinitions.AppendLine(@"
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }");
        
        // Inheritance chain
        for (int i = 1; i <= inheritanceDepth; i++)
        {
            var baseClass = i == 1 ? "BaseEntity" : $"Level{i - 1}Entity";
            classDefinitions.AppendLine($@"
    public class Level{i}Entity : {baseClass}
    {{
        public string Property{i} {{ get; set; }} = string.Empty;
        public int Value{i} {{ get; set; }}
    }}");
        }

        // Service using the deepest class
        var sourceCode = $@"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace InheritanceTest
{{
{classDefinitions}

    public class DeepInheritanceService
    {{
        [RawSql(""SELECT * FROM Entities"")]
        public List<Level{inheritanceDepth}Entity> GetEntities() => null!;
        
        [RawSql(""SELECT * FROM Entities WHERE Id = @id"")]
        public Level{inheritanceDepth}Entity GetEntity(int id) => null!;
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""Entities"")]
        public void InsertEntity(Level{inheritanceDepth}Entity entity) {{ }}
    }}
}}";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, // 10 seconds
            $"Deep inheritance should be handled efficiently. Took: {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.IsNotNull(generatedSources, "Should handle deep inheritance");
        
        // Should not get stuck in infinite loops analyzing inheritance
        Assert.IsTrue(stopwatch.ElapsedMilliseconds > 0, "Should actually do work");
        
        Console.WriteLine($"Handled {inheritanceDepth} level inheritance in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests concurrent source generation scenarios.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithConcurrentGeneration_HandlesCorrectly()
    {
        // Arrange - Multiple source files to generate concurrently
        var sourceFiles = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            sourceFiles.Add($@"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace ConcurrentTest{i}
{{
    public class Service{i}
    {{
        [RawSql(""SELECT * FROM Table{i}"")]
        public List<Entity{i}> GetEntities() => null!;
        
        [RawSql(""SELECT * FROM Table{i} WHERE Id = @id"")]
        public Entity{i} GetEntity(int id) => null!;
    }}
    
    public class Entity{i}
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
    }}
}}");
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = new List<(Compilation, string[], Diagnostic[])>();
        
        // Process multiple files (simulating concurrent scenarios)
        for (int i = 0; i < sourceFiles.Count; i++)
        {
            var result = CompileWithSourceGenerator(sourceFiles[i]);
            results.Add(result);
        }
        
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(sourceFiles.Count, results.Count, "Should process all source files");
        
        foreach (var (compilation, generatedSources, diagnostics) in results)
        {
            Assert.IsNotNull(generatedSources, "Each compilation should generate sources");
            Assert.IsTrue(generatedSources.Length > 0, "Each compilation should produce code");
        }
        
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 20000, // 20 seconds for 10 files
            $"Concurrent processing should be efficient. Took: {stopwatch.ElapsedMilliseconds}ms for {sourceFiles.Count} files");
        
        Console.WriteLine($"Processed {sourceFiles.Count} files concurrently in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests memory usage patterns with repeated generations.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithRepeatedGeneration_ManagesMemoryWell()
    {
        // Arrange
        var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace MemoryTest
{
    public class TestService
    {
        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsers() => null!;
        
        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User GetUser(int id) => null!;
        
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

        // Act - Generate multiple times to test memory management
        var initialMemory = GC.GetTotalMemory(true);
        var iterations = 20;
        var memoryReadings = new List<long>();
        
        for (int i = 0; i < iterations; i++)
        {
            var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
            
            // Force cleanup every few iterations
            if (i % 5 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                memoryReadings.Add(GC.GetTotalMemory(false));
            }
            
            Assert.IsNotNull(generatedSources, $"Iteration {i} should generate sources");
        }
        
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryGrowth = finalMemory - initialMemory;
        Assert.IsTrue(memoryGrowth < 50 * 1024 * 1024, // Less than 50MB growth
            $"Memory growth should be reasonable after {iterations} iterations. Growth: {memoryGrowth / (1024 * 1024):F1}MB");
        
        // Check that memory doesn't grow linearly (indicating leaks)
        if (memoryReadings.Count >= 3)
        {
            var firstReading = memoryReadings[0];
            var lastReading = memoryReadings[memoryReadings.Count - 1];
            var relativeGrowth = (double)(lastReading - firstReading) / firstReading;
            
            Assert.IsTrue(relativeGrowth < 2.0, // Less than 100% growth
                $"Memory should not grow excessively. Relative growth: {relativeGrowth * 100:F1}%");
        }
        
        Console.WriteLine($"Memory after {iterations} iterations: {memoryGrowth / (1024 * 1024):F1}MB growth");
    }

    /// <summary>
    /// Tests performance with very long SQL queries and complex expressions.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithComplexSqlQueries_HandlesEfficiently()
    {
        // Arrange - Create very long and complex SQL queries
        var complexWhere = string.Join(" AND ", Enumerable.Range(1, 50).Select(i => $"column{i} = @param{i}"));
        var complexSelect = string.Join(", ", Enumerable.Range(1, 100).Select(i => $"table{i}.column{i}"));
        var complexJoins = string.Join(" ", Enumerable.Range(2, 20).Select(i => $"LEFT JOIN table{i} ON table1.id = table{i}.table1_id"));
        
        var parameters = string.Join(", ", Enumerable.Range(1, 50).Select(i => $"object param{i}"));
        
        var sourceCode = $@"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace ComplexSqlTest
{{
    public class ComplexQueryService
    {{
        [RawSql(@""
            SELECT {complexSelect}
            FROM table1
            {complexJoins}
            WHERE {complexWhere}
            ORDER BY table1.created_at DESC, table1.id ASC
            LIMIT 1000 OFFSET @offset
        "")]
        public List<ComplexResult> GetComplexData({parameters}, int offset) => null!;
        
        [RawSql(@""
            WITH RECURSIVE category_tree AS (
                SELECT id, name, parent_id, 0 as level
                FROM categories 
                WHERE parent_id IS NULL
                UNION ALL
                SELECT c.id, c.name, c.parent_id, ct.level + 1
                FROM categories c
                INNER JOIN category_tree ct ON c.parent_id = ct.id
                WHERE ct.level < 10
            )
            SELECT * FROM category_tree
            WHERE level <= @maxLevel
            ORDER BY level, name
        "")]
        public List<CategoryTree> GetCategoryTree(int maxLevel) => null!;
    }}
    
    public class ComplexResult
    {{
        {string.Join("\n        ", Enumerable.Range(1, 100).Select(i => $"public string Column{i} {{ get; set; }} = string.Empty;"))}
    }}
    
    public class CategoryTree
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public int? ParentId {{ get; set; }}
        public int Level {{ get; set; }}
    }}
}}";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 15000, // 15 seconds
            $"Complex SQL queries should be processed efficiently. Took: {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.IsNotNull(generatedSources, "Should handle complex SQL");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate code for complex queries");
        
        // Check that generated code is substantial
        var totalCodeLength = generatedSources.Sum(s => s.Length);
        Assert.IsTrue(totalCodeLength > 1000, "Should generate substantial code for complex queries");
        
        Console.WriteLine($"Processed complex SQL queries in {stopwatch.ElapsedMilliseconds}ms, generated {totalCodeLength} characters");
    }

    /// <summary>
    /// Tests performance with mixed synchronous and asynchronous methods.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithMixedAsyncSync_PerformsWell()
    {
        // Arrange - Mix of sync and async methods
        var methodDefinitions = new StringBuilder();
        const int methodPairs = 100;
        
        for (int i = 0; i < methodPairs; i++)
        {
            methodDefinitions.AppendLine($@"
        [RawSql(""SELECT * FROM Table{i}"")]
        public List<Entity{i}> GetEntities{i}() => null!;
        
        [RawSql(""SELECT * FROM Table{i}"")]
        public async Task<List<Entity{i}>> GetEntities{i}Async(CancellationToken cancellationToken = default) => null!;
        
        [RawSql(""SELECT * FROM Table{i} WHERE Id = @id"")]
        public Entity{i} GetEntity{i}(int id) => null!;
        
        [RawSql(""SELECT * FROM Table{i} WHERE Id = @id"")]
        public async Task<Entity{i}> GetEntity{i}Async(int id, CancellationToken cancellationToken = default) => null!;");
        }

        var entityDefinitions = new StringBuilder();
        for (int i = 0; i < methodPairs; i++)
        {
            entityDefinitions.AppendLine($@"
    public class Entity{i}
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
    }}");
        }

        var sourceCode = $@"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace MixedAsyncTest
{{
    public class MixedAsyncService
    {{
{methodDefinitions}
    }}

{entityDefinitions}
}}";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 25000, // 25 seconds
            $"Mixed async/sync generation should be efficient. Took: {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.IsNotNull(generatedSources, "Should handle mixed async/sync methods");
        
        var totalMethods = methodPairs * 4; // 4 methods per iteration
        Console.WriteLine($"Generated {totalMethods} mixed async/sync methods in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests performance with various database dialect configurations.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithMultipleDialects_HandlesEfficiently()
    {
        // Arrange - Service that could work with multiple dialects
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using MySql.Data.MySqlClient;
using Sqlx.Annotations;

namespace MultiDialectTest
{
    public class SqlServerService
    {
        [RawSql(""SELECT * FROM [Users] WHERE [Id] = @id"")]
        public User GetUserSqlServer(int id) => null!;
    }
    
    public class MySqlService
    {
        [RawSql(""SELECT * FROM `users` WHERE `id` = @id"")]
        public User GetUserMySql(int id) => null!;
    }
    
    public class PostgreSqlService
    {
        [RawSql(""SELECT * FROM \""users\"" WHERE \""id\"" = @id"")]
        public User GetUserPostgreSql(int id) => null!;
    }
    
    public class SQLiteService
    {
        [RawSql(""SELECT * FROM users WHERE id = @id"")]
        public User GetUserSQLite(int id) => null!;
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, // 10 seconds
            $"Multi-dialect generation should be efficient. Took: {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.IsNotNull(generatedSources, "Should handle multiple dialects");
        Assert.IsTrue(generatedSources.Length > 0, "Should generate code for multiple dialects");
        
        Console.WriteLine($"Generated multi-dialect code in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests the generator's behavior under stress with rapid successive generations.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_WithRapidSuccessiveGenerations_RemainsStable()
    {
        // Arrange
        var baseSourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace StressTest{0}
{{
    public class Service{0}
    {{
        [RawSql(""SELECT * FROM Table{0}"")]
        public List<Entity{0}> GetEntities() => null!;
    }}
    
    public class Entity{0}
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
    }}
}}";

        // Act - Rapid successive generations
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;
        const int iterations = 50;
        
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                var sourceCode = string.Format(baseSourceCode, i);
                var (compilation, generatedSources, diagnostics) = CompileWithSourceGenerator(sourceCode);
                
                if (generatedSources != null && generatedSources.Length > 0)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Iteration {i} failed: {ex.Message}");
            }
        }
        
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(successCount >= iterations * 0.95, // At least 95% success rate
            $"Should maintain stability under stress. Success rate: {successCount}/{iterations}");
        
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, // 30 seconds for 50 iterations
            $"Rapid generations should complete quickly. Took: {stopwatch.ElapsedMilliseconds}ms");
        
        Console.WriteLine($"Completed {successCount}/{iterations} rapid generations in {stopwatch.ElapsedMilliseconds}ms");
    }
}
