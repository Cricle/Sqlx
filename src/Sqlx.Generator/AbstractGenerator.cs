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
using System.Linq;
using System.Text;

/// <summary>
/// Refactored stored procedures generator with improved architecture.
/// </summary>
public abstract partial class AbstractGenerator : ISourceGenerator
{
    private readonly ITypeInferenceService _typeInferenceService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly OperationGeneratorFactory _operationFactory;
    private readonly IAttributeHandler _attributeHandler;
    private readonly IMethodAnalyzer _methodAnalyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractGenerator"/> class.
    /// </summary>
    protected AbstractGenerator()
    {
        _typeInferenceService = new TypeInferenceService();
        _codeGenerationService = new CodeGenerationService();
        _operationFactory = new OperationGeneratorFactory();
        _attributeHandler = new AttributeHandler();
        _methodAnalyzer = new MethodAnalyzer();
    }

    /// <inheritdoc/>
    public abstract void Initialize(GeneratorInitializationContext context);

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            // Retrieve the populated receiver
            if (context.SyntaxContextReceiver is not ISqlxSyntaxReceiver receiver)
            {
                return;
            }

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
            // Report diagnostic for unexpected errors
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SQLX9999",
                    "Unexpected error in code generation",
                    "An unexpected error occurred: {0}",
                    "CodeGeneration",
                    DiagnosticSeverity.Error,
                    true),
                null,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
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

    private void ProcessExistingMethods(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols)
    {
        // Group methods by containing class and generate code
        foreach (var group in receiver.Methods.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            var containingType = (INamedTypeSymbol)group.Key!;
            var methods = group.ToList();
            
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
    }

    private void ProcessRepositoryClasses(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols)
    {
        foreach (var repositoryClass in receiver.RepositoryClasses)
        {
            var generationContext = new RepositoryGenerationContext(
                context,
                repositoryClass,
                symbols.RepositoryForAttributeSymbol,
                symbols.TableNameAttributeSymbol,
                symbols.SqlxAttributeSymbol!,
                _typeInferenceService,
                _codeGenerationService,
                _operationFactory,
                _attributeHandler,
                _methodAnalyzer);

            _codeGenerationService.GenerateRepositoryImplementation(generationContext);
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
        return _attributeHandler.GenerateSqlxAttribute(method, entityType, tableName);
    }

    /// <summary>
    /// Generates SqlxAttribute from existing attribute data.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The generated SqlxAttribute string.</returns>
    protected virtual string GenerateSqlxAttribute(AttributeData attribute)
    {
        return _attributeHandler.GenerateSqlxAttribute(attribute);
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
}
