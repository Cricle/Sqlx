// -----------------------------------------------------------------------
// <copyright file="TableNameAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies the database table name for an entity.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class |
        System.AttributeTargets.Interface | System.AttributeTargets.Method |
        System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class TableNameAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableNameAttribute"/> class.
        /// </summary>
        /// <param name="tableName">The database table name.</param>
        public TableNameAttribute(string tableName)
        {
            TableName = tableName ?? throw new System.ArgumentNullException(nameof(tableName));
        }

        /// <summary>
        /// Gets the database table name.
        /// </summary>
        public string TableName { get; }
    }
}
