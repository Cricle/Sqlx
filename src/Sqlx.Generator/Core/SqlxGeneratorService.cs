// -----------------------------------------------------------------------
// <copyright file="SqlxGeneratorService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Unified service implementation for Sqlx code generation.
/// Combines type inference, method analysis, and code generation capabilities.
/// </summary>
public class SqlxGeneratorService : ISqlxGeneratorService
{
    private readonly ITypeInferenceService _typeInferenceService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly ISqlTemplateEngine _templateEngine;
    private readonly AttributeHandler _attributeHandler;

    /// <summary>
    /// Gets the type inference service.
    /// </summary>
    public ITypeInferenceService TypeInferenceService => _typeInferenceService;

    /// <summary>
    /// Gets the code generation service.
    /// </summary>
    public ICodeGenerationService CodeGenerationService => _codeGenerationService;

    /// <summary>
    /// Gets the template engine.
    /// </summary>
    public ISqlTemplateEngine TemplateEngine => _templateEngine;

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    public AttributeHandler AttributeHandler => _attributeHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlxGeneratorService"/> class.
    /// </summary>
    public SqlxGeneratorService()
    {
        _typeInferenceService = new TypeInferenceService();
        _codeGenerationService = new CodeGenerationService();
        _templateEngine = new SqlTemplateEngine();
        _attributeHandler = new AttributeHandler();
    }

    /// <inheritdoc/>
    public INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method)
        => _typeInferenceService.InferEntityTypeFromMethod(method);
}
