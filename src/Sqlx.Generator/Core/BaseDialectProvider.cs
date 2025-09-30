// -----------------------------------------------------------------------
// <copyright file="BaseDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator;

/// <summary>
/// Base class for database dialect providers with common functionality.
/// </summary>
internal abstract class BaseDialectProvider : IDatabaseDialectProvider
{
    /// <inheritdoc />
    public abstract SqlDefine SqlDefine { get; }

    /// <inheritdoc />
    public abstract SqlDefineTypes DialectType { get; }

    /// <inheritdoc />
    public abstract string GenerateLimitClause(int? limit, int? offset);

    /// <inheritdoc />
    public abstract string GenerateInsertWithReturning(string tableName, string[] columns);

    /// <inheritdoc />
    public virtual string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        var valuesClauses = new string[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            var parameters = columns.Select(c => $"@{c.ToLowerInvariant()}{i}").ToArray();
            valuesClauses[i] = $"({string.Join(", ", parameters)})";
        }

        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES {string.Join(", ", valuesClauses)}";
    }

    /// <inheritdoc />
    public abstract string GenerateUpsert(string tableName, string[] columns, string[] keyColumns);

    /// <inheritdoc />
    public abstract string GetDatabaseTypeName(Type dotNetType);

    /// <inheritdoc />
    public abstract string FormatDateTime(DateTime dateTime);

    /// <inheritdoc />
    public abstract string GetCurrentDateTimeSyntax();

    /// <inheritdoc />
    public abstract string GetConcatenationSyntax(params string[] expressions);

    /// <summary>
    /// Helper method to generate basic insert statement parts.
    /// </summary>
    protected (string tableName, string[] wrappedColumns, string[] parameters) GetInsertParts(string tableName, string[] columns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var parameters = columns.Select(c => $"@{c.ToLowerInvariant()}").ToArray();
        return (wrappedTableName, wrappedColumns, parameters);
    }
}


