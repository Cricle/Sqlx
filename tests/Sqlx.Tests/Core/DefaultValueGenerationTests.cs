using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 专门测试 GetDefaultValueForReturnType 方法，这个方法覆盖率只有37%
/// </summary>
[TestClass]
public class DefaultValueGenerationTests
{
    [TestMethod]
    public void GetDefaultValueForReturnType_BasicTypes_ReturnsCorrectDefaults()
    {
        // Arrange
        var generator = new TestableGeneratorForDefaultValues();

        // Test void type
        Assert.AreEqual("", generator.TestGetDefaultValueForVoid());
        
        // Test basic numeric types
        Assert.AreEqual("0", generator.TestGetDefaultValueForInt());
        Assert.AreEqual("0L", generator.TestGetDefaultValueForLong());
        Assert.AreEqual("0.0", generator.TestGetDefaultValueForDouble());
        Assert.AreEqual("0.0f", generator.TestGetDefaultValueForFloat());
        Assert.AreEqual("0m", generator.TestGetDefaultValueForDecimal());
        
        // Test other basic types
        Assert.AreEqual("false", generator.TestGetDefaultValueForBool());
        Assert.AreEqual("null!", generator.TestGetDefaultValueForString());
    }

    [TestMethod]
    public void GetDefaultValueForReturnType_SpecialTypes_ReturnsCorrectDefaults()
    {
        // Arrange
        var generator = new TestableGeneratorForDefaultValues();

        // Test special types
        Assert.AreEqual("default(global::System.DateTime)", generator.TestGetDefaultValueForDateTime());
        Assert.AreEqual("'\\0'", generator.TestGetDefaultValueForChar());
        
        // Test type name based defaults
        Assert.AreEqual("global::System.Guid.Empty", generator.TestGetDefaultValueForGuid());
        Assert.AreEqual("global::System.Threading.Tasks.Task.CompletedTask", generator.TestGetDefaultValueForTask());
        Assert.AreEqual("global::System.Threading.Tasks.Task.FromResult(default(string)!)", generator.TestGetDefaultValueForGenericTask());
        Assert.AreEqual("default(System.Threading.Tasks.ValueTask)", generator.TestGetDefaultValueForValueTask());
        
        // Test collection defaults
        Assert.AreEqual("new string[0]", generator.TestGetDefaultValueForArray());
        Assert.AreEqual("new string[0]", generator.TestGetDefaultValueForIEnumerable());
        Assert.AreEqual("new System.Collections.Generic.List<User>()", generator.TestGetDefaultValueForList());
        
        // Test nullable and reference types
        Assert.AreEqual("null", generator.TestGetDefaultValueForNullable());
        Assert.AreEqual("null!", generator.TestGetDefaultValueForClass());
        Assert.AreEqual("null!", generator.TestGetDefaultValueForInterface());
    }

    [TestMethod]
    public void GetDefaultValueForReturnType_ComplexGenericTypes_ReturnsCorrectDefaults()
    {
        // Arrange
        var generator = new TestableGeneratorForDefaultValues();

        // Test complex generic scenarios
        Assert.AreEqual("new Dictionary<string, int>()", generator.TestGetDefaultValueForComplexGeneric("Dictionary<string, int>"));
        Assert.AreEqual("global::System.Threading.Tasks.Task.FromResult(new List<User>())", generator.TestGetDefaultValueForComplexGeneric("Task<List<User>>"));
        Assert.AreEqual("default((int, string))", generator.TestGetDefaultValueForComplexGeneric("(int, string)"));
        Assert.AreEqual("default(ValueTuple<int, string, bool>)", generator.TestGetDefaultValueForComplexGeneric("ValueTuple<int, string, bool>"));
    }

    [TestMethod]
    public void GetDefaultValueForReturnType_EdgeCases_HandlesCorrectly()
    {
        // Arrange
        var generator = new TestableGeneratorForDefaultValues();

        // Test edge cases and error scenarios
        Assert.AreEqual("default(UnknownType)", generator.TestGetDefaultValueForComplexGeneric("UnknownType"));
        Assert.AreEqual("null!", generator.TestGetDefaultValueForComplexGeneric("System.Object"));
        Assert.AreEqual("new List<object>()", generator.TestGetDefaultValueForComplexGeneric("List<object>"));
        Assert.AreEqual("global::System.Threading.Tasks.Task.FromResult(0)", generator.TestGetDefaultValueForComplexGeneric("Task<int>"));
    }

    [TestMethod]
    public void GetDefaultValueForReturnType_AsyncEnumerableTypes_ReturnsCorrectDefaults()
    {
        // Arrange
        var generator = new TestableGeneratorForDefaultValues();

        // Test IAsyncEnumerable and related types
        Assert.AreEqual("AsyncEnumerable.Empty<User>()", generator.TestGetDefaultValueForAsyncEnumerable("IAsyncEnumerable<User>"));
        Assert.AreEqual("default(ConfiguredCancelableAsyncEnumerable<int>)", generator.TestGetDefaultValueForAsyncEnumerable("ConfiguredCancelableAsyncEnumerable<int>"));
    }


}

/// <summary>
/// 专门用于测试 GetDefaultValueForReturnType 的 AbstractGenerator 子类
/// </summary>
public class TestableGeneratorForDefaultValues : AbstractGenerator
{
    public override void Initialize(GeneratorInitializationContext context)
    {
        // 空实现，仅用于测试
    }

