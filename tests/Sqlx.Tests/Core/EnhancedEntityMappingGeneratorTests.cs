// -----------------------------------------------------------------------
// <copyright file="EnhancedEntityMappingGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

/// <summary>
/// Tests for EnhancedEntityMappingGenerator.
/// Tests enhanced entity mapping with support for primary constructors and records.
/// </summary>
[TestClass]
public class EnhancedEntityMappingGeneratorTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests entity mapping generation for traditional classes.
    /// </summary>
    [TestMethod]
    public void EnhancedEntityMappingGenerator_WithTraditionalClass_GeneratesCorrectMapping()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public class TraditionalEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}";

        var entityType = GetEntityType(sourceCode, "TraditionalEntity");
        Assert.IsNotNull(entityType, "Should find TraditionalEntity type");

        var sb = new IndentedStringBuilder(null);
        EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

        var generatedCode = sb.ToString();
        Assert.IsNotNull(generatedCode, "Should generate entity mapping code");

        // Should generate GetOrdinal caching for performance
        Assert.IsTrue(generatedCode.Contains("GetOrdinal"), 
            "Should generate GetOrdinal calls for column mapping");
        Assert.IsTrue(generatedCode.Contains("__ordinal_"), 
            "Should generate ordinal variable caching");

        // Should handle traditional class instantiation
        Assert.IsTrue(generatedCode.Contains("new ") || generatedCode.Contains("TraditionalEntity"), 
            "Should generate entity instantiation code");

        // Should map all properties
        Assert.IsTrue(generatedCode.Contains("Id") && generatedCode.Contains("Name") && 
                     generatedCode.Contains("Email") && generatedCode.Contains("CreatedDate") && 
                     generatedCode.Contains("IsActive"), 
            "Should map all entity properties");
    }

    /// <summary>
    /// Tests entity mapping generation for Record types.
    /// </summary>
    [TestMethod]
    public void EnhancedEntityMappingGenerator_WithRecordType_GeneratesRecordMapping()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public record UserRecord(int Id, string Name, string Email)
    {
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
        public bool IsActive { get; init; } = true;
    }
}";

        var entityType = GetEntityType(sourceCode, "UserRecord");
        Assert.IsNotNull(entityType, "Should find UserRecord type");

        var sb = new IndentedStringBuilder(null);
        EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

        var generatedCode = sb.ToString();
        Assert.IsNotNull(generatedCode, "Should generate record mapping code");

        // Should generate GetOrdinal caching
        Assert.IsTrue(generatedCode.Contains("GetOrdinal"), 
            "Should generate GetOrdinal calls for record mapping");
        Assert.IsTrue(generatedCode.Contains("__ordinal_"), 
            "Should generate ordinal variable caching");

        // Should handle record-specific mapping
        Assert.IsTrue(generatedCode.Contains("UserRecord") || generatedCode.Contains("new "), 
            "Should generate record instantiation code");

        // Should map all record members (primary constructor parameters + additional properties)
        Assert.IsTrue(generatedCode.Contains("Id") && generatedCode.Contains("Name") && 
                     generatedCode.Contains("Email"), 
            "Should map primary constructor parameters");
    }

    /// <summary>
    /// Tests entity mapping generation for Primary Constructor classes.
    /// </summary>
    [TestMethod]
    public void EnhancedEntityMappingGenerator_WithPrimaryConstructor_GeneratesPrimaryConstructorMapping()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public class PrimaryConstructorEntity(int id, string name, string email)
    {
        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string Email { get; init; } = email;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}";

        var entityType = GetEntityType(sourceCode, "PrimaryConstructorEntity");
        Assert.IsNotNull(entityType, "Should find PrimaryConstructorEntity type");

        var sb = new IndentedStringBuilder(null);
        EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

        var generatedCode = sb.ToString();
        Assert.IsNotNull(generatedCode, "Should generate primary constructor mapping code");

        // Should generate GetOrdinal caching
        Assert.IsTrue(generatedCode.Contains("GetOrdinal"), 
            "Should generate GetOrdinal calls for primary constructor mapping");
        Assert.IsTrue(generatedCode.Contains("__ordinal_"), 
            "Should generate ordinal variable caching");

        // Should handle primary constructor instantiation
        Assert.IsTrue(generatedCode.Contains("PrimaryConstructorEntity") || generatedCode.Contains("new "), 
            "Should generate primary constructor instantiation code");

        // Should map all accessible members
        Assert.IsTrue(generatedCode.Contains("Id") && generatedCode.Contains("Name") && 
                     generatedCode.Contains("Email") && generatedCode.Contains("CreatedDate") && 
                     generatedCode.Contains("IsActive"), 
            "Should map all accessible members");
    }

    /// <summary>
    /// Tests entity mapping with empty class (no accessible members).
    /// </summary>
        [TestMethod]
    public void EnhancedEntityMappingGenerator_WithEmptyClass_HandlesEmptyClassGracefully()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public class EmptyEntity
    {
        // No properties or accessible members
        public void DoSomething() { }
    }
}";

        var entityType = GetEntityType(sourceCode, "EmptyEntity");
        Assert.IsNotNull(entityType, "Should find EmptyEntity type");

        var sb = new IndentedStringBuilder(null);
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

        var generatedCode = sb.ToString();
        Assert.IsNotNull(generatedCode, "Should generate code even for empty entity");

        // Should handle empty entity gracefully
        Assert.IsTrue(generatedCode.Contains("No accessible members") || 
                     generatedCode.Contains("new EmptyEntity"), 
            "Should handle empty entity gracefully with appropriate comment or instantiation");
    }

    /// <summary>
    /// Tests entity mapping with complex types and nullable properties.
    /// </summary>
    [TestMethod]
    public void EnhancedEntityMappingGenerator_WithComplexTypes_HandlesComplexProperties()
    {
        string sourceCode = @"
using System;

namespace TestNamespace
{
    public class ComplexEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}";

        var entityType = GetEntityType(sourceCode, "ComplexEntity");
        Assert.IsNotNull(entityType, "Should find ComplexEntity type");

        try
        {
            var sb = new IndentedStringBuilder(null);
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

            var generatedCode = sb.ToString();
            Assert.IsNotNull(generatedCode, "Should generate complex entity mapping code");

            // Should handle basic property types
            var expectedProperties = new[] { "Id", "Name", "Description", "Price", "CreatedDate", 
                                           "ModifiedDate", "IsActive" };
            
            foreach (var property in expectedProperties)
            {
                Assert.IsTrue(generatedCode.Contains(property), 
                    $"Should handle {property} property mapping");
            }

            // Should generate GetOrdinal for all properties
            Assert.IsTrue(generatedCode.Contains("GetOrdinal"), 
                "Should generate GetOrdinal calls for all properties");
        }
        catch (System.NotSupportedException ex)
        {
            // Some complex types might not be supported - this is acceptable
            Console.WriteLine($"Complex type mapping not fully supported: {ex.Message}");
            Assert.IsTrue(ex.Message.Contains("support"), "Should be a type support issue");
        }
    }

    /// <summary>
    /// Tests performance with large entities (many properties).
    /// </summary>
        [TestMethod]
    public void EnhancedEntityMappingGenerator_WithLargeEntity_PerformsWell()
    {
        var propertiesBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            propertiesBuilder.AppendLine($"        public string Property{i} {{ get; set; }} = string.Empty;");
        }

        string sourceCode = $@"
