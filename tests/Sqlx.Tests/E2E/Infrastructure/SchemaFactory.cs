// <copyright file="SchemaFactory.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Text;
using System.Text.RegularExpressions;

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Factory for generating dialect-specific DDL statements from common schema definitions.
/// </summary>
public class SchemaFactory : ISchemaFactory
{
    /// <inheritdoc/>
    public string TranslateSchema(string commonSchema, DatabaseType dbType)
    {
        // Simple translation - replace common types with dialect-specific types
        var translated = commonSchema;

        translated = dbType switch
        {
            DatabaseType.MySQL => TranslateToMySQL(translated),
            DatabaseType.PostgreSQL => TranslateToPostgreSQL(translated),
            DatabaseType.SqlServer => TranslateToSqlServer(translated),
            DatabaseType.SQLite => TranslateToSQLite(translated),
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        return translated;
    }

    /// <inheritdoc/>
    public string GetCreateDatabaseSql(string dbName, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => $"CREATE DATABASE `{dbName}`",
            DatabaseType.PostgreSQL => $"CREATE DATABASE \"{dbName}\"",
            DatabaseType.SqlServer => $"CREATE DATABASE [{dbName}]",
            DatabaseType.SQLite => string.Empty, // SQLite doesn't need CREATE DATABASE
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    /// <inheritdoc/>
    public string GetDropDatabaseSql(string dbName, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => $"DROP DATABASE IF EXISTS `{dbName}`",
            DatabaseType.PostgreSQL => $"DROP DATABASE IF EXISTS \"{dbName}\"",
            DatabaseType.SqlServer => $"DROP DATABASE IF EXISTS [{dbName}]",
            DatabaseType.SQLite => string.Empty, // SQLite doesn't need DROP DATABASE
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static string TranslateToMySQL(string schema)
    {
        var result = schema;

        // Type mappings
        result = Regex.Replace(result, @"\bBIGSERIAL\b", "BIGINT AUTO_INCREMENT", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bSERIAL\b", "INT AUTO_INCREMENT", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bBOOLEAN\b", "TINYINT(1)", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bTEXT\b", "TEXT", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bTIMESTAMP\b", "DATETIME", RegexOptions.IgnoreCase);

        return result;
    }

    private static string TranslateToPostgreSQL(string schema)
    {
        var result = schema;

        // Type mappings
        result = Regex.Replace(result, @"\bAUTO_INCREMENT\b", "SERIAL", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bBIGINT AUTO_INCREMENT\b", "BIGSERIAL", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bINT AUTO_INCREMENT\b", "SERIAL", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bDATETIME\b", "TIMESTAMP", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bTINYINT\(1\)\b", "BOOLEAN", RegexOptions.IgnoreCase);

        return result;
    }

    private static string TranslateToSqlServer(string schema)
    {
        var result = schema;

        // Type mappings
        result = Regex.Replace(result, @"\bBIGSERIAL\b", "BIGINT IDENTITY(1,1)", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bSERIAL\b", "INT IDENTITY(1,1)", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bAUTO_INCREMENT\b", "IDENTITY(1,1)", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bBOOLEAN\b", "BIT", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bTEXT\b", "NVARCHAR(MAX)", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bVARCHAR\b", "NVARCHAR", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bTIMESTAMP\b", "DATETIME2", RegexOptions.IgnoreCase);

        return result;
    }

    private static string TranslateToSQLite(string schema)
    {
        var result = schema;

        // Type mappings
        result = Regex.Replace(result, @"\bBIGSERIAL\b", "INTEGER", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bSERIAL\b", "INTEGER", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bBIGINT AUTO_INCREMENT\b", "INTEGER", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bINT AUTO_INCREMENT\b", "INTEGER", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bAUTO_INCREMENT\b", string.Empty, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bBOOLEAN\b", "INTEGER", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bTINYINT\(1\)\b", "INTEGER", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bDATETIME\b", "TEXT", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bTIMESTAMP\b", "TEXT", RegexOptions.IgnoreCase);

        // SQLite uses AUTOINCREMENT keyword differently
        result = Regex.Replace(
            result,
            @"(\w+)\s+INTEGER\s+PRIMARY\s+KEY",
            "$1 INTEGER PRIMARY KEY AUTOINCREMENT",
            RegexOptions.IgnoreCase);

        return result;
    }
}
