// -----------------------------------------------------------------------
// <copyright file="CodeValidator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SqlxMigration.Commands;

/// <summary>
/// Validates migrated Sqlx code for correctness and best practices.
/// </summary>
public class CodeValidator
{
    private readonly ILogger _logger;
    private readonly List<ValidationIssue> _issues = new();

    public CodeValidator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates the migrated code for common issues and best practices.
    /// </summary>
    /// <param name="projectPath">Path to the project or solution.</param>
    /// <param name="strict">Whether to use strict validation mode.</param>
    public async Task ValidateAsync(string projectPath, bool strict)
    {
        _logger.LogInformation("🔍 Validating Sqlx code: {Path}", projectPath);
        _logger.LogInformation("📊 Strict mode: {Strict}", strict);

        try
        {
            _issues.Clear();

            var sourceFiles = GetSourceFiles(projectPath);
            foreach (var file in sourceFiles)
            {
                await ValidateFileAsync(file, strict);
            }

            PrintValidationResults();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Validation failed");
            throw;
        }
    }

    private async Task ValidateFileAsync(string filePath, bool strict)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(content);
        var root = syntaxTree.GetRoot();

        // Check for Sqlx-specific issues
        ValidateSqlxUsage(root, filePath);
        ValidateRepositoryPatterns(root, filePath);
        ValidateSqlSyntax(root, filePath, strict);
        ValidatePerformanceIssues(root, filePath, strict);
        ValidateSecurityIssues(root, filePath);
        ValidateBestPractices(root, filePath, strict);
    }

    private void ValidateSqlxUsage(SyntaxNode root, string filePath)
    {
        // Check for proper using statements
        var hasNecessaryUsings = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name?.ToString().Contains("Sqlx.Annotations") == true);

        var hasSqlxAttributes = root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Any(a => a.Name.ToString().Contains("Sqlx") || 
                     a.Name.ToString().Contains("SqlExecuteType") ||
                     a.Name.ToString().Contains("RepositoryFor"));

        if (hasSqlxAttributes && !hasNecessaryUsings)
        {
            AddIssue(ValidationSeverity.Error, filePath, 1, 
                "Missing 'using Sqlx.Annotations;' statement", 
                "Add 'using Sqlx.Annotations;' to the top of the file");
        }

        // Check for repository interface implementations
        var repositoryInterfaces = root.DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
            .Where(i => i.Identifier.ValueText.EndsWith("Repository"));

        foreach (var repoInterface in repositoryInterfaces)
        {
            var hasRepositoryForAttribute = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Any(c => c.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString().Contains("RepositoryFor") &&
                             a.ArgumentList?.Arguments.Any(arg => 
                                 arg.Expression.ToString().Contains(repoInterface.Identifier.ValueText)) == true));

            if (!hasRepositoryForAttribute)
            {
                AddIssue(ValidationSeverity.Warning, filePath, 
                    repoInterface.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    $"Repository interface {repoInterface.Identifier.ValueText} has no implementation",
                    $"Create a class with [RepositoryFor(typeof({repoInterface.Identifier.ValueText}))] attribute");
            }
        }
    }

    private void ValidateRepositoryPatterns(SyntaxNode root, string filePath)
    {
        var repositoryClasses = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("RepositoryFor")));

        foreach (var repoClass in repositoryClasses)
        {
            // Check for proper connection field
            var hasConnectionField = repoClass.Members
                .OfType<FieldDeclarationSyntax>()
                .Any(f => f.Declaration.Variables.Any(v => 
                    v.Identifier.ValueText.Contains("connection") &&
                    f.Declaration.Type.ToString().Contains("DbConnection")));

            if (!hasConnectionField)
            {
                AddIssue(ValidationSeverity.Error, filePath,
                    repoClass.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    $"Repository class {repoClass.Identifier.ValueText} missing DbConnection field",
                    "Add 'private readonly DbConnection connection;' field");
            }

            // Check for proper SqlDefine attribute
            var hasSqlDefineAttribute = repoClass.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("SqlDefine"));

            if (!hasSqlDefineAttribute)
            {
                AddIssue(ValidationSeverity.Warning, filePath,
                    repoClass.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    $"Repository class {repoClass.Identifier.ValueText} missing SqlDefine attribute",
                    "Add [SqlDefine(SqlDefineTypes.YourDatabase)] attribute to specify database dialect");
            }

            // Check for partial class modifier
            if (!repoClass.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                AddIssue(ValidationSeverity.Error, filePath,
                    repoClass.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    $"Repository class {repoClass.Identifier.ValueText} must be partial",
                    "Add 'partial' modifier to the class declaration");
            }
        }
    }

    private void ValidateSqlSyntax(SyntaxNode root, string filePath, bool strict)
    {
        var sqlxAttributes = root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(a => a.Name.ToString().Contains("Sqlx"));

        foreach (var attribute in sqlxAttributes)
        {
            var sqlString = ExtractSqlFromAttribute(attribute);
            if (!string.IsNullOrEmpty(sqlString))
            {
                ValidateSqlString(sqlString, filePath, 
                    attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1, strict);
            }
        }
    }

    private void ValidateSqlString(string sql, string filePath, int lineNumber, bool strict)
    {
        // Basic SQL validation
        if (string.IsNullOrWhiteSpace(sql))
        {
            AddIssue(ValidationSeverity.Error, filePath, lineNumber,
                "Empty SQL string", "Provide a valid SQL statement");
            return;
        }

        var upperSql = sql.ToUpperInvariant().Trim();

        // Check SQL structure
        var validStarters = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "WITH" };
        if (!validStarters.Any(starter => upperSql.StartsWith(starter)))
        {
            AddIssue(ValidationSeverity.Error, filePath, lineNumber,
                "Invalid SQL statement", "SQL should start with SELECT, INSERT, UPDATE, DELETE, or WITH");
        }

        // Check for unmatched quotes
        if (sql.Count(c => c == '\'') % 2 != 0)
        {
            AddIssue(ValidationSeverity.Error, filePath, lineNumber,
                "Unmatched single quotes in SQL", "Ensure all single quotes are properly paired");
        }

        // Check for unmatched parentheses
        if (sql.Count(c => c == '(') != sql.Count(c => c == ')'))
        {
            AddIssue(ValidationSeverity.Error, filePath, lineNumber,
                "Unmatched parentheses in SQL", "Ensure all parentheses are properly paired");
        }

        if (strict)
        {
            ValidateStrictSqlRules(sql, filePath, lineNumber);
        }
    }

    private void ValidateStrictSqlRules(string sql, string filePath, int lineNumber)
    {
        var upperSql = sql.ToUpperInvariant();

        // Check for SELECT *
        if (upperSql.Contains("SELECT *"))
        {
            AddIssue(ValidationSeverity.Warning, filePath, lineNumber,
                "SELECT * usage detected", "Consider specifying column names for better performance");
        }

        // Check for missing WHERE in UPDATE/DELETE
        if ((upperSql.Contains("UPDATE ") || upperSql.Contains("DELETE ")) && !upperSql.Contains("WHERE"))
        {
            AddIssue(ValidationSeverity.Warning, filePath, lineNumber,
                "UPDATE/DELETE without WHERE clause", "Consider adding WHERE clause to avoid affecting all rows");
        }
    }

    private void ValidatePerformanceIssues(SyntaxNode root, string filePath, bool strict)
    {
        // Check for potential N+1 query patterns
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var sqlxAttributes = method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(a => a.Name.ToString().Contains("Sqlx"));

            if (sqlxAttributes.Count() > 1)
            {
                AddIssue(ValidationSeverity.Info, filePath,
                    method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    $"Method {method.Identifier.ValueText} has multiple Sqlx attributes",
                    "Consider consolidating SQL operations for better performance");
            }
        }
    }

    private void ValidateSecurityIssues(SyntaxNode root, string filePath)
    {
        var sqlxAttributes = root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(a => a.Name.ToString().Contains("Sqlx"));

        foreach (var attribute in sqlxAttributes)
        {
            var sqlString = ExtractSqlFromAttribute(attribute);
            if (!string.IsNullOrEmpty(sqlString))
            {
                // Check for potential SQL injection vulnerabilities
                if (sqlString.Contains("+") || sqlString.Contains("EXEC") || sqlString.Contains("xp_"))
                {
                    AddIssue(ValidationSeverity.Warning, filePath,
                        attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        "Potential security issue in SQL", "Use parameterized queries to prevent SQL injection");
                }
            }
        }
    }

    private void ValidateBestPractices(SyntaxNode root, string filePath, bool strict)
    {
        // Check for async method naming
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var isAsync = method.ReturnType.ToString().Contains("Task");
            var hasAsyncSuffix = method.Identifier.ValueText.EndsWith("Async");

            if (isAsync && !hasAsyncSuffix && strict)
            {
                AddIssue(ValidationSeverity.Info, filePath,
                    method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    $"Async method {method.Identifier.ValueText} should end with 'Async'",
                    "Follow async naming conventions");
            }

            if (!isAsync && hasAsyncSuffix)
            {
                AddIssue(ValidationSeverity.Warning, filePath,
                    method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    $"Method {method.Identifier.ValueText} has 'Async' suffix but is not async",
                    "Remove 'Async' suffix or make the method async");
            }
        }
    }

    private string? ExtractSqlFromAttribute(AttributeSyntax attribute)
    {
        var argumentList = attribute.ArgumentList;
        if (argumentList?.Arguments.Count > 0)
        {
            var firstArgument = argumentList.Arguments[0];
            if (firstArgument.Expression is LiteralExpressionSyntax literal)
            {
                return literal.Token.ValueText;
            }
        }
        return null;
    }

    private void AddIssue(ValidationSeverity severity, string filePath, int lineNumber, string message, string suggestion)
    {
        _issues.Add(new ValidationIssue
        {
            Severity = severity,
            FilePath = filePath,
            LineNumber = lineNumber,
            Message = message,
            Suggestion = suggestion
        });
    }

    private string[] GetSourceFiles(string projectPath)
    {
        if (Path.GetExtension(projectPath).Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var solutionDir = Path.GetDirectoryName(projectPath)!;
            return Directory.GetFiles(solutionDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToArray();
        }
        else
        {
            var projectDir = Path.GetDirectoryName(projectPath)!;
            return Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToArray();
        }
    }

    private void PrintValidationResults()
    {
        var errors = _issues.Where(i => i.Severity == ValidationSeverity.Error).ToArray();
        var warnings = _issues.Where(i => i.Severity == ValidationSeverity.Warning).ToArray();
        var infos = _issues.Where(i => i.Severity == ValidationSeverity.Info).ToArray();

        _logger.LogInformation("📊 Validation Results:");
        _logger.LogInformation("   ❌ Errors: {Count}", errors.Length);
        _logger.LogInformation("   ⚠️ Warnings: {Count}", warnings.Length);
        _logger.LogInformation("   ℹ️ Info: {Count}", infos.Length);

        if (errors.Any())
        {
            _logger.LogInformation("\n❌ ERRORS:");
            foreach (var error in errors)
            {
                _logger.LogError("   📄 {File}:{Line} - {Message}", 
                    Path.GetFileName(error.FilePath), error.LineNumber, error.Message);
                _logger.LogInformation("      💡 {Suggestion}", error.Suggestion);
            }
        }

        if (warnings.Any())
        {
            _logger.LogInformation("\n⚠️ WARNINGS:");
            foreach (var warning in warnings.Take(10)) // Show first 10 warnings
            {
                _logger.LogWarning("   📄 {File}:{Line} - {Message}", 
                    Path.GetFileName(warning.FilePath), warning.LineNumber, warning.Message);
            }
            if (warnings.Length > 10)
            {
                _logger.LogInformation("   ... and {More} more warnings", warnings.Length - 10);
            }
        }

        if (_issues.Count == 0)
        {
            _logger.LogInformation("✅ No validation issues found! Your Sqlx code looks great! 🎉");
        }
        else if (errors.Length == 0)
        {
            _logger.LogInformation("✅ No critical errors found. Address warnings to improve code quality.");
        }
        else
        {
            _logger.LogInformation("❌ Please fix the errors before proceeding.");
        }
    }
}

/// <summary>
/// Represents a validation issue.
/// </summary>
public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

/// <summary>
/// Severity levels for validation issues.
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}
