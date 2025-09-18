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
        }

        /// <summary>
        /// Gets the SQL operation type.
        /// </summary>
        public SqlOperation Operation { get; }
    }
}

