// -----------------------------------------------------------------------
// <copyright file="SqlxGeneratorService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlx.Generator.Core;

/// <summary>
/// Unified service implementation for Sqlx code generation.
/// Combines type inference, method analysis, and code generation capabilities.
/// </summary>
public class SqlxGeneratorService : ISqlxGeneratorService
{
    private readonly ITypeInferenceService _typeInferenceService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly OperationGeneratorFactory _operationFactory;
    private readonly IAttributeHandler _attributeHandler;
    private readonly IMethodAnalyzer _methodAnalyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlxGeneratorService"/> class.
    /// </summary>
    public SqlxGeneratorService()
    {
        _typeInferenceService = new TypeInferenceService();
        _codeGenerationService = new CodeGenerationService();
        _operationFactory = new OperationGeneratorFactory();
        _attributeHandler = new AttributeHandler();
        _methodAnalyzer = new MethodAnalyzer();
    }

    /// <inheritdoc/>
    public INamedTypeSymbol? InferEntityTypeFromInterface(INamedTypeSymbol serviceInterface)
        => _typeInferenceService.InferEntityTypeFromServiceInterface(serviceInterface);

    /// <inheritdoc/>
    public INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method)
        => _typeInferenceService.InferEntityTypeFromMethod(method);

    /// <inheritdoc/>
    public INamedTypeSymbol? GetServiceInterface(INamedTypeSymbol repositoryClass, Compilation compilation)
    {
        var repositoryForAttr = repositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute");

        if (repositoryForAttr?.ConstructorArguments.Length > 0)
        {
            var typeArg = repositoryForAttr.ConstructorArguments[0];
            if (typeArg.Value is INamedTypeSymbol serviceType)
                return serviceType;
        }

        // Fallback: Parse syntax directly when semantic model fails (common in test environments)
        return GetServiceInterfaceFromSyntax(repositoryClass, compilation);
    }

    private INamedTypeSymbol? GetServiceInterfaceFromSyntax(INamedTypeSymbol repositoryClass, Compilation compilation)
    {
        // Get the syntax reference for the repository class
        var syntaxRef = repositoryClass.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef?.GetSyntax() is not ClassDeclarationSyntax classSyntax)
            return null;

        // Look for RepositoryFor attribute in syntax
        foreach (var attrList in classSyntax.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString();
                if (attrName is "RepositoryFor" or "RepositoryForAttribute")
                {
                    // Parse the typeof() argument
                    if (attr.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeofExpr)
                    {
                        // Try to resolve the type name to a symbol
                        var semanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
                        var typeInfo = semanticModel.GetTypeInfo(typeofExpr.Type);
                        if (typeInfo.Type is INamedTypeSymbol interfaceType)
                            return interfaceType;
                    }
                }
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public string GetTableName(INamedTypeSymbol? entityType, INamedTypeSymbol? tableNameAttributeSymbol)
        => _typeInferenceService.GetTableNameFromEntity(entityType, tableNameAttributeSymbol);

    /// <inheritdoc/>
    public string GetTableName(INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceInterface, INamedTypeSymbol? tableNameAttributeSymbol)
        => _typeInferenceService.GetTableName(repositoryClass, serviceInterface, tableNameAttributeSymbol);

    /// <inheritdoc/>
    public MethodAnalysisResult AnalyzeMethod(IMethodSymbol method)
        => _methodAnalyzer.AnalyzeMethod(method);


    /// <inheritdoc/>
    public void GenerateRepositoryImplementation(GenerationContext context)
    {
        if (context.RepositoryClass == null)
        {
            return;
        }

        // Skip if the class has SqlTemplate attribute
        if (context.RepositoryClass.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SqlTemplate"))
        {
            return;
        }

        // Re-enabled repository generation after fixing yield issue
        // Get the service interface from RepositoryFor attribute
        var serviceInterface = GetServiceInterface(context.RepositoryClass, context.Compilation);
        if (serviceInterface == null)
        {
            return;
        }

        var entityType = InferEntityTypeFromInterface(serviceInterface);
        var tableName = GetTableName(entityType, null);

        // Generate the repository implementation
        GenerateRepositoryClass(context, serviceInterface, entityType, tableName);

        var generatedCode = context.StringBuilder.ToString().Trim();

        // Add source to compilation
        var sourceText = SourceText.From(generatedCode, Encoding.UTF8);
        var fileName = $"{context.RepositoryClass.ToDisplayString().Replace(".", "_")}.Repository.g.cs";

        context.ExecutionContext.AddSource(fileName, sourceText);
    }

    /// <inheritdoc/>
    public void GenerateAttributes(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
        => _attributeHandler.GenerateOrCopyAttributes(sb, method, entityType, tableName);

    private void GenerateFallbackMethod(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var isAsync = returnType.Contains("Task");
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine($"public {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        if (isAsync)
        {
            if (returnType == "Task" || returnType == "System.Threading.Tasks.Task")
            {
                sb.AppendLine("return Task.CompletedTask;");
            }
            else
            {
                var innerType = ExtractInnerTypeFromTask(returnType);
                sb.AppendLine($"return Task.FromResult(default({innerType}));");
            }
        }
        else
        {
            if (returnType != "void")
            {
                sb.AppendLine($"return default({returnType});");
            }
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateRepositoryClass(GenerationContext context, INamedTypeSymbol serviceInterface, INamedTypeSymbol? entityType, string tableName)
    {
        var repositoryClass = context.RepositoryClass!;
        var sb = context.StringBuilder;

        // Generate file header
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();

        // Generate usings
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Data.Common;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();

        // Generate namespace
        var namespaceName = repositoryClass.ContainingNamespace.ToDisplayString();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        // Generate class declaration
        sb.AppendLine($"partial class {repositoryClass.Name} : {serviceInterface.ToDisplayString()}");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate methods
        var interfaceMethods = serviceInterface.GetMembers().OfType<IMethodSymbol>().ToList();
        foreach (var method in interfaceMethods)
        {
            GenerateRepositoryMethodDirect(sb, method, entityType, tableName);
        }

        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateRepositoryMethodDirect(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        // Generate method signature first
        var returnType = method.ReturnType.ToDisplayString();
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        var isAsync = returnType.Contains("Task");

        sb.AppendLine($"public {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        // For now, always generate simple fallback implementation
        // TODO: Implement proper operation generation for RepositoryFor
        GenerateSimpleFallback(sb, returnType, isAsync);

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateSimpleFallback(IndentedStringBuilder sb, string returnType, bool isAsync)
    {
        if (isAsync)
        {
            if (returnType == "Task" || returnType == "System.Threading.Tasks.Task")
            {
                sb.AppendLine("return Task.CompletedTask;");
            }
            else
            {
                // Extract inner type from Task<T>
                var innerType = ExtractInnerTypeFromTask(returnType);
                sb.AppendLine($"return Task.FromResult(default({innerType}));");
            }
        }
        else
        {
            if (returnType != "void")
            {
                sb.AppendLine($"return default({returnType});");
            }
        }
    }

    private string ExtractInnerTypeFromTask(string taskType)
    {
        // Handle various Task<T> formats
        if (taskType.StartsWith("Task<") && taskType.EndsWith(">"))
        {
            return taskType.Substring(5, taskType.Length - 6);
        }
        if (taskType.StartsWith("System.Threading.Tasks.Task<") && taskType.EndsWith(">"))
        {
            return taskType.Substring(28, taskType.Length - 29);
        }

        // Fallback for complex generic types
        var startIndex = taskType.IndexOf('<');
        var endIndex = taskType.LastIndexOf('>');
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return taskType.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        return "object"; // Safe fallback
    }
}
