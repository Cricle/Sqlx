// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypeAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies CRUD operation types and target table names.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method,
        AllowMultiple = false, Inherited = false)]
    public sealed class SqlExecuteTypeAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecuteTypeAttribute"/> class.
        /// </summary>
        /// <param name="executeType">The SQL operation type.</param>
        /// <param name="tableName">The target table name.</param>
        public SqlExecuteTypeAttribute(SqlExecuteTypes executeType, string tableName)
        {
            ExecuteType = executeType;
            TableName = tableName ?? throw new System.ArgumentNullException(nameof(tableName));
        }

        /// <summary>
        /// Gets the SQL operation type.
        /// </summary>
        public SqlExecuteTypes ExecuteType { get; }

        /// <summary>
        /// Gets the target table name.
        /// </summary>
        public string TableName { get; }
    }
}
