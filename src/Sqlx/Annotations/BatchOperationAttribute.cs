// -----------------------------------------------------------------------
// <copyright file="BatchOperationAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Annotations;

using System;

/// <summary>
/// Marks a method as batch operation with automatic performance optimization.
/// </summary>
/// <remarks>
/// Enables automatic batching for insert/update/delete operations.
/// Generated code splits large collections into batches to avoid parameter limits.
/// </remarks>
/// <example>
/// <code>
/// [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values}}")]
/// [BatchOperation(BatchSize = 1000)]
/// Task&lt;int&gt; BatchInsertAsync(IEnumerable&lt;User&gt; users);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BatchOperationAttribute : Attribute
{
    /// <summary>Gets or sets batch size (records per batch). Default: 1000.</summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>Gets or sets whether to use transactions. Default: true.</summary>
    public bool UseTransaction { get; set; } = true;
}