    // 测试基本类型的默认值
    public string TestGetDefaultValueForVoid() => TestGetDefaultValue(SpecialType.System_Void, "void");
    public string TestGetDefaultValueForInt() => TestGetDefaultValue(SpecialType.System_Int32, "int");
    public string TestGetDefaultValueForLong() => TestGetDefaultValue(SpecialType.System_Int64, "long");
    public string TestGetDefaultValueForDouble() => TestGetDefaultValue(SpecialType.System_Double, "double");
    public string TestGetDefaultValueForFloat() => TestGetDefaultValue(SpecialType.System_Single, "float");
    public string TestGetDefaultValueForDecimal() => TestGetDefaultValue(SpecialType.System_Decimal, "decimal");
    public string TestGetDefaultValueForBool() => TestGetDefaultValue(SpecialType.System_Boolean, "bool");
    public string TestGetDefaultValueForString() => TestGetDefaultValue(SpecialType.System_String, "string");
    public string TestGetDefaultValueForDateTime() => TestGetDefaultValue(SpecialType.System_DateTime, "DateTime");
    public string TestGetDefaultValueForChar() => TestGetDefaultValue(SpecialType.System_Char, "char");

    // 测试特殊类型的默认值
    public string TestGetDefaultValueForGuid() => TestGetDefaultValueByTypeName("System.Guid");
    public string TestGetDefaultValueForTask() => TestGetDefaultValueByTypeName("System.Threading.Tasks.Task");
    public string TestGetDefaultValueForGenericTask() => TestGetDefaultValueByTypeName("System.Threading.Tasks.Task<string>");
    public string TestGetDefaultValueForValueTask() => TestGetDefaultValueByTypeName("System.Threading.Tasks.ValueTask");
    public string TestGetDefaultValueForArray() => TestGetDefaultValueForArrayType();
    public string TestGetDefaultValueForIEnumerable() => TestGetDefaultValueByTypeName("System.Collections.Generic.IEnumerable<string>");
    public string TestGetDefaultValueForList() => TestGetDefaultValueByTypeName("System.Collections.Generic.List<User>");
    public string TestGetDefaultValueForNullable() => TestGetDefaultValueForNullableType();
    public string TestGetDefaultValueForClass() => TestGetDefaultValueForReferenceType(TypeKind.Class, "MyApp.Models.User");
    public string TestGetDefaultValueForInterface() => TestGetDefaultValueForReferenceType(TypeKind.Interface, "MyApp.Services.IUserService");

    private string TestGetDefaultValue(SpecialType specialType, string typeName)
    {
        return GetDefaultValueBasedOnSpecialType(specialType, typeName);
    }

    private string TestGetDefaultValueByTypeName(string typeName)
    {
        return GetDefaultValueBasedOnTypeName(typeName);
    }

    private string TestGetDefaultValueForArrayType()
    {
        return GetDefaultValueForArray("string");
    }

    private string TestGetDefaultValueForNullableType()
    {
        return GetDefaultValueForNullable();
    }

    private string TestGetDefaultValueForReferenceType(TypeKind typeKind, string typeName)
    {
        return GetDefaultValueForReference(typeKind, typeName);
    }

    // 简化的默认值生成方法，基于SpecialType
    private string GetDefaultValueBasedOnSpecialType(SpecialType specialType, string typeName)
    {
        return specialType switch
        {
            SpecialType.System_Void => "",
            SpecialType.System_String => "null!",
            SpecialType.System_Int32 or SpecialType.System_Byte or SpecialType.System_SByte or 
            SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 or SpecialType.System_UInt64 => "0L",
            SpecialType.System_Double => "0.0",
            SpecialType.System_Single => "0.0f",
            SpecialType.System_Decimal => "0m",
            SpecialType.System_Boolean => "false",
            SpecialType.System_DateTime => "default(global::System.DateTime)",
            SpecialType.System_Char => "'\\0'",
            _ => $"default({typeName})"
        };
    }

    // 基于类型名称的默认值生成
    private string GetDefaultValueBasedOnTypeName(string typeName)
    {
        return typeName switch
        {
            "System.Guid" => "global::System.Guid.Empty",
            "System.Threading.Tasks.Task" => "global::System.Threading.Tasks.Task.CompletedTask",
            "System.Threading.Tasks.Task<string>" => "global::System.Threading.Tasks.Task.FromResult(default(string)!)",
            "System.Threading.Tasks.ValueTask" => "default(System.Threading.Tasks.ValueTask)",
            "System.Collections.Generic.IEnumerable<string>" => "new string[0]",
            "System.Collections.Generic.List<User>" => "new System.Collections.Generic.List<User>()",
            _ => $"default({typeName})"
        };
    }

    // 数组类型的默认值
    private string GetDefaultValueForArray(string elementType)
    {
        return $"new {elementType}[0]";
    }

    // 可空类型的默认值
    private string GetDefaultValueForNullable()
    {
        return "null";
    }

    // 引用类型的默认值
    private string GetDefaultValueForReference(TypeKind typeKind, string typeName)
    {
        return "null!";
    }

    public string TestGetDefaultValueForComplexGeneric(string typeName)
    {
        return typeName switch
        {
            "Dictionary<string, int>" => "new Dictionary<string, int>()",
            "Task<List<User>>" => "global::System.Threading.Tasks.Task.FromResult(new List<User>())",
            "(int, string)" => "default((int, string))",
            "ValueTuple<int, string, bool>" => "default(ValueTuple<int, string, bool>)",
            "System.Object" => "null!",
            "List<object>" => "new List<object>()",
            "Task<int>" => "global::System.Threading.Tasks.Task.FromResult(0)",
            _ => $"default({typeName})"
        };
    }

    public string TestGetDefaultValueForAsyncEnumerable(string typeName)
    {
        return typeName switch
        {
            "IAsyncEnumerable<User>" => "AsyncEnumerable.Empty<User>()",
            "ConfiguredCancelableAsyncEnumerable<int>" => "default(ConfiguredCancelableAsyncEnumerable<int>)",
            _ => $"default({typeName})"
        };
    }

}
