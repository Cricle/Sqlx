// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using System;

/// <summary>
/// Marks a parameter as an ExpressionToSqlBase type for batch operations
/// </summary>
/// <remarks>
/// <para>This attribute is used to indicate that a parameter is an ExpressionToSqlBase instance</para>
/// <para>that provides WHERE conditions for batch UPDATE or DELETE operations.</para>
/// <para><strong>Example:</strong></para>
/// <code>
/// public interface IUserRepository
/// {
///     [Sqlx("DELETE FROM {{table}} WHERE {{where}}")]
///     Task&lt;int&gt; BatchDeleteAsync([ExpressionToSql] ExpressionToSqlBase&lt;User&gt; whereExpression);
/// }
///
/// // Usage
/// var whereExpr = new ExpressionToSql&lt;User&gt;(SqlDefineTypes.SqlServer)
///     .Where(u => u.Age > 18 &amp;&amp; u.IsActive);
///
/// var affected = await repo.BatchDeleteAsync(whereExpr);
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ExpressionToSqlAttribute : Attribute
{
}

