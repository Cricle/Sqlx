// <copyright file="DbConnectionExtensions.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Sqlx;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for DbConnection.
/// </summary>
public static class DbConnectionExtensions
{
    /// <summary>
    /// Batch executes a SQL command for multiple entities.
    /// Uses loop execution (reflection-free).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="entities">The entities to process.</param>
    /// <param name="binder">The parameter binder.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="parameterPrefix">The parameter prefix (default: @).</param>
    /// <param name="batchSize">Max commands per batch (default: 1000).</param>
    /// <param name="commandTimeout">Command timeout in seconds (null = use default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total rows affected.</returns>
    public static Task<int> ExecuteBatchAsync<TEntity>(
        this DbConnection connection,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        DbTransaction? transaction = null,
        string parameterPrefix = "@",
        int batchSize = DbBatchExecutor.DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return DbBatchExecutor.ExecuteAsync(
            connection,
            transaction,
            sql,
            entities,
            binder,
            parameterPrefix,
            batchSize,
            commandTimeout,
            cancellationToken);
    }

    public static List<TResult> SqlxQuery<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        return SqlxQuery<TResult, TResult>(connection, template, dialect, transaction);
    }

    public static List<TResult> SqlxQuery<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        return SqlxQuery<TResult, TResult>(connection, template, dialect, parameters, transaction);
    }

    public static List<TResult> SqlxQuery<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return ExecuteQueryList<TResult>(connection, prepared, transaction);
    }

    public static List<TResult> SqlxQuery<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return ExecuteQueryList<TResult>(connection, prepared, transaction);
    }

    public static Task<List<TResult>> SqlxQueryAsync<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return SqlxQueryAsync<TResult, TResult>(connection, template, dialect, transaction, cancellationToken);
    }

    public static Task<List<TResult>> SqlxQueryAsync<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return SqlxQueryAsync<TResult, TResult>(
            connection,
            template,
            dialect,
            parameters,
            transaction,
            cancellationToken);
    }

    public static Task<List<TResult>> SqlxQueryAsync<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return ExecuteQueryListAsync<TResult>(connection, prepared, transaction, cancellationToken);
    }

    public static Task<List<TResult>> SqlxQueryAsync<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return ExecuteQueryListAsync<TResult>(connection, prepared, transaction, cancellationToken);
    }

    public static TResult? SqlxQueryFirstOrDefault<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        return SqlxQueryFirstOrDefault<TResult, TResult>(connection, template, dialect, transaction);
    }

    public static TResult? SqlxQueryFirstOrDefault<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        return SqlxQueryFirstOrDefault<TResult, TResult>(connection, template, dialect, parameters, transaction);
    }

    public static TResult? SqlxQueryFirstOrDefault<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return ExecuteQueryFirstOrDefault<TResult>(connection, prepared, transaction);
    }

    public static TResult? SqlxQueryFirstOrDefault<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return ExecuteQueryFirstOrDefault<TResult>(connection, prepared, transaction);
    }

    public static Task<TResult?> SqlxQueryFirstOrDefaultAsync<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return SqlxQueryFirstOrDefaultAsync<TResult, TResult>(connection, template, dialect, transaction, cancellationToken);
    }

    public static Task<TResult?> SqlxQueryFirstOrDefaultAsync<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return SqlxQueryFirstOrDefaultAsync<TResult, TResult>(
            connection,
            template,
            dialect,
            parameters,
            transaction,
            cancellationToken);
    }

    public static Task<TResult?> SqlxQueryFirstOrDefaultAsync<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return ExecuteQueryFirstOrDefaultAsync<TResult>(connection, prepared, transaction, cancellationToken);
    }

    public static Task<TResult?> SqlxQueryFirstOrDefaultAsync<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return ExecuteQueryFirstOrDefaultAsync<TResult>(connection, prepared, transaction, cancellationToken);
    }

    public static TResult SqlxQuerySingle<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        return SqlxQuerySingle<TResult, TResult>(connection, template, dialect, transaction);
    }

    public static TResult SqlxQuerySingle<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        return SqlxQuerySingle<TResult, TResult>(connection, template, dialect, parameters, transaction);
    }

    public static TResult SqlxQuerySingle<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return ExecuteQuerySingle<TResult>(connection, prepared, transaction);
    }

    public static TResult SqlxQuerySingle<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return ExecuteQuerySingle<TResult>(connection, prepared, transaction);
    }

    public static Task<TResult> SqlxQuerySingleAsync<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return SqlxQuerySingleAsync<TResult, TResult>(connection, template, dialect, transaction, cancellationToken);
    }

    public static Task<TResult> SqlxQuerySingleAsync<TResult>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return SqlxQuerySingleAsync<TResult, TResult>(
            connection,
            template,
            dialect,
            parameters,
            transaction,
            cancellationToken);
    }

    public static Task<TResult> SqlxQuerySingleAsync<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return ExecuteQuerySingleAsync<TResult>(connection, prepared, transaction, cancellationToken);
    }

    public static Task<TResult> SqlxQuerySingleAsync<TResult, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return ExecuteQuerySingleAsync<TResult>(connection, prepared, transaction, cancellationToken);
    }

    public static int SqlxExecute<TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return DbExecutor.ExecuteNonQuery(connection, prepared.Sql, prepared.Parameters, transaction);
    }

    public static int SqlxExecute<TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return DbExecutor.ExecuteNonQuery(connection, prepared.Sql, prepared.Parameters, transaction);
    }

    public static Task<int> SqlxExecuteAsync<TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return DbExecutor.ExecuteNonQueryAsync(connection, prepared.Sql, prepared.Parameters, transaction, cancellationToken);
    }

    public static Task<int> SqlxExecuteAsync<TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return DbExecutor.ExecuteNonQueryAsync(connection, prepared.Sql, prepared.Parameters, transaction, cancellationToken);
    }

    public static TScalar SqlxScalar<TScalar, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return TypeConverter.Convert<TScalar>(DbExecutor.ExecuteScalar(connection, prepared.Sql, prepared.Parameters, transaction));
    }

    public static TScalar SqlxScalar<TScalar, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return TypeConverter.Convert<TScalar>(DbExecutor.ExecuteScalar(connection, prepared.Sql, prepared.Parameters, transaction));
    }

    public static async Task<TScalar> SqlxScalarAsync<TScalar, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect);
        return TypeConverter.Convert<TScalar>(
            await DbExecutor.ExecuteScalarAsync(connection, prepared.Sql, prepared.Parameters, transaction, cancellationToken).ConfigureAwait(false));
    }

    public static async Task<TScalar> SqlxScalarAsync<TScalar, TEntity>(
        this DbConnection connection,
        string template,
        SqlDialect dialect,
        object? parameters,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = PrepareSql<TEntity>(template, dialect, parameters);
        return TypeConverter.Convert<TScalar>(
            await DbExecutor.ExecuteScalarAsync(connection, prepared.Sql, prepared.Parameters, transaction, cancellationToken).ConfigureAwait(false));
    }

    private static PreparedSql PrepareSql<TEntity>(string template, SqlDialect dialect)
    {
        var context = PlaceholderContext.Create<TEntity>(dialect);
        using var builder = new SqlBuilder(context);
        builder.AppendTemplate(template);
        var command = builder.Build();
        return new PreparedSql(command.Sql, NormalizeCommandParameters(command.Parameters, dialect));
    }

    private static PreparedSql PrepareSql<TEntity>(
        string template,
        SqlDialect dialect,
        object? parameters)
    {
        var context = PlaceholderContext.Create<TEntity>(dialect);
        using var builder = new SqlBuilder(context);
        if (parameters is null)
        {
            builder.AppendTemplate(template);
        }
        else
        {
            builder.AppendTemplate(template, NormalizeParameters(parameters));
        }
        var command = builder.Build();
        return new PreparedSql(command.Sql, NormalizeCommandParameters(command.Parameters, dialect));
    }

    private static IReadOnlyDictionary<string, object?> NormalizeParameters(object parameters)
    {
        if (parameters is IReadOnlyDictionary<string, object?> readOnlyDictionary)
        {
            return readOnlyDictionary;
        }

        var type = parameters.GetType();
        var properties = PropertyCache.GetOrAdd(
            type,
            static key => key.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        var dictionary = new Dictionary<string, object?>(properties.Length, StringComparer.Ordinal);
        foreach (var property in properties)
        {
            var value = property.GetValue(parameters);
            AddParameterAlias(dictionary, property.Name, value);

            var camelCaseName = ToCamelCase(property.Name);
            if (!string.Equals(camelCaseName, property.Name, StringComparison.Ordinal))
            {
                AddParameterAlias(dictionary, camelCaseName, value);
            }

            var snakeCaseName = ToSnakeCase(property.Name);
            if (!string.Equals(snakeCaseName, property.Name, StringComparison.Ordinal) &&
                !string.Equals(snakeCaseName, camelCaseName, StringComparison.Ordinal))
            {
                AddParameterAlias(dictionary, snakeCaseName, value);
            }
        }

        return dictionary;
    }

    private static void AddParameterAlias(
        Dictionary<string, object?> dictionary,
        string name,
        object? value)
    {
        if (!dictionary.ContainsKey(name))
        {
            dictionary[name] = value;
        }
    }

    private static IReadOnlyDictionary<string, object?>? NormalizeCommandParameters(
        IReadOnlyDictionary<string, object?> parameters,
        SqlDialect dialect)
    {
        if (parameters.Count == 0)
        {
            return null;
        }

        var normalized = new Dictionary<string, object?>(parameters.Count, StringComparer.Ordinal);
        foreach (var parameter in parameters)
        {
            var key = parameter.Key;
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            normalized[HasParameterPrefix(key) ? key : dialect.CreateParameter(key)] = parameter.Value;
        }

        return normalized;
    }

    private static bool HasParameterPrefix(string parameterName)
    {
        return parameterName[0] is '@' or ':' or '$' or '?';
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return char.ToLowerInvariant(value[0]).ToString();
        }

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var buffer = new char[value.Length * 2];
        var index = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current))
            {
                if (i > 0)
                {
                    buffer[index++] = '_';
                }

                buffer[index++] = char.ToLowerInvariant(current);
            }
            else
            {
                buffer[index++] = current;
            }
        }

        return new string(buffer, 0, index);
    }

    private static List<TResult> ExecuteQueryList<TResult>(
        DbConnection connection,
        PreparedSql prepared,
        DbTransaction? transaction)
    {
        return DbExecutor.ExecuteReader(
            connection,
            prepared.Sql,
            prepared.Parameters,
            ResolveResultReader<TResult>(),
            transaction).ToList();
    }

    private static TResult? ExecuteQueryFirstOrDefault<TResult>(
        DbConnection connection,
        PreparedSql prepared,
        DbTransaction? transaction)
    {
        return DbExecutor.ExecuteFirstOrDefault(
            connection,
            prepared.Sql,
            prepared.Parameters,
            ResolveResultReader<TResult>(),
            transaction);
    }

    private static TResult ExecuteQuerySingle<TResult>(
        DbConnection connection,
        PreparedSql prepared,
        DbTransaction? transaction)
    {
        return DbExecutor.ExecuteReader(
            connection,
            prepared.Sql,
            prepared.Parameters,
            ResolveResultReader<TResult>(),
            transaction).Single();
    }

    private static async Task<List<TResult>> ExecuteQueryListAsync<TResult>(
        DbConnection connection,
        PreparedSql prepared,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var enumerator = DbExecutor.ExecuteReaderAsync(
            connection,
            prepared.Sql,
            prepared.Parameters,
            ResolveResultReader<TResult>(),
            transaction,
            cancellationToken);

        var results = new List<TResult>();
        try
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                results.Add(enumerator.Current);
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        return results;
    }

    private static async Task<TResult?> ExecuteQueryFirstOrDefaultAsync<TResult>(
        DbConnection connection,
        PreparedSql prepared,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var enumerator = DbExecutor.ExecuteReaderAsync(
            connection,
            prepared.Sql,
            prepared.Parameters,
            ResolveResultReader<TResult>(),
            transaction,
            cancellationToken);

        try
        {
            return await enumerator.MoveNextAsync().ConfigureAwait(false)
                ? enumerator.Current
                : default;
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<TResult> ExecuteQuerySingleAsync<TResult>(
        DbConnection connection,
        PreparedSql prepared,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var enumerator = DbExecutor.ExecuteReaderAsync(
            connection,
            prepared.Sql,
            prepared.Parameters,
            ResolveResultReader<TResult>(),
            transaction,
            cancellationToken);

        try
        {
            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }

            var result = enumerator.Current;

            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Sequence contains more than one element.");
            }

            return result;
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static IResultReader<TResult> ResolveResultReader<TResult>()
    {
        if (SqlQuery<TResult>.ResultReader is IResultReader<TResult> reader)
        {
            return reader;
        }

        if (CanCreateDynamicReader(typeof(TResult)))
        {
            reader = new DynamicResultReader<TResult>();
            SqlQuery<TResult>.ResultReader = reader;
            return reader;
        }

        throw new InvalidOperationException(
            $"No result reader is available for type '{typeof(TResult).FullName}'. " +
            "Use SqlxScalar for scalar values or register a result reader.");
    }

    private static bool CanCreateDynamicReader(Type type)
    {
        if (type.IsPrimitive ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(Guid) ||
            type == typeof(TimeSpan) ||
            type.FullName == "System.DateOnly" ||
            type.FullName == "System.TimeOnly")
        {
            return false;
        }

        return !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>));
    }

    private sealed class PreparedSql
    {
        public PreparedSql(string sql, IReadOnlyDictionary<string, object?>? parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }

        public string Sql { get; }

        public IReadOnlyDictionary<string, object?>? Parameters { get; }
    }

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

#if NET6_0_OR_GREATER
    /// <summary>
    /// Batch executes a SQL command for multiple entities using DbBatch.
    /// Uses typed parameter factory (reflection-free).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TParameter">The DbParameter type for the database provider.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="entities">The entities to process.</param>
    /// <param name="binder">The parameter binder.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="parameterPrefix">The parameter prefix (default: @).</param>
    /// <param name="batchSize">Max commands per batch (default: 1000).</param>
    /// <param name="commandTimeout">Command timeout in seconds (null = use default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total rows affected.</returns>
    public static Task<int> ExecuteBatchAsync<TEntity, TParameter>(
        this DbConnection connection,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        DbTransaction? transaction = null,
        string parameterPrefix = "@",
        int batchSize = DbBatchExecutor.DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TParameter : DbParameter, new()
    {
        return DbBatchExecutor.ExecuteAsync<TEntity, TParameter>(
            connection,
            transaction,
            sql,
            entities,
            binder,
            parameterPrefix,
            batchSize,
            commandTimeout,
            cancellationToken);
    }
#endif
}
