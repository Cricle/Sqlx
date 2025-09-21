// -----------------------------------------------------------------------
// <copyright file="AbstractGenerator.Refactored.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Sqlx.Generator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Simplified stored procedures generator with unified service architecture.
/// </summary>
public abstract partial class AbstractGenerator : ISourceGenerator
{
    private readonly ISqlxGeneratorService _generatorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractGenerator"/> class.
    /// </summary>
    protected AbstractGenerator()
    {
        _generatorService = new SqlxGeneratorService();
    }

    /// <inheritdoc/>
    public abstract void Initialize(GeneratorInitializationContext context);

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            // Get the syntax receiver efficiently
            ISqlxSyntaxReceiver? receiver = context.SyntaxReceiver as ISqlxSyntaxReceiver ??
                                           context.SyntaxContextReceiver as ISqlxSyntaxReceiver;

            if (receiver == null)
            {
                return;
            }

            // Process collected syntax nodes to populate symbol lists
            ProcessCollectedSyntaxNodes(context, receiver);

            // Get required symbol references
            var symbolReferences = GetRequiredSymbols(context);
            if (!symbolReferences.IsValid)
            {
                context.ReportDiagnostic(Diagnostic.Create(Messages.SP0001, null));
                return;
            }

            // Process existing methods (class-based generation)
            ProcessExistingMethods(context, receiver, symbolReferences);

