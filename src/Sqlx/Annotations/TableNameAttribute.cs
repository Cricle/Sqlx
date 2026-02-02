// -----------------------------------------------------------------------
// <copyright file="TableNameAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies the table name for a class or struct.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct,
        AllowMultiple = false, Inherited = false)]
    public sealed class TableNameAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableNameAttribute"/> class.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        public TableNameAttribute(string tableName)
        {
            TableName = tableName ?? throw new System.ArgumentNullException(nameof(tableName));
        }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Gets or sets the method name to call for getting the table name dynamically.
        /// The method must be static and return a string.
        /// When set, this takes precedence over <see cref="TableName"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// [TableName("default_table", Method = nameof(GetTableName))]
        /// public class MyEntity
        /// {
        ///     public static string GetTableName() => $"table_{DateTime.Now:yyyyMM}";
        /// }
        /// </code>
        /// </example>
        public string? Method { get; set; }
    }
}

