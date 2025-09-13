// -----------------------------------------------------------------------
// <copyright file="DiagnosticHelperTests.cs" company="Cricle">
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
/// Tests for DiagnosticHelper utility class.
/// Tests diagnostic creation, validation, and code quality analysis functionality.
/// </summary>
[TestClass]
public class DiagnosticHelperTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests basic diagnostic creation functionality.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_CreateDiagnostic_CreatesCorrectDiagnostic()
    {
        var diagnostic = DiagnosticHelper.CreateDiagnostic(
            "SQLX_TEST_001",
            "Test Diagnostic",
            "This is a test diagnostic with parameter: {0}",
            DiagnosticSeverity.Warning,
            null,
            "TestValue");

        Assert.IsNotNull(diagnostic, "Should create diagnostic");
        Assert.AreEqual("SQLX_TEST_001", diagnostic.Id, "Should have correct ID");
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity, "Should have correct severity");
        Assert.IsTrue(diagnostic.GetMessage().Contains("TestValue"), "Should format message with parameters");
        Assert.IsTrue(diagnostic.GetMessage().Contains("This is a test diagnostic"), "Should contain base message");
    }

    /// <summary>
    /// Tests Primary Constructor diagnostic creation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_CreatePrimaryConstructorDiagnostic_CreatesSpecificDiagnostic()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public class TestEntity(int id, string name)
    {
        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
    }
}";

        var entityType = GetEntityType(sourceCode, "TestEntity");
        var diagnostic = DiagnosticHelper.CreatePrimaryConstructorDiagnostic(
            "Missing required parameter validation",
            entityType);

        Assert.IsNotNull(diagnostic, "Should create primary constructor diagnostic");
        Assert.AreEqual("SQLX1001", diagnostic.Id, "Should have correct primary constructor diagnostic ID");
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity, "Should be warning severity");
        Assert.IsTrue(diagnostic.GetMessage().Contains("TestEntity"), "Should reference entity type name");
        Assert.IsTrue(diagnostic.GetMessage().Contains("Missing required parameter validation"), "Should contain issue description");
    }

    /// <summary>
    /// Tests Record type diagnostic creation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_CreateRecordTypeDiagnostic_CreatesRecordSpecificDiagnostic()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public record UserRecord(int Id, string Name, string Email);
}";

        var entityType = GetEntityType(sourceCode, "UserRecord");
        var diagnostic = DiagnosticHelper.CreateRecordTypeDiagnostic(
            "Record property mapping issue",
            entityType);

        Assert.IsNotNull(diagnostic, "Should create record type diagnostic");
        Assert.AreEqual("SQLX1002", diagnostic.Id, "Should have correct record type diagnostic ID");
        Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity, "Should be info severity");
        Assert.IsTrue(diagnostic.GetMessage().Contains("UserRecord"), "Should reference record type name");
        Assert.IsTrue(diagnostic.GetMessage().Contains("Record property mapping issue"), "Should contain issue description");
    }

    /// <summary>
    /// Tests entity inference diagnostic creation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_CreateEntityInferenceDiagnostic_CreatesInferenceDiagnostic()
    {
        var diagnostic = DiagnosticHelper.CreateEntityInferenceDiagnostic(
            "Unable to infer entity type",
            "GetUserById");

        Assert.IsNotNull(diagnostic, "Should create entity inference diagnostic");
        Assert.AreEqual("SQLX1003", diagnostic.Id, "Should have correct entity inference diagnostic ID");
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity, "Should be warning severity");
        Assert.IsTrue(diagnostic.GetMessage().Contains("GetUserById"), "Should reference method name");
        Assert.IsTrue(diagnostic.GetMessage().Contains("Unable to infer entity type"), "Should contain inference issue");
    }

    /// <summary>
    /// Tests performance suggestion diagnostic creation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_CreatePerformanceSuggestion_CreatesPerformanceDiagnostic()
    {
        var diagnostic = DiagnosticHelper.CreatePerformanceSuggestion(
            "Consider using async methods for better performance",
            "GetAllUsers");

        Assert.IsNotNull(diagnostic, "Should create performance suggestion diagnostic");
        Assert.AreEqual("SQLX2001", diagnostic.Id, "Should have correct performance suggestion diagnostic ID");
        Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity, "Should be info severity");
        Assert.IsTrue(diagnostic.GetMessage().Contains("GetAllUsers"), "Should reference method name");
        Assert.IsTrue(diagnostic.GetMessage().Contains("async methods"), "Should contain performance suggestion");
    }

    /// <summary>
    /// Tests entity type validation for various entity scenarios.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_ValidateEntityType_ValidatesEntityCorrectly()
    {
        // Test valid entity
        string validEntityCode = @"
namespace TestNamespace
{
    public class ValidEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}";

        var validEntity = GetEntityType(validEntityCode, "ValidEntity");
        var validationIssues = DiagnosticHelper.ValidateEntityType(validEntity);

        Assert.IsNotNull(validationIssues, "Should return validation results");
        Assert.AreEqual(0, validationIssues.Count, "Valid entity should have no validation issues");
    }

    /// <summary>
    /// Tests entity validation with abstract class.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_ValidateEntityType_DetectsAbstractClass()
    {
        string abstractEntityCode = @"
namespace TestNamespace
{
    public abstract class AbstractEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public abstract void ProcessData();
    }
}";

        var abstractEntity = GetEntityType(abstractEntityCode, "AbstractEntity");
        var validationIssues = DiagnosticHelper.ValidateEntityType(abstractEntity);

        Assert.IsNotNull(validationIssues, "Should return validation results");
        Assert.IsTrue(validationIssues.Count > 0, "Abstract entity should have validation issues");
        Assert.IsTrue(validationIssues.Any(issue => issue.Contains("抽象类")), 
            "Should detect abstract class issue");
    }

    /// <summary>
    /// Tests entity validation with no accessible constructors.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_ValidateEntityType_DetectsNoPublicConstructors()
    {
        string privateConstructorCode = @"
namespace TestNamespace
{
    public class PrivateConstructorEntity
    {
        private PrivateConstructorEntity() { }
        
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        var privateConstructorEntity = GetEntityType(privateConstructorCode, "PrivateConstructorEntity");
        var validationIssues = DiagnosticHelper.ValidateEntityType(privateConstructorEntity);

        Assert.IsNotNull(validationIssues, "Should return validation results");
        Assert.IsTrue(validationIssues.Count > 0, "Entity with private constructor should have validation issues");
        Assert.IsTrue(validationIssues.Any(issue => issue.Contains("公共构造函数")), 
            "Should detect missing public constructor issue");
    }

    /// <summary>
    /// Tests entity validation with no mappable properties.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_ValidateEntityType_DetectsNoMappableProperties()
    {
        string noPropertiesCode = @"
namespace TestNamespace
{
    public class NoPropertiesEntity
    {
        public void DoSomething() { }
        public void ProcessData() { }
    }
}";

        var noPropertiesEntity = GetEntityType(noPropertiesCode, "NoPropertiesEntity");
        var validationIssues = DiagnosticHelper.ValidateEntityType(noPropertiesEntity);

        Assert.IsNotNull(validationIssues, "Should return validation results");
        Assert.IsTrue(validationIssues.Count > 0, "Entity with no properties should have validation issues");
        Assert.IsTrue(validationIssues.Any(issue => issue.Contains("可映射的属性")), 
            "Should detect missing mappable properties issue");
    }

    /// <summary>
    /// Tests Record validation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_ValidateEntityType_ValidatesRecordTypes()
    {
        string recordCode = @"
namespace TestNamespace
{
    public record ValidRecord(int Id, string Name, string Email)
    {
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    }
    
    public record EmptyRecord
    {
        public string Data { get; init; } = string.Empty;
    }
}";

        var validRecord = GetEntityType(recordCode, "ValidRecord");
        var emptyRecord = GetEntityType(recordCode, "EmptyRecord");

        var validRecordIssues = DiagnosticHelper.ValidateEntityType(validRecord);
        var emptyRecordIssues = DiagnosticHelper.ValidateEntityType(emptyRecord);

        Assert.IsNotNull(validRecordIssues, "Should return validation results for valid record");
        Assert.AreEqual(0, validRecordIssues.Count, "Valid record should have no validation issues");

        Assert.IsNotNull(emptyRecordIssues, "Should return validation results for empty record");
        Assert.IsTrue(emptyRecordIssues.Any(issue => issue.Contains("主构造函数")), 
            "Empty record should have primary constructor validation issue");
    }

    /// <summary>
    /// Tests performance suggestion generation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_GeneratePerformanceSuggestions_GeneratesRelevantSuggestions()
    {
        string entityCode = @"
namespace TestNamespace
{
    public class LargeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}";

        var largeEntity = GetEntityType(entityCode, "LargeEntity");
        var suggestions = DiagnosticHelper.GeneratePerformanceSuggestions(largeEntity);

        Assert.IsNotNull(suggestions, "Should return performance suggestions");
        Assert.IsTrue(suggestions.Count > 0, "Should generate at least some performance suggestions");
        
        // Check for common performance suggestions
        var suggestionsText = string.Join(" ", suggestions);
        Assert.IsTrue(suggestionsText.Contains("Record") || suggestionsText.Contains("性能") || 
                     suggestionsText.Contains("优化"), 
            "Should contain performance-related suggestions");
    }

    /// <summary>
    /// Tests type analysis report generation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_GenerateTypeAnalysisReport_GeneratesDetailedReport()
    {
        string entityCode = @"
namespace TestNamespace
{
    public class ComplexEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        
        public void ProcessData() { }
        private string InternalData { get; set; } = string.Empty;
    }
}";

        var complexEntity = GetEntityType(entityCode, "ComplexEntity");
        var report = DiagnosticHelper.GenerateTypeAnalysisReport(complexEntity);

        Assert.IsNotNull(report, "Should generate type analysis report");
        Assert.IsTrue(report.Length > 0, "Report should not be empty");
        Assert.IsTrue(report.Contains("ComplexEntity"), "Report should contain entity type name");
        Assert.IsTrue(report.Contains("属性") || report.Contains("Properties") || report.Contains("Id"), 
            "Report should contain property information");
        Assert.IsTrue(report.Contains("类型分析") || report.Contains("Type Analysis") || report.Contains("分析"), 
            "Report should contain analysis information");
    }

    /// <summary>
    /// Tests generated code validation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_ValidateGeneratedCode_DetectsCodeIssues()
    {
        string entityCode = @"
namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}";

        var testEntity = GetEntityType(entityCode, "TestEntity");

        // Test code with unsafe type conversion
        string unsafeCode = @"
var entity = new TestEntity();
entity.CreatedDate = (DateTime)reader[""created_date""];
entity.Name = reader[""name""].ToString();
";

        var unsafeIssues = DiagnosticHelper.ValidateGeneratedCode(unsafeCode, testEntity);
        
        Assert.IsNotNull(unsafeIssues, "Should return validation issues");
        Assert.IsTrue(unsafeIssues.Count > 0, "Unsafe code should have validation issues");
        Assert.IsTrue(unsafeIssues.Any(issue => issue.Contains("DateTime") && issue.Contains("不安全")), 
            "Should detect unsafe DateTime conversion");

        // Test code without null checks
        string noNullCheckCode = @"
var entity = new TestEntity();
entity.Name = reader.GetString(""name"");
entity.Id = reader.GetInt32(""id"");
";

        var nullCheckIssues = DiagnosticHelper.ValidateGeneratedCode(noNullCheckCode, testEntity);
        
        Assert.IsNotNull(nullCheckIssues, "Should return validation issues");
        Assert.IsTrue(nullCheckIssues.Any(issue => issue.Contains("null") && issue.Contains("检查")), 
            "Should detect missing null checks");

        // Test good code
        string goodCode = @"
var entity = new TestNamespace.TestEntity();
entity.Name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
entity.CreatedDate = reader.GetDateTime(1);
";

        var goodCodeIssues = DiagnosticHelper.ValidateGeneratedCode(goodCode, testEntity);
        
        Assert.IsNotNull(goodCodeIssues, "Should return validation results");
        // Good code might still have some issues but should have fewer overall problems
        Assert.IsTrue(goodCodeIssues.Count <= unsafeIssues.Count, 
            "Good code should have fewer issues than unsafe code");
    }

    /// <summary>
    /// Tests diagnostic IDs constants.
    /// </summary>
    [TestMethod]
    public void DiagnosticIds_Constants_AreCorrectlyDefined()
    {
        Assert.AreEqual("SQLX1001", DiagnosticIds.PrimaryConstructorIssue, 
            "Primary constructor ID should be correct");
        Assert.AreEqual("SQLX1002", DiagnosticIds.RecordTypeIssue, 
            "Record type ID should be correct");
        Assert.AreEqual("SQLX1003", DiagnosticIds.EntityInferenceIssue, 
            "Entity inference ID should be correct");
        Assert.AreEqual("SQLX2001", DiagnosticIds.PerformanceSuggestion, 
            "Performance suggestion ID should be correct");
        Assert.AreEqual("SQLX2002", DiagnosticIds.CodeQualityWarning, 
            "Code quality warning ID should be correct");
        Assert.AreEqual("SQLX3001", DiagnosticIds.GenerationError, 
            "Generation error ID should be correct");
    }

    /// <summary>
    /// Tests edge cases in diagnostic creation.
    /// </summary>
    [TestMethod]
    public void DiagnosticHelper_EdgeCases_HandledCorrectly()
    {
        // Test with null/empty parameters
        var diagnostic1 = DiagnosticHelper.CreateDiagnostic(
            "",
            "",
            "",
            DiagnosticSeverity.Error);
        
        Assert.IsNotNull(diagnostic1, "Should handle empty strings");
        
        // Test with multiple parameters
        var diagnostic2 = DiagnosticHelper.CreateDiagnostic(
            "SQLX_MULTI",
            "Multi Parameter Test",
            "Values: {0}, {1}, {2}",
            DiagnosticSeverity.Warning,
            null,
            "First", 42, true);
        
        Assert.IsNotNull(diagnostic2, "Should handle multiple parameters");
        var message = diagnostic2.GetMessage();
        Assert.IsTrue(message.Contains("First") && message.Contains("42") && message.Contains("True"), 
            "Should format all parameters correctly");

        // Test diagnostic creation with different severities
        foreach (DiagnosticSeverity severity in Enum.GetValues<DiagnosticSeverity>())
        {
            var diagnostic = DiagnosticHelper.CreateDiagnostic(
                $"SQLX_{severity}",
                $"Test {severity}",
                $"Testing severity {severity}",
                severity);
            
            Assert.AreEqual(severity, diagnostic.Severity, 
                $"Should correctly set {severity} severity");
        }
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
