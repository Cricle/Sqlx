// -----------------------------------------------------------------------
// <copyright file="IncludeDeletedAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx.Annotations;

/// <summary>
/// When applied to a repository method on an entity with <see cref="SoftDeleteAttribute"/>,
/// this attribute instructs the source generator to NOT filter out soft-deleted records.
/// Use this when you need to query all records including those marked as deleted.
/// </summary>
/// <example>
/// <code>
/// [SoftDelete]
/// public class User
/// {
///     public long Id { get; set; }
///     public bool IsDeleted { get; set; }
/// }
/// 
/// public interface IUserRepository
/// {
///     // This will filter: WHERE is_deleted = false
///     Task&lt;List&lt;User&gt;&gt; GetAllAsync();
///     
///     // This will NOT filter (includes deleted records)
///     [IncludeDeleted]
///     Task&lt;List&lt;User&gt;&gt; GetAllIncludingDeletedAsync();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class IncludeDeletedAttribute : Attribute
{
}

