// -----------------------------------------------------------------------
// <copyright file="IMethodAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Interface for method analysis services.
/// </summary>
public interface IMethodAnalyzer
{
    /// <summary>
    /// Analyzes a method to determine its operation type.
    /// </summary>
    /// <param name="method">The method to analyze.</param>
    /// <returns>The method analysis result.</returns>
    MethodAnalysisResult AnalyzeMethod(IMethodSymbol method);

    /// <summary>
    /// Determines if a method is async.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True if async.</returns>
    bool IsAsyncMethod(IMethodSymbol method);

    /// <summary>
    /// Gets the return type for async methods.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>The actual return type.</returns>
    ITypeSymbol GetActualReturnType(IMethodSymbol method);
}

/// <summary>
/// Result of method analysis.
/// </summary>
public class MethodAnalysisResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MethodAnalysisResult"/> class.
    /// </summary>
    public MethodAnalysisResult(
        MethodOperationType operationType,
        bool isAsync,
        ITypeSymbol returnType,
        bool isCollection,
        bool isScalar)
    {
        OperationType = operationType;
        IsAsync = isAsync;
        ReturnType = returnType;
        IsCollection = isCollection;
        IsScalar = isScalar;
    }

    /// <summary>
    /// Gets the operation type.
    /// </summary>
    public MethodOperationType OperationType { get; }

    /// <summary>
    /// Gets a value indicating whether the method is async.
    /// </summary>
    public bool IsAsync { get; }

    /// <summary>
    /// Gets the return type.
    /// </summary>
    public ITypeSymbol ReturnType { get; }

    /// <summary>
    /// Gets a value indicating whether the return type is a collection.
    /// </summary>
    public bool IsCollection { get; }

    /// <summary>
    /// Gets a value indicating whether the return type is scalar.
    /// </summary>
    public bool IsScalar { get; }
}

/// <summary>
/// Types of method operations.
/// </summary>
public enum MethodOperationType
{
    /// <summary>
    /// Unknown operation.
    /// </summary>
    Unknown,

    /// <summary>
    /// Insert operation.
    /// </summary>
    Insert,

    /// <summary>
    /// Update operation.
    /// </summary>
    Update,

    /// <summary>
    /// Delete operation.
    /// </summary>
    Delete,

    /// <summary>
    /// Select operation.
    /// </summary>
    Select,

    /// <summary>
    /// Scalar operation (count, exists, etc.).
    /// </summary>
    Scalar,

    /// <summary>
    /// Custom operation with explicit SQL.
    /// </summary>
    Custom
}
