// -----------------------------------------------------------------------
// <copyright file="BasicGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Basic tests for source generator functionality.
/// </summary>
public class BasicGeneratorTests
{
    [Fact]
    public void CSharpGenerator_CanBeInstantiated()
    {
        // Act
        var generator = new CSharpGenerator();

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeAssignableTo<ISourceGenerator>();
    }

    [Fact]
    public void AbstractGenerator_CanBeInstantiated()
    {
        // Act
        var generator = new CSharpGenerator();

        // Assert
        generator.Should().BeAssignableTo<AbstractGenerator>();
    }
}
