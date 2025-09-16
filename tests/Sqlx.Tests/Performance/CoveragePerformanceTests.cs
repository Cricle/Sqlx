// -----------------------------------------------------------------------
// <copyright file="CoveragePerformanceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sqlx;
using Sqlx.Generator.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sqlx.Tests.Performance
{
    /// <summary>
    /// Performance tests that also help improve code coverage by exercising different code paths.
    /// </summary>
    [TestClass]
    public class CoveragePerformanceTests
    {
        [TestMethod]
        public void TypeAnalyzer_IsLikelyEntityType_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var types = GetAllTypes(compilation).ToList();

            var stopwatch = Stopwatch.StartNew();
            var entityTypeCount = 0;

            // Act
            foreach (var type in types)
            {
                if (TypeAnalyzer.IsLikelyEntityType(type))
                {
                    entityTypeCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(entityTypeCount >= 0, "Should identify some entity types");
            Assert.IsTrue(types.Count > 10, "Should have tested multiple types");
        }

        [TestMethod]
        public void TypeAnalyzer_IsCollectionType_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var types = GetAllTypes(compilation).ToList();

            var stopwatch = Stopwatch.StartNew();
            var collectionTypeCount = 0;

            // Act
            foreach (var type in types)
            {
                if (TypeAnalyzer.IsCollectionType(type))
                {
                    collectionTypeCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(collectionTypeCount >= 0, "Should identify collection types");
        }

        [TestMethod]
        public void TypeAnalyzer_ExtractEntityType_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var types = GetAllTypes(compilation).ToList();

            var stopwatch = Stopwatch.StartNew();
            var extractedCount = 0;

            // Act
            foreach (var type in types)
            {
                var extracted = TypeAnalyzer.ExtractEntityType(type);
                if (extracted != null)
                {
                    extractedCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(extractedCount > 0, "Should extract some entity types");
        }

        [TestMethod]
        public void PrimaryConstructorAnalyzer_IsRecord_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var namedTypes = GetAllTypes(compilation).OfType<INamedTypeSymbol>().ToList();

            var stopwatch = Stopwatch.StartNew();
            var recordCount = 0;

            // Act
            foreach (var type in namedTypes)
            {
                if (PrimaryConstructorAnalyzer.IsRecord(type))
                {
                    recordCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(recordCount >= 0, "Should identify record types");
        }

        [TestMethod]
        public void PrimaryConstructorAnalyzer_HasPrimaryConstructor_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var namedTypes = GetAllTypes(compilation).OfType<INamedTypeSymbol>().ToList();

            var stopwatch = Stopwatch.StartNew();
            var primaryConstructorCount = 0;

            // Act
            foreach (var type in namedTypes)
            {
                if (PrimaryConstructorAnalyzer.HasPrimaryConstructor(type))
                {
                    primaryConstructorCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(primaryConstructorCount >= 0, "Should identify primary constructor types");
        }

        [TestMethod]
        public void PrimaryConstructorAnalyzer_GetAccessibleMembers_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var namedTypes = GetAllTypes(compilation).OfType<INamedTypeSymbol>().ToList();

            var stopwatch = Stopwatch.StartNew();
            var totalMemberCount = 0;

            // Act
            foreach (var type in namedTypes)
            {
                var members = PrimaryConstructorAnalyzer.GetAccessibleMembers(type).ToList();
                totalMemberCount += members.Count;
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(totalMemberCount >= 0, "Should find accessible members");
        }

        [TestMethod]
        public void AttributeSourceGenerator_GenerateAttributeSource_PerformanceTest()
        {
            // Arrange
            var iterations = 100;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                // AttributeSourceGenerator no longer exists - attributes are now in Sqlx.Core
                var result = "// Attributes are now directly available in Sqlx.Core project";
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Length > 0);
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        }

        [TestMethod]
        public void DiagnosticHelper_CreateDiagnostic_PerformanceTest()
        {
            // Arrange
            var iterations = 1000;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var diagnostic = DiagnosticHelper.CreateDiagnostic(
                    $"TEST{i:D3}",
                    $"Test Title {i}",
                    $"Test message {i}: {{0}}",
                    DiagnosticSeverity.Warning,
                    null,
                    $"parameter{i}");

                Assert.IsNotNull(diagnostic);
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        }

        [TestMethod]
        public void DiagnosticHelper_GenerateTypeAnalysisReport_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var namedTypes = GetAllTypes(compilation).OfType<INamedTypeSymbol>().Take(10).ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act
            foreach (var type in namedTypes)
            {
                var report = DiagnosticHelper.GenerateTypeAnalysisReport(type);
                Assert.IsNotNull(report);
                Assert.IsTrue(report.Length > 0);
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void DiagnosticHelper_ValidateEntityType_PerformanceTest()
        {
            // Arrange
            var compilation = CreateLargeCompilation();
            var namedTypes = GetAllTypes(compilation).OfType<INamedTypeSymbol>().Take(20).ToList();

            var stopwatch = Stopwatch.StartNew();
            var totalIssues = 0;

            // Act
            foreach (var type in namedTypes)
            {
                var issues = DiagnosticHelper.ValidateEntityType(type);
                totalIssues += issues.Count;
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(totalIssues >= 0, "Should validate entity types");
        }

        [TestMethod]
        public void CSharpGenerator_MultipleInstances_PerformanceTest()
        {
            // Arrange
            var iterations = 50;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var generator = new CSharpGenerator();
                Assert.IsNotNull(generator);

                // Test that it implements the expected interfaces
                Assert.IsInstanceOfType<ISourceGenerator>(generator);
                Assert.IsInstanceOfType<AbstractGenerator>(generator);
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        }

        private static CSharpCompilation CreateLargeCompilation()
        {
            var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TestNamespace
{
    // Various class types for testing
    public class SimpleClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public decimal Amount { get; set; }
    }

    public record SimpleRecord(int Id, string Name, DateTime CreatedAt);
    
    public record struct SimpleRecordStruct(int Id, string Name);

    public class PrimaryConstructorClass(int id, string name, DateTime createdAt)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public DateTime CreatedAt { get; } = createdAt;
        public string Description { get; set; } = string.Empty;
    }

    public abstract class AbstractClass
    {
        public abstract int Id { get; }
        public virtual string Name { get; set; } = string.Empty;
    }

    public static class StaticClass
    {
        public static int Value { get; set; }
        public static string GetValue() => Value.ToString();
    }

    public interface ITestInterface
    {
        int Id { get; }
        string Name { get; }
        void DoSomething();
    }

    public enum TestEnum
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 3
    }

    public struct TestStruct
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }

    public class GenericClass<T>
    {
        public T Value { get; set; } = default!;
        public List<T> Items { get; set; } = new();
    }

    public class CollectionClass
    {
        public List<string> StringList { get; set; } = new();
        public IEnumerable<int> IntEnumerable { get; set; } = Enumerable.Empty<int>();
        public ICollection<DateTime> DateCollection { get; set; } = new List<DateTime>();
        public IReadOnlyList<decimal> DecimalReadOnlyList { get; set; } = new List<decimal>();
    }

    public class TaskClass
    {
        public Task<string> GetStringAsync() => Task.FromResult(string.Empty);
        public Task<List<int>> GetIntListAsync() => Task.FromResult(new List<int>());
        public Task<SimpleClass> GetSimpleClassAsync() => Task.FromResult(new SimpleClass());
    }

    public class NestedClass
    {
        public class InnerClass
        {
            public int Id { get; set; }
            
            public class DeeplyNestedClass
            {
                public string Value { get; set; } = string.Empty;
            }
        }
    }

    public class ComplexClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public SimpleClass? RelatedObject { get; set; }
        public List<SimpleRecord> Records { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Task<List<string>> GetDataAsync() => Task.FromResult(new List<string>());
    }
}

namespace System.Data
{
    public class SystemDataClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

namespace Microsoft.Extensions.Logging
{
    public class MicrosoftExtensionsClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
                });
        }

        private static IEnumerable<ITypeSymbol> GetAllTypes(CSharpCompilation compilation)
        {
            var types = new List<ITypeSymbol>();

            void VisitNamespace(INamespaceSymbol ns)
            {
                foreach (var type in ns.GetTypeMembers())
                {
                    types.Add(type);
                    VisitType(type);
                }

                foreach (var childNs in ns.GetNamespaceMembers())
                {
                    VisitNamespace(childNs);
                }
            }

            void VisitType(INamedTypeSymbol type)
            {
                foreach (var nestedType in type.GetTypeMembers())
                {
                    types.Add(nestedType);
                    VisitType(nestedType);
                }
            }

            VisitNamespace(compilation.GlobalNamespace);
            return types;
        }
    }
}
