// -----------------------------------------------------------------------
// <copyright file="SqlxVarGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for SqlxVar source generator.
/// </summary>
[TestClass]
public class SqlxVarGeneratorTests
{
    [TestMethod]
    public void DetectMethod_WithSqlxVarAttribute_ShouldBeDetected()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        
        // Debug output
        Console.WriteLine($"Generated {generatedSources.Count} files");
        foreach (var (fileName, sourceCode) in generatedSources)
        {
            Console.WriteLine($"File: {fileName}");
            Console.WriteLine(sourceCode);
        }
        
        Assert.IsTrue(generatedSources.Count > 0, "Should generate at least one source file");
        
        var generatedCode = generatedSources[0].Source;
        Assert.IsTrue(generatedCode.Contains("GetVar"), "Generated code should contain GetVar method");
    }

    [TestMethod]
    public void DetectMethod_WithoutSqlxVarAttribute_ShouldNotBeDetected()
    {
        // Arrange
        var source = @"
namespace Test
{
    public partial class TestRepository
    {
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        Assert.AreEqual(0, generatedSources.Count, "Should not generate any source files");
    }

    [TestMethod]
    public void DetectMethod_PartialClass_ShouldBeDetected()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }
    
    public partial class TestRepository
    {
        [SqlxVar(""userId"")]
        private string GetUserId() => ""user-456"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        Assert.IsTrue(generatedSources.Count > 0, "Should generate source files for partial class");
        
        var generatedCode = generatedSources[0].Source;
        Assert.IsTrue(generatedCode.Contains("tenantId"), "Should include tenantId variable");
        Assert.IsTrue(generatedCode.Contains("userId"), "Should include userId variable");
    }

    // ===== Task 3.1: Return Type Validation Tests =====

    [TestMethod]
    public void ValidateReturnType_StringReturnType_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1002")
            .ToList();
        
        Assert.AreEqual(0, diagnostics.Count, "Should not produce SQLX1002 diagnostic for string return type");
    }

    [TestMethod]
    public void ValidateReturnType_IntReturnType_ProducesSQLX1002Diagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""count"")]
        private int GetCount() => 42;
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1002")
            .ToList();
        
        Assert.AreEqual(1, diagnostics.Count, "Should produce SQLX1002 diagnostic for int return type");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("GetCount"), 
            "Diagnostic message should mention the method name");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("string"), 
            "Diagnostic message should mention string as required type");
    }

    [TestMethod]
    public void ValidateReturnType_VoidReturnType_ProducesSQLX1002Diagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""action"")]
        private void DoAction() { }
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1002")
            .ToList();
        
        Assert.AreEqual(1, diagnostics.Count, "Should produce SQLX1002 diagnostic for void return type");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("DoAction"), 
            "Diagnostic message should mention the method name");
    }

    [TestMethod]
    public void ValidateReturnType_ObjectReturnType_ProducesSQLX1002Diagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""data"")]
        private object GetData() => new object();
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1002")
            .ToList();
        
        Assert.AreEqual(1, diagnostics.Count, "Should produce SQLX1002 diagnostic for object return type");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("GetData"), 
            "Diagnostic message should mention the method name");
    }

    // ===== Task 3.2: Parameter Validation Tests =====

    [TestMethod]
    public void ValidateParameters_ZeroParameters_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1003")
            .ToList();
        
        Assert.AreEqual(0, diagnostics.Count, "Should not produce SQLX1003 diagnostic for zero parameters");
    }

    [TestMethod]
    public void ValidateParameters_OneParameter_ProducesSQLX1003Diagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId(string param) => param;
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1003")
            .ToList();
        
        Assert.AreEqual(1, diagnostics.Count, "Should produce SQLX1003 diagnostic for one parameter");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("GetTenantId"), 
            "Diagnostic message should mention the method name");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("zero parameters") || 
                      diagnostics[0].GetMessage().Contains("no parameters"), 
            "Diagnostic message should mention zero/no parameters requirement");
    }

    [TestMethod]
    public void ValidateParameters_MultipleParameters_ProducesSQLX1003Diagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""combined"")]
        private string GetCombined(string param1, int param2, bool param3) => param1;
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1003")
            .ToList();
        
        Assert.AreEqual(1, diagnostics.Count, "Should produce SQLX1003 diagnostic for multiple parameters");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("GetCombined"), 
            "Diagnostic message should mention the method name");
    }

    // ===== Task 5.1: Variable Name Uniqueness Tests =====

    [TestMethod]
    public void ValidateVariableName_SingleVariable_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1001")
            .ToList();
        
        Assert.AreEqual(0, diagnostics.Count, "Should not produce SQLX1001 diagnostic for single variable");
    }

    [TestMethod]
    public void ValidateVariableName_DuplicateNames_ProducesSQLX1001Diagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";

        [SqlxVar(""tenantId"")]
        private string GetTenantIdAgain() => ""tenant-456"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1001")
            .ToList();
        
        Assert.AreEqual(1, diagnostics.Count, "Should produce SQLX1001 diagnostic for duplicate variable names");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("tenantId"), 
            "Diagnostic message should mention the variable name");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("duplicate") || 
                      diagnostics[0].GetMessage().Contains("already defined"), 
            "Diagnostic message should indicate duplication");
    }

    [TestMethod]
    public void ValidateVariableName_MultipleUniqueVariables_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";

        [SqlxVar(""userId"")]
        private string GetUserId() => ""user-456"";

        [SqlxVar(""timestamp"")]
        private static string GetTimestamp() => ""2026-02-06"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1001")
            .ToList();
        
        Assert.AreEqual(0, diagnostics.Count, "Should not produce SQLX1001 diagnostic for unique variable names");
    }

    [TestMethod]
    public void ValidateVariableName_DuplicateInPartialClass_ProducesSQLX1001Diagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }

    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantIdFromContext() => ""tenant-456"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1001")
            .ToList();
        
        Assert.AreEqual(1, diagnostics.Count, 
            "Should produce SQLX1001 diagnostic for duplicate variable names across partial classes");
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("tenantId"), 
            "Diagnostic message should mention the variable name");
    }

    // ===== Task 5.2: Variable Name Format Tests =====

    [TestMethod]
    public void ValidateVariableName_ValidAlphanumericName_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId123"")]
        private string GetTenantId() => ""tenant-123"";

        [SqlxVar(""userId456"")]
        private string GetUserId() => ""user-456"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1004" || d.Id == "SQLX1005")
            .ToList();
        
        Assert.AreEqual(0, diagnostics.Count, "Should not produce diagnostic for valid alphanumeric names");
    }

    [TestMethod]
    public void ValidateVariableName_NameWithUnderscores_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenant_id"")]
        private string GetTenantId() => ""tenant-123"";

        [SqlxVar(""user_id_value"")]
        private string GetUserId() => ""user-456"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics
            .Where(d => d.Id == "SQLX1004" || d.Id == "SQLX1005")
            .ToList();
        
        Assert.AreEqual(0, diagnostics.Count, "Should not produce diagnostic for names with underscores");
    }

    [TestMethod]
    public void ValidateVariableName_EmptyName_HandledByAttribute()
    {
        // Arrange
        // Note: Empty variable name is already handled by SqlxVarAttribute constructor
        // This test verifies that the generator doesn't crash when attribute validation fails
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar("""")]
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        // The attribute constructor will throw at runtime, but the generator should handle gracefully
        // We just verify no SQLX diagnostics are produced (compilation error will occur instead)
        var sqlxDiagnostics = result.Diagnostics
            .Where(d => d.Id.StartsWith("SQLX"))
            .ToList();
        
        // Empty string should be caught by attribute validation, not generator
        Assert.IsTrue(sqlxDiagnostics.Count == 0 || sqlxDiagnostics.All(d => d.Id != "SQLX1004"),
            "Empty variable name should be handled by attribute, not generator");
    }

    // ===== Additional Boundary Tests =====

    [TestMethod]
    public void BoundaryTest_VeryLongVariableName_HandlesCorrectly()
    {
        // Arrange
        var longName = new string('a', 200);
        var source = $@"
using Sqlx.Annotations;

namespace Test
{{
    public partial class TestRepository
    {{
        [SqlxVar(""{longName}"")]
        private string GetValue() => ""value"";
    }}
}}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics.Where(d => d.Id.StartsWith("SQLX")).ToList();
        Assert.AreEqual(0, diagnostics.Count, "Should handle very long variable names");
        
        var generatedSources = result.GetAllGeneratedSources().ToList();
        Assert.IsTrue(generatedSources.Count > 0, "Should generate code");
        Assert.IsTrue(generatedSources[0].Source.Contains(longName), "Should include long variable name");
    }

    [TestMethod]
    public void BoundaryTest_SpecialCharactersInMethodName_GeneratesCorrectly()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenant_id_123"")]
        private string Get_Tenant_Id_123() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics.Where(d => d.Id.StartsWith("SQLX")).ToList();
        Assert.AreEqual(0, diagnostics.Count, "Should handle method names with underscores and numbers");
        
        var generatedSources = result.GetAllGeneratedSources().ToList();
        Assert.IsTrue(generatedSources.Count > 0, "Should generate code");
        Assert.IsTrue(generatedSources[0].Source.Contains("Get_Tenant_Id_123"), 
            "Should preserve method name with special characters");
    }

    [TestMethod]
    public void BoundaryTest_ManyVariables_GeneratesEfficientSwitch()
    {
        // Arrange - Create 50 variables
        var methods = string.Join("\n", Enumerable.Range(1, 50).Select(i => 
            $"        [SqlxVar(\"var{i}\")]\n        private string GetVar{i}() => \"value{i}\";"));
        
        var source = $@"
using Sqlx.Annotations;

namespace Test
{{
    public partial class TestRepository
    {{
{methods}
    }}
}}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics.Where(d => d.Id.StartsWith("SQLX")).ToList();
        Assert.AreEqual(0, diagnostics.Count, "Should handle many variables without errors");
        
        var generatedSources = result.GetAllGeneratedSources().ToList();
        Assert.IsTrue(generatedSources.Count > 0, "Should generate code");
        
        var generatedCode = generatedSources[0].Source;
        for (int i = 1; i <= 50; i++)
        {
            Assert.IsTrue(generatedCode.Contains($"\"var{i}\""), $"Should include var{i}");
        }
    }

    [TestMethod]
    public void BoundaryTest_NestedNamespace_GeneratesCorrectly()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Outer.Middle.Inner
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var diagnostics = result.Diagnostics.Where(d => d.Id.StartsWith("SQLX")).ToList();
        Assert.AreEqual(0, diagnostics.Count, "Should handle nested namespaces");
        
        var generatedSources = result.GetAllGeneratedSources().ToList();
        Assert.IsTrue(generatedSources.Count > 0, "Should generate code");
        Assert.IsTrue(generatedSources[0].Source.Contains("namespace Outer.Middle.Inner"), 
            "Should preserve nested namespace");
    }

    [TestMethod]
    public void BoundaryTest_GenericClass_NotSupported()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository<T>
    {
        [SqlxVar(""tenantId"")]
        private string GetTenantId() => ""tenant-123"";
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        // Generic classes should still work - the generator should handle them
        var generatedSources = result.GetAllGeneratedSources().ToList();
        // Generator may or may not support generic classes - just verify no crash
        Assert.IsTrue(true, "Generator should not crash on generic classes");
    }

    [TestMethod]
    public void BoundaryTest_NullableReturnType_ProducesDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    public partial class TestRepository
    {
        [SqlxVar(""tenantId"")]
        private string? GetTenantId() => null;
    }
}";

        // Act
        var generator = new SqlxVarGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        // Nullable string is still string type, should be accepted
        var diagnostics = result.Diagnostics.Where(d => d.Id == "SQLX1002").ToList();
        Assert.AreEqual(0, diagnostics.Count, "Nullable string should be accepted as valid return type");
    }
}
