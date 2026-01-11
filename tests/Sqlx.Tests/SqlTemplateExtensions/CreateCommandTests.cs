using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Sqlx.Tests.SqlTemplateExtensions;

public class CreateCommandTests : SqlTemplateExtensionsTestBase
{
    [Fact]
    public void CreateCommand_WithValidTemplate_ShouldCreateCommand()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT * FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 });

        // Act
        using var command = template.CreateCommand(Connection);

        // Assert
        Assert.NotNull(command);
        Assert.Equal("SELECT * FROM Users WHERE Id = @id", command.CommandText);
        Assert.Single(command.Parameters);
    }

    [Fact]
    public void CreateCommand_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT 1");

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => template.CreateCommand(null!));
        Assert.Equal("connection", ex.ParamName);
        Assert.Contains("cannot be null", ex.Message);
    }

    [Fact]
    public void CreateCommand_WithTransaction_ShouldSetTransaction()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT 1");
        using var transaction = Connection.BeginTransaction();

        // Act
        using var command = template.CreateCommand(Connection, transaction);

        // Assert
        Assert.NotNull(command.Transaction);
        Assert.Equal(transaction, command.Transaction);
    }

    [Fact]
    public void CreateCommand_WithTimeout_ShouldSetCommandTimeout()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT 1");
        const int timeout = 30;

        // Act
        using var command = template.CreateCommand(Connection, commandTimeout: timeout);

        // Assert
        Assert.Equal(timeout, command.CommandTimeout);
    }

    [Fact]
    public void CreateCommand_WithParameters_ShouldAddAllParameters()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT * FROM Users WHERE Age >= @minAge AND Name LIKE @name",
            new Dictionary<string, object?>
            {
                ["@minAge"] = 25,
                ["@name"] = "A%"
            });

        // Act
        using var command = template.CreateCommand(Connection);

        // Assert
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(25, ((IDataParameter)command.Parameters["@minAge"]!).Value);
        Assert.Equal("A%", ((IDataParameter)command.Parameters["@name"]!).Value);
    }

    [Fact]
    public void CreateCommand_WithNullParameterValue_ShouldConvertToDBNull()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "INSERT INTO Users (Name, Email) VALUES (@name, @email)",
            new Dictionary<string, object?>
            {
                ["@name"] = "Test",
                ["@email"] = null
            });

        // Act
        using var command = template.CreateCommand(Connection);

        // Assert
        var emailParam = (IDataParameter)command.Parameters["@email"]!;
        Assert.Equal(DBNull.Value, emailParam.Value);
    }

    [Fact]
    public void CreateCommand_WithParameterOverrides_ShouldUseOverrideValues()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT * FROM Users WHERE Id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 });

        var overrides = new Dictionary<string, object?> { ["@id"] = 999 };

        // Act
        using var command = template.CreateCommand(Connection, parameterOverrides: overrides);

        // Assert
        var idParam = (IDataParameter)command.Parameters["@id"]!;
        Assert.Equal(999, idParam.Value);
    }

    [Fact]
    public void CreateCommand_WithPartialOverrides_ShouldMixOriginalAndOverride()
    {
        // Arrange
        var template = CreateSimpleTemplate(
            "SELECT * FROM Users WHERE Age >= @minAge AND Name LIKE @name",
            new Dictionary<string, object?>
            {
                ["@minAge"] = 25,
                ["@name"] = "A%"
            });

        var overrides = new Dictionary<string, object?> { ["@minAge"] = 30 };

        // Act
        using var command = template.CreateCommand(Connection, parameterOverrides: overrides);

        // Assert
        Assert.Equal(30, ((IDataParameter)command.Parameters["@minAge"]!).Value);
        Assert.Equal("A%", ((IDataParameter)command.Parameters["@name"]!).Value);
    }

    [Fact]
    public void CreateCommand_WithEmptyParameters_ShouldCreateCommandWithoutParameters()
    {
        // Arrange
        var template = CreateSimpleTemplate("SELECT COUNT(*) FROM Users");

        // Act
        using var command = template.CreateCommand(Connection);

        // Assert
        Assert.NotNull(command);
        Assert.Empty(command.Parameters);
    }
}
