// -----------------------------------------------------------------------
// <copyright file="ICodeGenerationService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
}
