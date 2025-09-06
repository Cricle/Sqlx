// -----------------------------------------------------------------------
// <copyright file="TestAttributesTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using FluentAssertions;
using Xunit;

public class TestAttributesTests
{
    [Fact]
    public void CheckAttributes_ShouldRunWithoutErrors()
    {
        // Act & Assert - Should not throw
        var act = () => TestAttributes.CheckAttributes();
        act.Should().NotThrow();
    }

    [Fact]
    public void TestAttributes_MethodShouldExist()
    {
        // Arrange
        var type = typeof(TestAttributes);

        // Act
        var method = type.GetMethod("CheckAttributes");

        // Assert
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
        method.IsPublic.Should().BeTrue();
    }
}
