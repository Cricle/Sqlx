// -----------------------------------------------------------------------
// <copyright file="SqlTemplateGenerationTestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.SqlTemplateGeneration
{
    /// <summary>
    /// Base class for SqlTemplate generation tests.
    /// Provides common utilities for testing source generation.
    /// </summary>
    public abstract class SqlTemplateGenerationTestBase
    {
        /// <summary>
        /// Creates a compilation from source code.
        /// </summary>
        protected static Compilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Data.IDbConnection).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            };

            // Add Sqlx assembly reference
            var sqlxAssembly = typeof(SqlTemplate).Assembly;
            references.Add(MetadataReference.CreateFromFile(sqlxAssembly.Location));

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }

        /// <summary>
        /// Runs the source generator on the compilation.
        /// </summary>
        protected static GeneratorDriverRunResult RunGenerator(Compilation compilation)
        {
            var generator = new CSharpGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            var runResult = driver.GetRunResult();
            return runResult;
        }

        /// <summary>
        /// Gets the generated source code for a specific file.
        /// </summary>
        protected static string? GetGeneratedSource(GeneratorDriverRunResult runResult, string fileName)
        {
            var generatedFile = runResult.GeneratedTrees
                .FirstOrDefault(t => t.FilePath.EndsWith(fileName));

            return generatedFile?.ToString();
        }

        /// <summary>
        /// Checks if the generated code contains a specific string.
        /// </summary>
        protected static bool GeneratedCodeContains(GeneratorDriverRunResult runResult, string searchText)
        {
            return runResult.GeneratedTrees
                .Any(tree => tree.ToString().Contains(searchText));
        }

        /// <summary>
        /// Gets all diagnostics from the generator run.
        /// </summary>
        protected static IEnumerable<Diagnostic> GetDiagnostics(GeneratorDriverRunResult runResult)
        {
            return runResult.Diagnostics;
        }
    }
}
