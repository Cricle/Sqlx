// -----------------------------------------------------------------------
// <copyright file="SqlTemplate.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System.Data.Common;

namespace Sqlx.Annotations
{
    /// <summary>
    /// Represents a SQL template with parameterized command text and parameters.
    /// </summary>
    public readonly record struct SqlTemplate(string Sql, DbParameter[] Parameters);
}
