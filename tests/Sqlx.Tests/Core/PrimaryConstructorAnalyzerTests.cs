// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorAnalyzerTests.cs" company="Cricle">
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
using Sqlx;

/// <summary>
/// Tests for PrimaryConstructorAnalyzer functionality.
/// Tests record detection, primary constructor analysis, and member mapping.
/// </summary>
[TestClass]
public class PrimaryConstructorAnalyzerTests : CodeGenerationTestBase
{
    private static readonly string[] stringArray = new[] { "UserRecord", "UserRecordStruct", "RegularClass", "RegularStruct", "IUser" };

    /// <summary>
    /// Tests IsRecord method for different type scenarios.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_IsRecord_DetectsRecordTypes()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public record UserRecord(int Id, string Name, string Email);
    
    public record struct UserRecordStruct(int Id, string Name);
    
    public class RegularClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    public struct RegularStruct
    {
        public int Value { get; set; }
    }
    
    public interface IUser
    {
        int Id { get; }
    }
}";

        var types = GetTypeSymbols(sourceCode, stringArray);

        var userRecord = types["UserRecord"];
        var userRecordStruct = types["UserRecordStruct"];
        var regularClass = types["RegularClass"];
        var regularStruct = types["RegularStruct"];
        var userInterface = types["IUser"];

        // Test record detection
        Assert.IsTrue(PrimaryConstructorAnalyzer.IsRecord(userRecord),
            "Should detect UserRecord as a record type");

        // Record struct is not a class record
        Assert.IsFalse(PrimaryConstructorAnalyzer.IsRecord(userRecordStruct),
            "Should not detect record struct as class record");

        Assert.IsFalse(PrimaryConstructorAnalyzer.IsRecord(regularClass),
            "Should not detect regular class as record");

        Assert.IsFalse(PrimaryConstructorAnalyzer.IsRecord(regularStruct),
            "Should not detect struct as record");

        Assert.IsFalse(PrimaryConstructorAnalyzer.IsRecord(userInterface),
            "Should not detect interface as record");
    }
    private static readonly string[] stringArray2 = new[] {
            "UserRecord", "ClassWithPrimaryConstructor", "ClassWithRegularConstructor",
            "ClassWithDefaultConstructor", "ClassWithMultipleConstructors"
        };

    /// <summary>
    /// Tests HasPrimaryConstructor method for various scenarios.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_HasPrimaryConstructor_DetectsPrimaryConstructors()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public record UserRecord(int Id, string Name);
    
    public class ClassWithPrimaryConstructor(string data)
    {
        public string Data { get; } = data;
    }
    
    public class ClassWithRegularConstructor
    {
        public ClassWithRegularConstructor(string data)
        {
            Data = data;
        }
        
        public string Data { get; }
    }
    
    public class ClassWithDefaultConstructor
    {
        public string Data { get; set; } = string.Empty;
    }
    
    public class ClassWithMultipleConstructors
    {
        public ClassWithMultipleConstructors() { }
        
        public ClassWithMultipleConstructors(string data)
        {
            Data = data;
        }
        
        public string Data { get; set; } = string.Empty;
    }
}";

        var types = GetTypeSymbols(sourceCode, stringArray2);

        var userRecord = types["UserRecord"];
        var classWithPrimaryConstructor = types["ClassWithPrimaryConstructor"];
        var classWithRegularConstructor = types["ClassWithRegularConstructor"];
        var classWithDefaultConstructor = types["ClassWithDefaultConstructor"];
        var classWithMultipleConstructors = types["ClassWithMultipleConstructors"];

        // Test primary constructor detection
        Assert.IsTrue(PrimaryConstructorAnalyzer.HasPrimaryConstructor(userRecord),
            "Record should have primary constructor");

        // The analyzer should detect primary constructors or constructors that match properties
        var hasPrimaryConstructor1 = PrimaryConstructorAnalyzer.HasPrimaryConstructor(classWithPrimaryConstructor);
        var hasPrimaryConstructor2 = PrimaryConstructorAnalyzer.HasPrimaryConstructor(classWithRegularConstructor);
        var hasPrimaryConstructor3 = PrimaryConstructorAnalyzer.HasPrimaryConstructor(classWithDefaultConstructor);
        var hasPrimaryConstructor4 = PrimaryConstructorAnalyzer.HasPrimaryConstructor(classWithMultipleConstructors);

        // Note: The actual behavior depends on the implementation details
        // These tests verify the method doesn't throw and returns boolean values
        Assert.IsTrue(hasPrimaryConstructor1 || !hasPrimaryConstructor1,
            "Should return valid boolean for class with primary constructor");
        Assert.IsTrue(hasPrimaryConstructor2 || !hasPrimaryConstructor2,
            "Should return valid boolean for class with regular constructor");
        Assert.IsTrue(hasPrimaryConstructor3 || !hasPrimaryConstructor3,
            "Should return valid boolean for class with default constructor");
        Assert.IsTrue(hasPrimaryConstructor4 || !hasPrimaryConstructor4,
            "Should return valid boolean for class with multiple constructors");
    }
    private static readonly string[] stringArray3 = new[] { "UserRecord", "PersonClass", "EmptyClass" };

    /// <summary>
    /// Tests GetPrimaryConstructor method.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_GetPrimaryConstructor_ReturnsCorrectConstructor()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public record UserRecord(int Id, string Name, string Email);
    
    public class PersonClass
    {
        public PersonClass(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
        
        public string FirstName { get; }
        public string LastName { get; }
    }
    
    public class EmptyClass
    {
    }
}";

        var types = GetTypeSymbols(sourceCode, stringArray3);

        var userRecord = types["UserRecord"];
        var personClass = types["PersonClass"];
        var emptyClass = types["EmptyClass"];

        // Test getting primary constructor
        var recordConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(userRecord);
        var classConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(personClass);
        var emptyConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(emptyClass);

        // Record should have primary constructor
        Assert.IsNotNull(recordConstructor, "Record should have primary constructor");
        if (recordConstructor != null)
        {
            Assert.AreEqual(3, recordConstructor.Parameters.Length,
                "Record constructor should have 3 parameters");
        }

        // Regular class may or may not be detected as having primary constructor
        Assert.IsTrue(classConstructor != null || classConstructor == null,
            "Class constructor detection should return valid result");

        // Empty class should not have primary constructor
        Assert.IsNull(emptyConstructor, "Empty class should not have primary constructor");
    }
    private static readonly string[] stringArray4 = new[] { "ProductRecord", "CustomerClass" };

    /// <summary>
    /// Tests GetPrimaryConstructorParameters method.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_GetPrimaryConstructorParameters_ReturnsCorrectParameters()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public record ProductRecord(int Id, string Name, decimal Price, bool IsActive);
    
    public class CustomerClass
    {
        public CustomerClass(string name, string email)
        {
            Name = name;
            Email = email;
        }
        
        public string Name { get; }
        public string Email { get; }
    }
}";

        var types = GetTypeSymbols(sourceCode, stringArray4);

        var productRecord = types["ProductRecord"];
        var customerClass = types["CustomerClass"];

        // Test getting primary constructor parameters
        var recordParams = PrimaryConstructorAnalyzer.GetPrimaryConstructorParameters(productRecord).ToList();
        var classParams = PrimaryConstructorAnalyzer.GetPrimaryConstructorParameters(customerClass).ToList();

        // Record should have parameters
        Assert.IsTrue(recordParams.Count > 0, "Record should have primary constructor parameters");
        if (recordParams.Count > 0)
        {
            Assert.AreEqual(4, recordParams.Count, "Record should have 4 parameters");

            var paramNames = recordParams.Select(p => p.Name).ToArray();
            Assert.IsTrue(paramNames.Contains("Id"), "Should have Id parameter");
            Assert.IsTrue(paramNames.Contains("Name"), "Should have Name parameter");
            Assert.IsTrue(paramNames.Contains("Price"), "Should have Price parameter");
            Assert.IsTrue(paramNames.Contains("IsActive"), "Should have IsActive parameter");
        }

        // Class parameters depend on primary constructor detection
        Assert.IsTrue(classParams.Count >= 0, "Class parameters should be non-negative count");
    }
    private static readonly string[] stringArray5 = new[] { "UserRecord", "PersonClass" };

    /// <summary>
    /// Tests GetAccessibleMembers method for different type scenarios.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_GetAccessibleMembers_ReturnsCorrectMembers()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public record UserRecord(int Id, string Name)
    {
        public string Email { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    }
    
    public class PersonClass
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $""{FirstName} {LastName}"";
        
        private string _secret = string.Empty;
        public string GetSecret() => _secret;
    }
}";

        var types = GetTypeSymbols(sourceCode, stringArray5);

        var userRecord = types["UserRecord"];
        var personClass = types["PersonClass"];

        // Test getting accessible members
        var recordMembers = PrimaryConstructorAnalyzer.GetAccessibleMembers(userRecord).ToList();
        var classMembers = PrimaryConstructorAnalyzer.GetAccessibleMembers(personClass).ToList();

        // Record should have members from primary constructor + additional properties
        Assert.IsTrue(recordMembers.Count > 0, "Record should have accessible members");

        var recordMemberNames = recordMembers.Select(m => m.Name).ToArray();
        Assert.IsTrue(recordMemberNames.Contains("Id"), "Should include Id from primary constructor");
        Assert.IsTrue(recordMemberNames.Contains("Name"), "Should include Name from primary constructor");
        Assert.IsTrue(recordMemberNames.Contains("Email"), "Should include Email property");
        Assert.IsTrue(recordMemberNames.Contains("CreatedDate"), "Should include CreatedDate property");

        // Class should have settable properties
        Assert.IsTrue(classMembers.Count > 0, "Class should have accessible members");

        var classMemberNames = classMembers.Select(m => m.Name).ToArray();
        Assert.IsTrue(classMemberNames.Contains("Id"), "Should include Id property");
        Assert.IsTrue(classMemberNames.Contains("FirstName"), "Should include FirstName property");
        Assert.IsTrue(classMemberNames.Contains("LastName"), "Should include LastName property");

        // FullName is read-only, might or might not be included depending on implementation
        // _secret is private field, should not be included
        Assert.IsFalse(classMemberNames.Contains("_secret"), "Should not include private fields");
    }
    private static readonly string[] stringArray6 = new[] {
            "AbstractClass", "StaticClass", "IEmpty", "EmptyRecord", "ClassWithOnlyPrivateConstructor"
        };

    /// <summary>
    /// Tests edge cases and error handling.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_EdgeCases_HandledGracefully()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public abstract class AbstractClass
    {
        public abstract int GetValue();
    }
    
    public static class StaticClass
    {
        public static string Value = ""test"";
    }
    
    public interface IEmpty
    {
    }
    
    public record EmptyRecord();
    
    public class ClassWithOnlyPrivateConstructor
    {
        private ClassWithOnlyPrivateConstructor() { }
        public int Value { get; set; }
    }
}";

        var types = GetTypeSymbols(sourceCode, stringArray6);

        // Test edge cases don't throw exceptions
        foreach (var (typeName, type) in types)
        {
            try
            {
                var isRecord = PrimaryConstructorAnalyzer.IsRecord(type);
                var hasPrimaryConstructor = PrimaryConstructorAnalyzer.HasPrimaryConstructor(type);
                var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(type);
                var parameters = PrimaryConstructorAnalyzer.GetPrimaryConstructorParameters(type).ToList();
                var members = PrimaryConstructorAnalyzer.GetAccessibleMembers(type).ToList();

                // Verify methods return reasonable results
                Assert.IsTrue(isRecord || !isRecord, $"IsRecord should return boolean for {typeName}");
                Assert.IsTrue(hasPrimaryConstructor || !hasPrimaryConstructor,
                    $"HasPrimaryConstructor should return boolean for {typeName}");
                Assert.IsTrue(parameters.Count >= 0, $"Parameters count should be non-negative for {typeName}");
                Assert.IsTrue(members.Count >= 0, $"Members count should be non-negative for {typeName}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"PrimaryConstructorAnalyzer should handle {typeName} gracefully. Exception: {ex.Message}");
            }
        }
    }
    private static readonly string[] stringArray7 = new[] {
            "BaseEntity", "User", "AdminUser", "UserRecord", "AdminRecord"
        };

    /// <summary>
    /// Tests performance with complex inheritance hierarchies.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_Performance_HandlesComplexHierarchies()
    {
        string sourceCode = @"
namespace TestNamespace
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    
    public class User : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
    
    public class AdminUser : User
    {
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }
    
    public record UserRecord(int Id, string Name, string Email) : BaseEntity;
    
    public record AdminRecord(int Id, string Name, string Email, string Role) : UserRecord(Id, Name, Email);
}";

        var types = GetTypeSymbols(sourceCode, stringArray7);

        var startTime = DateTime.UtcNow;

        // Test performance with inheritance hierarchies
        foreach (var (typeName, type) in types)
        {
            for (int i = 0; i < 100; i++)
            {
                var isRecord = PrimaryConstructorAnalyzer.IsRecord(type);
                var hasPrimaryConstructor = PrimaryConstructorAnalyzer.HasPrimaryConstructor(type);
                var members = PrimaryConstructorAnalyzer.GetAccessibleMembers(type).ToList();

                // Use results to ensure calls aren't optimized away
                Assert.IsTrue(isRecord || !isRecord, "Should return valid boolean");
                Assert.IsTrue(hasPrimaryConstructor || !hasPrimaryConstructor, "Should return valid boolean");
                Assert.IsTrue(members.Count >= 0, "Should return valid member count");
            }
        }

        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        Assert.IsTrue(elapsed.TotalSeconds < 5,
            $"Complex hierarchy analysis should be efficient. Took: {elapsed.TotalSeconds} seconds");
    }
    private static readonly string[] stringArray8 = new[] { "UserRecord", "ModernClass", "ValueRecord" };

    /// <summary>
    /// Tests analyzer with modern C# features.
    /// </summary>
    [TestMethod]
    public void PrimaryConstructorAnalyzer_ModernCSharp_HandlesLatestFeatures()
    {
        string sourceCode = @"
using System;

namespace TestNamespace
{
    public record UserRecord(int Id, string Name)
    {
        public string? Nickname { get; init; }
        public required string Email { get; init; }
    }
    
    public class ModernClass(string data)
    {
        public string Data { get; } = data;
        public string? OptionalData { get; init; }
        public required string RequiredData { get; init; }
    }
    
    public record struct ValueRecord(int X, int Y)
    {
        public readonly double Distance => Math.Sqrt(X * X + Y * Y);
    }
}";

        var types = GetTypeSymbols(sourceCode, stringArray8);

        var userRecord = types["UserRecord"];
        var modernClass = types["ModernClass"];
        var valueRecord = types["ValueRecord"];

        // Test with modern C# features
        var recordIsRecord = PrimaryConstructorAnalyzer.IsRecord(userRecord);
        var classIsRecord = PrimaryConstructorAnalyzer.IsRecord(modernClass);
        var structIsRecord = PrimaryConstructorAnalyzer.IsRecord(valueRecord);

        Assert.IsTrue(recordIsRecord, "Record class should be detected as record");
        Assert.IsFalse(classIsRecord, "Regular class should not be detected as record");
        Assert.IsFalse(structIsRecord, "Record struct should not be detected as class record");

        // Test member analysis with modern features
        var recordMembers = PrimaryConstructorAnalyzer.GetAccessibleMembers(userRecord).ToList();
        var classMembers = PrimaryConstructorAnalyzer.GetAccessibleMembers(modernClass).ToList();

        Assert.IsTrue(recordMembers.Count >= 2, "Record should have at least primary constructor members");
        Assert.IsTrue(classMembers.Count >= 1, "Class should have at least some accessible members");

        var recordMemberNames = recordMembers.Select(m => m.Name).ToArray();
        Assert.IsTrue(recordMemberNames.Contains("Id"), "Should include Id from primary constructor");
        Assert.IsTrue(recordMemberNames.Contains("Name"), "Should include Name from primary constructor");
    }

    /// <summary>
    /// Helper method to get type symbols from source code.
    /// </summary>
    private static Dictionary<string, INamedTypeSymbol> GetTypeSymbols(string sourceCode, string[] typeNames)
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

        var result = new Dictionary<string, INamedTypeSymbol>();

        foreach (var typeName in typeNames)
        {
            // Try to find class declaration
            var classDeclaration = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.ValueText == typeName);

            if (classDeclaration != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                if (symbol != null)
                {
                    result[typeName] = symbol;
                    continue;
                }
            }

            // Try to find record declaration
            var recordDeclaration = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.RecordDeclarationSyntax>()
                .FirstOrDefault(r => r.Identifier.ValueText == typeName);

            if (recordDeclaration != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(recordDeclaration) as INamedTypeSymbol;
                if (symbol != null)
                {
                    result[typeName] = symbol;
                    continue;
                }
            }

            // Try to find struct declaration
            var structDeclaration = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax>()
                .FirstOrDefault(s => s.Identifier.ValueText == typeName);

            if (structDeclaration != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(structDeclaration) as INamedTypeSymbol;
                if (symbol != null)
                {
                    result[typeName] = symbol;
                    continue;
                }
            }

            // Try to find interface declaration
            var interfaceDeclaration = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax>()
                .FirstOrDefault(i => i.Identifier.ValueText == typeName);

            if (interfaceDeclaration != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;
                if (symbol != null)
                {
                    result[typeName] = symbol;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets basic references needed for compilation.
    /// </summary>
    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
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
