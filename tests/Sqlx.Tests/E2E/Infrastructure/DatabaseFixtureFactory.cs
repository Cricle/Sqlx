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
        var databaseName = GenerateDatabaseName(dbType);
        var fixture = new DatabaseFixture(dbType, connectionString, databaseName);
        await fixture.InitializeAsync();
        return fixture;
    }

    private static string GenerateDatabaseName(DatabaseType dbType)
    {
        var databaseCode = dbType switch
        {
            DatabaseType.MySQL => "mysql",
            DatabaseType.PostgreSQL => "pgsql",
            DatabaseType.SqlServer => "sqlsrv",
            DatabaseType.SQLite => "sqlite",
            _ => "db",
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"sqlx_{databaseCode}_{timestamp}_{random}";
    }
}
