// -----------------------------------------------------------------------
// <copyright file="OperationGeneratorSimpleTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator.Core;
using System.Linq;

namespace Sqlx.Tests.Generator;

/// <summary>
/// Simple tests for OperationGeneratorFactory class.
/// </summary>
[TestClass]
public class OperationGeneratorSimpleTests : TestBase
{
    private OperationGeneratorFactory _factory = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _factory = new OperationGeneratorFactory();
    }

    [TestMethod]
    public void Constructor_CreatesFactory_WithDefaultGenerators()
    {
        // Arrange & Act
        var factory = new OperationGeneratorFactory();
        var generators = factory.GetAllGenerators();
        
        // Assert
        Assert.IsNotNull(factory);
        Assert.IsNotNull(generators);
        Assert.AreEqual(4, generators.Count());
    }

    [TestMethod]
    public void GetAllGenerators_ReturnsCollection()
    {
        // Act
        var generators = _factory.GetAllGenerators();
        
        // Assert
        Assert.IsNotNull(generators);
        Assert.AreEqual(4, generators.Count());
    }

    [TestMethod]
    public void RegisterGenerator_AddsToCollection()
    {
        // Arrange
        var customGenerator = new MockOperationGenerator();
        var initialCount = _factory.GetAllGenerators().Count();
        
        // Act
        _factory.RegisterGenerator(customGenerator);
        var newCount = _factory.GetAllGenerators().Count();
        
        // Assert
        Assert.AreEqual(initialCount + 1, newCount);
    }

    [TestMethod]
    public void RegisterGenerator_NullGenerator_DoesNotAdd()
    {
        // Arrange
        var initialCount = _factory.GetAllGenerators().Count();
        
        // Act
        _factory.RegisterGenerator(null!);
        var newCount = _factory.GetAllGenerators().Count();
        
        // Assert
        Assert.AreEqual(initialCount, newCount);
    }

    [TestMethod]
    public void RegisterGenerator_DuplicateType_DoesNotAddDuplicate()
    {
        // Arrange
        var customGenerator1 = new MockOperationGenerator();
        var customGenerator2 = new MockOperationGenerator();
        var initialCount = _factory.GetAllGenerators().Count();
        
        // Act
        _factory.RegisterGenerator(customGenerator1);
        _factory.RegisterGenerator(customGenerator2);
        var newCount = _factory.GetAllGenerators().Count();
        
        // Assert
        Assert.AreEqual(initialCount + 1, newCount); // Only one should be added
    }

    /// <summary>
    /// Mock implementation of IOperationGenerator for testing.
    /// </summary>
    private class MockOperationGenerator : IOperationGenerator
    {
        public string OperationName => "MockOperation";

        public bool CanHandle(Microsoft.CodeAnalysis.IMethodSymbol method)
        {
            return method?.Name?.StartsWith("Mock") == true;
        }

        public void GenerateOperation(OperationGenerationContext context)
        {
            // Mock implementation
        }
    }
}

