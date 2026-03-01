// <copyright file="IDatabaseFixtureFactory.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Factory for creating database fixtures.
/// </summary>
public interface IDatabaseFixtureFactory
{
    /// <summary>
    /// Creates a new database fixture for the specified database type.
    /// </summary>
    /// <param name="dbType">The database type.</param>
    /// <returns>A new database fixture.</returns>
    Task<IDatabaseFixture> CreateFixtureAsync(DatabaseType dbType);
}
