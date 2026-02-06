// -----------------------------------------------------------------------
// <copyright file="GeneratorTestHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Helper class for testing source generators.
/// </summary>
public static class GeneratorTestHelper
{
    /// <summary>
    /// Runs a source generator on the provided source code.
    /// </summary>
    /// <param name="source">The source code to process.</param>
    /// <param name="generator">The source generator to run.</param>
    /// <returns>The result of running the generator.</returns>
    public static GeneratorTestResult RunGenerator(string source, IIncrementalGenerator generator)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        // Get all loaded assemblies including Sqlx
        var references = new List<MetadataReference>();
        
        // Add standard references
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }
        
        // Explicitly add Sqlx assembly reference
        var sqlxAssembly = typeof(Sqlx.Annotations.SqlxVarAttribute).Assembly;
        if (!string.IsNullOrWhiteSpace(sqlxAssembly.Location))
        {
            references.Add(MetadataReference.CreateFromFile(sqlxAssembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var runResult = driver.GetRunResult();

        return new GeneratorTestResult(
            outputCompilation,
            diagnostics,
            runResult);
    }

    /// <summary>
    /// Normalizes whitespace in generated code for comparison.
    /// </summary>
    /// <param name="code">The code to normalize.</param>
    /// <returns>The normalized code.</returns>
    public static string NormalizeWhitespace(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return tree.GetRoot().NormalizeWhitespace().ToFullString();
    }
}

/// <summary>
/// Result of running a source generator test.
/// </summary>
public class GeneratorTestResult
{
    public GeneratorTestResult(
        Compilation compilation,
        ImmutableArray<Diagnostic> diagnostics,
        GeneratorDriverRunResult runResult)
    {
        Compilation = compilation;
        Diagnostics = diagnostics;
        RunResult = runResult;
    }

    /// <summary>
    /// Gets the output compilation.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// Gets the diagnostics produced during generation.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the generator run result.
    /// </summary>
    public GeneratorDriverRunResult RunResult { get; }

    /// <summary>
    /// Gets the generated source code.
    /// </summary>
    public string GetGeneratedSource()
    {
        if (RunResult.GeneratedTrees.Length == 0)
        {
            return string.Empty;
        }

        return RunResult.GeneratedTrees[0].ToString();
    }

    /// <summary>
    /// Gets all generated source files.
    /// </summary>
    public IEnumerable<(string FileName, string Source)> GetAllGeneratedSources()
    {
        foreach (var result in RunResult.Results)
        {
            foreach (var source in result.GeneratedSources)
            {
                yield return (source.HintName, source.SourceText.ToString());
            }
        }
    }
}
