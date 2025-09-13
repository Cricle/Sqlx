// -----------------------------------------------------------------------
// <copyright file="MessagesAndConstantsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for Messages and Constants classes.
/// Tests diagnostic message definitions and constant values used throughout the framework.
/// </summary>
[TestClass]
public class MessagesAndConstantsTests
{
    /// <summary>
    /// Tests that all Messages diagnostic descriptors are properly defined.
    /// </summary>
    [TestMethod]
    public void Messages_DiagnosticDescriptors_AreProperlyDefined()
    {
        // Test SP0001 - No stored procedure attribute
        var sp0001 = Messages.SP0001;
        Assert.IsNotNull(sp0001, "SP0001 should be defined");
        Assert.AreEqual("SP0001", sp0001.Id, "SP0001 should have correct ID");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0001.DefaultSeverity, "SP0001 should be error severity");
        Assert.IsTrue(sp0001.IsEnabledByDefault, "SP0001 should be enabled by default");

        // Test SP0002 - Invalid parameter type
        var sp0002 = Messages.SP0002;
        Assert.IsNotNull(sp0002, "SP0002 should be defined");
        Assert.AreEqual("SP0002", sp0002.Id, "SP0002 should have correct ID");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0002.DefaultSeverity, "SP0002 should be error severity");
        Assert.IsTrue(sp0002.MessageFormat.ToString().Contains("{0}"), "SP0002 should have parameter placeholder");

        // Test SP0003 - Missing return type
        var sp0003 = Messages.SP0003;
        Assert.IsNotNull(sp0003, "SP0003 should be defined");
        Assert.AreEqual("SP0003", sp0003.Id, "SP0003 should have correct ID");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0003.DefaultSeverity, "SP0003 should be error severity");
    }

    /// <summary>
    /// Tests Messages diagnostic descriptors for SQL-related errors.
    /// </summary>
    [TestMethod]
    public void Messages_SqlDiagnostics_AreCorrectlyConfigured()
    {
        // Test SP0004 - Invalid SQL syntax
        var sp0004 = Messages.SP0004;
        Assert.IsNotNull(sp0004, "SP0004 should be defined");
        Assert.AreEqual("SP0004", sp0004.Id, "SP0004 should have correct ID");
        Assert.IsTrue(sp0004.MessageFormat.ToString().Contains("SQL"), "SP0004 should mention SQL");
        Assert.IsTrue(sp0004.MessageFormat.ToString().Contains("{0}"), "SP0004 should have parameter placeholder");

        // Test SP0007 - No RawSqlAttribute or SqlxAttribute tag
        var sp0007 = Messages.SP0007;
        Assert.IsNotNull(sp0007, "SP0007 should be defined");
        Assert.AreEqual("SP0007", sp0007.Id, "SP0007 should have correct ID");
        Assert.IsTrue(sp0007.Title.ToString().Contains("RawSqlAttribute") || 
                     sp0007.Title.ToString().Contains("SqlxAttribute"), 
                     "SP0007 should mention required attributes");

        // Test SP0008 - Execute no query return must be int or Task<int>
        var sp0008 = Messages.SP0008;
        Assert.IsNotNull(sp0008, "SP0008 should be defined");
        Assert.AreEqual("SP0008", sp0008.Id, "SP0008 should have correct ID");
        Assert.IsTrue(sp0008.Description.ToString().Contains("int") || 
                     sp0008.Description.ToString().Contains("Task"), 
                     "SP0008 should mention return type requirements");
    }

    /// <summary>
    /// Tests Messages diagnostic descriptors for entity-related errors.
    /// </summary>
    [TestMethod]
    public void Messages_EntityDiagnostics_AreCorrectlyConfigured()
    {
        // Test SP0005 - Entity mapping error
        var sp0005 = Messages.SP0005;
        Assert.IsNotNull(sp0005, "SP0005 should be defined");
        Assert.AreEqual("SP0005", sp0005.Id, "SP0005 should have correct ID");
        Assert.IsTrue(sp0005.Title.ToString().Contains("Entity"), "SP0005 should mention entity");
        Assert.IsTrue(sp0005.MessageFormat.ToString().Contains("{0}"), "SP0005 should have parameter placeholder");

        // Test SP0009 - Repository interface not found
        var sp0009 = Messages.SP0009;
        Assert.IsNotNull(sp0009, "SP0009 should be defined");
        Assert.AreEqual("SP0009", sp0009.Id, "SP0009 should have correct ID");
        Assert.IsTrue(sp0009.Title.ToString().Contains("Repository"), "SP0009 should mention repository");

        // Test SP0010 - Table name not specified
        var sp0010 = Messages.SP0010;
        Assert.IsNotNull(sp0010, "SP0010 should be defined");
        Assert.AreEqual("SP0010", sp0010.Id, "SP0010 should have correct ID");
        Assert.IsTrue(sp0010.Title.ToString().Contains("Table"), "SP0010 should mention table");

        // Test SP0011 - Primary key not found
        var sp0011 = Messages.SP0011;
        Assert.IsNotNull(sp0011, "SP0011 should be defined");
        Assert.AreEqual("SP0011", sp0011.Id, "SP0011 should have correct ID");
        Assert.IsTrue(sp0011.Title.ToString().Contains("Primary key") || 
                     sp0011.Title.ToString().Contains("key"), 
                     "SP0011 should mention primary key");
    }

    /// <summary>
    /// Tests Messages diagnostic descriptors for async-related warnings.
    /// </summary>
    [TestMethod]
    public void Messages_AsyncDiagnostics_AreCorrectlyConfigured()
    {
        // Test SP0006 - Async method missing CancellationToken
        var sp0006 = Messages.SP0006;
        Assert.IsNotNull(sp0006, "SP0006 should be defined");
        Assert.AreEqual("SP0006", sp0006.Id, "SP0006 should have correct ID");
        Assert.IsTrue(sp0006.Title.ToString().Contains("Async") || 
                     sp0006.Title.ToString().Contains("CancellationToken"), 
                     "SP0006 should mention async or CancellationToken");
        Assert.IsTrue(sp0006.Description.ToString().Contains("CancellationToken"), 
                     "SP0006 description should mention CancellationToken");
    }

    /// <summary>
    /// Tests Messages diagnostic descriptors for duplicate method errors.
    /// </summary>
    [TestMethod]
    public void Messages_DuplicateMethodDiagnostics_AreCorrectlyConfigured()
    {
        // Test SP0012 - Duplicate method name
        var sp0012 = Messages.SP0012;
        Assert.IsNotNull(sp0012, "SP0012 should be defined");
        Assert.AreEqual("SP0012", sp0012.Id, "SP0012 should have correct ID");
        Assert.IsTrue(sp0012.Title.ToString().Contains("Duplicate"), "SP0012 should mention duplicate");
        Assert.IsTrue(sp0012.MessageFormat.ToString().Contains("{0}"), "SP0012 should have parameter placeholder");
    }

    /// <summary>
    /// Tests that all diagnostic messages have consistent category.
    /// </summary>
    [TestMethod]
    public void Messages_AllDiagnostics_HaveConsistentCategory()
    {
        var diagnostics = new[]
        {
            Messages.SP0001, Messages.SP0002, Messages.SP0003, Messages.SP0004,
            Messages.SP0005, Messages.SP0006, Messages.SP0007, Messages.SP0008,
            Messages.SP0009, Messages.SP0010, Messages.SP0011, Messages.SP0012
        };

        foreach (var diagnostic in diagnostics)
        {
            Assert.IsTrue(diagnostic.Category == "Internal" || diagnostic.Category == "Sqlx", 
                $"Diagnostic {diagnostic.Id} should have 'Internal' or 'Sqlx' category, but has '{diagnostic.Category}'");
            Assert.IsTrue(diagnostic.IsEnabledByDefault, 
                $"Diagnostic {diagnostic.Id} should be enabled by default");
            Assert.IsFalse(string.IsNullOrEmpty(diagnostic.Id), 
                $"Diagnostic should have non-empty ID");
            Assert.IsFalse(string.IsNullOrEmpty(diagnostic.Title.ToString()), 
                $"Diagnostic {diagnostic.Id} should have non-empty title");
        }
    }

    /// <summary>
    /// Tests Constants.SqlExecuteTypeValues are correctly defined.
    /// </summary>
    [TestMethod]
    public void Constants_SqlExecuteTypeValues_AreCorrectlyDefined()
    {
        // Test basic CRUD operations
        Assert.AreEqual(0, Constants.SqlExecuteTypeValues.Select, "Select should be 0");
        Assert.AreEqual(1, Constants.SqlExecuteTypeValues.Update, "Update should be 1");
        Assert.AreEqual(2, Constants.SqlExecuteTypeValues.Insert, "Insert should be 2");
        Assert.AreEqual(3, Constants.SqlExecuteTypeValues.Delete, "Delete should be 3");

        // Test batch operations
        Assert.AreEqual(4, Constants.SqlExecuteTypeValues.BatchInsert, "BatchInsert should be 4");
        Assert.AreEqual(5, Constants.SqlExecuteTypeValues.BatchUpdate, "BatchUpdate should be 5");
        Assert.AreEqual(6, Constants.SqlExecuteTypeValues.BatchDelete, "BatchDelete should be 6");
        Assert.AreEqual(7, Constants.SqlExecuteTypeValues.BatchCommand, "BatchCommand should be 7");

        // Test that all values are unique
        var values = new[]
        {
            Constants.SqlExecuteTypeValues.Select,
            Constants.SqlExecuteTypeValues.Update,
            Constants.SqlExecuteTypeValues.Insert,
            Constants.SqlExecuteTypeValues.Delete,
            Constants.SqlExecuteTypeValues.BatchInsert,
            Constants.SqlExecuteTypeValues.BatchUpdate,
            Constants.SqlExecuteTypeValues.BatchDelete,
            Constants.SqlExecuteTypeValues.BatchCommand
        };

        var uniqueValues = values.Distinct().ToArray();
        Assert.AreEqual(values.Length, uniqueValues.Length, "All SQL execute type values should be unique");
    }

    /// <summary>
    /// Tests Constants.GeneratedVariables are correctly defined.
    /// </summary>
    [TestMethod]
    public void Constants_GeneratedVariables_AreCorrectlyDefined()
    {
        // Test variable names follow consistent naming convention
        Assert.AreEqual("__conn__", Constants.GeneratedVariables.Connection, "Connection variable should follow naming convention");
        Assert.AreEqual("__cmd__", Constants.GeneratedVariables.Command, "Command variable should follow naming convention");
        Assert.AreEqual("__reader__", Constants.GeneratedVariables.Reader, "Reader variable should follow naming convention");
        Assert.AreEqual("__result__", Constants.GeneratedVariables.Result, "Result variable should follow naming convention");
        Assert.AreEqual("__data__", Constants.GeneratedVariables.Data, "Data variable should follow naming convention");
        Assert.AreEqual("__startTime__", Constants.GeneratedVariables.StartTime, "StartTime variable should follow naming convention");
        Assert.AreEqual("__exception__", Constants.GeneratedVariables.Exception, "Exception variable should follow naming convention");
        Assert.AreEqual("__elapsed__", Constants.GeneratedVariables.Elapsed, "Elapsed variable should follow naming convention");

        // Test that all variable names are unique
        var variableNames = new[]
        {
            Constants.GeneratedVariables.Connection,
            Constants.GeneratedVariables.Command,
            Constants.GeneratedVariables.Reader,
            Constants.GeneratedVariables.Result,
            Constants.GeneratedVariables.Data,
            Constants.GeneratedVariables.StartTime,
            Constants.GeneratedVariables.Exception,
            Constants.GeneratedVariables.Elapsed
        };

        var uniqueNames = variableNames.Distinct().ToArray();
        Assert.AreEqual(variableNames.Length, uniqueNames.Length, "All generated variable names should be unique");

        // Test that all variable names follow the __name__ pattern
        foreach (var variableName in variableNames)
        {
            Assert.IsTrue(variableName.StartsWith("__") && variableName.EndsWith("__"), 
                $"Variable name '{variableName}' should follow __name__ pattern");
            Assert.IsTrue(variableName.Length > 4, 
                $"Variable name '{variableName}' should have content between underscores");
        }
    }

    /// <summary>
    /// Tests that generated variable names don't conflict with common C# keywords.
    /// </summary>
    [TestMethod]
    public void Constants_GeneratedVariables_DontConflictWithKeywords()
    {
        var csharpKeywords = new[]
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };

        var variableNames = new[]
        {
            Constants.GeneratedVariables.Connection,
            Constants.GeneratedVariables.Command,
            Constants.GeneratedVariables.Reader,
            Constants.GeneratedVariables.Result,
            Constants.GeneratedVariables.Data,
            Constants.GeneratedVariables.StartTime,
            Constants.GeneratedVariables.Exception,
            Constants.GeneratedVariables.Elapsed
        };

        foreach (var variableName in variableNames)
        {
            // Remove the __ prefix and suffix to get the core name
            var coreName = variableName.Substring(2, variableName.Length - 4).ToLowerInvariant();
            Assert.IsFalse(csharpKeywords.Contains(coreName), 
                $"Generated variable name '{variableName}' should not conflict with C# keyword '{coreName}'");
        }
    }

    /// <summary>
    /// Tests SQL execute type values are in logical order.
    /// </summary>
    [TestMethod]
    public void Constants_SqlExecuteTypeValues_AreInLogicalOrder()
    {
        // Basic CRUD operations should come first (0-3)
        Assert.IsTrue(Constants.SqlExecuteTypeValues.Select < Constants.SqlExecuteTypeValues.BatchInsert, 
            "Basic operations should come before batch operations");
        Assert.IsTrue(Constants.SqlExecuteTypeValues.Update < Constants.SqlExecuteTypeValues.BatchInsert, 
            "Basic operations should come before batch operations");
        Assert.IsTrue(Constants.SqlExecuteTypeValues.Insert < Constants.SqlExecuteTypeValues.BatchInsert, 
            "Basic operations should come before batch operations");
        Assert.IsTrue(Constants.SqlExecuteTypeValues.Delete < Constants.SqlExecuteTypeValues.BatchInsert, 
            "Basic operations should come before batch operations");

        // Batch operations should be grouped together (4-7)
        var batchValues = new[]
        {
            Constants.SqlExecuteTypeValues.BatchInsert,
            Constants.SqlExecuteTypeValues.BatchUpdate,
            Constants.SqlExecuteTypeValues.BatchDelete,
            Constants.SqlExecuteTypeValues.BatchCommand
        };

        for (int i = 1; i < batchValues.Length; i++)
        {
            Assert.IsTrue(batchValues[i] > batchValues[i-1], 
                "Batch operation values should be in ascending order");
        }
    }

    /// <summary>
    /// Tests that diagnostic IDs follow consistent pattern.
    /// </summary>
    [TestMethod]
    public void Messages_DiagnosticIds_FollowConsistentPattern()
    {
        var diagnostics = new[]
        {
            Messages.SP0001, Messages.SP0002, Messages.SP0003, Messages.SP0004,
            Messages.SP0005, Messages.SP0006, Messages.SP0007, Messages.SP0008,
            Messages.SP0009, Messages.SP0010, Messages.SP0011, Messages.SP0012
        };

        foreach (var diagnostic in diagnostics)
        {
            // Should follow SP#### pattern
            Assert.IsTrue(diagnostic.Id.StartsWith("SP"), 
                $"Diagnostic ID '{diagnostic.Id}' should start with 'SP'");
            Assert.IsTrue(diagnostic.Id.Length >= 5, 
                $"Diagnostic ID '{diagnostic.Id}' should be at least 5 characters long");
            
            var numberPart = diagnostic.Id.Substring(2);
            Assert.IsTrue(int.TryParse(numberPart, out var number), 
                $"Diagnostic ID '{diagnostic.Id}' should have numeric suffix");
            Assert.IsTrue(number >= 1 && number <= 9999, 
                $"Diagnostic ID '{diagnostic.Id}' should have number between 0001 and 9999");
        }
    }

    /// <summary>
    /// Tests that constants classes are properly structured.
    /// </summary>
    [TestMethod]
    public void Constants_Classes_AreProperlyStructured()
    {
        // Test that Constants class exists and is static
        var constantsType = typeof(Constants);
        Assert.IsTrue(constantsType.IsAbstract && constantsType.IsSealed, 
            "Constants class should be static");

        // Test that nested classes exist and are static
        var sqlExecuteTypeValuesType = typeof(Constants.SqlExecuteTypeValues);
        Assert.IsTrue(sqlExecuteTypeValuesType.IsAbstract && sqlExecuteTypeValuesType.IsSealed, 
            "SqlExecuteTypeValues class should be static");

        var generatedVariablesType = typeof(Constants.GeneratedVariables);
        Assert.IsTrue(generatedVariablesType.IsAbstract && generatedVariablesType.IsSealed, 
            "GeneratedVariables class should be static");

        // Test that Messages class exists and is static
        var messagesType = typeof(Messages);
        Assert.IsTrue(messagesType.IsAbstract && messagesType.IsSealed, 
            "Messages class should be static");
    }
}
