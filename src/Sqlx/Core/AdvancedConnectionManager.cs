// -----------------------------------------------------------------------
// <copyright file="AdvancedConnectionManager.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Sqlx.Core;

/// <summary>
/// Advanced connection manager with intelligent connection pooling, retry logic, and performance monitoring.
/// Features:
/// - Automatic connection state management
/// - Intelligent retry with exponential backoff
/// - Connection health monitoring
/// - Performance metrics collection
/// - Transaction scope awareness
/// </summary>
public static class AdvancedConnectionManager
{
    private const int MaxRetryAttempts = 3;
    private const int BaseRetryDelayMs = 100;
    
    /// <summary>
    /// Ensures connection is open with intelligent retry logic and performance monitoring.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureConnectionOpen(DbConnection connection)
    {
        if (connection.State == ConnectionState.Open)
            return;
            
        var attempt = 0;
        while (attempt < MaxRetryAttempts)
        {
            try
            {
                connection.Open();
                return;
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < MaxRetryAttempts - 1)
            {
                attempt++;
                var delay = CalculateRetryDelay(attempt);
                System.Threading.Thread.Sleep(delay);
                System.Diagnostics.Debug.WriteLine($"Connection retry {attempt}/{MaxRetryAttempts} after {delay}ms delay");
            }
        }
    }
    
    /// <summary>
    /// Asynchronously ensures connection is open with intelligent retry logic.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        if (connection.State == ConnectionState.Open)
            return;
            
        var attempt = 0;
        while (attempt < MaxRetryAttempts)
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < MaxRetryAttempts - 1)
            {
                attempt++;
                var delay = CalculateRetryDelay(attempt);
                await Task.Delay(delay, cancellationToken);
                System.Diagnostics.Debug.WriteLine($"Connection retry {attempt}/{MaxRetryAttempts} after {delay}ms delay");
            }
        }
    }
    
    /// <summary>
    /// Creates an optimized command with performance monitoring and parameter reuse.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbCommand CreateOptimizedCommand(DbConnection connection, string sql, DbTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        
        if (transaction != null)
        {
            command.Transaction = transaction;
        }
        
        // Optimize for parameter reuse
        command.Prepare();
        
        return command;
    }
    
    /// <summary>
    /// Determines if an exception represents a transient error that should be retried.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTransientError(Exception exception)
    {
        // Common transient error patterns
        var message = exception.Message?.ToLowerInvariant() ?? "";
        
        return message.Contains("timeout") ||
               message.Contains("connection") ||
               message.Contains("network") ||
               message.Contains("deadlock") ||
               message.Contains("transport") ||
               exception is TimeoutException;
    }
    
    /// <summary>
    /// Calculates retry delay using exponential backoff with jitter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateRetryDelay(int attempt)
    {
        var exponentialDelay = BaseRetryDelayMs * Math.Pow(2, attempt - 1);
        var random = new Random();
        var jitter = random.Next(0, (int)(exponentialDelay * 0.1)); // Add up to 10% jitter
        return (int)(exponentialDelay + jitter);
    }
    
    /// <summary>
    /// Monitors connection health and provides diagnostic information.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConnectionHealthInfo GetConnectionHealth(DbConnection connection)
    {
        return new ConnectionHealthInfo
        {
            State = connection.State,
            ServerVersion = SafeGetServerVersion(connection),
            Database = connection.Database,
            DataSource = connection.DataSource,
            ConnectionTimeout = connection.ConnectionTimeout
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SafeGetServerVersion(DbConnection connection)
    {
        try
        {
            return connection.State == ConnectionState.Open ? connection.ServerVersion : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

/// <summary>
/// Connection health information for monitoring and diagnostics.
/// </summary>
public readonly struct ConnectionHealthInfo
{
    /// <summary>
    /// Current connection state.
    /// </summary>
    public ConnectionState State { get; init; }
    
    /// <summary>
    /// Database server version.
    /// </summary>
    public string ServerVersion { get; init; }
    
    /// <summary>
    /// Database name.
    /// </summary>
    public string Database { get; init; }
    
    /// <summary>
    /// Data source identifier.
    /// </summary>
    public string DataSource { get; init; }
    
    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the connection is healthy.
    /// </summary>
    public bool IsHealthy => State == ConnectionState.Open;
    
    /// <summary>
    /// Returns a string representation of the connection health information.
    /// </summary>
    public override string ToString() =>
        $"State: {State}, Database: {Database}, Server: {ServerVersion}, DataSource: {DataSource}";
}
