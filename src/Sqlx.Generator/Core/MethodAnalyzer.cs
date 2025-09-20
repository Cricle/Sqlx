// -----------------------------------------------------------------------
// <copyright file="MethodAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Sqlx.Generator.Core;

/// <summary>
/// Default implementation of method analyzer.
/// </summary>
public class MethodAnalyzer
{
    /// <summary>
    /// Analyzes a method to determine its characteristics for code generation.
    /// </summary>
    public MethodAnalysisResult AnalyzeMethod(IMethodSymbol method)
    {
        var isAsync = IsAsyncMethod(method);
        var returnType = GetActualReturnType(method);
        var isCollection = TypeAnalyzer.IsCollectionType(returnType);
        var isScalar = IsScalarType(returnType);
        var operationType = DetermineOperationType(method);

        return new MethodAnalysisResult(operationType, isAsync, returnType, isCollection, isScalar);
    }

    /// <summary>
    /// Determines if a method is asynchronous.
    /// </summary>
    public bool IsAsyncMethod(IMethodSymbol method)
    {
        if (method.ReturnType is not INamedTypeSymbol namedType) return false;

        // Check if it's a Task or Task<T>
        return namedType.Name == "Task" &&
               namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";
    }

    /// <summary>
    /// Gets the actual return type from async methods.
    /// </summary>
    public ITypeSymbol GetActualReturnType(IMethodSymbol method)
    {
        if (IsAsyncMethod(method) && method.ReturnType is INamedTypeSymbol taskType && taskType.TypeArguments.Length > 0)
        {
            return taskType.TypeArguments[0];
        }

        return method.ReturnType;
    }

    private MethodOperationType DetermineOperationType(IMethodSymbol method)
    {
        // Check for explicit SQL attributes first
        var sqlxAttr = method.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.Name == "SqlxAttribute");
        if (sqlxAttr != null)
        {
            return MethodOperationType.Custom;
        }

        var sqlExecuteTypeAttr = method.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (sqlExecuteTypeAttr != null)
        {
            return MethodOperationType.Custom;
        }

        // Analyze method name patterns
        var methodName = method.Name.ToLowerInvariant();

        // Insert operations
        if (methodName.Contains("create") ||
            methodName.Contains("insert") ||
            methodName.Contains("add"))
        {
            return MethodOperationType.Insert;
        }

        // Update operations
        if (methodName.Contains("update") ||
            methodName.Contains("modify") ||
            methodName.Contains("change"))
        {
            return MethodOperationType.Update;
        }

        // Delete operations
        if (methodName.Contains("delete") ||
            methodName.Contains("remove"))
        {
            return MethodOperationType.Delete;
        }

        // Scalar operations
        if (methodName.Contains("count") ||
            methodName.Contains("exists") ||
            methodName.Contains("sum") ||
            methodName.Contains("max") ||
            methodName.Contains("min") ||
            methodName.Contains("average") ||
            IsScalarType(GetActualReturnType(method)))
        {
            return MethodOperationType.Scalar;
        }

        // Select operations (default for query-like methods)
        if (methodName.Contains("get") ||
            methodName.Contains("find") ||
            methodName.Contains("select") ||
            methodName.Contains("query") ||
            methodName.Contains("list") ||
            !method.ReturnsVoid)
        {
            return MethodOperationType.Select;
        }

        return MethodOperationType.Unknown;
    }


    private bool IsScalarType(ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_Int32 ||
               type.SpecialType == SpecialType.System_Boolean ||
               type.SpecialType == SpecialType.System_Int64 ||
               type.SpecialType == SpecialType.System_Decimal ||
               type.SpecialType == SpecialType.System_Double ||
               type.SpecialType == SpecialType.System_Single ||
               type.SpecialType == SpecialType.System_String ||
               type.SpecialType == SpecialType.System_DateTime;
    }
}
