// <copyright file="ContainerManager.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics;
using MySqlConnector;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Manages Docker container lifecycle for E2E testing with singleton pattern.
/// </summary>
public class ContainerManager : IContainerManager
{
    private static readonly Lazy<ContainerManager> _instance = new(() => new ContainerManager());
    private readonly ConcurrentDictionary<DatabaseType, string> _connectionStrings = new();
    private readonly ConcurrentDictionary<DatabaseType, SemaphoreSlim> _initializationLocks = new();
    private readonly ConcurrentDictionary<DatabaseType, Task<string>> _initializationTasks = new();
    private MySqlContainer? _mySqlContainer;
    private PostgreSqlContainer? _postgreSqlContainer;
    private MsSqlContainer? _msSqlContainer;
    private bool _isInitialized;
    private bool _dockerAvailable;
    private bool _isDisposing;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerManager"/> class.
    /// </summary>
    private ContainerManager()
    {
        // Initialize locks for each database type that requires containers
        _initializationLocks[DatabaseType.MySQL] = new SemaphoreSlim(1, 1);
        _initializationLocks[DatabaseType.PostgreSQL] = new SemaphoreSlim(1, 1);
        _initializationLocks[DatabaseType.SqlServer] = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Gets the singleton instance of the container manager.
    /// </summary>
    public static ContainerManager Instance => _instance.Value;

    /// <inheritdoc/>
    public bool IsInitialized => _connectionStrings.Any(kvp => kvp.Key != DatabaseType.SQLite);

    /// <inheritdoc/>
    public async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            await process.WaitForExitAsync();

            _dockerAvailable = process.ExitCode == 0;
            return _dockerAvailable;
        }
        catch
        {
            _dockerAvailable = false;
            return false;
        }
    }

    /// <inheritdoc/>
    [Obsolete("Container initialization is now lazy. Containers will be started automatically when GetConnectionStringAsync is called. This method is kept for backward compatibility.")]
    public async Task InitializeContainersAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        if (!_dockerAvailable && !await IsDockerAvailableAsync())
        {
            throw new InvalidOperationException(
                "Docker is not available. Please ensure Docker Desktop is running.");
        }

        // Trigger lazy initialization for all database types by requesting their connection strings
        // This maintains backward compatibility while using the new lazy initialization infrastructure
        var tasks = new[]
        {
            GetConnectionStringAsync(DatabaseType.MySQL),
            GetConnectionStringAsync(DatabaseType.PostgreSQL),
            GetConnectionStringAsync(DatabaseType.SqlServer),
        };

        await Task.WhenAll(tasks);
        _isInitialized = true;
    }

    /// <inheritdoc/>
    public async Task<string> GetConnectionStringAsync(DatabaseType dbType)
    {
        // Fast path: check if connection string is already cached
        if (_connectionStrings.TryGetValue(dbType, out var cachedConnectionString))
        {
            return cachedConnectionString;
        }

        // SQLite special case: return immediately without Docker checks
        if (dbType == DatabaseType.SQLite)
        {
            var sqliteConnectionString = "Data Source=:memory:";
            _connectionStrings.TryAdd(dbType, sqliteConnectionString);
            return sqliteConnectionString;
        }

        // Check if disposal is in progress
        if (_isDisposing)
        {
            throw new ObjectDisposedException(
                nameof(ContainerManager),
                "ContainerManager is being disposed and cannot initialize new containers.");
        }

        // Check if Docker is available
        if (!_dockerAvailable && !await IsDockerAvailableAsync())
        {
            throw new InvalidOperationException(
                "Docker is not available. Please ensure Docker Desktop is running.");
        }

        // Thread-safe lazy initialization using double-check locking pattern
        var lockObj = _initializationLocks[dbType];
        await lockObj.WaitAsync();
        try
        {
            // Double-check: another thread might have initialized while we were waiting
            if (_connectionStrings.TryGetValue(dbType, out cachedConnectionString))
            {
                return cachedConnectionString;
            }

            // Initialize the container and get connection string
            var connectionString = await InitializeContainerAsync(dbType);
            
            // Cache the connection string
            _connectionStrings.TryAdd(dbType, connectionString);
            
            return connectionString;
        }
        finally
        {
            lockObj.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DisposeContainersAsync()
    {
        // Set flag to prevent new initializations during disposal
        _isDisposing = true;

        try
        {
            var disposeTasks = new List<Task>();

            // Dispose only containers that were actually initialized
            if (_mySqlContainer != null)
            {
                disposeTasks.Add(_mySqlContainer.DisposeAsync().AsTask());
            }

            if (_postgreSqlContainer != null)
            {
                disposeTasks.Add(_postgreSqlContainer.DisposeAsync().AsTask());
            }

            if (_msSqlContainer != null)
            {
                disposeTasks.Add(_msSqlContainer.DisposeAsync().AsTask());
            }

            await Task.WhenAll(disposeTasks);

            _mySqlContainer = null;
            _postgreSqlContainer = null;
            _msSqlContainer = null;
            _isInitialized = false;
            
            // Clear caches
            _connectionStrings.Clear();
            _initializationTasks.Clear();
            
            // Dispose semaphore locks
            foreach (var lockObj in _initializationLocks.Values)
            {
                lockObj.Dispose();
            }
            _initializationLocks.Clear();
        }
        finally
        {
            _isDisposing = false;
        }
    }

    /// <summary>
    /// Initializes a specific database container and returns its connection string.
    /// </summary>
    /// <param name="dbType">The database type to initialize.</param>
    /// <returns>The connection string for the initialized container.</returns>
    private async Task<string> InitializeContainerAsync(DatabaseType dbType)
    {
        try
        {
            return dbType switch
            {
                DatabaseType.MySQL => await InitializeAndGetConnectionStringAsync(
                    () => InitializeMySqlAsync(),
                    () => _mySqlContainer?.GetConnectionString() 
                        ?? throw new InvalidOperationException("MySQL container failed to initialize")),
                
                DatabaseType.PostgreSQL => await InitializeAndGetConnectionStringAsync(
                    () => InitializePostgreSqlAsync(),
                    () => _postgreSqlContainer?.GetConnectionString() 
                        ?? throw new InvalidOperationException("PostgreSQL container failed to initialize")),
                
                DatabaseType.SqlServer => await InitializeAndGetConnectionStringAsync(
                    () => InitializeSqlServerAsync(),
                    () => _msSqlContainer?.GetConnectionString() 
                        ?? throw new InvalidOperationException("SQL Server container failed to initialize")),
                
                _ => throw new NotSupportedException($"Database type {dbType} is not supported for container initialization"),
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Failed to initialize {dbType} container: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Helper method to initialize a container and get its connection string.
    /// </summary>
    private async Task<string> InitializeAndGetConnectionStringAsync(
        Func<Task> initializeFunc,
        Func<string> getConnectionStringFunc)
    {
        await initializeFunc();
        return getConnectionStringFunc();
    }

    private async Task InitializeMySqlAsync()
    {
        // Use Alpine-based MySQL image for faster startup and smaller size
        _mySqlContainer = new MySqlBuilder()
            .WithImage("mysql:8.0-oracle")  // MySQL doesn't have official Alpine, use Oracle Linux (smaller than Debian)
            .WithDatabase("testdb")
            .WithUsername("root")
            .WithPassword("testpass")
            .WithCommand("--innodb-use-native-aio=0")
            .Build();

        await _mySqlContainer.StartAsync();
    }

    private async Task InitializePostgreSqlAsync()
    {
        // Use Alpine-based PostgreSQL image for faster startup and smaller size
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        await _postgreSqlContainer.StartAsync();
    }

    private async Task InitializeSqlServerAsync()
    {
        // Use Azure SQL Edge because it is lighter and already cached in the local Docker environment.
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
            .WithPassword("Test@1234")
            .WithEnvironment("ACCEPT_EULA", "1")
            .WithEnvironment("MSSQL_PID", "Developer")
            .Build();

        await _msSqlContainer.StartAsync();
    }
}
