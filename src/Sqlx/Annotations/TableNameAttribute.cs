// -----------------------------------------------------------------------
// <copyright file="TableNameAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies the table name for a class.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class,
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
    }
}

