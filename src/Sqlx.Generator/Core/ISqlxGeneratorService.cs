// -----------------------------------------------------------------------
// <copyright file="ISqlxGeneratorService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Unified service interface for Sqlx code generation.
/// </summary>
public interface ISqlxGeneratorService
{
    /// <summary>
    /// Gets the type inference service.
    /// </summary>
    ITypeInferenceService TypeInferenceService { get; }

    /// <summary>
    /// Gets the code generation service.
    /// </summary>
    ICodeGenerationService CodeGenerationService { get; }

    /// <summary>
    /// Gets the template engine.
    /// </summary>
    ISqlTemplateEngine TemplateEngine { get; }

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    AttributeHandler AttributeHandler { get; }

    /// <summary>
    /// Infers the entity type from a method.
    /// </summary>
    INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method);
}
