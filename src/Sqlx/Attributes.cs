// -----------------------------------------------------------------------
// <copyright file="Attributes.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Annotations;

using System;

/// <summary>
/// Specifies that a class should act as a repository for the specified service type.
/// The service type can be an interface or abstract class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RepositoryForAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryForAttribute"/> class.
    /// </summary>
    /// <param name="serviceType">The service type this repository implements.</param>
    public RepositoryForAttribute(Type serviceType) => ServiceType = serviceType;
    
    /// <summary>
    /// Gets the service type this repository implements.
    /// </summary>
    public Type ServiceType { get; }
}

/// <summary>
/// Specifies the table name for database operations.
/// Can be applied to parameters, methods, or types.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class TableNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableNameAttribute"/> class.
    /// </summary>
    /// <param name="tableName">The table name to use for database operations.</param>
    public TableNameAttribute(string tableName) => TableName = tableName;
    
    /// <summary>
    /// Gets the table name to use for database operations.
    /// </summary>
    public string TableName { get; }
}

/// <summary>
/// Specifies SQL command for method execution.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SqlxAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlxAttribute"/> class.
    /// </summary>
    public SqlxAttribute() => StoredProcedureName = string.Empty;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlxAttribute"/> class with a stored procedure name.
    /// </summary>
    /// <param name="name">The stored procedure name or SQL command.</param>
    public SqlxAttribute(string name) => StoredProcedureName = name;
    
    /// <summary>
    /// Gets the stored procedure name or SQL command.
    /// </summary>
    public string StoredProcedureName { get; }
}

/// <summary>
/// Specifies raw SQL command for method execution.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RawSqlAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawSqlAttribute"/> class.
    /// </summary>
    public RawSqlAttribute() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RawSqlAttribute"/> class with SQL command.
    /// </summary>
    /// <param name="sql">The SQL command.</param>
    public RawSqlAttribute(string sql) { }
}

/// <summary>
/// Specifies that a parameter should be converted from expression to SQL.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ExpressionToSqlAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionToSqlAttribute"/> class.
    /// </summary>
    public ExpressionToSqlAttribute() { }
}

/// <summary>
/// Specifies SQL execution type for database operations.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SqlExecuteTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExecuteTypeAttribute"/> class.
    /// </summary>
    /// <param name="executeType">The SQL execution type.</param>
    /// <param name="tableName">The target table name.</param>
    public SqlExecuteTypeAttribute(SqlExecuteTypes executeType, string tableName)
    {
        ExecuteType = executeType;
        TableName = tableName;
    }
    
    /// <summary>
    /// Gets the SQL execution type.
    /// </summary>
    public SqlExecuteTypes ExecuteType { get; }
    
    /// <summary>
    /// Gets the target table name.
    /// </summary>
    public string TableName { get; }
}

/// <summary>
/// SQL execution types for database operations.
/// </summary>
public enum SqlExecuteTypes
{
    /// <summary>
    /// INSERT operation
    /// </summary>
    Insert,
    
    /// <summary>
    /// UPDATE operation
    /// </summary>
    Update,
    
    /// <summary>
    /// DELETE operation
    /// </summary>
    Delete,
    
    /// <summary>
    /// SELECT operation
    /// </summary>
    Select
}
