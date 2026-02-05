// <copyright file="IResultReader.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Reads a single entity from a data reader row.
/// The interface only handles the core mapping logic.
/// Use extension methods for sync/async and collection operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IResultReader<TEntity>
{
    /// <summary>
    /// Gets the number of properties/columns this reader handles.
    /// Used for optimizing ordinal caching.
    /// </summary>
    int PropertyCount { get; }

    /// <summary>
    /// Reads a single entity from the current row using IDataReader.
    /// Caller must call Read() before calling this method.
    /// </summary>
    /// <param name="reader">The data reader positioned at a row.</param>
    /// <returns>The entity.</returns>
    TEntity Read(IDataReader reader);

    /// <summary>
    /// Reads a single entity from the current row using pre-computed ordinals.
    /// This is an optimized version for reading multiple rows.
    /// Caller must call Read() before calling this method.
    /// </summary>
    /// <param name="reader">The data reader positioned at a row.</param>
    /// <param name="ordinals">Pre-computed column ordinals.</param>
    /// <returns>The entity.</returns>
    TEntity Read(IDataReader reader, System.ReadOnlySpan<int> ordinals);

    /// <summary>
    /// Gets the column ordinals for the entity properties.
    /// This allows caching ordinals for better performance when reading multiple rows.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="ordinals">Span to store the ordinals.</param>
    void GetOrdinals(IDataReader reader, System.Span<int> ordinals);
}

/// <summary>
/// Extension methods for IResultReader providing sync/async and collection operations.
/// </summary>
public static class ResultReaderExtensions
{
    /// <summary>
    /// Reads the first entity or default if no rows.
    /// </summary>
    public static TEntity? FirstOrDefault<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader)
    {
        if (!dataReader.Read()) return default;
        return reader.Read(dataReader);
    }

    /// <summary>
    /// Reads the first entity or default if no rows (async).
    /// </summary>
    public static async Task<TEntity?> FirstOrDefaultAsync<TEntity>(
        this IResultReader<TEntity> reader,
        DbDataReader dataReader,
        CancellationToken cancellationToken = default)
    {
        if (!await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false)) return default;
        return reader.Read(dataReader);
    }

    /// <summary>
    /// Reads all entities into a list.
    /// Uses stackalloc for ordinal caching to avoid heap allocations.
    /// </summary>
    public static List<TEntity> ToList<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader)
    {
        var list = new List<TEntity>();
        
        var propCount = reader.PropertyCount;
        if (propCount > 0)
        {
            // Use stackalloc for better performance
            Span<int> ordinals = stackalloc int[propCount];
            reader.GetOrdinals(dataReader, ordinals);
            
            while (dataReader.Read())
            {
                list.Add(reader.Read(dataReader, ordinals));
            }
        }
        else
        {
            while (dataReader.Read())
            {
                list.Add(reader.Read(dataReader));
            }
        }
        
        return list;
    }

    /// <summary>
    /// Reads all entities into a list with capacity hint.
    /// Uses stackalloc for ordinal caching to avoid heap allocations.
    /// </summary>
    public static List<TEntity> ToList<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader, int capacityHint)
    {
        var list = new List<TEntity>(capacityHint);
        
        var propCount = reader.PropertyCount;
        if (propCount > 0)
        {
            // Use stackalloc for better performance
            Span<int> ordinals = stackalloc int[propCount];
            reader.GetOrdinals(dataReader, ordinals);
            
            while (dataReader.Read())
            {
                list.Add(reader.Read(dataReader, ordinals));
            }
        }
        else
        {
            while (dataReader.Read())
            {
                list.Add(reader.Read(dataReader));
            }
        }
        
        return list;
    }

    /// <summary>
    /// Reads all entities into a list (async).
    /// Optimized version that caches column ordinals for better performance.
    /// Uses GC.AllocateUninitializedArray on supported platforms for better performance.
    /// </summary>
    public static async Task<List<TEntity>> ToListAsync<TEntity>(
        this IResultReader<TEntity> reader,
        DbDataReader dataReader,
        CancellationToken cancellationToken = default)
    {
        var list = new List<TEntity>();
        
        var propCount = reader.PropertyCount;
        if (propCount > 0)
        {
            // Pre-compute ordinals once
#if NETSTANDARD2_1
            var ordinals = new int[propCount];
#else
            var ordinals = GC.AllocateUninitializedArray<int>(propCount);
#endif
            reader.GetOrdinals(dataReader, ordinals);
            
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(reader.Read(dataReader, ordinals));
            }
        }
        else
        {
            // Fallback for types without properties
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(reader.Read(dataReader));
            }
        }
        
        return list;
    }

    /// <summary>
    /// Reads all entities into a list (async) with capacity hint.
    /// Optimized version that caches column ordinals for better performance.
    /// Uses GC.AllocateUninitializedArray on supported platforms for better performance.
    /// </summary>
    public static async Task<List<TEntity>> ToListAsync<TEntity>(
        this IResultReader<TEntity> reader,
        DbDataReader dataReader,
        int capacityHint,
        CancellationToken cancellationToken = default)
    {
        var list = new List<TEntity>(capacityHint);
        
        var propCount = reader.PropertyCount;
        if (propCount > 0)
        {
            // Pre-compute ordinals once
#if NETSTANDARD2_1
            var ordinals = new int[propCount];
#else
            var ordinals = GC.AllocateUninitializedArray<int>(propCount);
#endif
            reader.GetOrdinals(dataReader, ordinals);
            
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(reader.Read(dataReader, ordinals));
            }
        }
        else
        {
            // Fallback for types without properties
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(reader.Read(dataReader));
            }
        }
        
        return list;
    }
}
