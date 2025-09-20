// -----------------------------------------------------------------------
// <copyright file="MethodAnalysisResult.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Result of method analysis for code generation.
/// </summary>
public record MethodAnalysisResult(
    MethodOperationType OperationType,
    bool IsAsync,
    ITypeSymbol ReturnType,
    bool IsCollection,
    bool IsScalar);

/// <summary>
/// Types of SQL operations.
/// </summary>
public enum MethodOperationType
{
    Select,
    Insert,
    Update,
    Delete,
    Custom,
    Scalar,
    Unknown
}
