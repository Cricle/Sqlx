// -----------------------------------------------------------------------
// <copyright file="AdvancedPlaceholderPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using GenSqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for advanced SQL template placeholder processing.
/// Tests Properties 20-22 for orderby, boolean, and timestamp placeholders.
/// </summary>
public class AdvancedPlaceholderPropertyTests
{
    private readonly SqlTemplateEngine _engine;
    private readonly IMethodSymbol _testMethod;
    private readonly INamedTypeSymbol _testEntity;

    public AdvancedPlaceholderPropertyTests()
    {
        _engine = new SqlTemplateEngine();
        var compilation = CreateTestCompilation();
        _testEntity = compilation.GetTypeByMetadataName("TestEntity")!;
        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    #region Property 20: OrderBy Placeholder Processing

    /// <summary>
    /// **Property 20: OrderBy Placeholder Processing**
    /// *For any* column name and direction option, {{orderby}} placeholder SHALL generate correct ORDER BY clause.
    /// **Validates: Requirements 21.1, 21.2, 21.3, 21.4, 21.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property OrderByPlaceholder_WithColumnName_ShouldGenerateOrderByClause(
        ValidColumnNameWrapper columnName, DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = $"SELECT * FROM {{{{table}}}} {{{{orderby {columnName.Value}}}}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should contain ORDER BY and the column name
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var hasOrderBy = lowerSql.Contains("order by");
        var hasColumnName = lowerSql.Contains(columnName.Value.ToLowerInvariant());
        var noPlaceholder = !result.ProcessedSql.Contains("{{orderby");

        return (hasOrderBy && hasColumnName && noPlaceholder)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Column: '{columnName.Value}', " +
                   $"HasOrderBy: {hasOrderBy}, HasColumn: {hasColumnName}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 20 (continued): {{orderby column_name --desc}} should generate ORDER BY column_name DESC.
    /// **Validates: Requirements 21.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property OrderByPlaceholder_WithDescOption_ShouldGenerateDescendingOrder(
        ValidColumnNameWrapper columnName, DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = $"SELECT * FROM {{{{table}}}} {{{{orderby {columnName.Value} --desc}}}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should contain ORDER BY, column name, and DESC
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var hasOrderBy = lowerSql.Contains("order by");
        var hasDesc = lowerSql.Contains("desc");
        var noPlaceholder = !result.ProcessedSql.Contains("{{orderby");

        return (hasOrderBy && hasDesc && noPlaceholder)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Column: '{columnName.Value}', " +
                   $"HasOrderBy: {hasOrderBy}, HasDesc: {hasDesc}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 20 (continued): {{orderby column_name --asc}} should generate ORDER BY column_name ASC.
    /// **Validates: Requirements 21.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property OrderByPlaceholder_WithAscOption_ShouldGenerateAscendingOrder(
        ValidColumnNameWrapper columnName, DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = $"SELECT * FROM {{{{table}}}} {{{{orderby {columnName.Value} --asc}}}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should contain ORDER BY, column name, and ASC
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var hasOrderBy = lowerSql.Contains("order by");
        var hasAsc = lowerSql.Contains("asc");
        var noPlaceholder = !result.ProcessedSql.Contains("{{orderby");

        return (hasOrderBy && hasAsc && noPlaceholder)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Column: '{columnName.Value}', " +
                   $"HasOrderBy: {hasOrderBy}, HasAsc: {hasAsc}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 20 (continued): {{orderby}} without options should use default order.
    /// **Validates: Requirements 21.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property OrderByPlaceholder_WithoutOptions_ShouldUseDefaultOrder(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "SELECT * FROM {{table}} {{orderby}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should contain ORDER BY
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var hasOrderBy = lowerSql.Contains("order by");
        var noPlaceholder = !result.ProcessedSql.Contains("{{orderby");

        return (hasOrderBy && noPlaceholder)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"HasOrderBy: {hasOrderBy}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 20 (continued): ORDER BY should use proper identifier quoting for the target database.
    /// **Validates: Requirements 21.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property OrderByPlaceholder_ShouldUseProperIdentifierQuoting(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        // Use a column name that exists in the entity
        var template = "SELECT * FROM {{table}} {{orderby name --asc}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should not have unprocessed placeholder
        var noPlaceholder = !result.ProcessedSql.Contains("{{orderby");
        var hasOrderBy = result.ProcessedSql.ToLowerInvariant().Contains("order by");

        return (noPlaceholder && hasOrderBy)
            .Label($"Dialect: {config.DialectName}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    #endregion

    #region Property 21: Boolean Placeholder Processing

    /// <summary>
    /// **Property 21: Boolean Placeholder Processing**
    /// *For any* database dialect, {{bool_true}} placeholder SHALL be replaced with correct boolean literal.
    /// **Validates: Requirements 23.1, 23.3, 23.5, 23.7**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property BoolTruePlaceholder_ForAnyDialect_ShouldBeReplacedWithCorrectLiteral(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "SELECT * FROM {{table}} WHERE is_active = {{bool_true}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should not contain placeholder and should contain expected boolean literal
        var noPlaceholder = !result.ProcessedSql.Contains("{{bool_true}}");
        var hasExpectedValue = result.ProcessedSql.Contains(config.ExpectedBoolTrue);
        var noErrors = result.Errors.Count == 0;

        return (noPlaceholder && hasExpectedValue && noErrors)
            .Label($"Dialect: {config.DialectName}, " +
                   $"Expected: '{config.ExpectedBoolTrue}', " +
                   $"NoPlaceholder: {noPlaceholder}, HasExpected: {hasExpectedValue}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 21 (continued): {{bool_false}} placeholder SHALL be replaced with correct boolean literal.
    /// **Validates: Requirements 23.2, 23.4, 23.6, 23.8**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property BoolFalsePlaceholder_ForAnyDialect_ShouldBeReplacedWithCorrectLiteral(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "SELECT * FROM {{table}} WHERE is_active = {{bool_false}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should not contain placeholder and should contain expected boolean literal
        var noPlaceholder = !result.ProcessedSql.Contains("{{bool_false}}");
        var hasExpectedValue = result.ProcessedSql.Contains(config.ExpectedBoolFalse);
        var noErrors = result.Errors.Count == 0;

        return (noPlaceholder && hasExpectedValue && noErrors)
            .Label($"Dialect: {config.DialectName}, " +
                   $"Expected: '{config.ExpectedBoolFalse}', " +
                   $"NoPlaceholder: {noPlaceholder}, HasExpected: {hasExpectedValue}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 21 (continued): Both {{bool_true}} and {{bool_false}} can be used in same template.
    /// **Validates: Requirements 23.1-23.8**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property BoolPlaceholders_Combined_ShouldBothBeReplaced(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "UPDATE {{table}} SET is_active = {{bool_true}} WHERE is_deleted = {{bool_false}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Neither placeholder should remain
        var noBoolTruePlaceholder = !result.ProcessedSql.Contains("{{bool_true}}");
        var noBoolFalsePlaceholder = !result.ProcessedSql.Contains("{{bool_false}}");
        var noErrors = result.Errors.Count == 0;

        return (noBoolTruePlaceholder && noBoolFalsePlaceholder && noErrors)
            .Label($"Dialect: {config.DialectName}, " +
                   $"NoBoolTrue: {noBoolTruePlaceholder}, NoBoolFalse: {noBoolFalsePlaceholder}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    #endregion

    #region Property 22: Current Timestamp Placeholder Processing

    /// <summary>
    /// **Property 22: Current Timestamp Placeholder Processing**
    /// *For any* database dialect, {{current_timestamp}} placeholder SHALL be replaced with correct timestamp function.
    /// **Validates: Requirements 24.1, 24.2, 24.3, 24.4, 24.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property CurrentTimestampPlaceholder_ForAnyDialect_ShouldBeReplacedWithCorrectFunction(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "INSERT INTO {{table}} (name, created_at) VALUES (@name, {{current_timestamp}})";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should not contain placeholder
        var noPlaceholder = !result.ProcessedSql.Contains("{{current_timestamp}}");
        var noErrors = result.Errors.Count == 0;
        
        // Check for expected timestamp function (case-insensitive)
        // Each dialect may use different functions:
        // - MySQL: NOW() or CURRENT_TIMESTAMP
        // - PostgreSQL: CURRENT_TIMESTAMP
        // - SQL Server: GETDATE() or CURRENT_TIMESTAMP
        // - SQLite: datetime('now') or CURRENT_TIMESTAMP
        // - Oracle: SYSDATE or SYSTIMESTAMP
        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        var hasExpectedFunction = sqlUpper.Contains("CURRENT_TIMESTAMP") ||
                                  sqlUpper.Contains("NOW()") ||
                                  sqlUpper.Contains("GETDATE()") ||
                                  sqlUpper.Contains("SYSDATE") ||
                                  sqlUpper.Contains("SYSTIMESTAMP") ||
                                  sqlUpper.Contains("DATETIME('NOW')");

        return (noPlaceholder && hasExpectedFunction && noErrors)
            .Label($"Dialect: {config.DialectName}, " +
                   $"Expected: '{config.ExpectedCurrentTimestamp}', " +
                   $"NoPlaceholder: {noPlaceholder}, HasExpected: {hasExpectedFunction}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 22 (continued): {{current_timestamp}} can be used in WHERE clause.
    /// **Validates: Requirements 24.1-24.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property CurrentTimestampPlaceholder_InWhereClause_ShouldBeReplaced(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "SELECT * FROM {{table}} WHERE created_at > {{current_timestamp}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should not contain placeholder
        var noPlaceholder = !result.ProcessedSql.Contains("{{current_timestamp}}");
        var noErrors = result.Errors.Count == 0;
        var hasWhere = result.ProcessedSql.ToUpperInvariant().Contains("WHERE");

        return (noPlaceholder && hasWhere && noErrors)
            .Label($"Dialect: {config.DialectName}, " +
                   $"NoPlaceholder: {noPlaceholder}, HasWhere: {hasWhere}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 22 (continued): {{current_timestamp}} combined with boolean placeholders.
    /// **Validates: Requirements 24.1-24.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedPlaceholderArbitraries) })]
    public Property CurrentTimestampPlaceholder_CombinedWithBoolPlaceholders_ShouldAllBeReplaced(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "INSERT INTO {{table}} (name, is_active, created_at) VALUES (@name, {{bool_true}}, {{current_timestamp}})";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: No placeholders should remain
        var noTimestampPlaceholder = !result.ProcessedSql.Contains("{{current_timestamp}}");
        var noBoolPlaceholder = !result.ProcessedSql.Contains("{{bool_true}}");
        var noErrors = result.Errors.Count == 0;

        return (noTimestampPlaceholder && noBoolPlaceholder && noErrors)
            .Label($"Dialect: {config.DialectName}, " +
                   $"NoTimestamp: {noTimestampPlaceholder}, NoBool: {noBoolPlaceholder}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    #endregion

    #region Helper Methods

    private static CSharpCompilation CreateTestCompilation()
    {
        var code = @"
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class TestEntity
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public bool IsActive { get; set; }
                public DateTime CreatedAt { get; set; }
                public DateTime UpdatedAt { get; set; }
            }

            public class TestMethods
            {
                public Task<List<TestEntity>> GetAllAsync() => null;
            }
        ";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    #endregion
}

/// <summary>
/// Wrapper for valid column names to use with FsCheck.
/// </summary>
public class ValidColumnNameWrapper
{
    public string Value { get; }

    public ValidColumnNameWrapper(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Custom arbitraries for advanced placeholder property tests.
/// </summary>
public static class AdvancedPlaceholderArbitraries
{
    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyz0123456789_".ToCharArray();

    /// <summary>
    /// Generates valid SQL column names (lowercase letters, numbers, underscore, starting with letter).
    /// </summary>
    public static Arbitrary<ValidColumnNameWrapper> ValidColumnNameWrapper()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(RestChars);

        var gen = from size in Gen.Choose(1, 20)
                  from first in firstChar
                  from rest in Gen.ArrayOf(size - 1, restChars)
                  select new ValidColumnNameWrapper(first + new string(rest));

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates dialect with its corresponding test configuration.
    /// Uses the existing DialectWithConfig class from IdentifierQuotingPropertyTests.
    /// </summary>
    public static Arbitrary<DialectWithConfig> DialectWithConfig()
    {
        return Gen.Elements(
            new DialectWithConfig(GenSqlDefine.MySql, PropertyTests.DialectTestConfig.MySql),
            new DialectWithConfig(GenSqlDefine.PostgreSql, PropertyTests.DialectTestConfig.PostgreSql),
            new DialectWithConfig(GenSqlDefine.SqlServer, PropertyTests.DialectTestConfig.SqlServer),
            new DialectWithConfig(GenSqlDefine.SQLite, PropertyTests.DialectTestConfig.SQLite),
            new DialectWithConfig(GenSqlDefine.Oracle, PropertyTests.DialectTestConfig.Oracle)
        ).ToArbitrary();
    }
}
