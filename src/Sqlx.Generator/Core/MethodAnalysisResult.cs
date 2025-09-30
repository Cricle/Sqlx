// -----------------------------------------------------------------------
// <copyright file="MethodAnalysisResult.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator;

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
    /// <summary>SELECT查询操作</summary>
    Select,
    /// <summary>INSERT插入操作</summary>
    Insert,
    /// <summary>UPDATE更新操作</summary>
    Update,
    /// <summary>DELETE删除操作</summary>
    Delete,
    /// <summary>自定义SQL操作</summary>
    Custom,
    /// <summary>标量返回操作</summary>
    Scalar,
    /// <summary>未知操作类型</summary>
    Unknown
}
