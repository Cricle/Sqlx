// -----------------------------------------------------------------------
// <copyright file="OperationGeneratorFactory.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Sqlx.Generator.Core.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Generator.Core;

/// <summary>
/// Factory for creating operation generators.
/// </summary>
public class OperationGeneratorFactory
{
    private readonly List<IOperationGenerator> _generators;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationGeneratorFactory"/> class.
    /// </summary>
    public OperationGeneratorFactory()
    {
        _generators = new List<IOperationGenerator>
        {
            new InsertOperationGenerator(),
            new UpdateOperationGenerator(),
            new DeleteOperationGenerator(),
            new SelectOperationGenerator()
        };
    }

    /// <summary>
    /// Gets the appropriate generator for the specified method.
    /// </summary>
    /// <param name="method">The method to generate code for.</param>
    /// <returns>The operation generator or null if none found.</returns>
    public IOperationGenerator? GetGenerator(IMethodSymbol method)
    {
        return _generators.FirstOrDefault(g => g.CanHandle(method));
    }

    /// <summary>
    /// Gets all available generators.
    /// </summary>
    /// <returns>The list of generators.</returns>
    public IEnumerable<IOperationGenerator> GetAllGenerators()
    {
        return _generators.AsReadOnly();
    }

    /// <summary>
    /// Registers a custom operation generator.
    /// </summary>
    /// <param name="generator">The generator to register.</param>
    public void RegisterGenerator(IOperationGenerator generator)
    {
        if (generator != null && !_generators.Any(g => g.GetType() == generator.GetType()))
        {
            _generators.Insert(0, generator); // Insert at beginning for priority
        }
    }
}
