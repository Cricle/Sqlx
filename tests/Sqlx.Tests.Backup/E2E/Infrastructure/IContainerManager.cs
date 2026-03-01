// <copyright file="IContainerManager.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Manages Docker container lifecycle for E2E testing.
/// </summary>
public interface IContainerManager
{
    /// <summary>
    /// Checks if Docker is available on the system.
    /// </summary>
    /// <returns>True if Docker is available, false otherwise.</returns>
    Task<bool> IsDockerAvailableAsync();

    /// <summary>
    /// Initializes all database containers in parallel.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeContainersAsync();

    /// <summary>
    /// Gets the connection string for the specified database type.
    /// </summary>
    /// <param name="dbType">The database type.</param>
    /// <returns>The connection string.</returns>
    Task<string> GetConnectionStringAsync(DatabaseType dbType);

    /// <summary>
    /// Disposes all containers and cleans up resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisposeContainersAsync();

    /// <summary>
    /// Gets a value indicating whether containers have been initialized.
    /// </summary>
    bool IsInitialized { get; }
}
