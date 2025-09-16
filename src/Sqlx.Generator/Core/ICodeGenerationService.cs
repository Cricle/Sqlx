// -----------------------------------------------------------------------
// <copyright file="ICodeGenerationService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Interface for code generation services.
/// </summary>
public interface ICodeGenerationService
{
    /// <summary>
    /// Generates repository method implementation.
    /// </summary>
    /// <param name="context">The generation context.</param>
    void GenerateRepositoryMethod(RepositoryMethodContext context);

    /// <summary>
    /// Generates repository implementation.
    /// </summary>
    /// <param name="context">The repository generation context.</param>
    void GenerateRepositoryImplementation(RepositoryGenerationContext context);

    /// <summary>
    /// Generates method documentation.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="method">The method.</param>
    void GenerateMethodDocumentation(IndentedStringBuilder sb, IMethodSymbol method);

    /// <summary>
    /// Generates variable declarations for repository methods.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="method">The method.</param>
    void GenerateMethodVariables(IndentedStringBuilder sb, IMethodSymbol method);
}
