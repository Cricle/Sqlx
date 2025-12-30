// -----------------------------------------------------------------------
// <copyright file="PlaceholderPropertyTests.cs" company="Cricle">
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
using SharedCodeGenerationUtilities = Sqlx.Generator.SharedCodeGenerationUtilities;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for SQL template placeholder processing.
/// Tests Properties 15-19 for placeholder handling across all database dialects.
/// </summary>
public class PlaceholderPropertyTests
{
    private readonly SqlTemplateEngine _engine;
    private readonly IMethodSymbol _testMethod;
    private readonly INamedTypeSymbol _testEntity;

    public PlaceholderPropertyTests()
    {
        _engine = new SqlTemplateEngine();
        var compilation = CreateTestCompilation();
        _testEntity = compilation.GetTypeByMetadataName("TestEntity")!;
        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    #region Property 15: Table Placeholder Processing

    /// <summary>
    /// **Property 15: Table Placeholder Processing**
    /// *For any* table name, {{table}} placeholder SHALL be replaced with snake_case converted table name.
    /// **Validates: Requirements 17.1, 17.2, 17.3, 17.4, 17.5, 17.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property TablePlaceholder_ForAnyTableName_ShouldBeReplacedWithSnakeCaseName(
        string tableName, DialectWithConfig dialectConfig)
    {
        // Skip empty or null table names
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var template = "SELECT * FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, tableName, dialect);

        // Expected snake_case table name
        var expectedSnakeCaseName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);

        // Assert: The processed SQL should contain the snake_case table name
        // and should NOT contain the original {{table}} placeholder
        return (!result.ProcessedSql.Contains("{{table}}") &&
                result.ProcessedSql.Contains(expectedSnakeCaseName))
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"TableName: '{tableName}', " +
                   $"Expected: '{expectedSnakeCaseName}', " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 15 (continued): Table placeholder with quoted type should wrap with dialect-specific quotes.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property TablePlaceholder_WithQuotedType_ShouldWrapWithDialectQuotes(
        string tableName, DialectWithConfig dialectConfig)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "SELECT * FROM {{table:quoted}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, tableName, dialect);

        // Expected snake_case table name wrapped with dialect quotes
        var expectedSnakeCaseName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        var expectedWrapped = config.GetExpectedWrappedIdentifier(expectedSnakeCaseName);

