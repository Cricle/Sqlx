// -----------------------------------------------------------------------
// <copyright file="CodeAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SqlxMigration.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SqlxMigration.Core;

/// <summary>
/// Analyzes existing code to identify migration opportunities.
/// </summary>
public class CodeAnalyzer
{
    private readonly ILogger _logger;
    private static readonly Regex SqlStringRegex = new(@"""([^""]*(?:SELECT|INSERT|UPDATE|DELETE)[^""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CodeAnalyzer(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes the project or solution for migration opportunities.
    /// </summary>
    /// <param name="path">Path to project or solution.</param>
    /// <param name="outputPath">Output file path.</param>
    /// <param name="format">Output format.</param>
    public async Task AnalyzeAsync(string path, string? outputPath, AnalysisFormat format)
    {
        _logger.LogInformation("🔍 Starting analysis of: {Path}", path);

        var analysisResult = new AnalysisResult
        {
            ProjectPath = path,
            AnalyzedAt = DateTime.UtcNow
        };

        try
        {
            if (Path.GetExtension(path).Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                await AnalyzeSolutionAsync(path, analysisResult);
            }
            else
            {
                await AnalyzeProjectAsync(path, analysisResult);
            }

            await OutputResultAsync(analysisResult, outputPath, format);
            
            PrintSummary(analysisResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Analysis failed");
            throw;
        }
    }

    private async Task AnalyzeSolutionAsync(string solutionPath, AnalysisResult result)
    {
        _logger.LogInformation("📁 Analyzing solution: {Path}", solutionPath);
        
        // For now, analyze each project individually
        // In a real implementation, you'd use MSBuild APIs to parse the solution
        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);

        foreach (var projectFile in projectFiles)
        {
            await AnalyzeProjectAsync(projectFile, result);
        }
    }

    private async Task AnalyzeProjectAsync(string projectPath, AnalysisResult result)
    {
        _logger.LogInformation("📄 Analyzing project: {Path}", Path.GetFileName(projectPath));

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var sourceFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToArray();

        var projectAnalysis = new ProjectAnalysis
        {
            ProjectPath = projectPath,
            ProjectName = Path.GetFileNameWithoutExtension(projectPath)
        };

        foreach (var sourceFile in sourceFiles)
        {
            await AnalyzeSourceFileAsync(sourceFile, projectAnalysis);
        }

        result.Projects.Add(projectAnalysis);
    }

    private async Task AnalyzeSourceFileAsync(string filePath, ProjectAnalysis projectAnalysis)
    {
        var sourceCode = await File.ReadAllTextAsync(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetRoot();

        var fileAnalysis = new FileAnalysis
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };

        // Analyze for Dapper usage
        AnalyzeDapperUsage(root, fileAnalysis);

        // Analyze for EF Core usage
        AnalyzeEntityFrameworkUsage(root, fileAnalysis);

        // Find SQL strings
        AnalyzeSqlStrings(sourceCode, fileAnalysis);

        // Find repository patterns
        AnalyzeRepositoryPatterns(root, fileAnalysis);

        if (fileAnalysis.HasMigrationOpportunities)
        {
            projectAnalysis.Files.Add(fileAnalysis);
        }
    }

    private void AnalyzeDapperUsage(SyntaxNode root, FileAnalysis fileAnalysis)
    {
        // Find using statements for Dapper
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name?.ToString().Contains("Dapper") == true);

        foreach (var usingDirective in usingDirectives)
        {
            fileAnalysis.DapperUsages.Add(new DapperUsage
            {
                Type = DapperUsageType.UsingStatement,
                LineNumber = usingDirective.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Code = usingDirective.ToString().Trim()
            });
        }

        // Find method calls like Query, QueryAsync, Execute, etc.
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess?.Name?.Identifier.ValueText is string methodName)
            {
                if (IsDapperMethod(methodName))
                {
                    fileAnalysis.DapperUsages.Add(new DapperUsage
                    {
                        Type = GetDapperUsageType(methodName),
                        MethodName = methodName,
                        LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Code = invocation.ToString().Trim(),
                        SqlCode = ExtractSqlFromInvocation(invocation)
                    });
                }
            }
        }
    }

    private void AnalyzeEntityFrameworkUsage(SyntaxNode root, FileAnalysis fileAnalysis)
    {
        // Find DbContext inheritance
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classDeclarations)
        {
            if (classDecl.BaseList?.Types.Any(t => t.Type.ToString().Contains("DbContext")) == true)
            {
                fileAnalysis.EntityFrameworkUsages.Add(new EntityFrameworkUsage
                {
                    Type = EFUsageType.DbContext,
                    ClassName = classDecl.Identifier.ValueText,
                    LineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Code = classDecl.Identifier.ValueText
                });
            }
        }

        // Find DbSet properties
        var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();

        foreach (var property in properties)
        {
            if (property.Type.ToString().Contains("DbSet"))
            {
                fileAnalysis.EntityFrameworkUsages.Add(new EntityFrameworkUsage
                {
                    Type = EFUsageType.DbSet,
                    PropertyName = property.Identifier.ValueText,
                    LineNumber = property.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Code = property.ToString().Trim()
                });
            }
        }

        // Find LINQ queries
        var queryExpressions = root.DescendantNodes().OfType<QueryExpressionSyntax>();

        foreach (var query in queryExpressions)
        {
            fileAnalysis.EntityFrameworkUsages.Add(new EntityFrameworkUsage
            {
                Type = EFUsageType.LinqQuery,
                LineNumber = query.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Code = query.ToString().Trim()
            });
        }
    }

    private void AnalyzeSqlStrings(string sourceCode, FileAnalysis fileAnalysis)
    {
        var matches = SqlStringRegex.Matches(sourceCode);
        var lines = sourceCode.Split('\n');

        foreach (Match match in matches)
        {
            var lineNumber = sourceCode.Take(match.Index).Count(c => c == '\n') + 1;
            
            fileAnalysis.SqlStrings.Add(new SqlStringUsage
            {
                SqlCode = match.Groups[1].Value,
                LineNumber = lineNumber,
                OperationType = DetermineSqlOperation(match.Groups[1].Value)
            });
        }
    }

    private void AnalyzeRepositoryPatterns(SyntaxNode root, FileAnalysis fileAnalysis)
    {
        var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
            .Where(i => i.Identifier.ValueText.EndsWith("Repository") || 
                       i.Identifier.ValueText.EndsWith("Service"));

        foreach (var interfaceDecl in interfaces)
        {
            var methods = interfaceDecl.Members.OfType<MethodDeclarationSyntax>().ToArray();
            
            if (methods.Any())
            {
                fileAnalysis.RepositoryPatterns.Add(new RepositoryPattern
                {
                    InterfaceName = interfaceDecl.Identifier.ValueText,
                    LineNumber = interfaceDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    MethodCount = methods.Length,
                    Methods = methods.Select(m => m.Identifier.ValueText).ToArray()
                });
            }
        }
    }

    private static bool IsDapperMethod(string methodName)
    {
        return methodName is "Query" or "QueryAsync" or "QueryFirst" or "QueryFirstAsync" or 
               "QuerySingle" or "QuerySingleAsync" or "Execute" or "ExecuteAsync" or
               "QueryMultiple" or "QueryMultipleAsync";
    }

    private static DapperUsageType GetDapperUsageType(string methodName)
    {
        return methodName switch
        {
            "Query" or "QueryAsync" or "QueryFirst" or "QueryFirstAsync" or "QuerySingle" or "QuerySingleAsync" => DapperUsageType.Query,
            "Execute" or "ExecuteAsync" => DapperUsageType.Execute,
            "QueryMultiple" or "QueryMultipleAsync" => DapperUsageType.QueryMultiple,
            _ => DapperUsageType.Other
        };
    }

    private static string? ExtractSqlFromInvocation(InvocationExpressionSyntax invocation)
    {
        var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (firstArg?.Expression is LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText;
        }
        return null;
    }

    private static SqlOperationType DetermineSqlOperation(string sql)
    {
        var upperSql = sql.ToUpperInvariant().Trim();
        return upperSql switch
        {
            var s when s.StartsWith("SELECT") => SqlOperationType.Select,
            var s when s.StartsWith("INSERT") => SqlOperationType.Insert,
            var s when s.StartsWith("UPDATE") => SqlOperationType.Update,
            var s when s.StartsWith("DELETE") => SqlOperationType.Delete,
            _ => SqlOperationType.Other
        };
    }

    private async Task OutputResultAsync(AnalysisResult result, string? outputPath, AnalysisFormat format)
    {
        string output = format switch
        {
            AnalysisFormat.Json => JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
            AnalysisFormat.Console => FormatConsoleOutput(result),
            _ => FormatConsoleOutput(result)
        };

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, output);
            _logger.LogInformation("📄 Analysis report saved to: {Path}", outputPath);
        }
        else
        {
            Console.WriteLine(output);
        }
    }

    private string FormatConsoleOutput(AnalysisResult result)
    {
        var output = new System.Text.StringBuilder();
        
        output.AppendLine("📊 MIGRATION ANALYSIS REPORT");
        output.AppendLine("============================");
        output.AppendLine($"Project: {result.ProjectPath}");
        output.AppendLine($"Analyzed: {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
        output.AppendLine();

        foreach (var project in result.Projects)
        {
            output.AppendLine($"📁 Project: {project.ProjectName}");
            output.AppendLine($"   Files with migration opportunities: {project.Files.Count}");
            
            var totalDapper = project.Files.Sum(f => f.DapperUsages.Count);
            var totalEF = project.Files.Sum(f => f.EntityFrameworkUsages.Count);
            var totalSql = project.Files.Sum(f => f.SqlStrings.Count);
            var totalRepos = project.Files.Sum(f => f.RepositoryPatterns.Count);

            output.AppendLine($"   🔧 Dapper usages: {totalDapper}");
            output.AppendLine($"   🏗️ EF Core usages: {totalEF}");
            output.AppendLine($"   📝 SQL strings: {totalSql}");
            output.AppendLine($"   🏛️ Repository patterns: {totalRepos}");
            output.AppendLine();
        }

        return output.ToString();
    }

    private void PrintSummary(AnalysisResult result)
    {
        var totalFiles = result.Projects.Sum(p => p.Files.Count);
        var totalDapper = result.Projects.SelectMany(p => p.Files).Sum(f => f.DapperUsages.Count);
        var totalEF = result.Projects.SelectMany(p => p.Files).Sum(f => f.EntityFrameworkUsages.Count);

        _logger.LogInformation("✅ Analysis completed");
        _logger.LogInformation("📊 Summary: {Files} files, {Dapper} Dapper usages, {EF} EF usages", 
            totalFiles, totalDapper, totalEF);

        if (totalDapper > 0 || totalEF > 0)
        {
            _logger.LogInformation("💡 Run 'sqlx-migrate migrate' to start the migration process");
        }
        else
        {
            _logger.LogInformation("✨ No migration opportunities found");
        }
    }
}
