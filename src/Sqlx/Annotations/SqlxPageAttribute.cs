// -----------------------------------------------------------------------
// <copyright file="SqlxPageAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a method to return PagedResult with automatic count query generation.
    /// The generator will create both data query and count query for pagination.
    /// </summary>
    /// <example>
    /// <code>
    /// [SqlxPage]
    /// [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param offset}}")]
    /// Task&lt;PagedResult&lt;User&gt;&gt; GetPagedAsync(int pageSize = 20, int offset = 0);
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SqlxPageAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the custom count SQL template. If not set, auto-generates from data query.
        /// </summary>
        public string? CountTemplate { get; set; }
    }
}
