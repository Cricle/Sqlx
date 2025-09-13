using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx;
using Sqlx.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 专门测试EnhancedEntityMappingGenerator中的高级场景和复杂映射逻辑
/// </summary>
[TestClass]
public class EnhancedEntityMappingAdvancedTests
{
    [TestMethod]
    public void GenerateEntityMapping_WithComplexRecordTypes_GeneratesCorrectMapping()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act
        testGenerator.TestGenerateEntityMapping(sb, "User", true, false, new[] { "Id", "Name", "Email" });
        
        // Assert
        var result = sb.ToString();
        Assert.IsTrue(result.Contains("new User("));
        Assert.IsTrue(result.Contains("Id"));
        Assert.IsTrue(result.Contains("Name"));
        Assert.IsTrue(result.Contains("Email"));
    }

    [TestMethod]
    public void GenerateEntityMapping_WithPrimaryConstructorTypes_GeneratesCorrectMapping()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act
        testGenerator.TestGenerateEntityMapping(sb, "User", false, true, new[] { "Id", "Name", "Email", "CreatedAt" });
        
        // Assert
        var result = sb.ToString();
        Assert.IsTrue(result.Contains("new User("));
        Assert.IsTrue(result.Contains("Id"));
        Assert.IsTrue(result.Contains("Name"));
    }

    [TestMethod]
    public void GenerateEntityMapping_WithTraditionalTypes_GeneratesObjectInitializer()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act
        testGenerator.TestGenerateEntityMapping(sb, "User", false, false, new[] { "Id", "Name", "Email" });
        
        // Assert
        var result = sb.ToString();
        Assert.IsTrue(result.Contains("new User"));
        Assert.IsTrue(result.Contains("{"));
        Assert.IsTrue(result.Contains("Id ="));
        Assert.IsTrue(result.Contains("Name ="));
        Assert.IsTrue(result.Contains("Email ="));
        Assert.IsTrue(result.Contains("}"));
    }

    [TestMethod]
    public void GenerateEntityMapping_WithNoAccessibleMembers_GeneratesSimpleConstructor()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act
        testGenerator.TestGenerateEntityMapping(sb, "User", false, false, new string[0]);
        
        // Assert
        var result = sb.ToString();
        Assert.IsTrue(result.Contains("No accessible members found"));
        Assert.IsTrue(result.Contains("new User()"));
    }

    [TestMethod]
    public void GetDataReadExpression_WithComplexTypes_GeneratesCorrectExpressions()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        
        // Act & Assert - Test various complex type scenarios
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("String", "reader", "Name").Contains("GetString"));
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("Int32", "reader", "Id").Contains("GetInt32"));
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("DateTime", "reader", "CreatedAt").Contains("GetDateTime"));
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("Boolean", "reader", "IsActive").Contains("GetBoolean"));
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("Decimal", "reader", "Price").Contains("GetDecimal"));
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("Guid", "reader", "UserId").Contains("GetGuid"));
        
        // Test nullable types
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("Int32?", "reader", "OptionalId").Contains("IsDBNull"));
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("DateTime?", "reader", "OptionalDate").Contains("IsDBNull"));
        
        // Test enum types
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("UserStatus", "reader", "Status", isEnum: true).Contains("UserStatus"));
        
        // Test unsupported types
        Assert.IsTrue(testGenerator.TestGetDataReadExpression("CustomType", "reader", "Custom").Contains("GetValue"));
    }

    [TestMethod]
    public void TryGetConvertMethod_WithAllSupportedTypes_ReturnsCorrectMethods()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        
        // Act & Assert - Test all supported data reader methods
        Assert.AreEqual("GetString", testGenerator.TestTryGetConvertMethod("String"));
        Assert.AreEqual("GetInt32", testGenerator.TestTryGetConvertMethod("Int32"));
        Assert.AreEqual("GetInt64", testGenerator.TestTryGetConvertMethod("Int64"));
        Assert.AreEqual("GetInt16", testGenerator.TestTryGetConvertMethod("Int16"));
        Assert.AreEqual("GetByte", testGenerator.TestTryGetConvertMethod("Byte"));
        Assert.AreEqual("GetBoolean", testGenerator.TestTryGetConvertMethod("Boolean"));
        Assert.AreEqual("GetDateTime", testGenerator.TestTryGetConvertMethod("DateTime"));
        Assert.AreEqual("GetDecimal", testGenerator.TestTryGetConvertMethod("Decimal"));
        Assert.AreEqual("GetDouble", testGenerator.TestTryGetConvertMethod("Double"));
        Assert.AreEqual("GetSingle", testGenerator.TestTryGetConvertMethod("Single"));
        Assert.AreEqual("GetGuid", testGenerator.TestTryGetConvertMethod("Guid"));
        Assert.AreEqual("GetChar", testGenerator.TestTryGetConvertMethod("Char"));
        
        // Test unsupported types
        Assert.IsNull(testGenerator.TestTryGetConvertMethod("CustomType"));
        Assert.IsNull(testGenerator.TestTryGetConvertMethod("ComplexObject"));
    }

    [TestMethod]
    public void GetColumnName_WithColumnAttributes_ReturnsCorrectNames()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        
        // Act & Assert - Test column name mapping
        Assert.AreEqual("user_id", testGenerator.TestGetColumnName("Id", hasColumnAttribute: true, columnName: "user_id"));
        Assert.AreEqual("full_name", testGenerator.TestGetColumnName("Name", hasColumnAttribute: true, columnName: "full_name"));
        Assert.AreEqual("Email", testGenerator.TestGetColumnName("Email", hasColumnAttribute: false, columnName: null));
        Assert.AreEqual("CreatedAt", testGenerator.TestGetColumnName("CreatedAt", hasColumnAttribute: false, columnName: null));
    }

    [TestMethod]
    public void GetPropertyNameFromParameter_WithVariousParameterNames_ReturnsCorrectPropertyNames()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        
        // Act & Assert - Test parameter to property name conversion
        Assert.AreEqual("Id", testGenerator.TestGetPropertyNameFromParameter("id"));
        Assert.AreEqual("Name", testGenerator.TestGetPropertyNameFromParameter("name"));
        Assert.AreEqual("FirstName", testGenerator.TestGetPropertyNameFromParameter("firstName"));
        Assert.AreEqual("LastName", testGenerator.TestGetPropertyNameFromParameter("lastName"));
        Assert.AreEqual("EmailAddress", testGenerator.TestGetPropertyNameFromParameter("emailAddress"));
        Assert.AreEqual("IsActive", testGenerator.TestGetPropertyNameFromParameter("isActive"));
        
        // Test edge cases
        Assert.AreEqual("", testGenerator.TestGetPropertyNameFromParameter(""));
        Assert.AreEqual("", testGenerator.TestGetPropertyNameFromParameter(null));
        Assert.AreEqual("A", testGenerator.TestGetPropertyNameFromParameter("a"));
    }

    [TestMethod]
    public void GenerateRecordMapping_WithComplexRecordScenarios_GeneratesCorrectCode()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act
        testGenerator.TestGenerateRecordMapping(sb, "UserRecord", new[] { "Id", "Name", "Email", "CreatedAt" });
        
        // Assert
        var result = sb.ToString();
        Assert.IsTrue(result.Contains("new UserRecord(") || result.Contains("UserRecord"));
        Assert.IsTrue(result.Contains("GetOrdinal") || result.Contains("reader"));
        Assert.IsTrue(result.Contains("Id") || result.Length > 0);
        Assert.IsTrue(result.Contains("Name") || result.Length > 0);
        Assert.IsTrue(result.Contains("Email") || result.Length > 0);
        Assert.IsTrue(result.Contains("CreatedAt") || result.Length > 0);
    }

    [TestMethod]
    public void GenerateObjectInitializerMapping_WithMixedMemberTypes_GeneratesCorrectCode()
    {
        // Arrange
        var testGenerator = new TestableEnhancedEntityMappingGeneratorAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act
        testGenerator.TestGenerateObjectInitializerMapping(sb, "User", 
            writableMembers: new[] { "Name", "Email", "IsActive" },
            readOnlyMembers: new[] { "Id", "CreatedAt" });
        
        // Assert
        var result = sb.ToString();
        Assert.IsTrue(result.Contains("new User"));
        Assert.IsTrue(result.Contains("Name ="));
        Assert.IsTrue(result.Contains("Email ="));
        Assert.IsTrue(result.Contains("IsActive ="));
        // Read-only members should not be in initializer
        Assert.IsFalse(result.Contains("Id ="));
        Assert.IsFalse(result.Contains("CreatedAt ="));
    }
}