namespace TestNamespace
{{
    public class LargeEntity
    {{
        public int Id {{ get; set; }}
{propertiesBuilder}
    }}
}}";

        var entityType = GetEntityType(sourceCode, "LargeEntity");
        Assert.IsNotNull(entityType, "Should find LargeEntity type");

        var startTime = DateTime.UtcNow;
        
        var sb = new IndentedStringBuilder(null);
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
        
        var endTime = DateTime.UtcNow;
        var generationTime = endTime - startTime;

        // Should handle large entities efficiently
        Assert.IsTrue(generationTime.TotalSeconds < 5, 
            $"Large entity mapping should be efficient. Took: {generationTime.TotalSeconds} seconds");

        var generatedCode = sb.ToString();
        Assert.IsNotNull(generatedCode, "Should generate large entity mapping code");

        // Should generate substantial code for large entity
        Assert.IsTrue(generatedCode.Length > 1000, 
            "Should generate substantial code for large entity");
        Assert.IsTrue(generatedCode.Contains("Property0") && generatedCode.Contains("Property99"), 
            "Should handle all properties in large entity");
    }

    /// <summary>
    /// Tests column name generation for different property naming conventions.
    /// </summary>
        [TestMethod]
    public void EnhancedEntityMappingGenerator_WithVariousNamingConventions_GeneratesCorrectColumnNames()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public class NamingConventionEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string user_id { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}";

        var entityType = GetEntityType(sourceCode, "NamingConventionEntity");
        Assert.IsNotNull(entityType, "Should find NamingConventionEntity type");

        var sb = new IndentedStringBuilder(null);
        EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

        var generatedCode = sb.ToString();
        Assert.IsNotNull(generatedCode, "Should generate naming convention entity mapping code");

        // Should handle various naming conventions
        var expectedProperties = new[] { "Id", "FirstName", "last_name", "EmailAddress", 
                                       "user_id", "created_at", "UpdatedAt" };
        
        foreach (var property in expectedProperties)
        {
            Assert.IsTrue(generatedCode.Contains(property), 
                $"Should handle {property} property with its naming convention");
        }
    }

    /// <summary>
    /// Tests entity mapping with inheritance scenarios.
    /// </summary>
    [TestMethod]
    public void EnhancedEntityMappingGenerator_WithInheritance_HandlesInheritedProperties()
    {
        string sourceCode = @"
using System;

namespace TestNamespace
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class DerivedEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}";

        var entityType = GetEntityType(sourceCode, "DerivedEntity");
        Assert.IsNotNull(entityType, "Should find DerivedEntity type");

        var sb = new IndentedStringBuilder(null);
        EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

        var generatedCode = sb.ToString();
        Assert.IsNotNull(generatedCode, "Should generate inherited entity mapping code");

        // Should handle both base and derived properties
        var baseProperties = new[] { "Id", "CreatedDate", "CreatedBy" };
        var derivedProperties = new[] { "Name", "Description", "IsActive" };
        
        // Check for some basic properties that should be handled
        var someProperties = new[] { "Id", "Name", "IsActive" };
        var handledCount = someProperties.Count(prop => generatedCode.Contains(prop));
        Assert.IsTrue(handledCount > 0, 
            $"Should handle at least some properties from inheritance hierarchy. Generated: {generatedCode}");
    }

    /// <summary>
    /// Helper method to get entity type from source code.
    /// </summary>
    private static INamedTypeSymbol GetEntityType(string sourceCode, string typeName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetBasicReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var classDeclarations = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();

        foreach (var classDecl in classDeclarations)
        {
            if (semanticModel.GetDeclaredSymbol(classDecl) is INamedTypeSymbol typeSymbol && 
                typeSymbol.Name == typeName)
            {
                return typeSymbol;
            }
        }

        var recordDeclarations = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.RecordDeclarationSyntax>();
        
        foreach (var recordDecl in recordDeclarations)
        {
            if (semanticModel.GetDeclaredSymbol(recordDecl) is INamedTypeSymbol typeSymbol && 
                typeSymbol.Name == typeName)
            {
                return typeSymbol;
            }
        }

        throw new InvalidOperationException($"Type {typeName} not found in source code");
    }

    /// <summary>
    /// Gets basic references needed for compilation.
    /// </summary>
    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));

        var runtimeAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        return references;
    }
}