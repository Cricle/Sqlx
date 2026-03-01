// <copyright file="TestDataBuilder.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Helpers;

/// <summary>
/// Builder for creating complex test data with fluent API.
/// Provides a convenient way to construct test data sets.
/// </summary>
public class TestDataBuilder
{
    private readonly List<TestEntity> _entities = new();
    private int _nextId = 1;

    /// <summary>
    /// Adds a TestEntity with optional configuration.
    /// </summary>
    /// <param name="configure">Optional action to configure the entity.</param>
    /// <returns>This builder for chaining.</returns>
    public TestDataBuilder WithEntity(Action<TestEntity>? configure = null)
    {
        var entity = TestEntityFactory.CreateTestEntity(id: _nextId++);
        configure?.Invoke(entity);
        _entities.Add(entity);
        return this;
    }

    /// <summary>
    /// Adds multiple TestEntity instances with default values.
    /// </summary>
    /// <param name="count">Number of entities to add.</param>
    /// <returns>This builder for chaining.</returns>
    public TestDataBuilder WithEntities(int count)
    {
        for (int i = 0; i < count; i++)
        {
            WithEntity();
        }

        return this;
    }

    /// <summary>
    /// Adds a TestEntity with specific property values.
    /// </summary>
    /// <param name="userName">User name for the entity.</param>
    /// <param name="isActive">Active status for the entity.</param>
    /// <returns>This builder for chaining.</returns>
    public TestDataBuilder WithEntity(string userName, bool isActive = true)
    {
        return WithEntity(e =>
        {
            e.UserName = userName;
            e.IsActive = isActive;
        });
    }

    /// <summary>
    /// Builds and returns the array of TestEntity instances.
    /// </summary>
    /// <returns>Array of configured TestEntity instances.</returns>
    public TestEntity[] Build()
    {
        return _entities.ToArray();
    }

    /// <summary>
    /// Resets the builder to start fresh.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public TestDataBuilder Reset()
    {
        _entities.Clear();
        _nextId = 1;
        return this;
    }
}
