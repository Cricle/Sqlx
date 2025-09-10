// -----------------------------------------------------------------------
// <copyright file="SqlxDiagnosticAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.VisualStudio.Diagnostics
{
    /// <summary>
    /// Analyzer for Sqlx-specific diagnostics and code quality issues.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SqlxDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        // Diagnostic descriptors
        public static readonly DiagnosticDescriptor SqlSyntaxError = new DiagnosticDescriptor(
            "SQLX001",
            "Invalid SQL syntax in Sqlx attribute",
            "SQL syntax error: {0}",
            "Sqlx",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The SQL statement contains syntax errors that may cause runtime issues.");

        public static readonly DiagnosticDescriptor ParameterMismatch = new DiagnosticDescriptor(
            "SQLX002",
            "Parameter mismatch in Sqlx SQL",
            "Parameter '{0}' used in SQL but not found in method parameters",
            "Sqlx",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "SQL parameters should match method parameters for type safety.");

        public static readonly DiagnosticDescriptor UnusedParameter = new DiagnosticDescriptor(
            "SQLX003",
            "Unused parameter in Sqlx method",
            "Method parameter '{0}' is not used in the SQL statement",
            "Sqlx",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Consider removing unused parameters to improve code clarity.");

        public static readonly DiagnosticDescriptor PerformanceWarning = new DiagnosticDescriptor(
            "SQLX004",
            "Potential performance issue in SQL",
            "SQL statement may have performance issues: {0}",
            "Sqlx",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Consider optimizing the SQL statement for better performance.");

        public static readonly DiagnosticDescriptor SecurityWarning = new DiagnosticDescriptor(
            "SQLX005",
            "Potential SQL injection vulnerability",
            "Potential SQL injection risk: {0}",
            "Sqlx",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Use parameterized queries to prevent SQL injection attacks.");

        public static readonly DiagnosticDescriptor DialectMismatch = new DiagnosticDescriptor(
            "SQLX006",
            "SQL dialect compatibility issue",
            "SQL syntax may not be compatible with the specified database dialect: {0}",
            "Sqlx",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Ensure SQL syntax is compatible with the target database dialect.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                SqlSyntaxError,
                ParameterMismatch,
                UnusedParameter,
                PerformanceWarning,
                SecurityWarning,
                DialectMismatch);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Find Sqlx attributes
            foreach (var attributeList in methodDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsSqlxAttribute(attribute))
                    {
                        AnalyzeSqlxAttribute(context, methodDeclaration, attribute);
                    }
                }
            }
        }

        private static void AnalyzeInterface(SyntaxNodeAnalysisContext context)
        {
            var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;

            // Check if interface has RepositoryFor attribute
            foreach (var attributeList in interfaceDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsRepositoryForAttribute(attribute))
                    {
                        AnalyzeRepositoryInterface(context, interfaceDeclaration);
                    }
                }
            }
        }

        private static bool IsSqlxAttribute(AttributeSyntax attribute)
        {
            var name = attribute.Name.ToString();
            return name == "Sqlx" || name == "SqlxAttribute" ||
                   name == "SqlExecuteType" || name == "SqlExecuteTypeAttribute";
        }

        private static bool IsRepositoryForAttribute(AttributeSyntax attribute)
        {
            var name = attribute.Name.ToString();
            return name == "RepositoryFor" || name == "RepositoryForAttribute";
        }

        private static void AnalyzeSqlxAttribute(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, AttributeSyntax attribute)
        {
            var sqlText = ExtractSqlFromAttribute(attribute);
            if (string.IsNullOrEmpty(sqlText))
                return;

            var methodParameters = method.ParameterList.Parameters.Select(p => p.Identifier.ValueText).ToArray();

            // Check SQL syntax
            AnalyzeSqlSyntax(context, attribute, sqlText);

            // Check parameter usage
            AnalyzeParameterUsage(context, method, attribute, sqlText, methodParameters);

            // Check for performance issues
            AnalyzePerformanceIssues(context, attribute, sqlText);

            // Check for security issues
            AnalyzeSecurityIssues(context, attribute, sqlText);
        }

        private static void AnalyzeRepositoryInterface(SyntaxNodeAnalysisContext context, InterfaceDeclarationSyntax interfaceDeclaration)
        {
            // Analyze repository interface for common patterns and issues
            var methods = interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                // Check if all methods have appropriate Sqlx attributes
                var hasSqlxAttribute = method.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(IsSqlxAttribute);

                if (!hasSqlxAttribute)
                {
                    var diagnostic = Diagnostic.Create(
                        PerformanceWarning,
                        method.GetLocation(),
                        "Repository method without Sqlx attribute may not be auto-implemented");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static string ExtractSqlFromAttribute(AttributeSyntax attribute)
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

        private static void AnalyzeSqlSyntax(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, string sqlText)
        {
            // Basic SQL syntax validation
            var errors = ValidateSqlSyntax(sqlText);
            foreach (var error in errors)
            {
                var diagnostic = Diagnostic.Create(
                    SqlSyntaxError,
                    attribute.GetLocation(),
                    error);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeParameterUsage(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method,
            AttributeSyntax attribute, string sqlText, string[] methodParameters)
        {
            // Find SQL parameters
            var sqlParameters = ExtractSqlParameters(sqlText);

            // Check for parameters used in SQL but not in method
            foreach (var sqlParam in sqlParameters)
            {
                if (!methodParameters.Any(mp => string.Equals(mp, sqlParam, StringComparison.OrdinalIgnoreCase)))
                {
                    var diagnostic = Diagnostic.Create(
                        ParameterMismatch,
                        attribute.GetLocation(),
                        sqlParam);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Check for unused method parameters
            foreach (var methodParam in methodParameters)
            {
                if (!sqlParameters.Any(sp => string.Equals(sp, methodParam, StringComparison.OrdinalIgnoreCase)))
                {
                    var parameterSyntax = method.ParameterList.Parameters
                        .FirstOrDefault(p => p.Identifier.ValueText == methodParam);

                    if (parameterSyntax != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            UnusedParameter,
                            parameterSyntax.GetLocation(),
                            methodParam);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static void AnalyzePerformanceIssues(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, string sqlText)
        {
            var issues = DetectPerformanceIssues(sqlText);
            foreach (var issue in issues)
            {
                var diagnostic = Diagnostic.Create(
                    PerformanceWarning,
                    attribute.GetLocation(),
                    issue);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeSecurityIssues(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, string sqlText)
        {
            var issues = DetectSecurityIssues(sqlText);
            foreach (var issue in issues)
            {
                var diagnostic = Diagnostic.Create(
                    SecurityWarning,
                    attribute.GetLocation(),
                    issue);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static string[] ValidateSqlSyntax(string sql)
        {
            var errors = new List<string>();

            // Basic syntax checks
            if (string.IsNullOrWhiteSpace(sql))
            {
                errors.Add("Empty SQL statement");
                return errors.ToArray();
            }

            // Check for unmatched quotes
            var singleQuoteCount = sql.Count(c => c == '\'');
            if (singleQuoteCount % 2 != 0)
            {
                errors.Add("Unmatched single quote");
            }

            // Check for unmatched parentheses
            var openParen = sql.Count(c => c == '(');
            var closeParen = sql.Count(c => c == ')');
            if (openParen != closeParen)
            {
                errors.Add("Unmatched parentheses");
            }

            // Check for basic SQL statement structure
            var trimmedSql = sql.Trim().ToUpperInvariant();
            var validStartKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "WITH" };
            if (!validStartKeywords.Any(keyword => trimmedSql.StartsWith(keyword)))
            {
                errors.Add("SQL statement should start with SELECT, INSERT, UPDATE, DELETE, or WITH");
            }

            return errors.ToArray();
        }

        private static string[] ExtractSqlParameters(string sql)
        {
            var parameters = new List<string>();

            // Match @parameter, :parameter, $1, $2, etc., and ? patterns
            var paramRegex = new Regex(@"[@:$]\w+|\$\d+|\?", RegexOptions.IgnoreCase);
            var matches = paramRegex.Matches(sql);

            foreach (Match match in matches)
            {
                var param = match.Value;
                if (param.StartsWith("@") || param.StartsWith(":"))
                {
                    parameters.Add(param.Substring(1)); // Remove the prefix
                }
                else if (param.StartsWith("$") && param.Length > 1)
                {
                    // PostgreSQL style parameters ($1, $2, etc.) - skip for now
                    continue;
                }
                else if (param == "?")
                {
                    // Positional parameters - skip for now
                    continue;
                }
            }

            return parameters.Distinct().ToArray();
        }

        private static string[] DetectPerformanceIssues(string sql)
        {
            var issues = new List<string>();
            var upperSql = sql.ToUpperInvariant();

            // Check for SELECT *
            if (upperSql.Contains("SELECT *"))
            {
                issues.Add("Using SELECT * may impact performance. Consider specifying column names.");
            }

            // Check for missing WHERE clause in UPDATE/DELETE
            if ((upperSql.Contains("UPDATE ") || upperSql.Contains("DELETE ")) && !upperSql.Contains("WHERE"))
            {
                issues.Add("UPDATE/DELETE without WHERE clause affects all rows");
            }

            // Check for LIKE with leading wildcard
            if (Regex.IsMatch(upperSql, @"LIKE\s+['""]%"))
            {
                issues.Add("LIKE with leading wildcard (%) prevents index usage");
            }

            // Check for OR conditions that might need UNION
            var orCount = Regex.Matches(upperSql, @"\bOR\b").Count;
            if (orCount > 2)
            {
                issues.Add("Multiple OR conditions may benefit from UNION for better performance");
            }

            return issues.ToArray();
        }

        private static string[] DetectSecurityIssues(string sql)
        {
            var issues = new List<string>();

            // Check for potential string concatenation (basic check)
            if (sql.Contains("+") && (sql.Contains("'") || sql.Contains("\"")))
            {
                issues.Add("Potential string concatenation in SQL - use parameters instead");
            }

            // Check for dynamic SQL patterns
            if (sql.Contains("EXEC") || sql.Contains("EXECUTE"))
            {
                issues.Add("Dynamic SQL execution detected - ensure proper validation");
            }

            return issues.ToArray();
        }
    }
}

