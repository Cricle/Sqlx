// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypeAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies the SQL execution type for a method.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method,
        AllowMultiple = false, Inherited = false)]
    public sealed class SqlExecuteTypeAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecuteTypeAttribute"/> class.
        /// </summary>
        /// <param name="operation">The SQL operation type.</param>
        public SqlExecuteTypeAttribute(SqlOperation operation)
        {
            Operation = operation;
            TableName = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecuteTypeAttribute"/> class with a specific table name.
        /// </summary>
        /// <param name="operation">The SQL operation type.</param>
        /// <param name="tableName">The table name to use for the operation. If not specified, uses TableName attribute or infers from entity type.</param>
        public SqlExecuteTypeAttribute(SqlOperation operation, string? tableName)
        {
            Operation = operation;
            TableName = tableName;
        }

        /// <summary>
        /// Gets the SQL operation type.
        /// </summary>
        public SqlOperation Operation { get; }

        /// <summary>
        /// Gets the table name to use for the operation.
        /// If null, the generator will use the TableName attribute or infer from the entity type.
        /// </summary>
        public string? TableName { get; }
    }
}

