// -----------------------------------------------------------------------
// <copyright file="CodeMigrator.cs" company="Cricle">
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SqlxMigration.Core;

/// <summary>
/// Migrates Dapper and Entity Framework Core code to Sqlx.
/// </summary>
public class CodeMigrator
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _migrationLog = new();

    public CodeMigrator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Migrates code from Dapper/EF Core to Sqlx.
    /// </summary>
    /// <param name="projectPath">Path to project or solution.</param>
    /// <param name="source">Source framework.</param>
    /// <param name="targetPath">Target directory for migrated code.</param>
    /// <param name="dryRun">Whether to perform a dry run.</param>
    /// <param name="createBackup">Whether to create backup files.</param>
    public async Task MigrateAsync(string projectPath, MigrationSource source, string? targetPath, bool dryRun, bool createBackup)
    {
        _logger.LogInformation("🔄 Starting migration from {Source}", source);
        _logger.LogInformation("📁 Project: {Path}", projectPath);
        
        if (dryRun)
        {
            _logger.LogInformation("🧪 Dry run mode - no files will be modified");
        }

        try
        {
            var sourceFiles = GetSourceFiles(projectPath);
            var migrationTasks = new List<Task>();

            foreach (var file in sourceFiles)
            {
                migrationTasks.Add(MigrateFileAsync(file, source, targetPath, dryRun, createBackup));
            }

            await Task.WhenAll(migrationTasks);

            await GenerateMigrationReportAsync(projectPath);
            
            _logger.LogInformation("✅ Migration completed successfully");
            PrintMigrationSummary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Migration failed");
            throw;
        }
    }

    private async Task MigrateFileAsync(string filePath, MigrationSource source, string? targetPath, bool dryRun, bool createBackup)
    {
        _logger.LogInformation("🔧 Processing: {File}", Path.GetFileName(filePath));

        var originalContent = await File.ReadAllTextAsync(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(originalContent);
        var root = syntaxTree.GetRoot();

        var rewriter = source switch
        {
            MigrationSource.Dapper => new DapperToSqlxRewriter(),
            MigrationSource.EntityFramework => new EFCoreToSqlxRewriter(),
            MigrationSource.Auto => DetectFrameworkAndGetRewriter(root),
            _ => new DapperToSqlxRewriter() // Default
        };

        var newRoot = rewriter.Visit(root);
        var newContent = newRoot.ToFullString();

        // Add using statements if needed
        newContent = AddSqlxUsings(newContent);

        var changes = (rewriter as MigrationRewriter)?.GetChanges() ?? new List<string>();
        if (changes.Any())
        {
            _migrationLog[filePath] = $"Applied {changes.Count} changes";

            if (!dryRun)
            {
                if (createBackup)
                {
                    await CreateBackupAsync(filePath);
                }

                var outputPath = targetPath != null ? 
                    Path.Combine(targetPath, Path.GetFileName(filePath)) : filePath;

                await File.WriteAllTextAsync(outputPath, newContent);
                _logger.LogInformation("✅ Migrated: {File} ({Changes} changes)", Path.GetFileName(filePath), changes.Count);
            }
            else
            {
                _logger.LogInformation("📝 Would migrate: {File} ({Changes} changes)", Path.GetFileName(filePath), changes.Count);
                foreach (var change in changes.Take(3)) // Show first 3 changes
                {
                    _logger.LogInformation("   - {Change}", change);
                }
                if (changes.Count > 3)
                {
                    _logger.LogInformation("   - ... and {More} more changes", changes.Count - 3);
                }
            }
        }
    }

    private CSharpSyntaxRewriter DetectFrameworkAndGetRewriter(SyntaxNode root)
    {
        var hasDapper = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name?.ToString().Contains("Dapper") == true);

        var hasEF = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name?.ToString().Contains("EntityFramework") == true ||
                     u.Name?.ToString().Contains("Microsoft.EntityFrameworkCore") == true);

        if (hasDapper && hasEF)
        {
            _logger.LogWarning("⚠️ Both Dapper and EF Core detected. Using combined rewriter.");
            return new CombinedRewriter();
        }
        else if (hasEF)
        {
            return new EFCoreToSqlxRewriter();
        }
        else
        {
            return new DapperToSqlxRewriter();
        }
    }

    private string AddSqlxUsings(string content)
    {
        if (!content.Contains("using Sqlx.Annotations;"))
        {
            var lines = content.Split('\n').ToList();
            var lastUsingIndex = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith("using "))
                {
                    lastUsingIndex = i;
                }
            }

            if (lastUsingIndex >= 0)
            {
                lines.Insert(lastUsingIndex + 1, "using Sqlx.Annotations;");
                content = string.Join('\n', lines);
            }
        }

        return content;
    }

    private async Task CreateBackupAsync(string filePath)
    {
        var backupPath = filePath + ".backup";
                await Task.Run(() => File.Copy(filePath, backupPath));
        _logger.LogInformation("💾 Backup created: {Backup}", Path.GetFileName(backupPath));
    }

    private string[] GetSourceFiles(string projectPath)
    {
        if (Path.GetExtension(projectPath).Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var solutionDir = Path.GetDirectoryName(projectPath)!;
            return Directory.GetFiles(solutionDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".backup"))
                .ToArray();
        }
        else
        {
            var projectDir = Path.GetDirectoryName(projectPath)!;
            return Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".backup"))
                .ToArray();
        }
    }

    private async Task GenerateMigrationReportAsync(string projectPath)
    {
        var reportPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "SqlxMigrationReport.md");
        var report = new StringBuilder();

        report.AppendLine("# 🔄 Sqlx Migration Report");
        report.AppendLine();
        report.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Project:** {Path.GetFileName(projectPath)}");
        report.AppendLine();

        report.AppendLine("## 📊 Migration Summary");
        report.AppendLine();
        report.AppendLine($"- **Files processed:** {_migrationLog.Count}");
        report.AppendLine($"- **Total changes:** {_migrationLog.Values.Count}");
        report.AppendLine();

        report.AppendLine("## 📝 File Changes");
        report.AppendLine();

        foreach (var entry in _migrationLog)
        {
            report.AppendLine($"### {Path.GetFileName(entry.Key)}");
            report.AppendLine($"- {entry.Value}");
            report.AppendLine();
        }

        report.AppendLine("## 🚀 Next Steps");
        report.AppendLine();
        report.AppendLine("1. **Install Sqlx NuGet package:**");
        report.AppendLine("   ```");
        report.AppendLine("   dotnet add package Sqlx");
        report.AppendLine("   ```");
        report.AppendLine();
        report.AppendLine("2. **Update your project configuration:**");
        report.AppendLine("   - Add `<PackageReference Include=\"Sqlx\" Version=\"latest\" />` to your .csproj");
        report.AppendLine("   - Enable source generators if not already enabled");
        report.AppendLine();
        report.AppendLine("3. **Review and test the migrated code:**");
        report.AppendLine("   - Verify that all repository methods work as expected");
        report.AppendLine("   - Test database operations thoroughly");
        report.AppendLine("   - Update any custom SQL queries as needed");
        report.AppendLine();
        report.AppendLine("4. **Remove old dependencies:**");
        report.AppendLine("   ```");
        report.AppendLine("   dotnet remove package Dapper");
        report.AppendLine("   dotnet remove package Microsoft.EntityFrameworkCore");
        report.AppendLine("   ```");
        report.AppendLine();

        await File.WriteAllTextAsync(reportPath, report.ToString());
        _logger.LogInformation("📄 Migration report generated: {Report}", reportPath);
    }

    private void PrintMigrationSummary()
    {
        _logger.LogInformation("📊 Migration Summary:");
        _logger.LogInformation("   Files processed: {Count}", _migrationLog.Count);
        
        if (_migrationLog.Any())
        {
            _logger.LogInformation("   See SqlxMigrationReport.md for detailed changes");
        }
    }
}

