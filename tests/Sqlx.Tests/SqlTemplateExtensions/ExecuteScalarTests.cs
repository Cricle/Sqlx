using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Sqlx.Tests.SqlTemplateExtensions;

public class ExecuteScalarTests : SqlTemplateExtensionsTestBase
{
    [Fact]
    public async Task ExecuteScalarAsync_NonGeneric_ShouldReturnObject()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT COUNT(*) FROM Users");

        // Act
        var result = await template.ExecuteScalarAsync(Connection);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<long>(result); // SQLite returns long for COUNT
        Assert.Equal(5L, result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_Generic_Int_ShouldReturnInt()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT COUNT(*) FROM Users");

        // Act
        var result = await template.ExecuteScalarAsync<int>(Connection);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_Generic_Long_ShouldReturnLong()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT COUNT(*) FROM Users");

        // Act
        var result = await template.ExecuteScalarAsync<long>(Connection);

        // Assert
        Assert.Equal(5L, result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_Generic_String_ShouldReturnString()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT Name FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 });

        // Act
        var result = await template.ExecuteScalarAsync<string>(Connection);

        // Assert
        Assert.Equal("Alice", result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_Generic_NullableInt_WithValue_ShouldReturnValue()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT Age FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 });

        // Act
        var result = await template.ExecuteScalarAsync<int?>(Connection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.Value);
    }

    [Fact]
    public async Task ExecuteScalarAsync_Generic_NullableInt_WithNull_ShouldReturnNull()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT NULL");

        // Act
        var result = await template.ExecuteScalarAsync<int?>(Connection);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithParameterOverrides_ShouldUseOverrideValues()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT Name FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 });

        var overrides = new Dictionary<string, object?> { ["@id"] = 2 };

        // Act
        var result = await template.ExecuteScalarAsync<string>(Connection, parameterOverrides: overrides);

        // Assert
        Assert.Equal("Bob", result);
    }

    [Fact]
    public void ExecuteScalar_Sync_NonGeneric_ShouldReturnObject()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT COUNT(*) FROM Users");

        // Act
        var result = template.ExecuteScalar(Connection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5L, result);
    }

    [Fact]
    public void ExecuteScalar_Sync_Generic_Int_ShouldReturnInt()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT COUNT(*) FROM Users");

        // Act
        var result = template.ExecuteScalar<int>(Connection);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void ExecuteScalar_Sync_Generic_String_ShouldReturnString()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT Name FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 });

        // Act
        var result = template.ExecuteScalar<string>(Connection);

        // Assert
        Assert.Equal("Alice", result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithDBNull_ShouldReturnNull()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT Email FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 3 }); // Charlie has null email

        // Act
        var result = await template.ExecuteScalarAsync<string>(Connection);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_TypeConversion_ShouldConvertTypes()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT Age FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 });

        // Act - SQLite returns long, convert to decimal
        var result = await template.ExecuteScalarAsync<decimal>(Connection);

        // Assert
        Assert.Equal(25m, result);
    }
}
