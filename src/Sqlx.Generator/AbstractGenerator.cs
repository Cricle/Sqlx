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
            if (context.SyntaxReceiver is not ISqlxSyntaxReceiver receiver)
            {
                return;
            }

            // Process collected syntax nodes to populate symbol lists
            ProcessCollectedSyntaxNodes(context, receiver);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("Getting required symbol references...");
#endif
            // Get required symbol references
            var symbolReferences = GetRequiredSymbols(context);
            if (!symbolReferences.IsValid)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Symbol references are not valid");
#endif
                context.ReportDiagnostic(Diagnostic.Create(Messages.SP0001, null));
                return;
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Symbol references are valid");
#endif

            // 创建诊断指导服务
            var diagnosticService = new DiagnosticGuidanceService(context);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("Processing existing methods...");
#endif
            // Process existing methods (class-based generation)
            ProcessExistingMethods(context, receiver, symbolReferences, diagnosticService);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("Processing repository classes...");
#endif
            // Process repository classes (interface-based generation)
            ProcessRepositoryClasses(context, receiver, symbolReferences, diagnosticService);

            // 生成诊断摘要
            var allMethods = GetAllAnalyzedMethods(receiver);
            if (allMethods.Any())
            {
                diagnosticService.GenerateDiagnosticSummary(allMethods);
            }
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

    private void ProcessExistingMethods(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols, DiagnosticGuidanceService diagnosticService)
    {
        try
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"ProcessExistingMethods: Processing {receiver.Methods.Count} methods");
            foreach (var method in receiver.Methods)
            {
                System.Diagnostics.Debug.WriteLine($"  Method: {method.Name} in {method.ContainingType?.Name ?? "null"}");
            }
#endif
            // Group methods by containing class and generate code
            foreach (var group in receiver.Methods.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                try
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Processing group for type: {group.Key?.Name ?? "null"}");
#endif
                    var containingType = (INamedTypeSymbol)group.Key!;
                    var methods = group.ToList();
            
                    // Skip classes that have RepositoryFor attribute - they are handled by ProcessRepositoryClasses
                    if (containingType.GetAttributes().Any(attr => attr.AttributeClass?.Name == "RepositoryForAttribute"))
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Skipping {containingType.Name} because it has RepositoryForAttribute");
#endif
                        continue;
                    }
                    
                    // Also skip methods from repository classes that might have been collected
                    if (receiver.RepositoryClasses.Contains(containingType, SymbolEqualityComparer.Default))
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Skipping {containingType.Name} because it's in repository classes list");
#endif
                        continue;
                    }
                    
                    // Skip all interface methods - they should only be processed through repository classes
                    if (containingType.TypeKind == TypeKind.Interface)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Skipping interface {containingType.Name} - interfaces should only be processed through repository classes");
#endif
                        continue;
                    }
                    
                    // 对每个方法执行诊断分析
                    foreach (var method in methods)
                    {
                        var sqlxAttr = method.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true);
                        
                        if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sql)
                        {
                            // 获取实体类型用于分析
                            var entityType = _typeInferenceService.InferEntityTypeFromMethod(method);
                            
                            // 执行全面的诊断分析
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
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Error processing group for type {group.Key?.Name ?? "null"}: {ex}");
#endif
                    // Report diagnostic for group processing errors
                    var diagnostic = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "SQLX9996",
                            "Error processing method group",
                            "Error processing method group for type {0}: {1}",
                            "CodeGeneration",
                            DiagnosticSeverity.Error,
                            true),
                        null,
                        group.Key?.Name ?? "unknown",
                        ex.Message);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error in ProcessExistingMethods: {ex}");
#endif
            // Report diagnostic for method processing errors
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SQLX9995",
                    "Error processing existing methods",
                    "Error in ProcessExistingMethods: {0}",
                    "CodeGeneration",
                    DiagnosticSeverity.Error,
                    true),
                null,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void ProcessRepositoryClasses(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver, SymbolReferences symbols, DiagnosticGuidanceService diagnosticService)
    {
        foreach (var repositoryClass in receiver.RepositoryClasses)
        {
            try
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Processing repository class: {repositoryClass.Name}");
#endif
                // 分析仓储类中的接口方法
                var repositoryForAttr = repositoryClass.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.RepositoryForAttributeSymbol));
                
                if (repositoryForAttr?.ConstructorArguments.FirstOrDefault().Value is INamedTypeSymbol interfaceType)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Found interface type: {interfaceType.Name}");
#endif
                    var interfaceMethods = interfaceType.GetMembers().OfType<IMethodSymbol>().ToList();
                    
                    // 对接口中的每个方法执行诊断分析
                    foreach (var method in interfaceMethods)
                    {
                        var sqlxAttr = method.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true);
                        
                        if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sql)
                        {
                            // 获取实体类型用于分析
                            var entityType = _typeInferenceService.InferEntityTypeFromMethod(method);
                            
                            // 执行全面的诊断分析
                            diagnosticService.PerformComprehensiveAnalysis(method, sql, entityType);
                        }
                    }
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Creating repository generation context for: {repositoryClass.Name}");
#endif
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

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Calling GenerateRepositoryImplementation for: {repositoryClass.Name}");
#endif
                _codeGenerationService.GenerateRepositoryImplementation(generationContext);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Completed GenerateRepositoryImplementation for: {repositoryClass.Name}");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Error processing repository class {repositoryClass.Name}: {ex}");
#endif
                // Report diagnostic for repository processing errors
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SQLX9997",
                        "Error processing repository class",
                        "Error processing repository class {0}: {1}",
                        "CodeGeneration",
                        DiagnosticSeverity.Error,
                        true),
                    null,
                    repositoryClass.Name,
                    ex.Message);
                context.ReportDiagnostic(diagnostic);
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
        try
        {
            var compilation = context.Compilation;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Processing {receiver.MethodSyntaxNodes.Count} method syntax nodes and {receiver.ClassSyntaxNodes.Count} class syntax nodes");
#endif

            // Process method syntax nodes
            foreach (var methodSyntax in receiver.MethodSyntaxNodes)
            {
                try
                {
                    var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(methodSyntax) is IMethodSymbol method)
                    {
                        if (HasSqlxAttribute(method))
                        {
                            receiver.Methods.Add(method);
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"Added method: {method.Name}");
#endif
                        }
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Error processing method syntax: {ex.Message}");
#endif
                }
            }

            // Process class syntax nodes
            foreach (var classSyntax in receiver.ClassSyntaxNodes)
            {
                try
                {
                    var semanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol type)
                    {
                        if (HasRepositoryForAttribute(type))
                        {
                            receiver.RepositoryClasses.Add(type);
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"Added repository class: {type.Name}");
#endif
                        }
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Error processing class syntax: {ex.Message}");
#endif
                }
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Final counts: {receiver.Methods.Count} methods, {receiver.RepositoryClasses.Count} repository classes");
#endif
        }
        catch (Exception ex)
        {
            // Report diagnostic for errors in processing
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SQLX9998",
                    "Error processing collected syntax nodes",
                    "Error in ProcessCollectedSyntaxNodes: {0}",
                    "CodeGeneration",
                    DiagnosticSeverity.Error,
                    true),
                null,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
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
