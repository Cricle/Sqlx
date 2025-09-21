// -----------------------------------------------------------------------
// <copyright file="SharedUtilitiesTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Sqlx.Generator.Core;
using Xunit;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for SharedCodeGenerationUtilities.
/// </summary>
public class SharedUtilitiesTests
{
    [Theory]
    [InlineData("Task<string>", "string")]
    [InlineData("System.Threading.Tasks.Task<int>", "int")]
    [InlineData("Task<User>", "User")]
    [InlineData("System.Threading.Tasks.Task<List<User>>", "List<User>")]
    [InlineData("Task", "object")]
    [InlineData("string", "object")]
    [InlineData("", "object")]
    public void ExtractInnerTypeFromTask_ShouldExtractCorrectType(string taskType, string expectedType)
    {
        // Act
        var result = SharedCodeGenerationUtilities.ExtractInnerTypeFromTask(taskType);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("SELECT * FROM Users", "SELECT * FROM Users")]
    [InlineData("SELECT \"test\"", "SELECT \\\"test\\\"")]
    [InlineData("UPDATE Users\r\nSET Name = 'John'", "UPDATE Users\\r\\nSET Name = 'John'")]
    [InlineData("", "")]
    public void EscapeSqlForCSharp_ShouldEscapeCorrectly(string input, string expected)
    {
        // Act
        var result = SharedCodeGenerationUtilities.EscapeSqlForCSharp(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void EscapeSqlForCSharp_WithNull_ShouldReturnEmpty()
    {
        // Act
        var result = SharedCodeGenerationUtilities.EscapeSqlForCSharp(null);

        // Assert
        result.Should().Be("");
    }

    [Theory]
    [InlineData("User", "user")]
    [InlineData("UserProfile", "user_profile")]
    [InlineData("XMLHttpRequest", "xmlhttp_request")]
    [InlineData("ID", "id")]
    [InlineData("userId", "user_id")]
    [InlineData("user_name", "user_name")]
    [InlineData("USER_ID", "user_id")]
    [InlineData("", "")]
    public void ConvertToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = SharedCodeGenerationUtilities.ConvertToSnakeCase(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertToSnakeCase_WithNull_ShouldReturnNull()
    {
        // Act
        var result = SharedCodeGenerationUtilities.ConvertToSnakeCase(null!);

        // Assert
        result.Should().BeNull();
    }
}
