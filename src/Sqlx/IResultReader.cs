// <copyright file="IResultReader.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

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
    /// Reads a single entity from the current row using IDataReader.
    /// Caller must call Read() before calling this method.
    /// </summary>
    /// <param name="reader">The data reader positioned at a row.</param>
    /// <returns>The entity.</returns>
    TEntity Read(IDataReader reader);

    /// <summary>
    /// Reads a single entity from the current row using pre-computed ordinals.
    /// Caller must call Read() before calling this method.
    /// </summary>
    /// <param name="reader">The data reader positioned at a row.</param>
    /// <param name="ordinals">Pre-computed column ordinals.</param>
    /// <returns>The entity.</returns>
    TEntity Read(IDataReader reader, int[] ordinals);

    /// <summary>
    /// Gets the column ordinals for this entity type.
    /// Call once per result set, then reuse for all rows.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <returns>Array of column ordinals.</returns>
    int[] GetOrdinals(IDataReader reader);
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
        var ordinals = reader.GetOrdinals(dataReader);
        return reader.Read(dataReader, ordinals);
    }

    /// <summary>
    /// Reads the first entity or default if no rows, using pre-computed ordinals.
    /// </summary>
    public static TEntity? FirstOrDefault<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader, int[] ordinals)
    {
        if (!dataReader.Read()) return default;
        return reader.Read(dataReader, ordinals);
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
        var ordinals = reader.GetOrdinals(dataReader);
        return reader.Read(dataReader, ordinals);
    }

    /// <summary>
    /// Reads the first entity or default if no rows (async), using pre-computed ordinals.
    /// </summary>
    public static async Task<TEntity?> FirstOrDefaultAsync<TEntity>(
        this IResultReader<TEntity> reader,
        DbDataReader dataReader,
        int[] ordinals,
        CancellationToken cancellationToken = default)
    {
        if (!await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false)) return default;
        return reader.Read(dataReader, ordinals);
    }

    /// <summary>
    /// Reads all entities into a list.
    /// </summary>
    public static List<TEntity> ToList<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader)
    {
        var list = new List<TEntity>();
        if (!dataReader.Read()) return list;
        
        var ordinals = reader.GetOrdinals(dataReader);
        do
        {
            list.Add(reader.Read(dataReader, ordinals));
        } while (dataReader.Read());
        
        return list;
    }

    /// <summary>
    /// Reads all entities into a list, using pre-computed ordinals.
    /// </summary>
    public static List<TEntity> ToList<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader, int[] ordinals)
    {
        var list = new List<TEntity>();
        if (!dataReader.Read()) return list;
        
        do
        {
            list.Add(reader.Read(dataReader, ordinals));
        } while (dataReader.Read());
        
        return list;
    }

    /// <summary>
    /// Reads all entities into a list (async).
    /// </summary>
    public static async Task<List<TEntity>> ToListAsync<TEntity>(
        this IResultReader<TEntity> reader,
        DbDataReader dataReader,
        CancellationToken cancellationToken = default)
    {
        var list = new List<TEntity>();
        if (!await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false)) return list;
        
        var ordinals = reader.GetOrdinals(dataReader);
        do
        {
            list.Add(reader.Read(dataReader, ordinals));
        } while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false));
        
        return list;
    }

    /// <summary>
    /// Reads all entities into a list (async), using pre-computed ordinals.
    /// </summary>
    public static async Task<List<TEntity>> ToListAsync<TEntity>(
        this IResultReader<TEntity> reader,
        DbDataReader dataReader,
        int[] ordinals,
        CancellationToken cancellationToken = default)
    {
        var list = new List<TEntity>();
        if (!await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false)) return list;
        
        do
        {
            list.Add(reader.Read(dataReader, ordinals));
        } while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false));
        
        return list;
    }
}
