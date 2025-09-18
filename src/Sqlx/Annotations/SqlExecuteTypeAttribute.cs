// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypeAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// SQL操作类型枚举。
    /// </summary>
    public enum SqlOperation
    {
        /// <summary>
        /// 表示SELECT查询操作。
        /// </summary>
        Select,

        /// <summary>
        /// 表示INSERT插入操作。
        /// </summary>
        Insert,

        /// <summary>
        /// 表示UPDATE更新操作。
        /// </summary>
        Update,

        /// <summary>
        /// 表示DELETE删除操作。
        /// </summary>
        Delete
    }

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
        public SqlExecuteTypeAttribute(SqlOperation executeType, string tableName)
        {
            ExecuteType = executeType;
            TableName = tableName ?? throw new System.ArgumentNullException(nameof(tableName));
        }

        /// <summary>
        /// Gets the SQL operation type.
        /// </summary>
        public SqlOperation ExecuteType { get; }

        /// <summary>
        /// Gets the target table name.
        /// </summary>
        public string TableName { get; }
    }
}
