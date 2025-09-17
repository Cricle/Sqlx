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
        ErrorHandler.ExecuteSafely(context, () =>
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

            // Create diagnostic guidance service
            var diagnosticService = new DiagnosticGuidanceService(context);

            // Process existing methods (class-based generation)
            ProcessExistingMethods(context, receiver, symbolReferences, diagnosticService);

            // Process repository classes (interface-based generation)
            ProcessRepositoryClasses(context, receiver, symbolReferences, diagnosticService);

            // Generate diagnostic summary
            var allMethods = GetAllAnalyzedMethods(receiver);
            if (allMethods.Any())
            {
                diagnosticService.GenerateDiagnosticSummary(allMethods);
            }
        }, "SQLX9999", "source generation");
    }

    /// <summary>
    /// Gets the required symbol references for code generation.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <returns>The symbol references.</returns>
    protected virtual SymbolReferences GetRequiredSymbols(GeneratorExecutionContext context)
    {
        return new SymbolReferences(
            sqlxAttributeSymbol: context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute"),
            expressionToSqlAttributeSymbol: context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.ExpressionToSqlAttribute"),
            sqlExecuteTypeAttributeSymbol: context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlExecuteTypeAttribute"),
            repositoryForAttributeSymbol: context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.RepositoryForAttribute"),
            tableNameAttributeSymbol: context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.TableNameAttribute"));
    }

    private void ProcessExistingMethods(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols, DiagnosticGuidanceService diagnosticService)
    {
        ErrorHandler.ExecuteSafely(context, () =>
        {
            // Group methods by containing class and generate code
            foreach (var group in receiver.Methods.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                ErrorHandler.ExecuteSafely(context, () =>
                {
                    var containingType = (INamedTypeSymbol)group.Key!;
                    var methods = group.ToList();

                    // Note: Classes with RepositoryFor attribute can still have individual [Sqlx] methods
                    // that need to be processed. Process them normally.

                    // Skip all interface methods - they should only be processed through repository classes
                    if (containingType.TypeKind == TypeKind.Interface)
                        return;

                    // Perform diagnostic analysis for each method
                    foreach (var method in methods)
                    {
                        var sqlxAttr = method.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true);

                        if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sql)
                        {
                            var entityType = _generatorService.InferEntityTypeFromMethod(method);
                            diagnosticService.PerformComprehensiveAnalysis(method, sql, entityType);
                        }
                    }

                    var ctx = new ClassGenerationContext(containingType, methods, symbols.SqlxAttributeSymbol!);
                    ctx.SetExecutionContext(context);

                    var sb = new IndentedStringBuilder(string.Empty);

                    if (ctx.CreateSource(sb))
                    {
                        var fileName = $"{containingType.ToDisplayString().Replace(".", "_")}.Sql.g.cs";
                        var sourceText = SourceText.From(sb.ToString().Trim(), Encoding.UTF8);
                        context.AddSource(fileName, sourceText);
                    }
                }, "SQLX9996", $"method group processing for {group.Key?.Name ?? "unknown"}");
            }
        }, "SQLX9995", "existing method processing");
    }

    private void ProcessRepositoryClasses(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols, DiagnosticGuidanceService diagnosticService)
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
                            diagnosticService.PerformComprehensiveAnalysis(method, sql, entityType);
                        }
                    }
                }

                var generationContext = new GenerationContext(context, repositoryClass, _generatorService);
                _generatorService.GenerateRepositoryImplementation(generationContext);
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportError(context, ex, "SQLX9997", "Repository class processing error",
                    "Error processing repository class {0}: {1}", repositoryClass.Name);
            }
        }
    }

    /// <summary>
    /// Generates or copies attributes for a method.
    /// </summary>
    /// <param name="method">The method to generate attributes for.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns>The generated SqlxAttribute string.</returns>
    protected virtual string GenerateSqlxAttribute(IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        var sb = new IndentedStringBuilder(string.Empty);
        _generatorService.GenerateAttributes(sb, method, entityType, tableName);
        return sb.ToString();
    }

    /// <summary>
    /// Generates SqlxAttribute from existing attribute data.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The generated SqlxAttribute string.</returns>
    protected virtual string GenerateSqlxAttribute(AttributeData attribute)
    {
        // For now, return a simple attribute string - this would need more complex logic
        return "[Sqlx(\"TODO: Extract SQL from attribute\")]";
    }

    /// <summary>
    /// Determines if a type is a collection type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection type.</returns>
    protected virtual bool IsCollectionType(ITypeSymbol type)
    {
        return TypeAnalyzer.IsCollectionType(type);
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
    /// 获取所有已分析的方法用于诊断摘要
    /// </summary>
    private IReadOnlyList<IMethodSymbol> GetAllAnalyzedMethods(ISqlxSyntaxReceiver receiver)
    {
        var methods = new List<IMethodSymbol>();

        // 收集所有直接带有Sqlx特性的方法
        methods.AddRange(receiver.Methods.Where(m =>
            m.GetAttributes().Any(a =>
                a.AttributeClass?.Name?.Contains("Sqlx") == true ||
                a.AttributeClass?.Name?.Contains("ExpressionToSql") == true)));

        // 收集仓储类中的接口方法
        foreach (var repositoryClass in receiver.RepositoryClasses)
        {
            var repositoryForAttr = repositoryClass.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryForAttribute");

            if (repositoryForAttr?.ConstructorArguments.FirstOrDefault().Value is INamedTypeSymbol interfaceType)
            {
                var interfaceMethods = interfaceType.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.GetAttributes().Any(a =>
                        a.AttributeClass?.Name?.Contains("Sqlx") == true ||
                        a.AttributeClass?.Name?.Contains("ExpressionToSql") == true))
                    .ToList();

                methods.AddRange(interfaceMethods);
            }
        }

        return methods;
    }

    /// <summary>
    /// Processes collected syntax nodes to populate symbol lists.
    /// </summary>
    private void ProcessCollectedSyntaxNodes(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver)
    {
        ErrorHandler.ExecuteSafely(context, () =>
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
        }, "SQLX9998", "syntax node processing");
    }

    /// <summary>
    /// Checks if a method has Sqlx attributes using semantic analysis.
    /// </summary>
    private static bool HasSqlxAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "SqlxAttribute" ||
            attr.AttributeClass?.Name == "Sqlx" ||
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