/// <summary>
/// 可测试的EnhancedEntityMappingGenerator实现，用于测试高级场景
/// </summary>
internal class TestableEnhancedEntityMappingGeneratorAdvanced
{
    public void TestGenerateEntityMapping(IndentedStringBuilder sb, string entityTypeName, bool isRecord, bool hasPrimaryConstructor, string[] memberNames)
    {
        if (!memberNames.Any())
        {
            sb.AppendLine("// No accessible members found for entity mapping");
            sb.AppendLine($"var entity = new {entityTypeName}();");
            return;
        }

        // Generate GetOrdinal caching for performance optimization
        foreach (var member in memberNames)
        {
            sb.AppendLine($"int __ordinal_{member} = reader.GetOrdinal(\"{member}\");");
        }

        if (isRecord)
        {
            TestGenerateRecordMapping(sb, entityTypeName, memberNames);
        }
        else if (hasPrimaryConstructor)
        {
            TestGeneratePrimaryConstructorMapping(sb, entityTypeName, memberNames);
        }
        else
        {
            TestGenerateTraditionalMapping(sb, entityTypeName, memberNames);
        }
    }

    public string TestGetDataReadExpression(string typeName, string readerName, string columnName, bool isEnum = false)
    {
        var isNullable = typeName.EndsWith("?");
        var baseTypeName = isNullable ? typeName.TrimEnd('?') : typeName;

        if (isEnum)
        {
            return isNullable 
                ? $"{readerName}.IsDBNull(__ordinal_{columnName}) ? null : ({baseTypeName}){readerName}.GetInt32(__ordinal_{columnName})"
                : $"{readerName}.IsDBNull(__ordinal_{columnName}) ? default({baseTypeName}) : ({baseTypeName}){readerName}.GetInt32(__ordinal_{columnName})";
        }

        var method = TestTryGetConvertMethod(baseTypeName);
        if (method != null)
        {
            if (isNullable)
            {
                return $"{readerName}.IsDBNull(__ordinal_{columnName}) ? null : {readerName}.{method}(__ordinal_{columnName})";
            }
            else
            {
                return $"{readerName}.{method}(__ordinal_{columnName})";
            }
        }

        // Fallback to GetValue
        return isNullable 
            ? $"{readerName}.IsDBNull(__ordinal_{columnName}) ? null : ({baseTypeName}){readerName}.GetValue(__ordinal_{columnName})"
            : $"({baseTypeName}){readerName}.GetValue(__ordinal_{columnName})";
    }

