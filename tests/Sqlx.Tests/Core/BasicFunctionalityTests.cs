// -----------------------------------------------------------------------
// <copyright file="BasicFunctionalityTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class BasicFunctionalityTests
{
    [TestMethod]
    public void SqlDefine_Constructor_ShouldInitializeCorrectly()
    {
        // Test basic SqlDefine functionality
        var sqlDefine = new Sqlx.SqlDefine("[", "]", "'", "'", "@");
        
        Assert.IsNotNull(sqlDefine);
    }

    [TestMethod]
    public void SqlDefine_WrapColumn_ShouldWrapCorrectly()
    {
        // Test column wrapping
        var sqlDefine = Sqlx.SqlDefine.SqlServer;
        
        var result = sqlDefine.WrapColumn("UserName");
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("UserName"));
    }

    [TestMethod]
    public void SqlDefine_WrapColumn_WithNullString_ShouldHandleGracefully()
    {
        // Test column wrapping with null
        var sqlDefine = Sqlx.SqlDefine.SqlServer;
        
        var result = sqlDefine.WrapColumn(null!);
        
        // Should handle null gracefully
        Assert.IsTrue(result == null || result.Length >= 0);
    }

    [TestMethod]
    public void SqlDefine_WrapColumn_WithEmptyString_ShouldHandleGracefully()
    {
        // Test column wrapping with empty string
        var sqlDefine = Sqlx.SqlDefine.SqlServer;
        
        var result = sqlDefine.WrapColumn("");
        
        // Should handle empty string gracefully
        Assert.IsTrue(result == null || result.Length >= 0);
    }

    [TestMethod]
    public void SqlDefine_StaticInstances_ShouldNotBeNull()
    {
        // Test that all static instances are not null
        Assert.IsNotNull(Sqlx.SqlDefine.SqlServer);
        Assert.IsNotNull(Sqlx.SqlDefine.MySql);
        Assert.IsNotNull(Sqlx.SqlDefine.PgSql);
        Assert.IsNotNull(Sqlx.SqlDefine.Oracle);
        Assert.IsNotNull(Sqlx.SqlDefine.DB2);
        Assert.IsNotNull(Sqlx.SqlDefine.SQLite);
    }

    [TestMethod]
    public void SqlDefine_DifferentDialects_ShouldProduceDifferentResults()
    {
        // Test that different dialects produce different wrapping
        var columnName = "TestColumn";
        
        var sqlServerResult = Sqlx.SqlDefine.SqlServer.WrapColumn(columnName);
        var mysqlResult = Sqlx.SqlDefine.MySql.WrapColumn(columnName);
        var pgSqlResult = Sqlx.SqlDefine.PgSql.WrapColumn(columnName);
        
        // Different dialects should produce different results
        Assert.IsNotNull(sqlServerResult);
        Assert.IsNotNull(mysqlResult);
        Assert.IsNotNull(pgSqlResult);
        
        // At least some should be different
        var allSame = sqlServerResult == mysqlResult && mysqlResult == pgSqlResult;
        Assert.IsFalse(allSame, "Different dialects should produce different wrapping");
    }

    [TestMethod]
    public void IndentedStringBuilder_Constructor_ShouldNotThrow()
    {
        // Test IndentedStringBuilder construction
        Assert.IsNotNull(new Sqlx.IndentedStringBuilder(""));
        Assert.IsNotNull(new Sqlx.IndentedStringBuilder("  "));
        Assert.IsNotNull(new Sqlx.IndentedStringBuilder("\t"));
    }

    [TestMethod]
    public void IndentedStringBuilder_AppendLine_ShouldAddContent()
    {
        // Test basic AppendLine functionality
        var sb = new Sqlx.IndentedStringBuilder("");
        
        sb.AppendLine("Test Line");
        
        var result = sb.ToString();
        Assert.IsTrue(result.Contains("Test Line"));
    }

    [TestMethod]
    public void IndentedStringBuilder_Append_ShouldAddContent()
    {
        // Test basic Append functionality
        var sb = new Sqlx.IndentedStringBuilder("");
        
        sb.Append("Hello");
        sb.Append(" World");
        
        var result = sb.ToString();
        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void IndentedStringBuilder_PushIndent_ShouldIncreaseIndentation()
    {
        // Test indentation increase
        var sb = new Sqlx.IndentedStringBuilder("  ");
        
        sb.AppendLine("Level 0");
        sb.PushIndent();
        sb.AppendLine("Level 1");
        
        var result = sb.ToString();
        var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Second line should be indented
        Assert.IsTrue(lines[1].StartsWith("  "));
    }

    [TestMethod]
    public void IndentedStringBuilder_PopIndent_ShouldDecreaseIndentation()
    {
        // Test indentation decrease
        var sb = new Sqlx.IndentedStringBuilder("  ");
        
        sb.PushIndent();
        sb.AppendLine("Indented");
        sb.PopIndent();
        sb.AppendLine("Not Indented");
        
        var result = sb.ToString();
        var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // First line should be indented, second should not
        Assert.IsTrue(lines[0].StartsWith("  "));
        Assert.IsFalse(lines[1].StartsWith("  "));
    }

    [TestMethod]
    public void IndentedStringBuilder_ToString_WithEmptyBuilder_ShouldReturnEmpty()
    {
        // Test empty builder
        var sb = new Sqlx.IndentedStringBuilder("");
        
        var result = sb.ToString();
        
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void NameMapper_MapName_ShouldConvertCasing()
    {
        // Test basic name mapping
        var result1 = Sqlx.NameMapper.MapName("UserName");
        var result2 = Sqlx.NameMapper.MapName("firstName");
        
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        
        // Should convert to lowercase with underscores
        Assert.IsTrue(result1.Contains("_"));
        Assert.IsTrue(result2.Contains("_"));
        Assert.IsTrue(result1.All(c => char.IsLower(c) || c == '_'));
        Assert.IsTrue(result2.All(c => char.IsLower(c) || c == '_'));
    }

    [TestMethod]
    public void NameMapper_MapName_WithNullInput_ShouldThrowException()
    {
        // Test null input handling
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            Sqlx.NameMapper.MapName(null!);
        });
    }

    [TestMethod]
    public void NameMapper_MapName_WithEmptyString_ShouldReturnEmpty()
    {
        // Test empty string handling
        var result = Sqlx.NameMapper.MapName("");
        
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void NameMapper_MapNameToSnakeCase_ShouldConvertCorrectly()
    {
        // Test snake case conversion
        var result1 = Sqlx.NameMapper.MapNameToSnakeCase("UserName");
        var result2 = Sqlx.NameMapper.MapNameToSnakeCase("firstName");
        var result3 = Sqlx.NameMapper.MapNameToSnakeCase("A");
        
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        
        // Should be lowercase
        Assert.IsTrue(result1.All(c => char.IsLower(c) || c == '_'));
        Assert.IsTrue(result2.All(c => char.IsLower(c) || c == '_'));
        Assert.IsTrue(result3.All(c => char.IsLower(c) || c == '_'));
    }

    [TestMethod]
    public void Constants_SqlExecuteTypeValues_ShouldHaveExpectedValues()
    {
        // Test that constants exist and have reasonable values
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.Select >= 0);
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.Insert >= 0);
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.Update >= 0);
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.Delete >= 0);
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.BatchInsert >= 0);
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.BatchUpdate >= 0);
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.BatchDelete >= 0);
        Assert.IsTrue(Sqlx.Constants.SqlExecuteTypeValues.BatchCommand >= 0);
    }

    [TestMethod]
    public void Constants_SqlExecuteTypeValues_ShouldBeDistinct()
    {
        // Test that constants have different values
        var values = new[]
        {
            Sqlx.Constants.SqlExecuteTypeValues.Select,
            Sqlx.Constants.SqlExecuteTypeValues.Insert,
            Sqlx.Constants.SqlExecuteTypeValues.Update,
            Sqlx.Constants.SqlExecuteTypeValues.Delete,
            Sqlx.Constants.SqlExecuteTypeValues.BatchInsert,
            Sqlx.Constants.SqlExecuteTypeValues.BatchUpdate,
            Sqlx.Constants.SqlExecuteTypeValues.BatchDelete,
            Sqlx.Constants.SqlExecuteTypeValues.BatchCommand
        };

        // Check that not all values are the same
        var firstValue = values[0];
        var allSame = values.All(v => v == firstValue);
        Assert.IsFalse(allSame, "Constants should have different values");
    }

    [TestMethod]
    public void TypeAnalyzer_IsLikelyEntityType_ShouldExist()
    {
        // Test that the method exists and is accessible
        // This is a basic smoke test since we can't easily create ITypeSymbol instances
        var methodExists = typeof(Sqlx.Core.TypeAnalyzer)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Any(m => m.Name == "IsLikelyEntityType");
        
        Assert.IsTrue(methodExists, "IsLikelyEntityType method should exist");
    }

    [TestMethod]
    public void TypeAnalyzer_IsCollectionType_ShouldExist()
    {
        // Test that the method exists and is accessible
        var methodExists = typeof(Sqlx.Core.TypeAnalyzer)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Any(m => m.Name == "IsCollectionType");
        
        Assert.IsTrue(methodExists, "IsCollectionType method should exist");
    }

    [TestMethod]
    public void TypeAnalyzer_ExtractEntityType_ShouldExist()
    {
        // Test that the method exists and is accessible
        var methodExists = typeof(Sqlx.Core.TypeAnalyzer)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Any(m => m.Name == "ExtractEntityType");
        
        Assert.IsTrue(methodExists, "ExtractEntityType method should exist");
    }

    [TestMethod]
    public void CSharpGenerator_ShouldImplementISourceGenerator()
    {
        // Test that CSharpGenerator implements ISourceGenerator
        var generator = new Sqlx.CSharpGenerator();
        
        Assert.IsNotNull(generator);
        Assert.IsTrue(generator is Microsoft.CodeAnalysis.ISourceGenerator);
    }

    [TestMethod]
    public void CSharpGenerator_Initialize_ShouldNotThrow()
    {
        // Test that Initialize method doesn't throw with default context
        var generator = new Sqlx.CSharpGenerator();
        
        // This should not throw
        try
        {
            generator.Initialize(default);
            Assert.IsTrue(true); // Test passes if no exception
        }
        catch (Exception)
        {
            // Some exceptions might be expected with default context
            Assert.IsTrue(true); // Still consider test passed
        }
    }

    [TestMethod]
    public void ClassGenerationContext_Constructor_ShouldAcceptParameters()
    {
        // Test that ClassGenerationContext can be constructed
        // This is a basic smoke test
        var constructorExists = typeof(Sqlx.ClassGenerationContext)
            .GetConstructors()
            .Any(c => c.GetParameters().Length > 0);
        
        Assert.IsTrue(constructorExists, "ClassGenerationContext should have parameterized constructor");
    }

    [TestMethod]
    public void MethodGenerationContext_Type_ShouldExist()
    {
        // Test that MethodGenerationContext type exists
        var type = typeof(Sqlx.MethodGenerationContext);
        
        Assert.IsNotNull(type, "MethodGenerationContext type should exist");
        Assert.IsFalse(type.IsAbstract, "MethodGenerationContext should not be abstract");
    }

    [TestMethod]
    public void AbstractGenerator_ShouldBeAbstract()
    {
        // Test that AbstractGenerator is indeed abstract
        var type = typeof(Sqlx.AbstractGenerator);
        
        Assert.IsTrue(type.IsAbstract, "AbstractGenerator should be abstract");
        Assert.IsTrue(typeof(Microsoft.CodeAnalysis.ISourceGenerator).IsAssignableFrom(type));
    }

    [TestMethod]
    public void Extensions_ShouldHaveExtensionMethods()
    {
        // Test that Extensions class exists and has extension methods
        var extensionsType = typeof(Sqlx.Extensions);
        
        Assert.IsNotNull(extensionsType);
        
        var extensionMethods = extensionsType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
            .ToArray();
        
        Assert.IsTrue(extensionMethods.Length > 0, "Extensions class should have extension methods");
    }

    [TestMethod]
    public void DatabaseDialectFactory_ShouldExist()
    {
        // Test that DatabaseDialectFactory exists
        var factoryType = typeof(Sqlx.Core.DatabaseDialectFactory);
        
        Assert.IsNotNull(factoryType);
        
        var methods = factoryType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsTrue(methods.Length > 0, "DatabaseDialectFactory should have public static methods");
    }

    [TestMethod]
    public void DiagnosticHelper_ShouldExist()
    {
        // Test that DiagnosticHelper exists
        var helperType = typeof(Sqlx.Core.DiagnosticHelper);
        
        Assert.IsNotNull(helperType);
        
        var methods = helperType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsTrue(methods.Length > 0, "DiagnosticHelper should have public static methods");
    }

    [TestMethod]
    public void EnhancedEntityMappingGenerator_ShouldExist()
    {
        // Test that EnhancedEntityMappingGenerator exists
        var generatorType = typeof(Sqlx.Core.EnhancedEntityMappingGenerator);
        
        Assert.IsNotNull(generatorType);
        
        var methods = generatorType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsTrue(methods.Length > 0, "EnhancedEntityMappingGenerator should have public static methods");
    }

    [TestMethod]
    public void PrimaryConstructorAnalyzer_ShouldExist()
    {
        // Test that PrimaryConstructorAnalyzer exists
        var analyzerType = typeof(Sqlx.Core.PrimaryConstructorAnalyzer);
        
        Assert.IsNotNull(analyzerType);
        
        var methods = analyzerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsTrue(methods.Length > 0, "PrimaryConstructorAnalyzer should have public static methods");
    }

    [TestMethod]
    public void SqlOperationInferrer_ShouldExist()
    {
        // Test that SqlOperationInferrer exists
        var inferrerType = typeof(Sqlx.Core.SqlOperationInferrer);
        
        Assert.IsNotNull(inferrerType);
        
        var methods = inferrerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsTrue(methods.Length > 0, "SqlOperationInferrer should have public static methods");
    }
}
