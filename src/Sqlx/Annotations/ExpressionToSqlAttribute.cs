// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a parameter as ExpressionToSqlBase type for batch operations.
    /// </summary>
    /// <remarks>
    /// Indicates that a parameter is an ExpressionToSqlBase instance providing WHERE conditions
    /// for batch UPDATE or DELETE operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Sqlx("DELETE FROM {{table}} WHERE {{where}}")]
    /// Task&lt;int&gt; BatchDeleteAsync([ExpressionToSql] ExpressionToSqlBase&lt;User&gt; whereExpression);
    ///
    /// // Usage:
    /// var whereExpr = new ExpressionToSql&lt;User&gt;().Where(u => u.Age > 18 &amp;&amp; u.IsActive);
    /// await repo.BatchDeleteAsync(whereExpr);
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ExpressionToSqlAttribute : System.Attribute
    {
    }
}
