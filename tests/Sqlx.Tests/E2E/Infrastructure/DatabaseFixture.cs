// <copyright file="DatabaseFixture.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Provides an isolated test database fixture.
/// When a database name is provided, the fixture owns that database and manages its lifecycle.
/// The legacy table-prefix path remains available for compatibility but is not the default.
/// </summary>
public class DatabaseFixture : IDatabaseFixture
{
    private readonly string _baseConnectionString;
    private readonly string _tablePrefix;
    private readonly bool _ownsDatabase;
    private readonly bool _useTablePrefix;
    private readonly List<string> _createdTables = new();
    private DbConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseFixture"/> class.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <param name="connectionString">The base connection string.</param>
    /// <param name="databaseName">The database name owned by this fixture. If null, the fixture uses the existing database from the connection string.</param>
    /// <param name="useTablePrefix">Whether to retain legacy table-prefix isolation.</param>
    public DatabaseFixture(
        DatabaseType databaseType,
        string connectionString,
        string? databaseName = null,
        bool useTablePrefix = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(connectionString));
        }

        DatabaseType = databaseType;
        _baseConnectionString = connectionString;
        _ownsDatabase = !string.IsNullOrWhiteSpace(databaseName);
        _useTablePrefix = useTablePrefix;
        _tablePrefix = useTablePrefix ? GenerateRandomTablePrefix() : string.Empty;
        DatabaseName = databaseName ?? ResolveExistingDatabaseName(databaseType, connectionString);
    }

    /// <inheritdoc/>
    public string DatabaseName { get; }

    /// <summary>
    /// Gets the unique table prefix for legacy prefix-based isolation.
    /// </summary>
    public string TablePrefix => _tablePrefix;

    /// <inheritdoc/>
    public DbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                throw new InvalidOperationException(
                    "Connection has not been initialized. Call InitializeAsync first.");
            }

            return _useTablePrefix ? new PrefixedDbConnection(_connection, this) : _connection;
        }
    }

    /// <inheritdoc/>
    public DatabaseType DatabaseType { get; }

    /// <summary>
    /// Initializes the database fixture and creates the owned database when needed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        if (_ownsDatabase && DatabaseType != DatabaseType.SQLite)
        {
            await CreateOwnedDatabaseAsync();
        }

        await CreateConnectionAsync();
    }

    /// <inheritdoc/>
    public async Task CreateSchemaAsync(string schemaDefinition)
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection is not open");
        }

        var modifiedSchema = _useTablePrefix ? ApplyTablePrefix(schemaDefinition) : schemaDefinition;

        using var command = _connection.CreateCommand();
        command.CommandText = modifiedSchema;
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task<int> InsertTestDataAsync<T>(IEnumerable<T> data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection is not open");
        }

        var entities = data as IList<T> ?? data.ToList();
        if (entities.Count == 0)
        {
            return 0;
        }

        var entityType = typeof(T);
        var tableName = TableNameResolver.Resolve(entityType);
        var dialect = GetDialect();
        var entityProvider = EntityProviderResolver.ResolveOrCreate<T>();
        var readableProperties = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToDictionary(static property => property.Name, StringComparer.Ordinal);

        var insertableColumns = entityProvider.Columns
            .Where(column => readableProperties.ContainsKey(column.PropertyName))
            .ToArray();

        if (insertableColumns.Length == 0)
        {
            throw new InvalidOperationException(
                $"No insertable columns were found for entity type '{entityType.Name}'.");
        }

        var connection = Connection;
        var insertedCount = 0;

        foreach (var entity in entities)
        {
            var values = new List<(ColumnMeta Column, object? Value)>(insertableColumns.Length);
            foreach (var column in insertableColumns)
            {
                var property = readableProperties[column.PropertyName];
                var value = property.GetValue(entity);

                if (ShouldSkipProperty(property, value))
                {
                    continue;
                }

                values.Add((column, value));
            }

            if (values.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Entity type '{entityType.Name}' did not provide any values to insert.");
            }

            using var command = connection.CreateCommand();
            command.CommandText = BuildInsertSql(dialect, tableName, values);

            for (var i = 0; i < values.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}";
                parameter.Value = values[i].Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            insertedCount += await command.ExecuteNonQueryAsync();
        }

        return insertedCount;
    }

    /// <inheritdoc/>
    public async Task CleanupAsync()
    {
        if (_connection != null)
        {
            try
            {
                if (_useTablePrefix)
                {
                    await DropTablesAsync();
                }
            }
            catch
            {
                // Best effort cleanup.
            }

            try
            {
                if (_connection.State != System.Data.ConnectionState.Closed)
                {
                    await _connection.CloseAsync();
                }
            }
            catch
            {
                // Best effort cleanup.
            }

            await _connection.DisposeAsync();
            _connection = null;
        }

        if (_ownsDatabase && DatabaseType != DatabaseType.SQLite)
        {
            try
            {
                await DropOwnedDatabaseAsync();
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    /// <inheritdoc/>
    public async Task<DbConnection> CreateNewConnectionAsync()
    {
        var connection = CreateConnection(CreateOperationalConnectionString());
        await connection.OpenAsync();
        return _useTablePrefix ? new PrefixedDbConnection(connection, this) : connection;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await CleanupAsync();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Applies the table prefix to SQL statements when prefix mode is enabled.
    /// </summary>
    /// <param name="sql">The original SQL statement.</param>
    /// <returns>The modified SQL.</returns>
    public string ApplyTablePrefixToSql(string sql)
    {
        if (!_useTablePrefix || string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        var patterns = new[]
        {
            @"(FROM|JOIN|INTO|UPDATE|TABLE)\s+([`\[\""']?)(\w+)([`\]\""']?)",
            @"(INSERT\s+INTO)\s+([`\[\""']?)(\w+)([`\]\""']?)",
        };

        var result = sql;
        foreach (var pattern in patterns)
        {
            result = Regex.Replace(
                result,
                pattern,
                match =>
                {
                    var keyword = match.Groups[1].Value;
                    var openQuote = match.Groups[2].Value;
                    var tableName = match.Groups[3].Value;
                    var closeQuote = match.Groups[4].Value;

                    if (tableName.StartsWith(_tablePrefix + "_", StringComparison.Ordinal))
                    {
                        return match.Value;
                    }

                    return $"{keyword} {openQuote}{_tablePrefix}_{tableName}{closeQuote}";
                },
                RegexOptions.IgnoreCase);
        }

        return result;
    }

    private string ApplyTablePrefix(string schemaDefinition)
    {
        var pattern = @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([`\[\""']?)(\w+)([`\]\""']?)";

        return Regex.Replace(
            schemaDefinition,
            pattern,
            match =>
            {
                var openQuote = match.Groups[1].Value;
                var tableName = match.Groups[2].Value;
                var closeQuote = match.Groups[3].Value;
                var prefixedName = $"{_tablePrefix}_{tableName}";
                _createdTables.Add(prefixedName);
                return $"CREATE TABLE {openQuote}{prefixedName}{closeQuote}";
            },
            RegexOptions.IgnoreCase);
    }

    private async Task DropTablesAsync()
    {
        if (_connection == null || _createdTables.Count == 0)
        {
            return;
        }

        foreach (var tableName in _createdTables)
        {
            try
            {
                using var command = _connection.CreateCommand();
                command.CommandText = DatabaseType switch
                {
                    DatabaseType.MySQL => $"DROP TABLE IF EXISTS `{tableName}`",
                    DatabaseType.PostgreSQL => $"DROP TABLE IF EXISTS \"{tableName}\" CASCADE",
                    DatabaseType.SqlServer => $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE [{tableName}]",
                    DatabaseType.SQLite => $"DROP TABLE IF EXISTS [{tableName}]",
                    _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
                };

                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best effort cleanup.
            }
        }

        _createdTables.Clear();
    }

    private async Task CreateOwnedDatabaseAsync()
    {
        await using var connection = CreateConnection(CreateAdminConnectionString());
        await connection.OpenAsync();

        var createSql = DatabaseType switch
        {
            DatabaseType.MySQL => $"CREATE DATABASE `{DatabaseName}`",
            DatabaseType.PostgreSQL => $"CREATE DATABASE \"{DatabaseName}\"",
            DatabaseType.SqlServer => $"CREATE DATABASE [{DatabaseName}]",
            _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
        };

        using var command = connection.CreateCommand();
        command.CommandText = createSql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropOwnedDatabaseAsync()
    {
        await using var connection = CreateConnection(CreateAdminConnectionString());
        await connection.OpenAsync();

        var dropSql = DatabaseType switch
        {
            DatabaseType.MySQL => $"DROP DATABASE IF EXISTS `{DatabaseName}`",
            DatabaseType.PostgreSQL => $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE)",
            DatabaseType.SqlServer => $"""
                IF DB_ID('{DatabaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{DatabaseName}];
                END
                """,
            _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
        };

        using var command = connection.CreateCommand();
        command.CommandText = dropSql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateConnectionAsync()
    {
        _connection = CreateConnection(CreateOperationalConnectionString());

        // Retry for MySQL which may not be fully ready immediately after container start
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _connection.OpenAsync();
                return;
            }
            catch when (attempt < maxAttempts && DatabaseType == DatabaseType.MySQL)
            {
                await Task.Delay(attempt * 1000);
                _connection.Dispose();
                _connection = CreateConnection(CreateOperationalConnectionString());
            }
        }
    }

    private string CreateOperationalConnectionString()
    {
        return DatabaseType switch
        {
            DatabaseType.MySQL => BuildMySqlConnectionString(DatabaseName),
            DatabaseType.PostgreSQL => BuildPostgreSqlConnectionString(DatabaseName),
            DatabaseType.SqlServer => BuildSqlServerConnectionString(DatabaseName),
            DatabaseType.SQLite => _ownsDatabase ? BuildSqliteConnectionString(DatabaseName) : _baseConnectionString,
            _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
        };
    }

    private string CreateAdminConnectionString()
    {
        return DatabaseType switch
        {
            DatabaseType.MySQL => BuildMySqlConnectionString("mysql"),
            DatabaseType.PostgreSQL => BuildPostgreSqlConnectionString("postgres"),
            DatabaseType.SqlServer => BuildSqlServerConnectionString("master"),
            _ => throw new NotSupportedException($"Database type {DatabaseType} does not use an admin connection"),
        };
    }

    private DbConnection CreateConnection(string connectionString)
    {
        return DatabaseType switch
        {
            DatabaseType.MySQL => new MySqlConnection(connectionString),
            DatabaseType.PostgreSQL => new NpgsqlConnection(connectionString),
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.SQLite => new SqliteConnection(connectionString),
            _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
        };
    }

    private string BuildMySqlConnectionString(string databaseName)
    {
        var builder = new MySqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = databaseName,
            Pooling = false,
        };
        return builder.ConnectionString;
    }

    private string BuildPostgreSqlConnectionString(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = databaseName,
            Pooling = false,
        };
        return builder.ConnectionString;
    }

    private string BuildSqlServerConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = databaseName,
            Pooling = false,
        };
        return builder.ConnectionString;
    }

    private static string BuildSqliteConnectionString(string databaseName)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databaseName,
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        };
        return builder.ToString();
    }

    private static string ResolveExistingDatabaseName(DatabaseType databaseType, string connectionString)
    {
        return databaseType switch
        {
            DatabaseType.MySQL => new MySqlConnectionStringBuilder(connectionString).Database ?? string.Empty,
            DatabaseType.PostgreSQL => new NpgsqlConnectionStringBuilder(connectionString).Database ?? string.Empty,
            DatabaseType.SqlServer => new SqlConnectionStringBuilder(connectionString).InitialCatalog ?? string.Empty,
            DatabaseType.SQLite => new SqliteConnectionStringBuilder(connectionString).DataSource ?? ":memory:",
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported"),
        };
    }

    private SqlDialect GetDialect()
    {
        return DatabaseType switch
        {
            DatabaseType.MySQL => SqlDefine.MySql,
            DatabaseType.PostgreSQL => SqlDefine.PostgreSql,
            DatabaseType.SqlServer => SqlDefine.SqlServer,
            DatabaseType.SQLite => SqlDefine.SQLite,
            _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
        };
    }

    private static string GenerateRandomTablePrefix()
    {
        var timestamp = DateTime.UtcNow.ToString("HHmmss");
        var random = Guid.NewGuid().ToString("N")[..6];
        return $"t{timestamp}{random}";
    }

    private static bool ShouldSkipProperty(PropertyInfo property, object? value)
    {
        if (!IsKeyProperty(property))
        {
            return false;
        }

        return IsDefaultValue(property.PropertyType, value);
    }

    private static bool IsKeyProperty(MemberInfo property)
    {
        return string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase) ||
               property.GetCustomAttributes()
                   .Any(static attribute => string.Equals(attribute.GetType().Name, "KeyAttribute", StringComparison.Ordinal));
    }

    private static bool IsDefaultValue(Type propertyType, object? value)
    {
        if (value == null)
        {
            return true;
        }

        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (!underlyingType.IsValueType)
        {
            return false;
        }

        return value.Equals(Activator.CreateInstance(underlyingType));
    }

    private static string BuildInsertSql(
        SqlDialect dialect,
        string tableName,
        IReadOnlyList<(ColumnMeta Column, object? Value)> values)
    {
        var columns = string.Join(", ", values.Select(item => dialect.WrapColumn(item.Column.Name)));
        var parameters = string.Join(", ", values.Select((_, index) => $"@p{index}"));
        return $"INSERT INTO {dialect.WrapColumn(tableName)} ({columns}) VALUES ({parameters})";
    }
}
