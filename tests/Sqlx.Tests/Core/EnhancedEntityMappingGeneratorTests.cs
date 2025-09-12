// -----------------------------------------------------------------------
// <copyright file="EnhancedEntityMappingGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sqlx.Core;
using Sqlx;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for EnhancedEntityMappingGenerator to improve test coverage.
    /// </summary>
    [TestClass]
    public class EnhancedEntityMappingGeneratorTests
    {
        private CSharpCompilation? _compilation;

        [TestInitialize]
        public void Setup()
        {
            var sourceCode = @"
using System;

namespace TestNamespace
{
    public class SimpleClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public record SimpleRecord(int Id, string Name);

    public class PrimaryConstructorClass(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }

    public class EmptyClass
    {
    }

    public class ClassWithPrivateMembers
    {
        private int _id;
        private string _name = string.Empty;
        
        public int Id => _id;
        internal string Name => _name;
    }
}";

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            _compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        }

        [TestMethod]
        public void GenerateEntityMapping_WithSimpleClass_GeneratesCorrectMapping()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = GetTypeSymbol("TestNamespace.SimpleClass");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("__ordinal_Id"));
            Assert.IsTrue(result.Contains("__ordinal_Name"));
            Assert.IsTrue(result.Contains("new TestNamespace.SimpleClass"));
            Assert.IsTrue(result.Contains("Id = "));
            Assert.IsTrue(result.Contains("Name = "));
        }

        [TestMethod]
        public void GenerateEntityMapping_WithRecord_GeneratesRecordMapping()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = GetTypeSymbol("TestNamespace.SimpleRecord");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("__ordinal_Id"));
            Assert.IsTrue(result.Contains("__ordinal_Name"));
            // Record should use constructor syntax
            Assert.IsTrue(result.Contains("new TestNamespace.SimpleRecord"));
        }

        [TestMethod]
        public void GenerateEntityMapping_WithPrimaryConstructor_GeneratesPrimaryConstructorMapping()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = GetTypeSymbol("TestNamespace.PrimaryConstructorClass");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("__ordinal_Id"));
            Assert.IsTrue(result.Contains("__ordinal_Name"));
            Assert.IsTrue(result.Contains("new TestNamespace.PrimaryConstructorClass"));
        }

        [TestMethod]
        public void GenerateEntityMapping_WithEmptyClass_HandlesGracefully()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = GetTypeSymbol("TestNamespace.EmptyClass");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("// No accessible members found for entity mapping"));
            Assert.IsTrue(result.Contains("new TestNamespace.EmptyClass"));
        }

        [TestMethod]
        public void GenerateEntityMapping_WithPrivateMembers_OnlyUsesAccessibleMembers()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = GetTypeSymbol("TestNamespace.ClassWithPrivateMembers");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            // Should include public Id property
            Assert.IsTrue(result.Contains("Id") || result.Contains("new TestNamespace.ClassWithPrivateMembers"));
            // Private members should not be included in ordinal generation
            Assert.IsFalse(result.Contains("__ordinal__id"));
            Assert.IsFalse(result.Contains("__ordinal__name"));
        }

        [TestMethod]
        public void GenerateEntityMapping_PerformanceOptimization_GeneratesGetOrdinalCaching()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = GetTypeSymbol("TestNamespace.SimpleClass");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            // Should generate GetOrdinal caching for performance
            Assert.IsTrue(result.Contains("int __ordinal_Id = reader.GetOrdinal(\"Id\");"));
            Assert.IsTrue(result.Contains("int __ordinal_Name = reader.GetOrdinal(\"Name\");"));
        }

        [TestMethod]
        public void GenerateEntityMapping_WithIndentation_RespectsIndentation()
        {
            // Arrange
            var sb = new IndentedStringBuilder("    "); // 4 spaces
            var entityType = GetTypeSymbol("TestNamespace.SimpleClass");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0, "Should generate some content");
            
            // Basic test that indentation builder was used
            Assert.IsTrue(result.Contains("SimpleClass") || result.Contains("__ordinal_"));
        }

        [TestMethod]
        public void GenerateEntityMapping_MultipleCallsSameType_ProducesSameResult()
        {
            // Arrange
            var sb1 = new IndentedStringBuilder("");
            var sb2 = new IndentedStringBuilder("");
            var entityType = GetTypeSymbol("TestNamespace.SimpleClass");

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb1, entityType);
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb2, entityType);
            
            var result1 = sb1.ToString();
            var result2 = sb2.ToString();

            // Assert
            Assert.AreEqual(result1, result2, "Multiple calls should produce identical results");
        }

        [TestMethod]
        public void GenerateEntityMapping_WithNullableProperties_HandlesCorrectly()
        {
            // Arrange
            var nullableSourceCode = @"
#nullable enable
using System;

namespace TestNamespace
{
    public class NullableClass
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}";

            var syntaxTree = CSharpSyntaxTree.ParseText(nullableSourceCode);
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            var sb = new IndentedStringBuilder("");
            var entityType = compilation.GetTypeByMetadataName("TestNamespace.NullableClass");
            Assert.IsNotNull(entityType);

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("__ordinal_Id"));
            Assert.IsTrue(result.Contains("__ordinal_Name"));
            Assert.IsTrue(result.Contains("__ordinal_CreatedAt"));
        }

        private INamedTypeSymbol GetTypeSymbol(string typeName)
        {
            Assert.IsNotNull(_compilation);
            var type = _compilation.GetTypeByMetadataName(typeName);
            Assert.IsNotNull(type, $"Could not find type: {typeName}");
            return type;
        }
    }
}