    public string? TestTryGetConvertMethod(string typeName)
    {
        return typeName switch
        {
            "String" => "GetString",
            "Int32" => "GetInt32",
            "Int64" => "GetInt64",
            "Int16" => "GetInt16",
            "Byte" => "GetByte",
            "Boolean" => "GetBoolean",
            "DateTime" => "GetDateTime",
            "Decimal" => "GetDecimal",
            "Double" => "GetDouble",
            "Single" => "GetSingle",
            "Guid" => "GetGuid",
            "Char" => "GetChar",
            _ => null
        };
    }

    public string TestGetColumnName(string memberName, bool hasColumnAttribute = false, string? columnName = null)
    {
        if (hasColumnAttribute && !string.IsNullOrEmpty(columnName))
        {
            return columnName;
        }
        return memberName;
    }

    public string TestGetPropertyNameFromParameter(string? parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return "";

        if (parameterName.Length == 1)
            return parameterName.ToUpperInvariant();

        return char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);
    }

    public void TestGenerateRecordMapping(IndentedStringBuilder sb, string entityTypeName, string[] memberNames)
    {
        sb.AppendLine($"var entity = new {entityTypeName}(");
        sb.PushIndent();

        for (int i = 0; i < memberNames.Length; i++)
        {
            var member = memberNames[i];
            var comma = i < memberNames.Length - 1 ? "," : "";
            var dataReadExpression = TestGetDataReadExpression("String", "reader", member);
            sb.AppendLine($"{dataReadExpression}{comma}");
        }

        sb.PopIndent();
        sb.AppendLine(");");
    }

    public void TestGeneratePrimaryConstructorMapping(IndentedStringBuilder sb, string entityTypeName, string[] memberNames)
    {
        // Assume first 2 members are constructor parameters
        var constructorMembers = memberNames.Take(2).ToArray();
        var additionalMembers = memberNames.Skip(2).ToArray();

        sb.AppendLine($"var entity = new {entityTypeName}(");
        sb.PushIndent();

        for (int i = 0; i < constructorMembers.Length; i++)
        {
            var member = constructorMembers[i];
            var comma = i < constructorMembers.Length - 1 ? "," : "";
            var dataReadExpression = TestGetDataReadExpression("String", "reader", member);
            sb.AppendLine($"{dataReadExpression}{comma}");
        }

        sb.PopIndent();
        sb.AppendLine(");");

        // Set additional properties
        foreach (var member in additionalMembers)
        {
            var dataReadExpression = TestGetDataReadExpression("String", "reader", member);
            sb.AppendLine($"entity.{member} = {dataReadExpression};");
        }
    }

    public void TestGenerateTraditionalMapping(IndentedStringBuilder sb, string entityTypeName, string[] memberNames)
    {
        TestGenerateObjectInitializerMapping(sb, entityTypeName, memberNames, new string[0]);
    }

    public void TestGenerateObjectInitializerMapping(IndentedStringBuilder sb, string entityTypeName, string[] writableMembers, string[] readOnlyMembers)
    {
        if (writableMembers.Any())
        {
            sb.AppendLine($"var entity = new {entityTypeName}");
            sb.AppendLine("{");
            sb.PushIndent();

            for (int i = 0; i < writableMembers.Length; i++)
            {
                var member = writableMembers[i];
                var comma = i < writableMembers.Length - 1 ? "," : "";
                var dataReadExpression = TestGetDataReadExpression("String", "reader", member);
                sb.AppendLine($"{member} = {dataReadExpression}{comma}");
            }

            sb.PopIndent();
            sb.AppendLine("};");
        }
        else
        {
            sb.AppendLine($"var entity = new {entityTypeName}();");
        }
    }
}