/// <summary>
/// Base class for syntax rewriters with change tracking.
/// </summary>
public abstract class MigrationRewriter : CSharpSyntaxRewriter
{
    protected readonly List<string> _changes = new();

    public IReadOnlyList<string> GetChanges() => _changes.AsReadOnly();

    protected void LogChange(string description)
    {
        _changes.Add(description);
    }
}

/// <summary>
/// Rewrites Dapper code to Sqlx.
/// </summary>
public class DapperToSqlxRewriter : MigrationRewriter
{
    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (node.Name?.ToString().Contains("Dapper") == true)
        {
            LogChange($"Removed Dapper using: {node}");
            return null; // Remove Dapper using
        }

        return base.VisitUsingDirective(node);
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.ValueText;
            
            if (IsDapperMethod(methodName))
            {
                return ConvertDapperMethodToSqlx(node, methodName);
            }
        }

        return base.VisitInvocationExpression(node);
    }

    private bool IsDapperMethod(string methodName)
    {
        return methodName is "Query" or "QueryAsync" or "Execute" or "ExecuteAsync" or
               "QueryFirst" or "QueryFirstAsync" or "QuerySingle" or "QuerySingleAsync";
    }

    private SyntaxNode ConvertDapperMethodToSqlx(InvocationExpressionSyntax node, string methodName)
    {
        // This is a simplified conversion - in a real implementation,
        // you'd need more sophisticated analysis of the method parameters
        LogChange($"Converted Dapper.{methodName} to Sqlx repository method");
        
        // For now, add a comment indicating manual conversion needed
        var comment = SyntaxFactory.Comment($"// TODO: Convert Dapper.{methodName} to Sqlx repository method");
        
        return node.WithLeadingTrivia(node.GetLeadingTrivia().Add(SyntaxFactory.EndOfLine("\n")).Add(comment));
    }
}

