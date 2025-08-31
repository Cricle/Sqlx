// -----------------------------------------------------------------------
// <copyright file="CodeGenerationTestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Base class for code generation tests using C# source generator.
/// </summary>
public abstract class CodeGenerationTestBase
{
    /// <summary>
    /// Compiles C# source code and runs the Sqlx source generator on it.
    /// </summary>
    /// <param name="source">The C# source code to compile.</param>
    /// <returns>The generated code output.</returns>
    protected static string GetCSharpGeneratedOutput(string source)
    {
        return Compile(source, NullableContextOptions.Enable);
    }

    /// <summary>
    /// Compiles C# source code and runs the Sqlx source generator on it.
    /// </summary>
    /// <param name="source">The C# source code to compile.</param>
    /// <param name="nullableOptions">The nullable reference types options.</param>
    /// <returns>The generated code output.</returns>
    protected static string GetCSharpGeneratedOutput(string source, NullableContextOptions nullableOptions)
    {
        return Compile(source, nullableOptions);
    }

    /// <summary>
    /// Verifies C# code compiles and generates code successfully.
    /// </summary>
    /// <param name="source">The C# source code to verify.</param>
    protected static void VerifyCSharp(string source)
    {
        var result = Compile(source, NullableContextOptions.Enable);

        // The verification passes if compilation and generation succeed without errors
        // The actual verification is done by the Assert.Fail calls in Compile method
    }

    /// <summary>
    /// Verifies C# code compiles and generates code successfully.
    /// </summary>
    /// <param name="source">The C# source code to verify.</param>
    /// <param name="nullableOptions">The nullable reference types options.</param>
    protected static void VerifyCSharp(string source, NullableContextOptions nullableOptions)
    {
        var result = Compile(source, nullableOptions);

        // The verification passes if compilation and generation succeed without errors
        // The actual verification is done by the Assert.Fail calls in Compile method
    }

    /// <summary>
    /// Compiles C# source code and runs the Sqlx source generator on it.
    /// </summary>
    /// <param name="source">The C# source code to compile.</param>
    /// <param name="nullableOptions">The nullable reference types options.</param>
    /// <returns>The generated code output.</returns>
    protected static string Compile(string source, NullableContextOptions nullableOptions = NullableContextOptions.Enable)
    {
        // Add necessary using statements to the source if not already present
        // Add required using statements at the top if not present
        if (!source.TrimStart().StartsWith("using"))
        {
            source = @"using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

" + source;
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
        };

        // Add safe assembly references
        SafeAddAssemblyReference(references, "System.Runtime");
        SafeAddAssemblyReference(references, "System.Collections");
        SafeAddAssemblyReference(references, "System.Data.Common");
        SafeAddAssemblyReference(references, "System.Linq.Expressions");
        SafeAddAssemblyReference(references, "System.ComponentModel.Primitives");
        SafeAddAssemblyReference(references, "Microsoft.EntityFrameworkCore");
        SafeAddAssemblyReference(references, "Microsoft.EntityFrameworkCore.Relational");

        // Add reference to the Sqlx assembly
        references.Add(MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location));

        // Add System.ComponentModel.DataAnnotations reference
        references.Add(MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new SyntaxTree[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(nullableOptions));

        // Create and run the source generator FIRST
        ISourceGenerator generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create(generator));
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

        // Check for generator errors first
        var generatorErrors = generateDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (generatorErrors.Any())
        {
            var errorMessages = string.Join("\n", generatorErrors.Select(d => d.GetMessage()));
            Assert.Fail($"Code generation failed with errors:\n{errorMessages}");
        }

        // Now check final compilation errors
        var finalDiagnostics = outputCompilation.GetDiagnostics();
        var finalCompilationErrors = finalDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (finalCompilationErrors.Any())
        {
            var errorMessages = string.Join("\n", finalCompilationErrors.Select(d => d.GetMessage()));
            Assert.Fail($"Final compilation failed with errors:\n{errorMessages}");
        }

        // Get the generated code
        var generatedFiles = outputCompilation.SyntaxTrees.Skip(1).ToList(); // Skip the original source
        if (generatedFiles.Any())
        {
            string output = string.Join("\n\n", generatedFiles.Select(tree => tree.ToString()));
            Console.WriteLine("Generated code:");
            Console.WriteLine(output);
            return output;
        }

        return string.Empty;
    }

    /// <summary>
    /// Safely adds an assembly reference to the list, ignoring if the assembly is not found.
    /// </summary>
    /// <param name="references">The reference list to add to.</param>
    /// <param name="assemblyName">The name of the assembly to load.</param>
    private static void SafeAddAssemblyReference(List<MetadataReference> references, string assemblyName)
    {
        try
        {
            var assembly = Assembly.Load(assemblyName);
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }
        catch (FileNotFoundException)
        {
            // Assembly not available, skip it for testing
            Console.WriteLine($"Warning: Assembly '{assemblyName}' not found, skipping reference");
        }
        catch (Exception ex)
        {
            // Other errors, also skip but log
            Console.WriteLine($"Warning: Failed to load assembly '{assemblyName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Adds required using statements to the source code if not already present.
    /// </summary>
    /// <param name="source">The C# source code.</param>
    /// <returns>The source code with required using statements.</returns>
    private static string AddRequiredUsingStatements(string source)
    {
        var requiredUsings = new[]
        {
            "using System;",
            "using System.Collections.Generic;",
            "using System.Data.Common;",
            "using System.Threading;",
            "using System.Threading.Tasks;",
        };

        // Check if source already has using statements
        if (source.Contains("using "))
        {
            return source; // Don't modify if it already has using statements
        }

        // Insert using statements at the beginning
        var usingStatements = string.Join("\n", requiredUsings) + "\n\n";
        return usingStatements + source;
    }
}
