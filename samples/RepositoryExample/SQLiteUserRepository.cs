// -----------------------------------------------------------------------
// <copyright file="SQLiteUserRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Sqlx.Annotations;

/// <summary>
/// SQLite implementation using RepositoryFor source generator.
/// This demonstrates automatic SQL generation and database operations for SQLite.
/// All IUserService methods are automatically implemented by the source generator.
/// </summary>
[RepositoryFor(typeof(IUserService))]
public partial class SQLiteUserRepository
{
    private readonly DbConnection connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteUserRepository"/> class.
    /// </summary>
    /// <param name="connection">The SQLite database connection.</param>
    public SQLiteUserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    // ========================================
    // RepositoryFor 源生成器演示 (SQLite)
    // RepositoryFor Source Generator Demo (SQLite)
    // 
    // 所有 IUserService 接口的方法将由源生成器自动实现
    // All IUserService interface methods will be automatically implemented by the source generator
    // 
    // 生成的方法将包括：
    // Generated methods will include:
    // - GetAllUsers/GetAllUsersAsync -> [Sqlx("SELECT * FROM users")]
    // - GetUserById/GetUserByIdAsync -> [Sqlx("SELECT * FROM users WHERE Id = @id")]  
    // - CreateUser/CreateUserAsync -> [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    // - UpdateUser -> [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    // - DeleteUser -> [SqlExecuteType(SqlExecuteTypes.Delete, "users")]
    // ========================================

    /// <summary>
    /// Ensures the database connection is open.
    /// </summary>
    private void EnsureConnectionOpen()
    {
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
    }

    /// <summary>
    /// Adds a parameter to the command.
    /// </summary>
    /// <param name="command">The command to add the parameter to.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}