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
    /// </summary>
    public static List<TEntity> ToList<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader)
    {
        var list = new List<TEntity>();
        while (dataReader.Read())
        {
            list.Add(reader.Read(dataReader));
        }
        return list;
    }

    /// <summary>
    /// Reads all entities into a list with capacity hint.
    /// </summary>
    public static List<TEntity> ToList<TEntity>(this IResultReader<TEntity> reader, IDataReader dataReader, int capacityHint)
    {
        var list = new List<TEntity>(capacityHint);
        while (dataReader.Read())
        {
            list.Add(reader.Read(dataReader));
        }
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
        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(reader.Read(dataReader));
        }
        return list;
    }

    /// <summary>
    /// Reads all entities into a list (async) with capacity hint.
    /// </summary>
    public static async Task<List<TEntity>> ToListAsync<TEntity>(
        this IResultReader<TEntity> reader,
        DbDataReader dataReader,
        int capacityHint,
        CancellationToken cancellationToken = default)
    {
        var list = new List<TEntity>(capacityHint);
        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(reader.Read(dataReader));
        }
        return list;
    }
}
