// -----------------------------------------------------------------------
// <copyright file="RealDatabaseUserRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Sqlx.Annotations;

/// <summary>
/// Real database implementation using RepositoryFor source generator.
/// This demonstrates automatic SQL generation and database operations for SQL Server.
/// All IUserService methods are automatically implemented by the source generator.
/// </summary>
[RepositoryFor(typeof(IUserService))]
public partial class RealDatabaseUserRepository
{
    private readonly DbConnection connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="RealDatabaseUserRepository"/> class.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    public RealDatabaseUserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

}