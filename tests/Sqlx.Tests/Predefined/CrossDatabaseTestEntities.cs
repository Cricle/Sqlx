// -----------------------------------------------------------------------
// <copyright file="CrossDatabaseTestEntities.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined;

/// <summary>
/// Entity for cross-database integration tests.
/// Uses a simple schema that works across all supported databases.
/// </summary>
[TableName("crossdb_users")]
public class CrossDbUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

#region ICrudRepository Implementations

// ===== SQLite Repository =====
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICrudRepository<CrossDbUser, long>))]
public partial class CrossDbUserRepository_SQLite(IDbConnection connection)
    : ICrudRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// ===== PostgreSQL Repository =====
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(ICrudRepository<CrossDbUser, long>))]
public partial class CrossDbUserRepository_PostgreSQL(IDbConnection connection)
    : ICrudRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// ===== MySQL Repository =====
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(ICrudRepository<CrossDbUser, long>))]
public partial class CrossDbUserRepository_MySQL(IDbConnection connection)
    : ICrudRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// ===== SQL Server Repository =====
[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(ICrudRepository<CrossDbUser, long>))]
public partial class CrossDbUserRepository_SqlServer(IDbConnection connection)
    : ICrudRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region IQueryRepository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IQueryRepository<CrossDbUser, long>))]
public partial class CrossDbQueryRepository_SQLite(IDbConnection connection)
    : IQueryRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IQueryRepository<CrossDbUser, long>))]
public partial class CrossDbQueryRepository_PostgreSQL(IDbConnection connection)
    : IQueryRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IQueryRepository<CrossDbUser, long>))]
public partial class CrossDbQueryRepository_MySQL(IDbConnection connection)
    : IQueryRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IQueryRepository<CrossDbUser, long>))]
public partial class CrossDbQueryRepository_SqlServer(IDbConnection connection)
    : IQueryRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region ICommandRepository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICommandRepository<CrossDbUser, long>))]
public partial class CrossDbCommandRepository_SQLite(IDbConnection connection)
    : ICommandRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(ICommandRepository<CrossDbUser, long>))]
public partial class CrossDbCommandRepository_PostgreSQL(IDbConnection connection)
    : ICommandRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(ICommandRepository<CrossDbUser, long>))]
public partial class CrossDbCommandRepository_MySQL(IDbConnection connection)
    : ICommandRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(ICommandRepository<CrossDbUser, long>))]
public partial class CrossDbCommandRepository_SqlServer(IDbConnection connection)
    : ICommandRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region IBatchRepository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBatchRepository<CrossDbUser, long>))]
public partial class CrossDbBatchRepository_SQLite(IDbConnection connection)
    : IBatchRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// Note: PostgreSQL IBatchRepository excluded due to known source generator issues with batch SQL generation

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IBatchRepository<CrossDbUser, long>))]
public partial class CrossDbBatchRepository_MySQL(IDbConnection connection)
    : IBatchRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IBatchRepository<CrossDbUser, long>))]
public partial class CrossDbBatchRepository_SqlServer(IDbConnection connection)
    : IBatchRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region IAggregateRepository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAggregateRepository<CrossDbUser, long>))]
public partial class CrossDbAggregateRepository_SQLite(IDbConnection connection)
    : IAggregateRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IAggregateRepository<CrossDbUser, long>))]
public partial class CrossDbAggregateRepository_PostgreSQL(IDbConnection connection)
    : IAggregateRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IAggregateRepository<CrossDbUser, long>))]
public partial class CrossDbAggregateRepository_MySQL(IDbConnection connection)
    : IAggregateRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IAggregateRepository<CrossDbUser, long>))]
public partial class CrossDbAggregateRepository_SqlServer(IDbConnection connection)
    : IAggregateRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

