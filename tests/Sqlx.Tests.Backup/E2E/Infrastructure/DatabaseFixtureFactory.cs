// <copyright file="DatabaseFixtureFactory.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Factory for creating database fixtures with container manager integration.
/// </summary>
public class DatabaseFixtureFactory : IDatabaseFixtureFactory
{
    private readonly IContainerManager _containerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseFixtureFactory"/> class.
    /// </summary>
    /// <param name="containerManager">The container manager.</param>
    public DatabaseFixtureFactory(IContainerManager containerManager)
    {
        _containerManager = containerManager ?? throw new ArgumentNullException(nameof(containerManager));
    }

    /// <inheritdoc/>
    public async Task<IDatabaseFixture> CreateFixtureAsync(DatabaseType dbType)
    {
        // GetConnectionStringAsync handles lazy initialization for all database types
        var connectionString = await _containerManager.GetConnectionStringAsync(dbType);
        var fixture = new DatabaseFixture(dbType, connectionString);
        await fixture.InitializeAsync();
        return fixture;
    }
}
