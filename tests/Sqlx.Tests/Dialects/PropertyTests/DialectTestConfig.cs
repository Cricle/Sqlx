// -----------------------------------------------------------------------
// <copyright file="DialectTestConfig.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Configuration record for dialect-specific test expectations.
/// Contains expected values for each database dialect's SQL generation behavior.
/// </summary>
/// <param name="Dialect">The SQL dialect being tested</param>
/// <param name="DialectName">Human-readable name of the dialect</param>
/// <param name="ExpectedIdentifierQuoteLeft">Expected left quote character for identifiers</param>
/// <param name="ExpectedIdentifierQuoteRight">Expected right quote character for identifiers</param>
/// <param name="ExpectedParameterPrefix">Expected parameter prefix (@, $, :, ?)</param>
/// <param name="ExpectedBoolTrue">Expected boolean true literal</param>
/// <param name="ExpectedBoolFalse">Expected boolean false literal</param>
/// <param name="ExpectedCurrentTimestamp">Expected current timestamp function</param>
/// <param name="ExpectedConcatOperator">Expected string concatenation operator or function</param>
/// <param name="UsesLimitOffset">Whether dialect uses LIMIT/OFFSET syntax</param>
/// <param name="UsesFetchNext">Whether dialect uses FETCH NEXT syntax</param>
/// <param name="RequiresOrderByForPagination">Whether ORDER BY is required for pagination</param>
public record DialectTestConfig(
    Sqlx.Generator.SqlDefine Dialect,
    string DialectName,
    string ExpectedIdentifierQuoteLeft,
    string ExpectedIdentifierQuoteRight,
    string ExpectedParameterPrefix,
    string ExpectedBoolTrue,
    string ExpectedBoolFalse,
    string ExpectedCurrentTimestamp,
    string ExpectedConcatOperator,
    bool UsesLimitOffset,
    bool UsesFetchNext,
    bool RequiresOrderByForPagination)
{
    /// <summary>
    /// Gets the expected wrapped identifier format for a given column name.
    /// </summary>
    public string GetExpectedWrappedIdentifier(string identifier) =>
        $"{ExpectedIdentifierQuoteLeft}{identifier}{ExpectedIdentifierQuoteRight}";

    /// <summary>
    /// Gets the expected parameter format for a given parameter name.
    /// </summary>
    public string GetExpectedParameter(string paramName) =>
        $"{ExpectedParameterPrefix}{paramName}";

    /// <summary>
    /// Predefined configuration for MySQL dialect.
    /// </summary>
    public static readonly DialectTestConfig MySql = new(
        Dialect: Sqlx.Generator.SqlDefine.MySql,
        DialectName: "MySQL",
        ExpectedIdentifierQuoteLeft: "`",
        ExpectedIdentifierQuoteRight: "`",
        ExpectedParameterPrefix: "@",
        ExpectedBoolTrue: "1",
        ExpectedBoolFalse: "0",
        ExpectedCurrentTimestamp: "NOW()",
        ExpectedConcatOperator: "CONCAT",
        UsesLimitOffset: true,
        UsesFetchNext: false,
        RequiresOrderByForPagination: false);

    /// <summary>
    /// Predefined configuration for PostgreSQL dialect.
    /// </summary>
    public static readonly DialectTestConfig PostgreSql = new(
        Dialect: Sqlx.Generator.SqlDefine.PostgreSql,
        DialectName: "PostgreSQL",
        ExpectedIdentifierQuoteLeft: "\"",
        ExpectedIdentifierQuoteRight: "\"",
        ExpectedParameterPrefix: "@",
        ExpectedBoolTrue: "true",
        ExpectedBoolFalse: "false",
        ExpectedCurrentTimestamp: "CURRENT_TIMESTAMP",
        ExpectedConcatOperator: "||",
        UsesLimitOffset: true,
        UsesFetchNext: false,
        RequiresOrderByForPagination: false);

    /// <summary>
    /// Predefined configuration for SQL Server dialect.
    /// </summary>
    public static readonly DialectTestConfig SqlServer = new(
        Dialect: Sqlx.Generator.SqlDefine.SqlServer,
        DialectName: "SQL Server",
        ExpectedIdentifierQuoteLeft: "[",
        ExpectedIdentifierQuoteRight: "]",
        ExpectedParameterPrefix: "@",
        ExpectedBoolTrue: "1",
        ExpectedBoolFalse: "0",
        ExpectedCurrentTimestamp: "GETDATE()",
        ExpectedConcatOperator: "+",
        UsesLimitOffset: false,
        UsesFetchNext: true,
        RequiresOrderByForPagination: true);

    /// <summary>
    /// Predefined configuration for SQLite dialect.
    /// </summary>
    public static readonly DialectTestConfig SQLite = new(
        Dialect: Sqlx.Generator.SqlDefine.SQLite,
        DialectName: "SQLite",
        ExpectedIdentifierQuoteLeft: "[",
        ExpectedIdentifierQuoteRight: "]",
        ExpectedParameterPrefix: "@",
        ExpectedBoolTrue: "1",
        ExpectedBoolFalse: "0",
        ExpectedCurrentTimestamp: "datetime('now')",
        ExpectedConcatOperator: "||",
        UsesLimitOffset: true,
        UsesFetchNext: false,
        RequiresOrderByForPagination: false);

    /// <summary>
    /// Predefined configuration for Oracle dialect.
    /// </summary>
    public static readonly DialectTestConfig Oracle = new(
        Dialect: Sqlx.Generator.SqlDefine.Oracle,
        DialectName: "Oracle",
        ExpectedIdentifierQuoteLeft: "\"",
        ExpectedIdentifierQuoteRight: "\"",
        ExpectedParameterPrefix: ":",
        ExpectedBoolTrue: "1",
        ExpectedBoolFalse: "0",
        ExpectedCurrentTimestamp: "SYSDATE",
        ExpectedConcatOperator: "||",
        UsesLimitOffset: false,
        UsesFetchNext: true,
        RequiresOrderByForPagination: true);

    /// <summary>
    /// All dialect configurations for iteration in tests.
    /// </summary>
    public static readonly IReadOnlyList<DialectTestConfig> AllConfigs = new[]
    {
        MySql,
        PostgreSql,
        SqlServer,
        SQLite,
        Oracle
    };

    /// <summary>
    /// Gets the configuration for a specific dialect.
    /// </summary>
    public static DialectTestConfig GetConfig(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType switch
        {
            "MySql" => MySql,
            "PostgreSql" => PostgreSql,
            "SqlServer" => SqlServer,
            "SQLite" => SQLite,
            "Oracle" => Oracle,
            _ => throw new System.ArgumentException($"Unknown dialect: {dialect.DatabaseType}")
        };
    }

    /// <summary>
    /// Gets dialects that use LIMIT/OFFSET syntax.
    /// </summary>
    public static IEnumerable<DialectTestConfig> LimitOffsetDialects
    {
        get
        {
            yield return MySql;
            yield return PostgreSql;
            yield return SQLite;
        }
    }

    /// <summary>
    /// Gets dialects that use FETCH NEXT syntax.
    /// </summary>
    public static IEnumerable<DialectTestConfig> FetchNextDialects
    {
        get
        {
            yield return SqlServer;
            yield return Oracle;
        }
    }
}