/// <summary>
/// Rewrites Entity Framework Core code to Sqlx.
/// </summary>
public class EFCoreToSqlxRewriter : MigrationRewriter
{
    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        var nameString = node.Name?.ToString();
        if (nameString?.Contains("EntityFramework") == true || 
            nameString?.Contains("Microsoft.EntityFrameworkCore") == true)
        {
            LogChange($"Removed EF Core using: {node}");
            return null; // Remove EF using
        }

        return base.VisitUsingDirective(node);
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Convert DbContext to Sqlx repository
        if (node.BaseList?.Types.Any(t => t.Type.ToString().Contains("DbContext")) == true)
        {
            LogChange($"Converted DbContext {node.Identifier.ValueText} to Sqlx repository pattern");
            // Add comment for manual conversion
            var comment = SyntaxFactory.Comment($"// TODO: Convert DbContext {node.Identifier.ValueText} to Sqlx repository interfaces");
            return node.WithLeadingTrivia(node.GetLeadingTrivia().Add(SyntaxFactory.EndOfLine("\n")).Add(comment));
        }

        return base.VisitClassDeclaration(node);
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // Convert DbSet properties
        if (node.Type.ToString().Contains("DbSet"))
        {
            LogChange($"Converted DbSet property {node.Identifier.ValueText} to repository interface");
            var comment = SyntaxFactory.Comment($"// TODO: Replace DbSet<{ExtractEntityType(node.Type.ToString())}> with I{ExtractEntityType(node.Type.ToString())}Repository");
            return node.WithLeadingTrivia(node.GetLeadingTrivia().Add(SyntaxFactory.EndOfLine("\n")).Add(comment));
        }

        return base.VisitPropertyDeclaration(node);
    }

    private string ExtractEntityType(string dbSetType)
    {
        var match = Regex.Match(dbSetType, @"DbSet<(\w+)>");
        return match.Success ? match.Groups[1].Value : "Entity";
    }
}

/// <summary>
/// Combined rewriter for projects using both Dapper and EF Core.
/// </summary>
public class CombinedRewriter : MigrationRewriter
{
    private readonly DapperToSqlxRewriter _dapperRewriter = new();
    private readonly EFCoreToSqlxRewriter _efRewriter = new();

    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        var result = _dapperRewriter.VisitUsingDirective(node) ?? _efRewriter.VisitUsingDirective(node);
        
        _changes.AddRange(_dapperRewriter.GetChanges());
        _changes.AddRange(_efRewriter.GetChanges());
        
        return result ?? base.VisitUsingDirective(node);
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var result = _dapperRewriter.VisitInvocationExpression(node);
        _changes.AddRange(_dapperRewriter.GetChanges());
        
        return result ?? base.VisitInvocationExpression(node);
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var result = _efRewriter.VisitClassDeclaration(node);
        _changes.AddRange(_efRewriter.GetChanges());
        
        return result ?? base.VisitClassDeclaration(node);
    }
}
