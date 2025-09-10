// -----------------------------------------------------------------------
// <copyright file="AttributeTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Reflection;
using FluentAssertions;
using Sqlx.Annotations;
using Xunit;

/// <summary>
/// Unit tests for custom attributes.
/// </summary>
public class AttributeTests
{
    [Fact]
    public void RepositoryForAttribute_Constructor_ShouldSetServiceType()
    {
        // Arrange
        var serviceType = typeof(IUserService);

        // Act
        var attribute = new RepositoryForAttribute(serviceType);

        // Assert
        attribute.ServiceType.Should().Be(serviceType);
    }

    [Fact]
    public void RepositoryForAttribute_AttributeUsage_ShouldBeCorrect()
    {
        // Arrange
        var attributeType = typeof(RepositoryForAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usageAttribute.Should().NotBeNull();
        usageAttribute!.ValidOn.Should().Be(AttributeTargets.Class);
        usageAttribute.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void TableNameAttribute_Constructor_ShouldSetTableName()
    {
        // Arrange
        var tableName = "test_table";

        // Act
        var attribute = new TableNameAttribute(tableName);

        // Assert
        attribute.TableName.Should().Be(tableName);
    }

    [Fact]
    public void TableNameAttribute_AttributeUsage_ShouldBeCorrect()
    {
        // Arrange
        var attributeType = typeof(TableNameAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usageAttribute.Should().NotBeNull();
        var expectedTargets = AttributeTargets.Parameter | AttributeTargets.Method | 
                             AttributeTargets.Class | AttributeTargets.Interface;
        usageAttribute!.ValidOn.Should().Be(expectedTargets);
        usageAttribute.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void RepositoryForAttribute_OnClass_ShouldBeDetectable()
    {
        // Arrange
        var repositoryType = typeof(TestRepositoryWithAttribute);

        // Act
        var attribute = repositoryType.GetCustomAttribute<RepositoryForAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.ServiceType.Should().Be(typeof(ITestService));
    }

    [Fact]
    public void TableNameAttribute_OnClass_ShouldBeDetectable()
    {
        // Arrange
        var entityType = typeof(TestEntityWithTableName);

        // Act
        var attribute = entityType.GetCustomAttribute<TableNameAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.TableName.Should().Be("test_entities");
    }

    [Fact]
    public void TableNameAttribute_OnInterface_ShouldBeDetectable()
    {
        // Arrange
        var interfaceType = typeof(ITestServiceWithTableName);

        // Act
        var attribute = interfaceType.GetCustomAttribute<TableNameAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.TableName.Should().Be("service_table");
    }

    [Fact]
    public void Attributes_ShouldBeInCorrectNamespace()
    {
        // Act & Assert
        typeof(RepositoryForAttribute).Namespace.Should().Be("Sqlx.Annotations");
        typeof(TableNameAttribute).Namespace.Should().Be("Sqlx.Annotations");
    }

    [Fact]
    public void Attributes_ShouldBeSealed()
    {
        // Act & Assert
        typeof(RepositoryForAttribute).IsSealed.Should().BeTrue();
        typeof(TableNameAttribute).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Attributes_ShouldInheritFromAttribute()
    {
        // Act & Assert
        typeof(RepositoryForAttribute).Should().BeAssignableTo<Attribute>();
        typeof(TableNameAttribute).Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void RepositoryForAttribute_WithNullServiceType_ShouldThrowArgumentNullException()
    {
        // The constructor validates null parameters and throws ArgumentNullException
        // Arrange & Act & Assert
        var act = () => new RepositoryForAttribute(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceType");
    }

    [Fact]
    public void TableNameAttribute_WithNullTableName_ShouldThrowArgumentNullException()
    {
        // The constructor validates null parameters and throws ArgumentNullException
        // Arrange & Act & Assert
        var act = () => new TableNameAttribute(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tableName");
    }

    [Fact]
    public void TableNameAttribute_WithEmptyTableName_ShouldWork()
    {
        // Arrange & Act
        var attribute = new TableNameAttribute("");

        // Assert
        attribute.TableName.Should().Be("");
    }

    [Fact]
    public void RepositoryForAttribute_WithGenericServiceType_ShouldWork()
    {
        // Arrange
        var genericServiceType = typeof(IGenericService<>);

        // Act
        var attribute = new RepositoryForAttribute(genericServiceType);

        // Assert
        attribute.ServiceType.Should().Be(genericServiceType);
    }

    [Fact]
    public void RepositoryForAttribute_WithClosedGenericServiceType_ShouldWork()
    {
        // Arrange
        var closedGenericServiceType = typeof(IGenericService<User>);

        // Act
        var attribute = new RepositoryForAttribute(closedGenericServiceType);

        // Assert
        attribute.ServiceType.Should().Be(closedGenericServiceType);
    }
}

// Test helper types for attribute testing
public interface ITestService
{
    void DoSomething();
}

[TableName("service_table")]
public interface ITestServiceWithTableName
{
    void DoSomething();
}

[RepositoryFor(typeof(ITestService))]
public class TestRepositoryWithAttribute
{
}

[TableName("test_entities")]
public class TestEntityWithTableName
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IGenericService<T>
{
    void Process(T item);
}

[RepositoryFor(typeof(IGenericService<User>))]
public class GenericTestRepository
{
}