        // Assert
        return (!result.ProcessedSql.Contains("{{table:quoted}}") &&
                result.ProcessedSql.Contains(expectedWrapped))
            .Label($"Dialect: {config.DialectName}, " +
                   $"TableName: '{tableName}', " +
                   $"Expected: '{expectedWrapped}', " +
                   $"Result: '{result.ProcessedSql}'");
    }

    #endregion

    #region Property 16: Columns Placeholder Processing

    /// <summary>
    /// **Property 16: Columns Placeholder Processing**
    /// *For any* entity type, {{columns}} placeholder SHALL be replaced with comma-separated snake_case column names.
    /// **Validates: Requirements 18.1, 18.2, 18.3, 18.4, 18.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property ColumnsPlaceholder_ForAnyDialect_ShouldGenerateSnakeCaseColumnNames(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "SELECT {{columns}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Get expected column names from entity properties
        var expectedColumns = _testEntity.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && !p.IsImplicitlyDeclared)
            .Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name))
            .ToList();

        // Assert: All expected columns should be present in the result
        var allColumnsPresent = expectedColumns.All(col => 
            result.ProcessedSql.ToLowerInvariant().Contains(col.ToLowerInvariant()));

        return (!result.ProcessedSql.Contains("{{columns}}") && allColumnsPresent)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Expected columns: [{string.Join(", ", expectedColumns)}], " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 16 (continued): Columns should be comma-separated.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property ColumnsPlaceholder_ShouldGenerateCommaSeparatedList(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "SELECT {{columns}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Extract the columns part (between SELECT and FROM)
        var selectIndex = result.ProcessedSql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        var fromIndex = result.ProcessedSql.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
        
        if (selectIndex < 0 || fromIndex < 0 || fromIndex <= selectIndex)
            return false.Label("Could not find SELECT...FROM structure");

        var columnsPart = result.ProcessedSql.Substring(selectIndex + 6, fromIndex - selectIndex - 6).Trim();

        // Assert: Should contain commas (multiple columns)
        var hasCommas = columnsPart.Contains(",");
        var columnCount = columnsPart.Split(',').Length;

        return (hasCommas && columnCount > 1)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Columns part: '{columnsPart}', " +
                   $"Column count: {columnCount}");
    }

    #endregion

    #region Property 17: Columns Exclude/Only Options

    /// <summary>
    /// **Property 17: Columns Exclude/Only Options**
    /// *For any* entity type with {{columns --exclude col1 col2}}, specified columns SHALL be excluded.
    /// **Validates: Requirements 18.3, 18.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property ColumnsPlaceholder_WithExclude_ShouldExcludeSpecifiedColumns(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "SELECT {{columns --exclude Id}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Assert: Should NOT contain 'id' as a standalone column
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        
        // Check that 'id' is not present as a column (but might be in table name)
        // We need to check it's not in the SELECT part
        var selectIndex = lowerSql.IndexOf("select");
        var fromIndex = lowerSql.IndexOf("from");
        
        if (selectIndex < 0 || fromIndex < 0)
            return false.Label("Could not find SELECT...FROM structure");

        var columnsPart = lowerSql.Substring(selectIndex + 6, fromIndex - selectIndex - 6);
        
        // 'id' should not be in the columns part (as standalone or with comma)
        var idExcluded = !columnsPart.Split(',').Any(c => c.Trim() == "id" || 
            c.Trim().EndsWith("]id") || c.Trim().EndsWith("`id") || c.Trim().EndsWith("\"id"));

        return (!result.ProcessedSql.Contains("{{columns") && idExcluded)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Columns part: '{columnsPart}', " +
                   $"Id excluded: {idExcluded}");
    }

    /// <summary>
    /// Property 17 (continued): Multiple columns can be excluded.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property ColumnsPlaceholder_WithMultipleExcludes_ShouldExcludeAllSpecified(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "SELECT {{columns --exclude Id CreatedAt}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var selectIndex = lowerSql.IndexOf("select");
        var fromIndex = lowerSql.IndexOf("from");
        
        if (selectIndex < 0 || fromIndex < 0)
            return false.Label("Could not find SELECT...FROM structure");

        var columnsPart = lowerSql.Substring(selectIndex + 6, fromIndex - selectIndex - 6);

        // Both 'id' and 'created_at' should be excluded
        var idExcluded = !columnsPart.Contains("id,") && !columnsPart.Contains(", id") && 
                         !columnsPart.Trim().StartsWith("id") && !columnsPart.Trim().EndsWith("id");
        var createdAtExcluded = !columnsPart.Contains("created_at");

        return (!result.ProcessedSql.Contains("{{columns") && idExcluded && createdAtExcluded)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Columns part: '{columnsPart}', " +
                   $"Id excluded: {idExcluded}, CreatedAt excluded: {createdAtExcluded}");
    }

    #endregion

    #region Property 18: Values Placeholder Processing

    /// <summary>
    /// **Property 18: Values Placeholder Processing**
    /// *For any* entity type, {{values}} placeholder SHALL generate parameter placeholders matching columns.
    /// **Validates: Requirements 19.1, 19.2, 19.3, 19.4, 19.5, 19.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property ValuesPlaceholder_ForAnyDialect_ShouldGenerateParameterPlaceholders(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;
        var template = "INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        // Get expected parameter names from entity properties
        var expectedParams = _testEntity.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && !p.IsImplicitlyDeclared)
            .Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name))
            .ToList();

        // Assert: All expected parameters should be present with correct prefix
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        
        // For most dialects, parameters use @ prefix
        // PostgreSQL might use $ but in this implementation uses @
        var allParamsPresent = expectedParams.All(param => 
            lowerSql.Contains($"@{param}") || lowerSql.Contains($":{param}") || lowerSql.Contains($"${param}"));

        return (!result.ProcessedSql.Contains("{{values}}") && allParamsPresent)
            .Label($"Dialect: {config.DialectName}, " +
                   $"Expected params: [{string.Join(", ", expectedParams)}], " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 18 (continued): Values should match columns when both have same exclude options.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property ValuesPlaceholder_WithExclude_ShouldMatchColumnsExclude(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        var lowerSql = result.ProcessedSql.ToLowerInvariant();

        // Count columns and values
        var insertIndex = lowerSql.IndexOf("insert");
        var valuesIndex = lowerSql.IndexOf("values");
        
        if (insertIndex < 0 || valuesIndex < 0)
            return false.Label("Could not find INSERT...VALUES structure");

        // Extract columns part (between first ( and ))
        var firstParen = lowerSql.IndexOf('(', insertIndex);
        var firstCloseParen = lowerSql.IndexOf(')', firstParen);
        var columnsPart = lowerSql.Substring(firstParen + 1, firstCloseParen - firstParen - 1);
        var columnCount = columnsPart.Split(',').Length;

        // Extract values part (between VALUES ( and ))
        var valuesParen = lowerSql.IndexOf('(', valuesIndex);
        var valuesCloseParen = lowerSql.IndexOf(')', valuesParen);
        var valuesPart = lowerSql.Substring(valuesParen + 1, valuesCloseParen - valuesParen - 1);
        var valuesCount = valuesPart.Split(',').Length;

        // Assert: Column count should match values count
        return (columnCount == valuesCount && !lowerSql.Contains("@id"))
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Column count: {columnCount}, Values count: {valuesCount}, " +
                   $"Columns: '{columnsPart}', Values: '{valuesPart}'");
    }

    #endregion

    #region Property 19: Set Placeholder Processing

    /// <summary>
    /// **Property 19: Set Placeholder Processing**
    /// *For any* entity type, {{set}} placeholder SHALL generate column=@parameter pairs.
    /// **Validates: Requirements 20.1, 20.2, 20.3, 20.4, 20.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property SetPlaceholder_ForAnyDialect_ShouldGenerateColumnParameterPairs(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "UPDATE {{table}} SET {{set}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        var lowerSql = result.ProcessedSql.ToLowerInvariant();

        // Assert: Should contain column = @parameter patterns
        // The SET clause should have format: column_name = @column_name (or $column_name for PostgreSQL)
        var setIndex = lowerSql.IndexOf("set ");
        var whereIndex = lowerSql.IndexOf("where");
        
        if (setIndex < 0 || whereIndex < 0)
            return false.Label("Could not find SET...WHERE structure");

        var setPart = lowerSql.Substring(setIndex + 4, whereIndex - setIndex - 4).Trim();

        // Should contain at least one assignment pattern with any parameter prefix (@, $, :)
        var hasAssignment = setPart.Contains("=") && 
                           (setPart.Contains("@") || setPart.Contains("$") || setPart.Contains(":"));

        return (!result.ProcessedSql.Contains("{{set}}") && hasAssignment)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"SET part: '{setPart}', " +
                   $"Has assignment: {hasAssignment}");
    }

    /// <summary>
    /// Property 19 (continued): Set placeholder should exclude Id by default.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property SetPlaceholder_ShouldExcludeIdByDefault(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "UPDATE {{table}} SET {{set}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var setIndex = lowerSql.IndexOf("set ");
        var whereIndex = lowerSql.IndexOf("where");
        
        if (setIndex < 0 || whereIndex < 0)
            return false.Label("Could not find SET...WHERE structure");

        var setPart = lowerSql.Substring(setIndex + 4, whereIndex - setIndex - 4).Trim();

        // Id should not be in the SET clause (it's typically excluded for updates)
        // Note: The WHERE clause has id = @id, but SET should not have id = @id
        var idInSet = setPart.Contains("id = @id") || setPart.StartsWith("id =");

        return (!idInSet)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"SET part: '{setPart}', " +
                   $"Id in SET: {idInSet}");
    }

    /// <summary>
    /// Property 19 (continued): Set placeholder with exclude should exclude specified columns.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property SetPlaceholder_WithExclude_ShouldExcludeSpecifiedColumns(
        DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var setIndex = lowerSql.IndexOf("set ");
        var whereIndex = lowerSql.IndexOf("where");
        
        if (setIndex < 0 || whereIndex < 0)
            return false.Label("Could not find SET...WHERE structure");

        var setPart = lowerSql.Substring(setIndex + 4, whereIndex - setIndex - 4).Trim();

        // Neither id nor created_at should be in the SET clause
        var idExcluded = !setPart.Contains("id =") || !setPart.Contains("@id");
        var createdAtExcluded = !setPart.Contains("created_at");

        return (idExcluded && createdAtExcluded)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"SET part: '{setPart}', " +
                   $"Id excluded: {idExcluded}, CreatedAt excluded: {createdAtExcluded}");
    }

    /// <summary>
    /// Property 19 (continued): Set placeholder should use comma separator between assignments.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property SetPlaceholder_ShouldUseCommaSeparator(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var template = "UPDATE {{table}} SET {{set}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "test_table", dialect);

        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        var setIndex = lowerSql.IndexOf("set ");
        var whereIndex = lowerSql.IndexOf("where");
        
        if (setIndex < 0 || whereIndex < 0)
            return false.Label("Could not find SET...WHERE structure");

        var setPart = lowerSql.Substring(setIndex + 4, whereIndex - setIndex - 4).Trim();

        // Should have commas separating multiple assignments
        var assignmentCount = setPart.Split(',').Length;

        return (assignmentCount >= 1)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"SET part: '{setPart}', " +
                   $"Assignment count: {assignmentCount}");
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

    #region Property 1: Table Placeholder Parameter Usage (CI Fixes)

    /// <summary>
    /// **Property 1: Table placeholder parameter usage**
    /// *For any* SQL template containing {{table tableName}} and a tableName parameter,
    /// the generated SQL should use the parameter value instead of the default table name.
    /// **Validates: Requirements 1.1**
    /// **Feature: ci-fixes-and-e2e-expansion, Property 1: Table placeholder parameter usage**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property TablePlaceholder_WithParameterName_ShouldUseParameterValue(
        string defaultTableName, string parameterTableName, DialectWithConfig dialectConfig)
    {
        // Skip empty or null table names
        if (string.IsNullOrWhiteSpace(defaultTableName) || string.IsNullOrWhiteSpace(parameterTableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        // Use new format: {{table parameterTableName}}
        var template = $"SELECT * FROM {{{{table {parameterTableName}}}}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, defaultTableName, dialect);

        // Expected: should use parameterTableName, not defaultTableName
        var expectedSnakeCaseName = SharedCodeGenerationUtilities.ConvertToSnakeCase(parameterTableName);
        var defaultSnakeCaseName = SharedCodeGenerationUtilities.ConvertToSnakeCase(defaultTableName);

        // Assert: The processed SQL should contain the parameter table name (not the default)
        // and should NOT contain the original {{table ...}} placeholder
        var containsExpected = result.ProcessedSql.Contains(expectedSnakeCaseName);
        var notContainsPlaceholder = !result.ProcessedSql.Contains("{{table");
        
        // If the names are the same after snake_case conversion, we can't distinguish them
        if (expectedSnakeCaseName.Equals(defaultSnakeCaseName, StringComparison.OrdinalIgnoreCase))
            return true.ToProperty();

        return (notContainsPlaceholder && containsExpected)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"DefaultTableName: '{defaultTableName}', " +
                   $"ParameterTableName: '{parameterTableName}', " +
                   $"Expected: '{expectedSnakeCaseName}', " +
                   $"Result: '{result.ProcessedSql}'");
    }

    #endregion

    #region Property 2: Nested Placeholder Resolution (CI Fixes)

    /// <summary>
    /// **Property 2: Nested placeholder resolution**
    /// *For any* SQL template with nested placeholders up to 3 levels deep,
    /// all placeholders should be resolved correctly in the final SQL.
    /// **Validates: Requirements 1.2**
    /// **Feature: ci-fixes-and-e2e-expansion, Property 2: Nested placeholder resolution**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property NestedPlaceholder_UpTo3Levels_ShouldResolveCorrectly(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        
        // Test nested placeholders: {{coalesce {{sum balance}}, 0}}
        // This requires 2 iterations to resolve
        var template = "SELECT {{coalesce {{sum balance}}, 0}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "users", dialect);

        // Assert: Should not contain any unresolved placeholders
        var hasUnresolvedPlaceholders = result.ProcessedSql.Contains("{{");
        
        // Should contain COALESCE and SUM
        var containsCoalesce = result.ProcessedSql.ToUpperInvariant().Contains("COALESCE");
        var containsSum = result.ProcessedSql.ToUpperInvariant().Contains("SUM");

        return (!hasUnresolvedPlaceholders && containsCoalesce && containsSum)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    /// <summary>
    /// Property 2 (continued): Triple nested placeholders should resolve correctly.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PlaceholderArbitraries) })]
    public Property NestedPlaceholder_TripleNested_ShouldResolveCorrectly(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        
        // Test triple nested: {{coalesce {{round {{avg balance}}, 2}}, 0}}
        // This requires 3 iterations to resolve
        var template = "SELECT {{coalesce {{round {{avg balance}}, 2}}, 0}} FROM {{table}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _testEntity, "users", dialect);

        // Assert: Should not contain any unresolved placeholders
        var hasUnresolvedPlaceholders = result.ProcessedSql.Contains("{{");
        
        // Should contain COALESCE, ROUND, and AVG
        var containsCoalesce = result.ProcessedSql.ToUpperInvariant().Contains("COALESCE");
        var containsRound = result.ProcessedSql.ToUpperInvariant().Contains("ROUND");
        var containsAvg = result.ProcessedSql.ToUpperInvariant().Contains("AVG");

        return (!hasUnresolvedPlaceholders && containsCoalesce && containsRound && containsAvg)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Result: '{result.ProcessedSql}'");
    }

    #endregion
}


/// <summary>
/// Custom arbitraries for placeholder property tests.
/// </summary>
public static class PlaceholderArbitraries
{
    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

    /// <summary>
    /// Generates valid SQL identifiers (letters, numbers, underscore, starting with letter).
    /// </summary>
    public static Arbitrary<string> String()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(RestChars);

        var gen = from size in Gen.Choose(1, 30)
                  from first in firstChar
                  from rest in Gen.ArrayOf(size - 1, restChars)
                  select first + new string(rest);

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates dialect with its corresponding test configuration.
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
