// -----------------------------------------------------------------------
// <copyright file="BatchOperationAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx.Annotations;

/// <summary>
/// Specifies that a method performs batch operations and should automatically
/// split large datasets into multiple batches to avoid database parameter limits.
/// </summary>
/// <remarks>
/// Database parameter limits:
/// - SQL Server: 2100 parameters (default)
/// - PostgreSQL: 32767 bind parameters
/// - SQLite: 999 parameters (default)
/// - MySQL: 65535 placeholders
/// - Oracle: 1000 bind variables
/// 
/// This attribute ensures that batch operations respect these limits by automatically
/// chunking large datasets into appropriately sized batches.
/// </remarks>
/// <example>
/// <code>
/// [SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES {{values @entities}}")]
/// [BatchOperation(MaxBatchSize = 1000)]
/// Task&lt;int&gt; BatchInsertAsync(IEnumerable&lt;User&gt; entities);
/// 
/// // Usage:
/// var users = Enumerable.Range(1, 5000)
///     .Select(i => new User { Name = $"User{i}" })
///     .ToList();
/// 
/// var affected = await repo.BatchInsertAsync(users);
/// // Automatically splits into 5 batches of 1000 each
/// // Returns: 5000 (total affected rows)
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class BatchOperationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the maximum number of items per batch.
    /// Default is 1000.
    /// </summary>
    /// <remarks>
    /// This should be set based on:
    /// 1. Number of columns being inserted (more columns = fewer items per batch)
    /// 2. Database parameter limits
    /// 3. Performance considerations (too large = slow, too small = many round trips)
    /// </remarks>
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of parameters per batch.
    /// Default is 2100 (SQL Server limit).
    /// </summary>
    /// <remarks>
    /// This is used to calculate the actual batch size based on the number of columns.
    /// For example, if inserting 10 columns, the actual batch size would be min(MaxBatchSize, MaxParametersPerBatch / 10).
    /// </remarks>
    public int MaxParametersPerBatch { get; set; } = 2100;
}
