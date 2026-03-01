// <copyright file="TestEntityFactory.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Linq;

namespace Sqlx.Tests.Helpers;

/// <summary>
/// Factory for creating test entities with default or custom values.
/// Reduces code duplication by providing reusable entity creation methods.
/// </summary>
public static class TestEntityFactory
{
    /// <summary>
    /// Creates a TestEntity with default or custom values.
    /// </summary>
    /// <param name="id">Entity ID (default: 1).</param>
    /// <param name="userName">User name (default: "test_user").</param>
    /// <param name="isActive">Active status (default: true).</param>
    /// <param name="createdAt">Creation timestamp (default: DateTime.Now).</param>
    /// <returns>A new TestEntity instance.</returns>
    public static TestEntity CreateTestEntity(
        int? id = null,
        string? userName = null,
        bool? isActive = null,
        DateTime? createdAt = null)
    {
        return new TestEntity
        {
            Id = id ?? 1,
            UserName = userName ?? "test_user",
            IsActive = isActive ?? true,
            CreatedAt = createdAt ?? DateTime.Now
        };
    }

    /// <summary>
    /// Creates multiple TestEntity instances with sequential IDs.
    /// </summary>
    /// <param name="count">Number of entities to create.</param>
    /// <returns>Array of TestEntity instances.</returns>
    public static TestEntity[] CreateTestEntities(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestEntity(id: i, userName: $"user{i}"))
            .ToArray();
    }

    /// <summary>
    /// Creates a TestUser with default or custom values.
    /// Note: TestUser has different definitions across test files.
    /// This factory provides basic properties that are common.
    /// </summary>
    /// <param name="id">User ID (default: 1).</param>
    /// <param name="name">User name (default: "Test User").</param>
    /// <returns>A new TestUser instance.</returns>
    public static TestUser CreateTestUser(
        int? id = null,
        string? name = null)
    {
        return new TestUser
        {
            Id = id ?? 1,
            Name = name ?? "Test User"
        };
    }

    /// <summary>
    /// Creates a TestEntityWithNullable with default or custom values.
    /// </summary>
    /// <param name="id">Entity ID (default: 1).</param>
    /// <param name="name">Name (default: "test").</param>
    /// <param name="description">Description (default: null).</param>
    /// <returns>A new TestEntityWithNullable instance.</returns>
    public static TestEntityWithNullable CreateTestEntityWithNullable(
        int? id = null,
        string? name = null,
        string? description = null)
    {
        return new TestEntityWithNullable
        {
            Id = id ?? 1,
            Name = name ?? "test",
            Description = description
        };
    }
}
