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
/// Provides an isolated test database using table prefixes for isolation.
/// This approach is much faster than creating separate databases for each test.
/// Uses single container instance and single database with table-level isolation.
/// </summary>
public class DatabaseFixture : IDatabaseFixture
{
    private readonly string _connectionString;
    private DbConnection? _connection;
    private bool _disposed;
    private readonly string _tablePrefix;
    private readonly List<string> _createdTables = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseFixture"/> class.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <param name="connectionString">The connection string to the shared test database.</param>
    public DatabaseFixture(DatabaseType databaseType, string connectionString)
    {
        DatabaseType = databaseType;
        _connectionString = connectionString;
        _tablePrefix = GenerateRandomTablePrefix();
        DatabaseName = databaseType == DatabaseType.SQLite ? ":memory:" : "testdb";
    }

    /// <inheritdoc/>
    public string DatabaseName { get; }

    /// <summary>
    /// Gets the unique table prefix for this test fixture.
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

            // Return a wrapped connection that automatically applies table prefixes
            return new PrefixedDbConnection(_connection, this);
        }
    }

    /// <inheritdoc/>
    public DatabaseType DatabaseType { get; }

    /// <summary>
    /// Initializes the database fixture by creating the connection.
    /// No database creation needed - we use table prefixes for isolation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await CreateConnectionAsync();
    }

    /// <inheritdoc/>
    public async Task CreateSchemaAsync(string schemaDefinition)
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection is not open");
        }

        // Apply table prefix to isolate this test's tables
        var modifiedSchema = ApplyTablePrefix(schemaDefinition);

        using var command = _connection.CreateCommand();
        command.CommandText = modifiedSchema;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Applies the table prefix to all table names in the schema definition.
    /// This ensures each test has isolated tables in the shared database.
    /// </summary>
    /// <param name="schemaDefinition">The original schema definition.</param>
    /// <returns>The modified schema with prefixed table names.</returns>
    private string ApplyTablePrefix(string schemaDefinition)
    {
        // Pattern to match CREATE TABLE statements with various quote styles
        // Supports: CREATE TABLE table_name, CREATE TABLE `table_name`, CREATE TABLE [table_name], CREATE TABLE "table_name"
        var pattern = @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([`\[\""']?)(\w+)([`\]\""']?)";
        
        var result = Regex.Replace(
            schemaDefinition,
            pattern,
            match =>
            {
                var openQuote = match.Groups[1].Value;
                var tableName = match.Groups[2].Value;
                var closeQuote = match.Groups[3].Value;
                var prefixedName = $"{_tablePrefix}_{tableName}";
                
                // Track created tables for cleanup
                _createdTables.Add(prefixedName);
                
                return $"CREATE TABLE {openQuote}{prefixedName}{closeQuote}";
            },
            RegexOptions.IgnoreCase);

        return result;
    }

    /// <summary>
    /// Applies the table prefix to SQL statements (INSERT, UPDATE, DELETE, SELECT).
    /// This ensures all SQL operations use the isolated tables.
    /// </summary>
    /// <param name="sql">The original SQL statement.</param>
    /// <returns>The modified SQL with prefixed table names.</returns>
    public string ApplyTablePrefixToSql(string sql)
    {
        // Pattern to match table names in various SQL contexts
        // Matches: FROM table_name, JOIN table_name, INTO table_name, UPDATE table_name, TABLE table_name
        // Supports various quote styles: table_name, `table_name`, [table_name], "table_name"
        var patterns = new[]
        {
            // FROM, JOIN, INTO, UPDATE, TABLE keywords followed by table name
            @"(FROM|JOIN|INTO|UPDATE|TABLE)\s+([`\[\""']?)(\w+)([`\]\""']?)",
            // INSERT INTO with optional quotes
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
                    
                    // Skip if table name already has the prefix
                    if (tableName.StartsWith(_tablePrefix + "_", StringComparison.Ordinal))
                    {
                        return match.Value;
                    }
                    
                    var prefixedName = $"{_tablePrefix}_{tableName}";
                    
                    return $"{keyword} {openQuote}{prefixedName}{closeQuote}";
                },
                RegexOptions.IgnoreCase);
        }

        return result;
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
        // Drop all tables created by this fixture
        if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
        {
            try
            {
                await DropTablesAsync();
            }
            catch
            {
                // Ignore cleanup errors - best effort
            }

            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    /// <summary>
    /// Drops all tables created by this fixture.
    /// </summary>
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
                // Ignore individual table drop errors
            }
        }

        _createdTables.Clear();
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
    /// Generates a random table prefix for isolation.
    /// Uses a short, readable format for easier debugging.
    /// </summary>
    private static string GenerateRandomTablePrefix()
    {
        // Use timestamp + random for uniqueness and readability
        var timestamp = DateTime.UtcNow.ToString("HHmmss");
        var random = Guid.NewGuid().ToString("N").Substring(0, 6);
        return $"t{timestamp}{random}";
    }

    /// <inheritdoc/>
    public async Task<DbConnection> CreateNewConnectionAsync()
    {
        var connection = DatabaseType switch
        {
            DatabaseType.MySQL => (DbConnection)new MySqlConnection(_connectionString),
            DatabaseType.PostgreSQL => (DbConnection)new NpgsqlConnection(_connectionString),
            DatabaseType.SqlServer => (DbConnection)new SqlConnection(_connectionString),
            DatabaseType.SQLite => (DbConnection)new SqliteConnection(_connectionString),
            _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
        };

        await connection.OpenAsync();
        
        // Return a wrapped connection that automatically applies table prefixes
        return new PrefixedDbConnection(connection, this);
    }

    private async Task CreateConnectionAsync()
    {
        _connection = DatabaseType switch
        {
            DatabaseType.MySQL => new MySqlConnection(_connectionString),
            DatabaseType.PostgreSQL => new NpgsqlConnection(_connectionString),
            DatabaseType.SqlServer => new SqlConnection(_connectionString),
            DatabaseType.SQLite => new SqliteConnection(_connectionString),
            _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported"),
        };

        await _connection.OpenAsync();
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
