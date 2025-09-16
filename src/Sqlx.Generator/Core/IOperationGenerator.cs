// -----------------------------------------------------------------------
// <copyright file="IOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Interface for generating database operation code.
/// </summary>
public interface IOperationGenerator
{
    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Generates the operation code.
    /// </summary>
    /// <param name="context">The generation context.</param>
    void GenerateOperation(OperationGenerationContext context);

    /// <summary>
    /// Determines if this generator can handle the specified method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True if this generator can handle the method.</returns>
    bool CanHandle(IMethodSymbol method);
}
