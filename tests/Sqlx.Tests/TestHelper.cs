// -----------------------------------------------------------------------
// <copyright file="TestHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

/// <summary>
/// Helper class for unit tests with common utilities and test data.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Common test data for name mapping tests.
    /// </summary>
    public static readonly (string Input, string Expected)[] NameMappingTestCases =
    {
        ("personId", "person_id"),
        ("name", "name"),
        ("Name", "name"),
        ("PersonId", "person_id"),
        ("firstName", "first_name"),
        ("FirstName", "first_name"),
        ("userId", "user_id"),
        ("UserId", "user_id"),
        ("ID", "id"),
        ("URL", "url"),
        ("API", "api"),
        ("isActive", "is_active"),
        ("IsActive", "is_active"),
        ("createdAt", "created_at"),
        ("CreatedAt", "created_at"),
        ("updatedAt", "updated_at"),
        ("UpdatedAt", "updated_at"),
        (string.Empty, string.Empty),
        ("a", "a"),
        ("A", "a"),
        ("camelCase", "camel_case"),
        ("CamelCase", "camel_case"),
        ("PascalCase", "pascal_case"),
    };

    /// <summary>
    /// SQL define test cases for different database types.
    /// </summary>
    public static readonly (string Input, string MySqlExpected, string SqlServerExpected, string PostgreSqlExpected)[] SqlDefineTestCases =
    {
        ("columnName", "`columnName`", "[columnName]", "\"columnName\""),
        ("user_id", "`user_id`", "[user_id]", "\"user_id\""),
        (string.Empty, "``", "[]", "\"\""),
        ("table", "`table`", "[table]", "\"table\""),
    };

    /// <summary>
    /// Validates that two strings are equal with detailed error message.
    /// </summary>
    /// <param name="expected">The expected string value.</param>
    /// <param name="actual">The actual string value.</param>
    /// <param name="message">Optional custom message.</param>
    public static void AssertStringEqual(string expected, string actual, string? message = null)
    {
        if (string.IsNullOrEmpty(message))
        {
            message = $"Expected: '{expected}', Actual: '{actual}'";
        }

        Assert.AreEqual(expected, actual, message);
    }

    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="parameterName">The parameter name for error messages.</param>
    public static void AssertNotNullOrEmpty(string value, string parameterName = "value")
    {
        Assert.IsNotNull(value, $"{parameterName} should not be null");
        Assert.IsTrue(value.Length > 0, $"{parameterName} should not be empty");
    }

    /// <summary>
    /// Gets generated output from the source generator.
    /// </summary>
    /// <param name="source">The source code to compile.</param>
    /// <returns>Diagnostics and compilation with generated code.</returns>
    public static (ImmutableArray<Diagnostic> Diagnostics, Compilation Compilation) GetGeneratedOutput(string source)
    {
        var compilation = CreateCompilation(source);
        var driver = CreateDriver();
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        return (diagnostics, outputCompilation);
    }

    /// <summary>
    /// Gets generated code for a specific hint name.
    /// </summary>
    /// <param name="compilation">The compilation with generated code.</param>
    /// <param name="hintName">The hint name to search for (e.g., class name).</param>
    /// <returns>The generated code as a string.</returns>
    public static string GetGeneratedCode(Compilation compilation, string hintName)
    {
        var generatedSyntaxTrees = compilation.SyntaxTrees
            .Where(st => st.FilePath.Contains(hintName))
            .ToList();

        Assert.IsTrue(generatedSyntaxTrees.Any(), $"No generated code found for {hintName}");
        return generatedSyntaxTrees.First().ToString();
    }

    /// <summary>
    /// Creates a generator driver with the Sqlx generator.
    /// </summary>
    /// <returns>A new generator driver.</returns>
    private static GeneratorDriver CreateDriver()
    {
        var generator = new Sqlx.CSharpGenerator();
        return CSharpGeneratorDriver.Create(generator);
    }

    /// <summary>
    /// Creates a compilation from source code.
    /// </summary>
    /// <param name="source">The source code to compile.</param>
    /// <returns>A new compilation.</returns>
    private static Compilation CreateCompilation(string source)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Sqlx.Annotations.SqlxAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Data.Sqlite.SqliteConnection).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Diagnostics.DiagnosticSource").Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
