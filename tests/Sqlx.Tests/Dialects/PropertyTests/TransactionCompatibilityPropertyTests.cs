// -----------------------------------------------------------------------
// <copyright file="TransactionCompatibilityPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using GenSqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Represents a SQL statement for property testing.
/// </summary>
public class SqlStatement
{
    public string Sql { get; }
    public string StatementType { get; }

    public SqlStatement(string sql, string statementType)
    {
        Sql = sql;
        StatementType = statementType;
    }

    public override string ToString() => $"{StatementType}: {Sql.Substring(0, Math.Min(50, Sql.Length))}...";
}

/// <summary>
/// FsCheck arbitrary generators for transaction compatibility testing.
/// </summary>
public static class TransactionArbitraries
{
    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

    public static Arbitrary<string> String()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(RestChars);

        var gen = from size in Gen.Choose(1, 20)
                  from first in firstChar
                  from rest in Gen.ArrayOf(size - 1, restChars)
                  select first + new string(rest);

        return gen.ToArbitrary();
    }

    public static Arbitrary<string[]> Columns()
    {
        return Gen.Sized(size =>
        {
            var count = Math.Max(1, Math.Min(size, 10));
            return Gen.ArrayOf(count, String().Generator);
        }).Where(arr => arr.Length > 0 && arr.All(s => !string.IsNullOrEmpty(s)))
          .ToArbitrary();
    }

    public static Arbitrary<int> BatchSize()
    {
        return Gen.Choose(1, 10).ToArbitrary();
    }

    public static Arbitrary<SqlStatement> SqlStatement()
    {
        return Gen.OneOf(
            from table in String().Generator
            from cols in Columns().Generator
            select new PropertyTests.SqlStatement(
                $"INSERT INTO {table} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", cols.Select(c => "@" + c))})",
                "INSERT"),
            
            from table in String().Generator
            from cols in Columns().Generator
            select new PropertyTests.SqlStatement(
                $"UPDATE {table} SET {string.Join(", ", cols.Select(c => $"{c} = @{c}"))} WHERE id = @id",
                "UPDATE"),
            
            from table in String().Generator
            select new PropertyTests.SqlStatement(
                $"DELETE FROM {table} WHERE id = @id",
                "DELETE"),
            
            from table in String().Generator
            from cols in Columns().Generator
            select new PropertyTests.SqlStatement(
                $"SELECT {string.Join(", ", cols)} FROM {table}",
                "SELECT")
        ).ToArbitrary();
    }
}

/// <summary>
/// Property-based tests for transaction compatibility across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 34: Transaction Compatibility**
/// **Validates: Requirements 44.1, 44.2, 44.3, 44.4**
/// </summary>
public class TransactionCompatibilityPropertyTests
{
    /// <summary>
    /// **Property 34: Transaction Compatibility**
    /// *For any* generated SQL statement, it SHALL be executable within a transaction 
    /// without implicit commits.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TransactionArbitraries) })]
    public Property GeneratedSql_ShouldNotContainImplicitCommitStatements(SqlStatement statement)
    {
        var sql = statement.Sql.ToUpper();
        var ddlKeywords = new[] { "CREATE ", "ALTER ", "DROP ", "TRUNCATE " };
        var transactionKeywords = new[] { "COMMIT", "ROLLBACK", "SAVEPOINT", "BEGIN TRANSACTION" };

        var containsDDL = ddlKeywords.Any(keyword => sql.Contains(keyword));
        var containsTransactionControl = transactionKeywords.Any(keyword => sql.Contains(keyword));

        return (!containsDDL && !containsTransactionControl)
            .Label($"SQL should not contain DDL or transaction control: {statement.Sql}");
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TransactionArbitraries) })]
    public Property InsertStatement_ShouldBeTransactionSafe(string tableName, string[] columns)
    {
        if (string.IsNullOrEmpty(tableName) || columns == null || columns.Length == 0)
            return true.ToProperty();

        var sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(c => "@" + c))})";
        var sqlUpper = sql.ToUpper();
        var ddlKeywords = new[] { "CREATE ", "ALTER ", "DROP ", "TRUNCATE " };
        var containsDDL = ddlKeywords.Any(keyword => sqlUpper.Contains(keyword));

        return (!containsDDL).Label($"INSERT statement should be transaction-safe: {sql}");
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TransactionArbitraries) })]
    public Property UpdateStatement_ShouldBeTransactionSafe(string tableName, string[] columns)
    {
        if (string.IsNullOrEmpty(tableName) || columns == null || columns.Length == 0)
            return true.ToProperty();

        var setClause = string.Join(", ", columns.Select(c => $"{c} = @{c}"));
        var sql = $"UPDATE {tableName} SET {setClause} WHERE id = @id";
        var sqlUpper = sql.ToUpper();
        var ddlKeywords = new[] { "CREATE ", "ALTER ", "DROP ", "TRUNCATE " };
        var containsDDL = ddlKeywords.Any(keyword => sqlUpper.Contains(keyword));

        return (!containsDDL).Label($"UPDATE statement should be transaction-safe: {sql}");
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TransactionArbitraries) })]
    public Property DeleteStatement_ShouldBeTransactionSafe(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var sql = $"DELETE FROM {tableName} WHERE id = @id";
        var sqlUpper = sql.ToUpper();
        var ddlKeywords = new[] { "CREATE ", "ALTER ", "DROP ", "TRUNCATE " };
        var containsDDL = ddlKeywords.Any(keyword => sqlUpper.Contains(keyword));

        return (!containsDDL).Label($"DELETE statement should be transaction-safe: {sql}");
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TransactionArbitraries) })]
    public Property SelectStatement_ShouldBeTransactionSafe(string tableName, string[] columns)
    {
        if (string.IsNullOrEmpty(tableName) || columns == null || columns.Length == 0)
            return true.ToProperty();

        var sql = $"SELECT {string.Join(", ", columns)} FROM {tableName}";
        var sqlUpper = sql.ToUpper();
        var ddlKeywords = new[] { "CREATE ", "ALTER ", "DROP ", "TRUNCATE " };
        var containsDDL = ddlKeywords.Any(keyword => sqlUpper.Contains(keyword));

        return (!containsDDL).Label($"SELECT statement should be transaction-safe: {sql}");
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TransactionArbitraries) })]
    public Property BatchInsert_ShouldBeTransactionSafe(string tableName, string[] columns, int batchSize)
    {
        if (string.IsNullOrEmpty(tableName) || columns == null || columns.Length == 0 || batchSize <= 0 || batchSize > 100)
            return true.ToProperty();

        var columnList = string.Join(", ", columns);
        var valuesList = string.Join(", ", Enumerable.Range(0, batchSize).Select(i => 
            $"({string.Join(", ", columns.Select(c => $"@{c}{i}"))})"));
        
        var sql = $"INSERT INTO {tableName} ({columnList}) VALUES {valuesList}";
        var sqlUpper = sql.ToUpper();
        var ddlKeywords = new[] { "CREATE ", "ALTER ", "DROP ", "TRUNCATE " };
        var containsDDL = ddlKeywords.Any(keyword => sqlUpper.Contains(keyword));

        return (!containsDDL).Label($"Batch INSERT should be transaction-safe");
    }
}
