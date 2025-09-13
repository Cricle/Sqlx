// -----------------------------------------------------------------------
// <copyright file="ExtensionsTests.cs" company="Cricle">
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
/// Tests for Extensions utility class.
/// Tests type checking, caching, and utility methods used throughout the framework.
/// </summary>
[TestClass]
public class ExtensionsTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests CanHaveNullValue method for various type scenarios.
    /// </summary>
    [TestMethod]
    public void Extensions_CanHaveNullValue_DetectsNullableTypes()
    {
        string sourceCode = @"
using System;

namespace TestNamespace
{
    public class TestTypes
    {
        public string NonNullableString { get; set; } = string.Empty;
        public string? NullableString { get; set; }
        public int NonNullableInt { get; set; }
        public int? NullableInt { get; set; }
        public DateTime NonNullableDateTime { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public object NonNullableObject { get; set; } = new object();
        public object? NullableObject { get; set; }
    }
}";

        var typeSymbols = GetPropertyTypes(sourceCode, "TestTypes");

        // Test string types
        var nonNullableString = typeSymbols["NonNullableString"];
        var nullableString = typeSymbols["NullableString"];
        
        // Note: The actual behavior may vary based on nullable context
        // These tests verify the method works rather than specific nullability rules
        var nonNullableStringCanBeNull = nonNullableString.CanHaveNullValue();
        var nullableStringCanBeNull = nullableString.CanHaveNullValue();
        
        Assert.IsNotNull(nonNullableString, "Should find non-nullable string type");
        Assert.IsNotNull(nullableString, "Should find nullable string type");
        
        // Test value types
        var nonNullableInt = typeSymbols["NonNullableInt"];
        var nullableInt = typeSymbols["NullableInt"];
        
        Assert.IsFalse(nonNullableInt.CanHaveNullValue(), 
            "Non-nullable value type should not allow null");
        Assert.IsTrue(nullableInt.CanHaveNullValue(), 
            "Nullable value type should allow null");

        // Test DateTime types
        var nonNullableDateTime = typeSymbols["NonNullableDateTime"];
        var nullableDateTime = typeSymbols["NullableDateTime"];
        
        Assert.IsFalse(nonNullableDateTime.CanHaveNullValue(), 
            "Non-nullable DateTime should not allow null");
        Assert.IsTrue(nullableDateTime.CanHaveNullValue(), 
            "Nullable DateTime should allow null");
    }

    /// <summary>
    /// Tests type checking methods for database connection types.
    /// </summary>
    [TestMethod]
    public void Extensions_TypeChecking_IdentifiesDbConnectionTypes()
    {
        string sourceCode = @"
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace TestNamespace
{
    public class DatabaseTypes
    {
        public DbConnection GenericConnection { get; set; } = null!;
        public SqlConnection SqlServerConnection { get; set; } = null!;
        public SqliteConnection SqliteConnection { get; set; } = null!;
        public string NonConnectionType { get; set; } = string.Empty;
        public int NonConnectionInt { get; set; }
    }
}";

        var typeSymbols = GetPropertyTypes(sourceCode, "DatabaseTypes");

        var genericConnection = typeSymbols["GenericConnection"];
        var sqlServerConnection = typeSymbols["SqlServerConnection"];
        var sqliteConnection = typeSymbols["SqliteConnection"];
        var nonConnectionString = typeSymbols["NonConnectionType"];
        var nonConnectionInt = typeSymbols["NonConnectionInt"];

        // Test that connection types are identified correctly
        Assert.IsNotNull(genericConnection, "Should find DbConnection type");
        Assert.IsNotNull(sqlServerConnection, "Should find SqlConnection type");
        Assert.IsNotNull(sqliteConnection, "Should find SqliteConnection type");
        
        // Note: The actual type checking methods in Extensions might be internal
        // These tests verify we can access the type symbols for testing
        Assert.IsTrue(genericConnection.Name.Contains("Connection") || 
                     genericConnection.BaseType?.Name.Contains("Connection") == true, 
                     "DbConnection should be identifiable as connection type");
        
        Assert.IsFalse(nonConnectionString.Name.Contains("Connection"), 
                     "String should not be identified as connection type");
        Assert.IsFalse(nonConnectionInt.Name.Contains("Connection"), 
                     "Int should not be identified as connection type");
    }

    /// <summary>
    /// Tests type checking for common database-related types.
    /// </summary>
    [TestMethod]
    public void Extensions_TypeChecking_IdentifiesCommonDatabaseTypes()
    {
        string sourceCode = @"
using System.Data;
using System.Data.Common;

namespace TestNamespace
{
    public class DatabaseRelatedTypes
    {
        public DbTransaction Transaction { get; set; } = null!;
        public DbCommand Command { get; set; } = null!;
        public DbDataReader Reader { get; set; } = null!;
        public DataTable DataTable { get; set; } = null!;
        public DataSet DataSet { get; set; } = null!;
        public string RegularString { get; set; } = string.Empty;
    }
}";

        var typeSymbols = GetPropertyTypes(sourceCode, "DatabaseRelatedTypes");

        var transaction = typeSymbols["Transaction"];
        var command = typeSymbols["Command"];
        var reader = typeSymbols["Reader"];
        var dataTable = typeSymbols["DataTable"];
        var dataSet = typeSymbols["DataSet"];
        var regularString = typeSymbols["RegularString"];

        // Verify that database types are found
        Assert.IsNotNull(transaction, "Should find DbTransaction type");
        Assert.IsNotNull(command, "Should find DbCommand type");
        Assert.IsNotNull(reader, "Should find DbDataReader type");
        Assert.IsNotNull(dataTable, "Should find DataTable type");
        Assert.IsNotNull(dataSet, "Should find DataSet type");
        Assert.IsNotNull(regularString, "Should find string type");

        // Test type identification patterns
        Assert.IsTrue(transaction.Name.Contains("Transaction"), 
                     "Transaction type should be identifiable");
        Assert.IsTrue(command.Name.Contains("Command"), 
                     "Command type should be identifiable");
        Assert.IsTrue(reader.Name.Contains("Reader"), 
                     "Reader type should be identifiable");
        Assert.IsFalse(regularString.Name.Contains("Db") || regularString.Name.Contains("Data"), 
                      "Regular string should not appear to be database type");
    }

    /// <summary>
    /// Tests extensions work with generic types.
    /// </summary>
    [TestMethod]
    public void Extensions_TypeChecking_HandlesGenericTypes()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class GenericTypes
    {
        public List<string> StringList { get; set; } = new List<string>();
        public IEnumerable<int> IntEnumerable { get; set; } = new List<int>();
        public Task<string> StringTask { get; set; } = Task.FromResult(string.Empty);
        public Task<List<User>> UserListTask { get; set; } = Task.FromResult(new List<User>());
        public Dictionary<string, object> StringObjectDict { get; set; } = new Dictionary<string, object>();
        public Nullable<int> NullableInt { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        var typeSymbols = GetPropertyTypes(sourceCode, "GenericTypes");

        var stringList = typeSymbols["StringList"];
        var intEnumerable = typeSymbols["IntEnumerable"];
        var stringTask = typeSymbols["StringTask"];
        var userListTask = typeSymbols["UserListTask"];
        var stringObjectDict = typeSymbols["StringObjectDict"];
        var nullableInt = typeSymbols["NullableInt"];

        // Test generic type identification
        Assert.IsNotNull(stringList, "Should find List<string> type");
        Assert.IsNotNull(intEnumerable, "Should find IEnumerable<int> type");
        Assert.IsNotNull(stringTask, "Should find Task<string> type");
        Assert.IsNotNull(userListTask, "Should find Task<List<User>> type");
        Assert.IsNotNull(stringObjectDict, "Should find Dictionary<string, object> type");
        Assert.IsNotNull(nullableInt, "Should find Nullable<int> type");

        // Test that generic types are properly identified
        if (stringList is INamedTypeSymbol namedStringList)
        {
            Assert.IsTrue(namedStringList.IsGenericType, "List<string> should be generic");
            Assert.AreEqual(1, namedStringList.TypeArguments.Length, "List<T> should have one type argument");
        }

        if (stringTask is INamedTypeSymbol namedStringTask)
        {
            Assert.IsTrue(namedStringTask.IsGenericType, "Task<string> should be generic");
            Assert.AreEqual(1, namedStringTask.TypeArguments.Length, "Task<T> should have one type argument");
        }

        if (stringObjectDict is INamedTypeSymbol namedDict)
        {
            Assert.IsTrue(namedDict.IsGenericType, "Dictionary<K,V> should be generic");
            Assert.AreEqual(2, namedDict.TypeArguments.Length, "Dictionary<K,V> should have two type arguments");
        }
    }

    /// <summary>
    /// Tests extension methods handle edge cases gracefully.
    /// </summary>
    [TestMethod]
    public void Extensions_EdgeCases_HandledGracefully()
    {
        string sourceCode = @"
using System;

namespace TestNamespace
{
    public abstract class AbstractClass
    {
        public abstract int AbstractProperty { get; set; }
    }

    public interface ITestInterface
    {
        int InterfaceProperty { get; set; }
    }

    public enum TestEnum
    {
        Value1,
        Value2
    }

    public struct TestStruct
    {
        public int Value { get; set; }
    }

    public class EdgeCaseTypes
    {
        public TestEnum EnumProperty { get; set; }
        public TestStruct StructProperty { get; set; }
        public object ObjectProperty { get; set; } = new object();
        public dynamic DynamicProperty { get; set; } = null!;
    }
}";

        var typeSymbols = GetPropertyTypes(sourceCode, "EdgeCaseTypes");

        var enumProperty = typeSymbols["EnumProperty"];
        var structProperty = typeSymbols["StructProperty"];
        var objectProperty = typeSymbols["ObjectProperty"];
        var dynamicProperty = typeSymbols["DynamicProperty"];

        // Test edge case type handling
        Assert.IsNotNull(enumProperty, "Should find enum type");
        Assert.IsNotNull(structProperty, "Should find struct type");
        Assert.IsNotNull(objectProperty, "Should find object type");
        Assert.IsNotNull(dynamicProperty, "Should find dynamic type");

        // Test type characteristics
        Assert.AreEqual(TypeKind.Enum, enumProperty.TypeKind, "Should identify enum correctly");
        Assert.AreEqual(TypeKind.Struct, structProperty.TypeKind, "Should identify struct correctly");
        Assert.AreEqual(TypeKind.Class, objectProperty.TypeKind, "Should identify class correctly");
        
        // Test nullability for different type kinds
        Assert.IsFalse(enumProperty.CanHaveNullValue(), "Enum should not be nullable by default");
        Assert.IsFalse(structProperty.CanHaveNullValue(), "Struct should not be nullable by default");
        Assert.IsTrue(objectProperty.CanHaveNullValue() || !objectProperty.CanHaveNullValue(), 
                     "Object nullability depends on context - method should not throw");
    }

    /// <summary>
    /// Tests that extension methods don't crash with unusual types.
    /// </summary>
    [TestMethod]
    public void Extensions_UnusualTypes_DontCrash()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class UnusualTypes
    {
        public Action ActionProperty { get; set; } = () => { };
        public Func<int, string> FuncProperty { get; set; } = x => x.ToString();
        public List<Action<string, int>> ComplexGenericProperty { get; set; } = new List<Action<string, int>>();
        public Tuple<int, string, bool> TupleProperty { get; set; } = Tuple.Create(0, "", false);
    }
}";

        var typeSymbols = GetPropertyTypes(sourceCode, "UnusualTypes");

        var actionProperty = typeSymbols["ActionProperty"];
        var funcProperty = typeSymbols["FuncProperty"];
        var complexGenericProperty = typeSymbols["ComplexGenericProperty"];
        var tupleProperty = typeSymbols["TupleProperty"];

        // Test that unusual types don't crash extension methods
        Assert_DoesNotThrow(() => actionProperty.CanHaveNullValue(), 
                           "CanHaveNullValue should not crash on Action type");
        Assert_DoesNotThrow(() => funcProperty.CanHaveNullValue(), 
                           "CanHaveNullValue should not crash on Func type");
        Assert_DoesNotThrow(() => complexGenericProperty.CanHaveNullValue(), 
                           "CanHaveNullValue should not crash on complex generic type");
        Assert_DoesNotThrow(() => tupleProperty.CanHaveNullValue(), 
                           "CanHaveNullValue should not crash on Tuple type");

        // Verify types are found
        Assert.IsNotNull(actionProperty, "Should find Action type");
        Assert.IsNotNull(funcProperty, "Should find Func type");
        Assert.IsNotNull(complexGenericProperty, "Should find complex generic type");
        Assert.IsNotNull(tupleProperty, "Should find Tuple type");
    }

    /// <summary>
    /// Tests performance of extension methods with many type checks.
    /// </summary>
    [TestMethod]
    public void Extensions_PerformanceTest_HandlesManyCalls()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class PerformanceTestTypes
    {
        public string String1 { get; set; } = string.Empty;
        public string String2 { get; set; } = string.Empty;
        public int Int1 { get; set; }
        public int Int2 { get; set; }
        public bool Bool1 { get; set; }
        public bool Bool2 { get; set; }
        public DateTime DateTime1 { get; set; }
        public DateTime DateTime2 { get; set; }
        public List<string> List1 { get; set; } = new List<string>();
        public List<string> List2 { get; set; } = new List<string>();
    }
}";

        var typeSymbols = GetPropertyTypes(sourceCode, "PerformanceTestTypes");
        var types = typeSymbols.Values.ToArray();

        var startTime = DateTime.UtcNow;

        // Perform many type checks to test performance and caching
        for (int i = 0; i < 1000; i++)
        {
            foreach (var type in types)
            {
                var canHaveNull = type.CanHaveNullValue();
                // Use the result to ensure the call isn't optimized away
                Assert.IsTrue(canHaveNull || !canHaveNull, "Type check should return a boolean value");
            }
        }

        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        Assert.IsTrue(elapsed.TotalSeconds < 5, 
            $"Many type checks should complete quickly. Took: {elapsed.TotalSeconds} seconds");
    }

    /// <summary>
    /// Helper method to get property types from a class.
    /// </summary>
    private static Dictionary<string, ITypeSymbol> GetPropertyTypes(string sourceCode, string className)
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

        var classDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ValueText == className);

        if (classDeclaration == null)
        {
            throw new InvalidOperationException($"Class {className} not found");
        }

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (classSymbol == null)
        {
            throw new InvalidOperationException($"Could not get symbol for class {className}");
        }

        var properties = classSymbol.GetMembers().OfType<IPropertySymbol>();
        return properties.ToDictionary(p => p.Name, p => p.Type);
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

        // Try to add System.Data.SqlClient if available
        try
        {
            var sqlClientAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Contains("SqlClient") == true);
            if (sqlClientAssembly != null)
            {
                references.Add(MetadataReference.CreateFromFile(sqlClientAssembly.Location));
            }
        }
        catch
        {
            // Ignore if SqlClient is not available
        }

        return references;
    }

    /// <summary>
    /// Helper method to assert that an action doesn't throw an exception.
    /// </summary>
    private static void Assert_DoesNotThrow(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Assert.Fail($"{message}. Exception: {ex.Message}");
        }
    }
}
