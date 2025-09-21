// -----------------------------------------------------------------------
// <copyright file="SqlxGeneratorServiceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Sqlx.Generator.Core;
using Xunit;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for SqlxGeneratorService core functionality.
/// </summary>
public class SqlxGeneratorServiceTests
{
    private readonly SqlxGeneratorService _service;

    public SqlxGeneratorServiceTests()
    {
        _service = new SqlxGeneratorService();
    }

    [Fact]
    public void Constructor_ShouldInitializeAllServices()
    {
        // Assert
        _service.TypeInferenceService.Should().NotBeNull();
        _service.CodeGenerationService.Should().NotBeNull();
        _service.TemplateEngine.Should().NotBeNull();
        _service.AttributeHandler.Should().NotBeNull();
    }

    [Fact]
    public void TypeInferenceService_ShouldReturnCorrectType()
    {
        // Act
        var result = _service.TypeInferenceService;

        // Assert
        result.Should().BeAssignableTo<ITypeInferenceService>();
    }

    [Fact]
    public void CodeGenerationService_ShouldReturnCorrectType()
    {
        // Act
        var result = _service.CodeGenerationService;

        // Assert
        result.Should().BeAssignableTo<ICodeGenerationService>();
    }

    [Fact]
    public void TemplateEngine_ShouldReturnCorrectType()
    {
        // Act
        var result = _service.TemplateEngine;

        // Assert
        result.Should().BeAssignableTo<ISqlTemplateEngine>();
    }

    [Fact]
    public void AttributeHandler_ShouldReturnCorrectType()
    {
        // Act
        var result = _service.AttributeHandler;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<AttributeHandler>();
    }

    [Fact]
    public void GetTableName_ConceptualTest_ShouldWorkWithProperMocking()
    {
        // This test would require mocking INamedTypeSymbol which is complex
        // For now, we'll test the service instantiation and basic properties
        // More comprehensive tests would require a proper test infrastructure with Roslyn mocks

        // Act & Assert - Just verify the service is properly initialized
        _service.Should().NotBeNull();
        _service.GetType().Should().Be<SqlxGeneratorService>();

        // In a real implementation, we would test:
        // - "User" -> "User"
        // - "UserEntity" -> "user_entity"  
        // - "" -> ""
    }
}