            // Process repository classes (interface-based generation)
            ProcessRepositoryClasses(context, receiver, symbolReferences);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("SQLX9999", "Generator execution error",
                $"Error during code generation: {ex.Message}", "Generator", DiagnosticSeverity.Error, true),
                null));
        }
    }

    /// <summary>
    /// Gets the required symbol references for code generation.
    /// </summary>
    protected virtual SymbolReferences GetRequiredSymbols(GeneratorExecutionContext context) => new(
        context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute"),
        context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.ExpressionToSqlAttribute"),
        context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlExecuteTypeAttribute"),
        context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.RepositoryForAttribute"),
        context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.TableNameAttribute"));

    private void ProcessExistingMethods(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols)
    {
        try
        {
            // Group methods by containing class and generate code
            foreach (var group in receiver.Methods.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                try
                {
                    var containingType = (INamedTypeSymbol)group.Key!;
                    var methods = group.ToList();

                    // Note: Classes with RepositoryFor attribute can still have individual [Sqlx] methods
                    // that need to be processed. Process them normally.

                    // Skip all interface methods - they should only be processed through repository classes
                    if (containingType.TypeKind == TypeKind.Interface)
                        return;

                    // Generate code for methods

                    var ctx = new ClassGenerationContext(containingType, methods, symbols.SqlxAttributeSymbol!);
                    ctx.SetExecutionContext(context);

                    var sb = new IndentedStringBuilder(string.Empty);

                    if (ctx.CreateSource(sb))
                    {
                        var fileName = $"{containingType.ToDisplayString().Replace(".", "_")}.Sql.g.cs";
                        var sourceText = SourceText.From(sb.ToString().Trim(), Encoding.UTF8);
                        context.AddSource(fileName, sourceText);
                    }
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("SQLX9996", "Method group processing error",
                        $"Error processing method group for {group.Key?.Name ?? "unknown"}: {ex.Message}",
                        "Generator", DiagnosticSeverity.Error, true), null));
                }
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("SQLX9995", "Existing method processing error",
                $"Error processing existing methods: {ex.Message}",
                "Generator", DiagnosticSeverity.Error, true), null));
        }
    }

    private void ProcessRepositoryClasses(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols)
    {
        foreach (var repositoryClass in receiver.RepositoryClasses)
        {
            try
            {
                // Analyze interface methods in repository class
                var repositoryForAttr = repositoryClass.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.RepositoryForAttributeSymbol));

                if (repositoryForAttr?.ConstructorArguments.FirstOrDefault().Value is INamedTypeSymbol interfaceType)
                {
                    var interfaceMethods = interfaceType.GetMembers().OfType<IMethodSymbol>().ToList();

                    // Perform diagnostic analysis for each method in the interface
                    foreach (var method in interfaceMethods)
                    {
                        var sqlxAttr = method.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true);

                        if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sql)
                        {
                            var entityType = _generatorService.InferEntityTypeFromMethod(method);
                            // Diagnostic analysis was simplified
                        }
                    }
                }

                var generationContext = new RepositoryGenerationContext(
                    context,
                    repositoryClass,
                    symbols.RepositoryForAttributeSymbol,
                    symbols.TableNameAttributeSymbol,
                    _generatorService.TypeInferenceService,
                    _generatorService.TemplateEngine,
                    _generatorService.AttributeHandler);
                _generatorService.CodeGenerationService.GenerateRepositoryImplementation(generationContext);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("SQLX9997", "Repository class processing error",
                    $"Error processing repository class {repositoryClass.Name}: {ex.Message}",
                    "Generator", DiagnosticSeverity.Error, true), null));
            }
        }
    }

    /// <summary>
    /// Container for symbol references needed during code generation.
    /// </summary>
    protected class SymbolReferences
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolReferences"/> class.
        /// </summary>
        public SymbolReferences(
            INamedTypeSymbol? sqlxAttributeSymbol,
            INamedTypeSymbol? expressionToSqlAttributeSymbol,
            INamedTypeSymbol? sqlExecuteTypeAttributeSymbol,
            INamedTypeSymbol? repositoryForAttributeSymbol,
            INamedTypeSymbol? tableNameAttributeSymbol)
        {
            SqlxAttributeSymbol = sqlxAttributeSymbol;
            ExpressionToSqlAttributeSymbol = expressionToSqlAttributeSymbol;
            SqlExecuteTypeAttributeSymbol = sqlExecuteTypeAttributeSymbol;
            RepositoryForAttributeSymbol = repositoryForAttributeSymbol;
            TableNameAttributeSymbol = tableNameAttributeSymbol;
        }

        /// <summary>
        /// Gets the Sqlx attribute symbol.
        /// </summary>
        public INamedTypeSymbol? SqlxAttributeSymbol { get; }

        /// <summary>
        /// Gets the ExpressionToSql attribute symbol.
        /// </summary>
        public INamedTypeSymbol? ExpressionToSqlAttributeSymbol { get; }

        /// <summary>
        /// Gets the SqlExecuteType attribute symbol.
        /// </summary>
        public INamedTypeSymbol? SqlExecuteTypeAttributeSymbol { get; }

        /// <summary>
        /// Gets the RepositoryFor attribute symbol.
        /// </summary>
        public INamedTypeSymbol? RepositoryForAttributeSymbol { get; }

        /// <summary>
        /// Gets the TableName attribute symbol.
        /// </summary>
        public INamedTypeSymbol? TableNameAttributeSymbol { get; }

        /// <summary>
        /// Gets a value indicating whether the essential symbols are available.
        /// </summary>
        public bool IsValid => SqlxAttributeSymbol != null &&
                              ExpressionToSqlAttributeSymbol != null &&
                              SqlExecuteTypeAttributeSymbol != null;
    }


    /// <summary>
    /// Processes collected syntax nodes to populate symbol lists.
    /// </summary>
    private void ProcessCollectedSyntaxNodes(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver)
    {
        try
        {
            var compilation = context.Compilation;

            // Process method syntax nodes
            foreach (var methodSyntax in receiver.MethodSyntaxNodes)
            {
                try
                {
                    var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(methodSyntax) is IMethodSymbol method && HasSqlxAttribute(method))
                    {
                        receiver.Methods.Add(method);
                    }
                }
                catch
                {
                    // Silently ignore individual method processing errors
                }
            }

            // Process class syntax nodes
            foreach (var classSyntax in receiver.ClassSyntaxNodes)
            {
                try
                {
                    var semanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol type && HasRepositoryForAttribute(type))
                    {
                        receiver.RepositoryClasses.Add(type);
                    }
                }
                catch
                {
                    // Silently ignore individual class processing errors
                }
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("SQLX9998", "Syntax node processing error",
                $"Error processing syntax nodes: {ex.Message}",
                "Generator", DiagnosticSeverity.Error, true), null));
        }
    }

    /// <summary>
    /// Checks if a method has Sqlx attributes using semantic analysis.
    /// </summary>
    private static bool HasSqlxAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "SqlxAttribute" ||
            attr.AttributeClass?.Name == "Sqlx" ||
            attr.AttributeClass?.Name == "SqlTemplateAttribute" ||
            attr.AttributeClass?.Name == "SqlTemplate" ||
            attr.AttributeClass?.Name == "SqlExecuteTypeAttribute" ||
            attr.AttributeClass?.Name == "SqlExecuteType");
    }

    /// <summary>
    /// Checks if a type has RepositoryFor attribute using semantic analysis.
    /// </summary>
    private static bool HasRepositoryForAttribute(INamedTypeSymbol type)
    {
        return type.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "RepositoryForAttribute" ||
            attr.AttributeClass?.Name == "RepositoryFor");
    }
}
