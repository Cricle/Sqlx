// -----------------------------------------------------------------------
// <copyright file="TypeAnalyzerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class TypeAnalyzerTests
{
    private static Compilation CreateTestCompilation(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [TestMethod]
    public void IsLikelyEntityType_WithNull_ReturnsFalse()
    {
        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithPrimitiveType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(intType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithSystemNamespace_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(stringType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithEntityClass_ReturnsTrue()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(userType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithValueType_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace MyApp
{
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var pointType = compilation.GetTypeByMetadataName("MyApp.Point");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(pointType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithInterface_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace MyApp
{
    public interface IUser
    {
        int Id { get; set; }
        string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var interfaceType = compilation.GetTypeByMetadataName("MyApp.IUser");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(interfaceType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithEnum_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace MyApp
{
    public enum UserStatus
    {
        Active,
        Inactive
    }
}";
        var compilation = CreateTestCompilation(code);
        var enumType = compilation.GetTypeByMetadataName("MyApp.UserStatus");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(enumType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLikelyEntityType_WithAbstractClass_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace MyApp
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var abstractType = compilation.GetTypeByMetadataName("MyApp.BaseEntity");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(abstractType);

        // Assert
        Assert.IsTrue(result); // Abstract classes with properties are considered entity types
    }

    [TestMethod]
    public void IsLikelyEntityType_WithStaticClass_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace MyApp
{
    public static class UserHelper
    {
        public static void DoSomething() { }
    }
}";
        var compilation = CreateTestCompilation(code);
        var staticType = compilation.GetTypeByMetadataName("MyApp.UserHelper");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(staticType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsCollectionType_WithNonNamedType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.IsCollectionType(intType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsCollectionType_WithCollectionTypes_ReturnsTrue()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;
namespace MyApp
{
    public class Test
    {
        public List<string> StringList { get; set; }
        public IEnumerable<int> IntEnumerable { get; set; }
        public ICollection<object> ObjectCollection { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Test");
        
        // Act & Assert
        var stringListProperty = testType?.GetMembers("StringList").FirstOrDefault() as IPropertySymbol;
        var intEnumerableProperty = testType?.GetMembers("IntEnumerable").FirstOrDefault() as IPropertySymbol;
        var objectCollectionProperty = testType?.GetMembers("ObjectCollection").FirstOrDefault() as IPropertySymbol;

        Assert.IsTrue(TypeAnalyzer.IsCollectionType(stringListProperty?.Type!));
        Assert.IsTrue(TypeAnalyzer.IsCollectionType(intEnumerableProperty?.Type!));
        Assert.IsTrue(TypeAnalyzer.IsCollectionType(objectCollectionProperty?.Type!));
    }

    [TestMethod]
    public void IsCollectionType_WithNonCollectionType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsCollectionType(stringType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ExtractEntityType_WithNullType_ReturnsNull()
    {
        // Act
        var result = TypeAnalyzer.ExtractEntityType(null);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractEntityType_WithNonGenericType_ReturnsSameType()
    {
        // Arrange
        var compilation = CreateTestCompilation("class User { public int Id { get; set; } }");
        var userType = compilation.GetTypeByMetadataName("User");

        // Act
        var result = TypeAnalyzer.ExtractEntityType(userType!);

        // Assert
        Assert.AreEqual(userType, result);
    }

    [TestMethod]
    public void ExtractEntityType_WithGenericCollection_ReturnsElementType()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;
namespace MyApp
{
    public class User { }
    public class Test
    {
        public List<User> Users { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Test");
        var usersProperty = testType?.GetMembers("Users").FirstOrDefault() as IPropertySymbol;

        // Act
        var result = TypeAnalyzer.ExtractEntityType(usersProperty?.Type!);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("User", result?.Name);
    }

    [TestMethod]
    public void ExtractEntityType_WithTask_ReturnsTaskResult()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;
namespace MyApp
{
    public class User { }
    public class Test
    {
        public Task<User> GetUserAsync() => null;
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Test");
        var getUserMethod = testType?.GetMembers("GetUserAsync").FirstOrDefault() as IMethodSymbol;

        // Act
        var result = TypeAnalyzer.ExtractEntityType(getUserMethod?.ReturnType!);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("User", result?.Name);
    }

    [TestMethod]
    public void ExtractEntityType_WithNestedGeneric_HandlesCorrectly()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;
using System.Threading.Tasks;
namespace MyApp
{
    public class User { }
    public class Test
    {
        public Task<List<User>> GetUsersAsync() => null;
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Test");
        var getUsersMethod = testType?.GetMembers("GetUsersAsync").FirstOrDefault() as IMethodSymbol;

        // Act
        var result = TypeAnalyzer.ExtractEntityType(getUsersMethod?.ReturnType!);

        // Assert
        Assert.IsNotNull(result);
        // Should extract the User from Task<List<User>>
        Assert.AreEqual("User", result?.Name);
    }

    [TestMethod]
    public void IsSystemNamespace_Performance_ExecutesQuickly()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var startTime = System.DateTime.UtcNow;
        for (int i = 0; i < 1000; i++)
        {
            TypeAnalyzer.IsLikelyEntityType(stringType);
        }
        var endTime = System.DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        Assert.IsTrue(duration.TotalMilliseconds < 100, $"Performance test took {duration.TotalMilliseconds}ms, expected < 100ms");
    }
}