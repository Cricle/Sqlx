// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Indicates that a parameter should be converted from a LINQ expression to SQL.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Parameter,
        AllowMultiple = false, Inherited = false)]
    public sealed class ExpressionToSqlAttribute : System.Attribute
    {
    }
}
