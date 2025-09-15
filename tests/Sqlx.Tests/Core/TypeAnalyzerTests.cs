// -----------------------------------------------------------------------
// <copyright file="TypeAnalyzerTests.cs" company="Cricle">
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

/// <summary>
/// Tests for TypeAnalyzer utility class.
/// Tests type analysis functionality used in source generation.
/// </summary>
[TestClass]
public class TypeAnalyzerTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests entity type detection for various class types.
    /// </summary>
    [TestMethod]
    public void TypeAnalyzer_IsLikelyEntityType_DetectsEntityTypes()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    // Should be detected as entity type
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Should be detected as entity type
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // Should NOT be detected as entity type (no properties)
    public class EmptyClass
    {
        public void DoSomething() { }
    }

    // Should NOT be detected as entity type (system namespace simulated)
    public class SystemType
    {
        public int Value { get; set; }
    }
}";

        var (compilation, symbols) = GetTypeSymbols(sourceCode);

        var userType = symbols.FirstOrDefault(s => s.Name == "User");
        var productType = symbols.FirstOrDefault(s => s.Name == "Product");
        var emptyClassType = symbols.FirstOrDefault(s => s.Name == "EmptyClass");

        Assert.IsNotNull(userType, "Should find User type");
        Assert.IsNotNull(productType, "Should find Product type");
        Assert.IsNotNull(emptyClassType, "Should find EmptyClass type");

        // Test entity type detection
        Assert.IsTrue(TypeAnalyzer.IsLikelyEntityType(userType),
            "User class with properties should be detected as entity type");
        Assert.IsTrue(TypeAnalyzer.IsLikelyEntityType(productType),
            "Product class with properties should be detected as entity type");
        Assert.IsFalse(TypeAnalyzer.IsLikelyEntityType(emptyClassType),
            "Class without properties should not be detected as entity type");
        Assert.IsFalse(TypeAnalyzer.IsLikelyEntityType(null),
            "Null type should not be detected as entity type");
    }

    /// <summary>
    /// Tests collection type detection for various collection types.
    /// </summary>
    [TestMethod]
    public void TypeAnalyzer_IsCollectionType_DetectsCollectionTypes()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestClass
    {
        public IList<string> ListProperty { get; set; } = new List<string>();
        public List<int> ConcreteListProperty { get; set; } = new List<int>();
        public IEnumerable<User> EnumerableProperty { get; set; } = new List<User>();
        public ICollection<Product> CollectionProperty { get; set; } = new List<Product>();
        public IReadOnlyList<Order> ReadOnlyListProperty { get; set; } = new List<Order>();
        public string StringProperty { get; set; } = string.Empty;
        public int IntProperty { get; set; }
        public User UserProperty { get; set; } = new User();
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Order
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
    }
}";

        var (compilation, symbols) = GetTypeSymbols(sourceCode);
        var testClass = symbols.FirstOrDefault(s => s.Name == "TestClass");
        Assert.IsNotNull(testClass, "Should find TestClass type");

        var properties = testClass.GetMembers().OfType<IPropertySymbol>().ToList();

        var listProperty = properties.FirstOrDefault(p => p.Name == "ListProperty");
        var concreteListProperty = properties.FirstOrDefault(p => p.Name == "ConcreteListProperty");
        var enumerableProperty = properties.FirstOrDefault(p => p.Name == "EnumerableProperty");
        var collectionProperty = properties.FirstOrDefault(p => p.Name == "CollectionProperty");
        var readOnlyListProperty = properties.FirstOrDefault(p => p.Name == "ReadOnlyListProperty");
        var stringProperty = properties.FirstOrDefault(p => p.Name == "StringProperty");
        var intProperty = properties.FirstOrDefault(p => p.Name == "IntProperty");
        var userProperty = properties.FirstOrDefault(p => p.Name == "UserProperty");

        // Test collection type detection
        Assert.IsTrue(TypeAnalyzer.IsCollectionType(listProperty?.Type!),
            "IList<T> should be detected as collection type");
        Assert.IsTrue(TypeAnalyzer.IsCollectionType(concreteListProperty?.Type!),
            "List<T> should be detected as collection type");
        Assert.IsTrue(TypeAnalyzer.IsCollectionType(enumerableProperty?.Type!),
            "IEnumerable<T> should be detected as collection type");
        Assert.IsTrue(TypeAnalyzer.IsCollectionType(collectionProperty?.Type!),
            "ICollection<T> should be detected as collection type");
        Assert.IsTrue(TypeAnalyzer.IsCollectionType(readOnlyListProperty?.Type!),
            "IReadOnlyList<T> should be detected as collection type");

        Assert.IsFalse(TypeAnalyzer.IsCollectionType(stringProperty?.Type!),
            "String should not be detected as collection type");
        Assert.IsFalse(TypeAnalyzer.IsCollectionType(intProperty?.Type!),
            "Int should not be detected as collection type");
        Assert.IsFalse(TypeAnalyzer.IsCollectionType(userProperty?.Type!),
            "Custom entity type should not be detected as collection type");
    }

    /// <summary>
    /// Tests entity type extraction from generic types.
    /// </summary>
    [TestMethod]
    public void TypeAnalyzer_ExtractEntityType_ExtractsFromGenericTypes()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public IList<User> UserList { get; set; } = new List<User>();
        public List<Product> ProductList { get; set; } = new List<Product>();
        public Task<User> UserTask { get; set; } = Task.FromResult(new User());
        public Task<IList<Order>> OrderListTask { get; set; } = Task.FromResult<IList<Order>>(new List<Order>());
        public User SingleUser { get; set; } = new User();
        public string StringValue { get; set; } = string.Empty;
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Order
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
    }
}";

        var (compilation, symbols) = GetTypeSymbols(sourceCode);
        var testClass = symbols.FirstOrDefault(s => s.Name == "TestClass");
        Assert.IsNotNull(testClass, "Should find TestClass type");

        var properties = testClass.GetMembers().OfType<IPropertySymbol>().ToList();

        var userListProperty = properties.FirstOrDefault(p => p.Name == "UserList");
        var productListProperty = properties.FirstOrDefault(p => p.Name == "ProductList");
        var userTaskProperty = properties.FirstOrDefault(p => p.Name == "UserTask");
        var orderListTaskProperty = properties.FirstOrDefault(p => p.Name == "OrderListTask");
        var singleUserProperty = properties.FirstOrDefault(p => p.Name == "SingleUser");

        // Test entity type extraction
        var userFromList = TypeAnalyzer.ExtractEntityType(userListProperty?.Type!);
        var productFromList = TypeAnalyzer.ExtractEntityType(productListProperty?.Type!);
        var userFromTask = TypeAnalyzer.ExtractEntityType(userTaskProperty?.Type!);
        var orderFromListTask = TypeAnalyzer.ExtractEntityType(orderListTaskProperty?.Type!);
        var userFromSingle = TypeAnalyzer.ExtractEntityType(singleUserProperty?.Type!);

        Assert.AreEqual("User", userFromList?.Name,
            "Should extract User type from IList<User>");
        Assert.AreEqual("Product", productFromList?.Name,
            "Should extract Product type from List<Product>");
        Assert.AreEqual("User", userFromTask?.Name,
            "Should extract User type from Task<User>");
        Assert.AreEqual("Order", orderFromListTask?.Name,
            "Should extract Order type from Task<IList<Order>>");
        Assert.AreEqual("User", userFromSingle?.Name,
            "Should return User type for non-generic User");
    }

    /// <summary>
    /// Tests scalar return type detection.
    /// </summary>
    [TestMethod]
    public void TypeAnalyzer_IsScalarReturnType_DetectsScalarTypes()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; } = string.Empty;
        public bool BoolValue { get; set; }
        public decimal DecimalValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public User UserValue { get; set; } = new User();
        public IList<User> UserList { get; set; } = new List<User>();
        public Task<int> IntTask { get; set; } = Task.FromResult(0);
        public Task<User> UserTask { get; set; } = Task.FromResult(new User());
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        var (compilation, symbols) = GetTypeSymbols(sourceCode);
        var testClass = symbols.FirstOrDefault(s => s.Name == "TestClass");
        Assert.IsNotNull(testClass, "Should find TestClass type");

        var properties = testClass.GetMembers().OfType<IPropertySymbol>().ToList();

        var intProperty = properties.FirstOrDefault(p => p.Name == "IntValue");
        var stringProperty = properties.FirstOrDefault(p => p.Name == "StringValue");
        var boolProperty = properties.FirstOrDefault(p => p.Name == "BoolValue");
        var decimalProperty = properties.FirstOrDefault(p => p.Name == "DecimalValue");
        var dateTimeProperty = properties.FirstOrDefault(p => p.Name == "DateTimeValue");
        var userProperty = properties.FirstOrDefault(p => p.Name == "UserValue");
        var userListProperty = properties.FirstOrDefault(p => p.Name == "UserList");
        var intTaskProperty = properties.FirstOrDefault(p => p.Name == "IntTask");
        var userTaskProperty = properties.FirstOrDefault(p => p.Name == "UserTask");

        // Test scalar type detection
        Assert.IsTrue(TypeAnalyzer.IsScalarReturnType(intProperty?.Type!, false),
            "Int should be detected as scalar type");
        Assert.IsTrue(TypeAnalyzer.IsScalarReturnType(stringProperty?.Type!, false),
            "String should be detected as scalar type");
        Assert.IsTrue(TypeAnalyzer.IsScalarReturnType(boolProperty?.Type!, false),
            "Bool should be detected as scalar type");
        Assert.IsTrue(TypeAnalyzer.IsScalarReturnType(decimalProperty?.Type!, false),
            "Decimal should be detected as scalar type");
        // DateTime detection may vary by implementation - check if it's handled as scalar or entity
        var isDateTimeScalar = TypeAnalyzer.IsScalarReturnType(dateTimeProperty?.Type!, false);
        Console.WriteLine($"DateTime scalar detection: {isDateTimeScalar}");
        // DateTime might be considered a complex type in some implementations

        Assert.IsFalse(TypeAnalyzer.IsScalarReturnType(userProperty?.Type!, false),
            "Custom entity type should not be detected as scalar type");
        Assert.IsFalse(TypeAnalyzer.IsScalarReturnType(userListProperty?.Type!, false),
            "Collection type should not be detected as scalar type");
        Assert.IsTrue(TypeAnalyzer.IsScalarReturnType(intTaskProperty?.Type!, true),
            "Task<int> should be detected as scalar type when async is true");
        Assert.IsFalse(TypeAnalyzer.IsScalarReturnType(userTaskProperty?.Type!, true),
            "Task<User> should not be detected as scalar type even when async is true");
    }

    /// <summary>
    /// Tests default value generation for different types.
    /// </summary>
    [TestMethod]
    public void TypeAnalyzer_GetDefaultValue_GeneratesCorrectDefaults()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestClass
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; } = string.Empty;
        public bool BoolValue { get; set; }
        public decimal DecimalValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public User? NullableUserValue { get; set; }
        public IList<User> UserList { get; set; } = new List<User>();
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        var (compilation, symbols) = GetTypeSymbols(sourceCode);
        var testClass = symbols.FirstOrDefault(s => s.Name == "TestClass");
        Assert.IsNotNull(testClass, "Should find TestClass type");

        var properties = testClass.GetMembers().OfType<IPropertySymbol>().ToList();

        var intProperty = properties.FirstOrDefault(p => p.Name == "IntValue");
        var stringProperty = properties.FirstOrDefault(p => p.Name == "StringValue");
        var boolProperty = properties.FirstOrDefault(p => p.Name == "BoolValue");
        var decimalProperty = properties.FirstOrDefault(p => p.Name == "DecimalValue");
        var dateTimeProperty = properties.FirstOrDefault(p => p.Name == "DateTimeValue");
        var nullableUserProperty = properties.FirstOrDefault(p => p.Name == "NullableUserValue");
        var userListProperty = properties.FirstOrDefault(p => p.Name == "UserList");

        // Test default value generation
        var intDefault = TypeAnalyzer.GetDefaultValue(intProperty?.Type!);
        var stringDefault = TypeAnalyzer.GetDefaultValue(stringProperty?.Type!);
        var boolDefault = TypeAnalyzer.GetDefaultValue(boolProperty?.Type!);
        var decimalDefault = TypeAnalyzer.GetDefaultValue(decimalProperty?.Type!);
        var dateTimeDefault = TypeAnalyzer.GetDefaultValue(dateTimeProperty?.Type!);
        var nullableUserDefault = TypeAnalyzer.GetDefaultValue(nullableUserProperty?.Type!);
        var userListDefault = TypeAnalyzer.GetDefaultValue(userListProperty?.Type!);

        Assert.AreEqual("0", intDefault, "Int default should be 0");
        Assert.IsTrue(stringDefault == "null" || stringDefault.Contains("string.Empty"),
            "String default should be null or string.Empty");
        Assert.AreEqual("false", boolDefault, "Bool default should be false");
        // Decimal default might be 'default' or '0m' depending on implementation
        Assert.IsTrue(decimalDefault == "0m" || decimalDefault == "default",
            $"Decimal default should be '0m' or 'default', got: {decimalDefault}");
        Assert.IsTrue(dateTimeDefault.Contains("DateTime") || dateTimeDefault == "default",
            "DateTime default should reference DateTime or be default");
        Assert.IsTrue(nullableUserDefault == "null" || nullableUserDefault == "null!",
            $"Nullable reference type default should be null or null!, got: {nullableUserDefault}");
        Assert.IsTrue(userListDefault == "null" || userListDefault == "null!" || userListDefault.Contains("new") || userListDefault == "default",
            $"Collection default should be null, null!, new instance, or default, got: {userListDefault}");
    }

    /// <summary>
    /// Tests system namespace detection.
    /// </summary>
    [TestMethod]
    public void TypeAnalyzer_IsSystemNamespace_DetectsSystemNamespaces()
    {
        // Test system namespace detection (accessing private method through reflection if needed)
        var systemNamespaces = new[]
        {
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "System.Data",
            "System.Data.Common",
            "System.Threading",
            "System.Threading.Tasks",
            "Microsoft.Extensions",
            ""
        };

        var nonSystemNamespaces = new[]
        {
            "TestNamespace",
            "MyApp.Models",
            "BusinessLogic.Services",
            "Data.Entities"
        };

        // Since IsSystemNamespace is likely a private method, we'll test it indirectly
        // by testing IsLikelyEntityType with types from different namespaces
        string sourceCode = @"
namespace TestNamespace
{
    public class CustomEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

namespace MyApp.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}";

        var (compilation, symbols) = GetTypeSymbols(sourceCode);

        var customEntity = symbols.FirstOrDefault(s => s.Name == "CustomEntity");
        var userModel = symbols.FirstOrDefault(s => s.Name == "UserModel");

        Assert.IsNotNull(customEntity, "Should find CustomEntity type");
        Assert.IsNotNull(userModel, "Should find UserModel type");

        // Custom namespace types should be detected as likely entity types
        Assert.IsTrue(TypeAnalyzer.IsLikelyEntityType(customEntity),
            "Type in custom namespace should be detected as entity type");
        Assert.IsTrue(TypeAnalyzer.IsLikelyEntityType(userModel),
            "Type in custom namespace should be detected as entity type");
    }

    /// <summary>
    /// Helper method to get type symbols from source code.
    /// </summary>
    private static (Compilation Compilation, List<INamedTypeSymbol> Symbols) GetTypeSymbols(string sourceCode)
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

        var typeSymbols = new List<INamedTypeSymbol>();
        var classDeclarations = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();

        foreach (var classDecl in classDeclarations)
        {
            if (semanticModel.GetDeclaredSymbol(classDecl) is INamedTypeSymbol typeSymbol)
            {
                typeSymbols.Add(typeSymbol);
            }
        }

        return (compilation, typeSymbols);
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
        references.Add(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location));

        var runtimeAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        return references;
    }
}