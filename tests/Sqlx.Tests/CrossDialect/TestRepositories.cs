// -----------------------------------------------------------------------
// <copyright file="TestRepositories.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using Sqlx.Annotations;

namespace Sqlx.Tests.CrossDialect;

// ============================================================================
// ICrudRepository implementations for all main dialects
// ============================================================================

[SqlxDebugger]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("test_entity")]
[RepositoryFor(typeof(ICrudRepository<CrossDialectTestEntity, long>))]
public partial class CrudRepositorySQLite : ICrudRepository<CrossDialectTestEntity, long>
{
    private readonly DbConnection _connection;
    public CrudRepositorySQLite(DbConnection connection) => _connection = connection;
}

[SqlxDebugger]
[SqlDefine(SqlDefineTypes.MySql)]
[TableName("test_entity")]
[RepositoryFor(typeof(ICrudRepository<CrossDialectTestEntity, long>))]
public partial class CrudRepositoryMySql : ICrudRepository<CrossDialectTestEntity, long>
{
    private readonly DbConnection _connection;
    public CrudRepositoryMySql(DbConnection connection) => _connection = connection;
}

[SqlxDebugger]
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("test_entity")]
[RepositoryFor(typeof(ICrudRepository<CrossDialectTestEntity, long>))]
public partial class CrudRepositoryPostgreSql : ICrudRepository<CrossDialectTestEntity, long>
{
    private readonly DbConnection _connection;
    public CrudRepositoryPostgreSql(DbConnection connection) => _connection = connection;
}

[SqlxDebugger]
[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("test_entity")]
[RepositoryFor(typeof(ICrudRepository<CrossDialectTestEntity, long>))]
public partial class CrudRepositorySqlServer : ICrudRepository<CrossDialectTestEntity, long>
{
    private readonly DbConnection _connection;
    public CrudRepositorySqlServer(DbConnection connection) => _connection = connection;
}

// ============================================================================
// IBatchRepository implementations (SQLite, MySQL, SqlServer only)
// NOTE: PostgreSQL has generator issues with batch_values placeholder
// ============================================================================

[SqlxDebugger]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("test_entity")]
[RepositoryFor(typeof(IBatchRepository<CrossDialectTestEntity, long>))]
public partial class BatchRepositorySQLite : IBatchRepository<CrossDialectTestEntity, long>
{
    private readonly DbConnection _connection;
    public BatchRepositorySQLite(DbConnection connection) => _connection = connection;
}

[SqlxDebugger]
[SqlDefine(SqlDefineTypes.MySql)]
[TableName("test_entity")]
[RepositoryFor(typeof(IBatchRepository<CrossDialectTestEntity, long>))]
public partial class BatchRepositoryMySql : IBatchRepository<CrossDialectTestEntity, long>
{
    private readonly DbConnection _connection;
    public BatchRepositoryMySql(DbConnection connection) => _connection = connection;
}

[SqlxDebugger]
[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("test_entity")]
[RepositoryFor(typeof(IBatchRepository<CrossDialectTestEntity, long>))]
public partial class BatchRepositorySqlServer : IBatchRepository<CrossDialectTestEntity, long>
{
    private readonly DbConnection _connection;
    public BatchRepositorySqlServer(DbConnection connection) => _connection = connection;
}