/*
// TEMPORARILY DISABLED: Source generator has bugs with these interfaces
// These will be re-enabled once the generator bugs are fixed

#region IAdvancedRepository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAdvancedRepository<CrossDbUser, long>))]
public partial class CrossDbAdvancedRepository_SQLite(IDbConnection connection)
    : IAdvancedRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;

    // Manual implementation for method-level generic methods not supported by source generator
    public Task<List<TResult>> QueryRawAsync<TResult>(string sql, CancellationToken cancellationToken = default)
        where TResult : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult> requires manual implementation");
    }

    public Task<List<TResult>> QueryRawAsync<TResult, TParams>(string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
        where TResult : class
        where TParams : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult, TParams> requires manual implementation");
    }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IAdvancedRepository<CrossDbUser, long>))]
public partial class CrossDbAdvancedRepository_PostgreSQL(IDbConnection connection)
    : IAdvancedRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;

    // Manual implementation for method-level generic methods not supported by source generator
    public Task<List<TResult>> QueryRawAsync<TResult>(string sql, CancellationToken cancellationToken = default)
        where TResult : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult> requires manual implementation");
    }

    public Task<List<TResult>> QueryRawAsync<TResult, TParams>(string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
        where TResult : class
        where TParams : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult, TParams> requires manual implementation");
    }
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IAdvancedRepository<CrossDbUser, long>))]
public partial class CrossDbAdvancedRepository_MySQL(IDbConnection connection)
    : IAdvancedRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;

    // Manual implementation for method-level generic methods not supported by source generator
    public Task<List<TResult>> QueryRawAsync<TResult>(string sql, CancellationToken cancellationToken = default)
        where TResult : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult> requires manual implementation");
    }

    public Task<List<TResult>> QueryRawAsync<TResult, TParams>(string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
        where TResult : class
        where TParams : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult, TParams> requires manual implementation");
    }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IAdvancedRepository<CrossDbUser, long>))]
public partial class CrossDbAdvancedRepository_SqlServer(IDbConnection connection)
    : IAdvancedRepository<CrossDbUser, long>
{
    protected readonly IDbConnection connection = connection;

    // Manual implementation for method-level generic methods not supported by source generator
    public Task<List<TResult>> QueryRawAsync<TResult>(string sql, CancellationToken cancellationToken = default)
        where TResult : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult> requires manual implementation");
    }

    public Task<List<TResult>> QueryRawAsync<TResult, TParams>(string sql, TParams? parameters = default, CancellationToken cancellationToken = default)
        where TResult : class
        where TParams : class
    {
        throw new NotImplementedException("QueryRawAsync<TResult, TParams> requires manual implementation");
    }
}

#endregion

#region ISchemaRepository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISchemaRepository<CrossDbUser>))]
public partial class CrossDbSchemaRepository_SQLite(IDbConnection connection)
    : ISchemaRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(ISchemaRepository<CrossDbUser>))]
public partial class CrossDbSchemaRepository_PostgreSQL(IDbConnection connection)
    : ISchemaRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(ISchemaRepository<CrossDbUser>))]
public partial class CrossDbSchemaRepository_MySQL(IDbConnection connection)
    : ISchemaRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(ISchemaRepository<CrossDbUser>))]
public partial class CrossDbSchemaRepository_SqlServer(IDbConnection connection)
    : ISchemaRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region IMaintenanceRepository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IMaintenanceRepository<CrossDbUser>))]
public partial class CrossDbMaintenanceRepository_SQLite(IDbConnection connection)
    : IMaintenanceRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IMaintenanceRepository<CrossDbUser>))]
public partial class CrossDbMaintenanceRepository_PostgreSQL(IDbConnection connection)
    : IMaintenanceRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IMaintenanceRepository<CrossDbUser>))]
public partial class CrossDbMaintenanceRepository_MySQL(IDbConnection connection)
    : IMaintenanceRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IMaintenanceRepository<CrossDbUser>))]
public partial class CrossDbMaintenanceRepository_SqlServer(IDbConnection connection)
    : IMaintenanceRepository<CrossDbUser>
{
    protected readonly IDbConnection connection = connection;
}

#endregion
*/
