// -----------------------------------------------------------------------
// <copyright file="SqlInjectionPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Sqlx.Annotations;
using Sqlx.Validation;
using Xunit;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for SQL injection prevention.
/// **Feature: sql-semantic-tdd-validation, Property 23: SQL Injection Prevention**
/// **Validates: Requirements 36.1, 36.2, 36.3, 36.4, 36.5, 36.6**
/// </summary>
public class SqlInjectionPropertyTests
{
    #region Property 23: SQL Injection Prevention

    /// <summary>
    /// **Property 23: SQL Injection Prevention - DROP keyword**
    /// *For any* dynamic placeholder input containing DROP keyword, validation SHALL reject the input.
    /// **Validates: Requirements 36.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property DynamicPlaceholder_WithDropKeyword_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Only test DROP-related injections
        if (!injectionAttempt.Value.Contains("DROP", StringComparison.OrdinalIgnoreCase))
            return true.ToProperty();

        // Act
        var isValidFragment = SqlValidator.IsValidFragment(injectionAttempt.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(injectionAttempt.Value.AsSpan());

        // Assert: Should be rejected (not valid fragment OR contains dangerous keyword)
        return (!isValidFragment || containsDangerous)
            .Label($"Input: '{injectionAttempt.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - DELETE keyword**
    /// *For any* dynamic placeholder input containing DELETE keyword in dangerous context, validation SHALL reject the input.
    /// **Validates: Requirements 36.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property DynamicPlaceholder_WithDeleteKeyword_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Only test DELETE-related injections (with semicolon indicating statement injection)
        if (!injectionAttempt.Value.Contains("DELETE", StringComparison.OrdinalIgnoreCase) ||
            !injectionAttempt.Value.Contains(";"))
            return true.ToProperty();

        // Act
        var isValidFragment = SqlValidator.IsValidFragment(injectionAttempt.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(injectionAttempt.Value.AsSpan());

        // Assert: Should be rejected
        return (!isValidFragment || containsDangerous)
            .Label($"Input: '{injectionAttempt.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - EXEC keyword**
    /// *For any* dynamic placeholder input containing EXEC keyword, validation SHALL reject the input.
    /// **Validates: Requirements 36.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property DynamicPlaceholder_WithExecKeyword_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Only test EXEC-related injections
        if (!injectionAttempt.Value.Contains("EXEC", StringComparison.OrdinalIgnoreCase))
            return true.ToProperty();

        // Act
        var isValidFragment = SqlValidator.IsValidFragment(injectionAttempt.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(injectionAttempt.Value.AsSpan());

        // Assert: Should be rejected
        return (!isValidFragment || containsDangerous)
            .Label($"Input: '{injectionAttempt.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - Comment markers (--)**
    /// *For any* dynamic placeholder input containing -- comment marker, validation SHALL reject the input.
    /// **Validates: Requirements 36.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property DynamicPlaceholder_WithDoubleHyphenComment_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Only test -- comment injections
        if (!injectionAttempt.Value.Contains("--"))
            return true.ToProperty();

        // Act
        var isValidFragment = SqlValidator.IsValidFragment(injectionAttempt.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(injectionAttempt.Value.AsSpan());

        // Assert: Should be rejected
        return (!isValidFragment || containsDangerous)
            .Label($"Input: '{injectionAttempt.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - Block comment markers (/*)**
    /// *For any* dynamic placeholder input containing /* block comment marker, validation SHALL reject the input.
    /// **Validates: Requirements 36.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property DynamicPlaceholder_WithBlockComment_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Only test /* comment injections
        if (!injectionAttempt.Value.Contains("/*"))
            return true.ToProperty();

        // Act
        var isValidFragment = SqlValidator.IsValidFragment(injectionAttempt.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(injectionAttempt.Value.AsSpan());

        // Assert: Should be rejected
        return (!isValidFragment || containsDangerous)
            .Label($"Input: '{injectionAttempt.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - Semicolon (statement separator)**
    /// *For any* dynamic placeholder input containing semicolon, validation SHALL reject the input.
    /// **Validates: Requirements 36.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property DynamicPlaceholder_WithSemicolon_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Only test semicolon injections
        if (!injectionAttempt.Value.Contains(";"))
            return true.ToProperty();

        // Act
        var isValidFragment = SqlValidator.IsValidFragment(injectionAttempt.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(injectionAttempt.Value.AsSpan());

        // Assert: Should be rejected
        return (!isValidFragment || containsDangerous)
            .Label($"Input: '{injectionAttempt.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - Combined dangerous patterns**
    /// *For any* SQL injection attempt string, validation SHALL reject the input.
    /// **Validates: Requirements 36.1, 36.2, 36.3, 36.4, 36.5, 36.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property DynamicPlaceholder_WithAnyDangerousPattern_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Act
        var isValidFragment = SqlValidator.IsValidFragment(injectionAttempt.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(injectionAttempt.Value.AsSpan());

        // Assert: Should be rejected (not valid fragment OR contains dangerous keyword)
        // All generated injection attempts should be caught by at least one validation
        return (!isValidFragment || containsDangerous)
            .Label($"Input: '{injectionAttempt.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - Identifier validation**
    /// *For any* SQL injection attempt used as identifier, validation SHALL reject the input.
    /// **Validates: Requirements 36.1-36.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property Identifier_WithSqlInjectionAttempt_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Act
        var isValidIdentifier = SqlValidator.IsValidIdentifier(injectionAttempt.Value.AsSpan());

        // Assert: SQL injection attempts should never be valid identifiers
        return (!isValidIdentifier)
            .Label($"Input: '{injectionAttempt.Value}', IsValidIdentifier: {isValidIdentifier}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - TablePart validation**
    /// *For any* SQL injection attempt used as table part, validation SHALL reject the input.
    /// **Validates: Requirements 36.1-36.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property TablePart_WithSqlInjectionAttempt_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt)
    {
        // Act
        var isValidTablePart = SqlValidator.IsValidTablePart(injectionAttempt.Value.AsSpan());

        // Assert: SQL injection attempts should never be valid table parts
        return (!isValidTablePart)
            .Label($"Input: '{injectionAttempt.Value}', IsValidTablePart: {isValidTablePart}");
    }

    /// <summary>
    /// **Property 23: SQL Injection Prevention - Generic Validate method**
    /// *For any* SQL injection attempt and *for any* DynamicSqlType, validation SHALL reject the input.
    /// **Validates: Requirements 36.1-36.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property Validate_WithSqlInjectionAttempt_ShouldBeRejected(
        SqlInjectionAttemptWrapper injectionAttempt, DynamicSqlType sqlType)
    {
        // Act
        var isValid = SqlValidator.Validate(injectionAttempt.Value.AsSpan(), sqlType);

        // Assert: SQL injection attempts should never be valid for any type
        return (!isValid)
            .Label($"Input: '{injectionAttempt.Value}', Type: {sqlType}, IsValid: {isValid}");
    }

    #endregion

    #region Safe Input Tests (Negative Tests)

    /// <summary>
    /// Property: Safe inputs should be accepted by fragment validation.
    /// This ensures we don't have false positives.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property SafeFragment_ShouldBeAccepted(SafeFragmentWrapper safeFragment)
    {
        // Act
        var isValidFragment = SqlValidator.IsValidFragment(safeFragment.Value.AsSpan());
        var containsDangerous = SqlValidator.ContainsDangerousKeyword(safeFragment.Value.AsSpan());

        // Assert: Safe fragments should be valid and not contain dangerous keywords
        return (isValidFragment && !containsDangerous)
            .Label($"Input: '{safeFragment.Value}', IsValidFragment: {isValidFragment}, ContainsDangerous: {containsDangerous}");
    }

    /// <summary>
    /// Property: Safe identifiers should be accepted by identifier validation.
    /// This ensures we don't have false positives.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(SqlInjectionArbitraries) })]
    public Property SafeIdentifier_ShouldBeAccepted(SafeIdentifierWrapper safeIdentifier)
    {
        // Act
        var isValidIdentifier = SqlValidator.IsValidIdentifier(safeIdentifier.Value.AsSpan());

        // Assert: Safe identifiers should be valid
        return isValidIdentifier
            .Label($"Input: '{safeIdentifier.Value}', IsValidIdentifier: {isValidIdentifier}");
    }

    #endregion
}


/// <summary>
/// Wrapper for SQL injection attempt strings to use with FsCheck.
/// </summary>
public class SqlInjectionAttemptWrapper
{
    public string Value { get; }

    public SqlInjectionAttemptWrapper(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Wrapper for safe SQL fragment strings to use with FsCheck.
/// </summary>
public class SafeFragmentWrapper
{
    public string Value { get; }

    public SafeFragmentWrapper(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Wrapper for safe SQL identifier strings to use with FsCheck.
/// </summary>
public class SafeIdentifierWrapper
{
    public string Value { get; }

    public SafeIdentifierWrapper(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Custom arbitraries for SQL injection property tests.
/// </summary>
public static class SqlInjectionArbitraries
{
    /// <summary>
    /// Generates SQL injection attack strings for security testing.
    /// These are common SQL injection patterns that should be rejected.
    /// </summary>
    public static Arbitrary<SqlInjectionAttemptWrapper> SqlInjectionAttemptWrapper()
    {
        // DROP keyword injections (Requirement 36.1)
        var dropInjections = new[]
        {
            "'; DROP TABLE users; --",
            "DROP TABLE users",
            "1; DROP TABLE users",
            "admin'; DROP TABLE users--",
            "x' OR 1=1; DROP DATABASE test; --",
            "DROP DATABASE production",
            "'; DROP INDEX idx_users; --"
        };

        // DELETE keyword injections (Requirement 36.2)
        var deleteInjections = new[]
        {
            "1; DELETE FROM users",
            "'; DELETE FROM users WHERE 1=1; --",
            "admin'; DELETE FROM users--",
            "x; DELETE FROM orders; --"
        };

        // EXEC keyword injections (Requirement 36.3)
        var execInjections = new[]
        {
            "1; EXEC xp_cmdshell('dir')",
            "'; EXEC sp_executesql @sql; --",
            "EXEC master..xp_cmdshell 'dir'",
            "admin'; EXEC sp_addlogin 'hacker'; --",
            "'; EXECUTE sp_configure; --"
        };

        // Comment marker injections -- (Requirement 36.4)
        var commentInjections = new[]
        {
            "admin'--",
            "admin' --",
            "1 OR 1=1--",
            "'; SELECT * FROM users--",
            "x' OR 'a'='a'--"
        };

        // Block comment injections /* (Requirement 36.5)
        var blockCommentInjections = new[]
        {
            "/* comment */ DROP TABLE",
            "admin'/**/OR/**/1=1",
            "1/**/OR/**/1=1",
            "'; SELECT * FROM users /*",
            "x'/**/OR/**/1=1"
        };

        // Semicolon injections (Requirement 36.6)
        var semicolonInjections = new[]
        {
            "admin'; SELECT * FROM passwords;",
            "1; UPDATE users SET admin=1",
            "'; TRUNCATE TABLE users;",
            "x'; INSERT INTO users VALUES('hacker');",
            "1; ALTER TABLE users ADD COLUMN hacked INT;"
        };

        // Combined/complex injections
        var combinedInjections = new[]
        {
            "'; DROP TABLE users; --",
            "1 OR 1=1; DELETE FROM users; --",
            "admin'/**/; EXEC xp_cmdshell('dir'); --",
            "UNION SELECT * FROM passwords; DROP TABLE users; --",
            "'; TRUNCATE TABLE users; /* cleanup */",
            "x'; ALTER TABLE users DROP COLUMN password; --"
        };

        var allInjections = dropInjections
            .Concat(deleteInjections)
            .Concat(execInjections)
            .Concat(commentInjections)
            .Concat(blockCommentInjections)
            .Concat(semicolonInjections)
            .Concat(combinedInjections)
            .ToArray();

        return Gen.Elements(allInjections)
            .Select(s => new SqlInjectionAttemptWrapper(s))
            .ToArbitrary();
    }

    /// <summary>
    /// Generates safe SQL fragment strings that should be accepted.
    /// </summary>
    public static Arbitrary<SafeFragmentWrapper> SafeFragmentWrapper()
    {
        var safeFragments = new[]
        {
            "age > 18",
            "status = 'active'",
            "age > 18 AND status = 'active'",
            "name LIKE '%John%'",
            "created_at >= '2024-01-01'",
            "id IN (1, 2, 3)",
            "price BETWEEN 10 AND 100",
            "category = 'electronics' OR category = 'books'",
            "is_active = 1",
            "user_id = @userId",
            "name IS NOT NULL",
            "quantity > 0 AND price < 1000"
        };

        return Gen.Elements(safeFragments)
            .Select(s => new SafeFragmentWrapper(s))
            .ToArbitrary();
    }

    /// <summary>
    /// Generates safe SQL identifier strings that should be accepted.
    /// </summary>
    public static Arbitrary<SafeIdentifierWrapper> SafeIdentifierWrapper()
    {
        var firstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_".ToCharArray();
        var restChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

        var gen = from size in Gen.Choose(1, 30)
                  from first in Gen.Elements(firstChars)
                  from rest in Gen.ArrayOf(size - 1, Gen.Elements(restChars))
                  select new SafeIdentifierWrapper(first + new string(rest));

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates DynamicSqlType enum values for testing.
    /// </summary>
    public static Arbitrary<DynamicSqlType> DynamicSqlType()
    {
        return Gen.Elements(
            Sqlx.Annotations.DynamicSqlType.Identifier,
            Sqlx.Annotations.DynamicSqlType.Fragment,
            Sqlx.Annotations.DynamicSqlType.TablePart
        ).ToArbitrary();
    }
}
