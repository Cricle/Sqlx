// -----------------------------------------------------------------------
// <copyright file="BatchOperationAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using System;

/// <summary>
/// Marks a method as a batch operation with automatic performance optimization
/// </summary>
/// <remarks>
/// <para>This attribute enables automatic batching for insert/update/delete operations.</para>
/// <para>The generated code will automatically split large collections into batches</para>
/// <para>to avoid parameter limit issues and improve performance.</para>
/// <para><strong>Example:</strong></para>
/// <code>
/// public interface IUserRepository
/// {
///     [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values}}")]
///     [BatchOperation(BatchSize = 1000)]
///     Task&lt;int&gt; BatchInsertAsync(IEnumerable&lt;User&gt; users);
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BatchOperationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the batch size (number of records per batch).
    /// Default is 1000.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to use transactions for batch operations.
    /// Default is true.
    /// </summary>
    public bool UseTransaction { get; set; } = true;
}

