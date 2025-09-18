// -----------------------------------------------------------------------
// <copyright file="EnhancedDiagnosticService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.Generator.Core
{
    /// <summary>
    /// 增强的诊断服务，专注于列名匹配和用户指导
    /// 目标：让用户更专注业务逻辑而非SQL细节
    /// </summary>
    internal class EnhancedDiagnosticService
    {
        private readonly GeneratorExecutionContext _context;
        private static readonly Dictionary<string, DiagnosticDescriptor> _diagnostics = CreateDiagnostics();

        public EnhancedDiagnosticService(GeneratorExecutionContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 执行增强的列名匹配分析
        /// </summary>
        public void AnalyzeColumnMapping(IMethodSymbol method, string sql, INamedTypeSymbol? entityType)
        {
            if (entityType == null) return;

            var location = method.Locations.FirstOrDefault();
            
            // 1. 分析列名匹配模式
            AnalyzeColumnNamingPatterns(method, sql, entityType, location);
            
            // 2. 检测潜在的映射问题
            DetectMappingIssues(method, sql, entityType, location);
            
            // 3. 提供智能优化建议
            SuggestOptimizations(method, sql, entityType, location);
            
            // 4. 生成用户友好的指导
            ProvideUserGuidance(method, sql, entityType, location);
        }

        /// <summary>
        /// 分析列名匹配模式
        /// </summary>
        private void AnalyzeColumnNamingPatterns(IMethodSymbol method, string sql, INamedTypeSymbol entityType, Location? location)
        {
            var properties = GetEntityProperties(entityType);
            var sqlColumns = ExtractColumnNamesFromSql(sql);
            
            var unmatchedProperties = new List<string>();
            var suggestedMappings = new List<(string Property, string SuggestedColumn)>();

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var possibleColumnNames = GeneratePossibleColumnNames(propertyName);
                
                // 检查是否有匹配的列
                var matchedColumn = sqlColumns.FirstOrDefault(col => 
                    possibleColumnNames.Any(possible => string.Equals(col, possible, StringComparison.OrdinalIgnoreCase)));

                if (matchedColumn == null)
                {
                    unmatchedProperties.Add(propertyName);
                    
                    // 找到最可能的匹配
                    var bestMatch = FindBestColumnMatch(propertyName, sqlColumns);
                    if (bestMatch != null)
                    {
                        suggestedMappings.Add((propertyName, bestMatch));
                    }
                }
            }

            // 报告未匹配的属性
            if (unmatchedProperties.Any())
            {
                var message = $"检测到 {unmatchedProperties.Count} 个属性可能无法映射到SQL列：{string.Join(", ", unmatchedProperties)}";
                ReportDiagnostic("SQLX5001", message, location);
            }

            // 提供映射建议
            foreach (var (property, suggestedColumn) in suggestedMappings)
            {
                var message = $"属性 '{property}' 可能对应数据库列 '{suggestedColumn}'。" +
                            $"建议使用 [Column(\"{suggestedColumn}\")] 特性或配置命名策略。";
                ReportDiagnostic("SQLX5002", message, location, DiagnosticSeverity.Info);
            }
        }

        /// <summary>
        /// 检测映射问题
        /// </summary>
        private void DetectMappingIssues(IMethodSymbol method, string sql, INamedTypeSymbol entityType, Location? location)
        {
            // 检测SELECT *的使用
            if (Regex.IsMatch(sql, @"\bSELECT\s+\*\b", RegexOptions.IgnoreCase))
            {
                var properties = GetEntityProperties(entityType);
                var message = $"检测到 SELECT * 的使用。实体 '{entityType.Name}' 有 {properties.Count} 个属性，" +
                            $"建议明确指定需要的列以提高性能：{{{{columns:basic}}}} 或 {{{{columns:exclude=large_fields}}}}";
                ReportDiagnostic("SQLX5003", message, location);
            }

            // 检测可能的性能问题
            var largeTextProperties = GetEntityProperties(entityType)
                .Where(p => IsLargeTextProperty(p))
                .ToList();

            if (largeTextProperties.Any())
            {
                var message = $"实体包含大型文本字段：{string.Join(", ", largeTextProperties.Select(p => p.Name))}。" +
                            $"建议在不需要时排除这些字段：{{{{columns:exclude={string.Join(",", largeTextProperties.Select(p => p.Name))}}}}}";
                ReportDiagnostic("SQLX5004", message, location, DiagnosticSeverity.Info);
            }
        }

        /// <summary>
        /// 提供优化建议
        /// </summary>
        private void SuggestOptimizations(IMethodSymbol method, string sql, INamedTypeSymbol entityType, Location? location)
        {
            var suggestions = new List<string>();

            // 检查是否可以使用智能模板
            if (!sql.Contains("{{"))
            {
                suggestions.Add("考虑使用智能模板语法减少手动SQL编写");
            }

            // 检查分页
            if (!Regex.IsMatch(sql, @"\b(LIMIT|TOP|OFFSET|FETCH)\b", RegexOptions.IgnoreCase) && 
                method.ReturnType.ToString().Contains("List"))
            {
                suggestions.Add("列表查询建议添加分页：{{paginate:size=20}}");
            }

            // 检查排序
            if (!Regex.IsMatch(sql, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase))
            {
                suggestions.Add("考虑添加排序以确保结果一致性：{{sort:created_at,desc}}");
            }

            // 检查索引提示
            var whereColumns = ExtractWhereColumns(sql);
            if (whereColumns.Any())
            {
                suggestions.Add($"WHERE条件中的列建议添加索引：{string.Join(", ", whereColumns)}");
            }

            if (suggestions.Any())
            {
                var message = $"性能优化建议：\n• {string.Join("\n• ", suggestions)}";
                ReportDiagnostic("SQLX5005", message, location, DiagnosticSeverity.Info);
            }
        }

        /// <summary>
        /// 提供用户友好的指导
        /// </summary>
        private void ProvideUserGuidance(IMethodSymbol method, string sql, INamedTypeSymbol entityType, Location? location)
        {
            var guidancePoints = new List<string>();

            // 智能模板建议
            if (ShouldSuggestSmartTemplate(sql, entityType))
            {
                guidancePoints.Add("💡 尝试智能模板：SELECT {{columns:auto}} FROM {{table:auto}}");
            }

            // 命名策略建议
            var namingStrategy = DetectNamingStrategy(sql, entityType);
            if (namingStrategy != null)
            {
                guidancePoints.Add($"🏗️ 检测到{namingStrategy}命名风格，建议配置：[SqlTemplate(NamingStrategy = ColumnNamingStrategy.{namingStrategy})]");
            }

            // 业务场景建议
            var businessPattern = DetectBusinessPattern(method.Name, sql);
            if (businessPattern != null)
            {
                guidancePoints.Add($"📋 检测到{businessPattern}模式，考虑使用预定义模板提高开发效率");
            }

            // 安全建议
            if (HasSecurityConcerns(sql))
            {
                guidancePoints.Add("🛡️ 检测到潜在安全风险，建议使用参数化查询：{{param}}语法");
            }

            if (guidancePoints.Any())
            {
                var message = $"💼 开发效率提升建议：\n{string.Join("\n", guidancePoints)}";
                ReportDiagnostic("SQLX5006", message, location, DiagnosticSeverity.Info);
            }
        }

        /// <summary>
        /// 生成可能的列名
        /// </summary>
        private List<string> GeneratePossibleColumnNames(string propertyName)
        {
            var possible = new List<string>
            {
                propertyName, // 原始名称
                propertyName.ToLowerInvariant(), // 小写
                ConvertToSnakeCase(propertyName), // snake_case
                ConvertToKebabCase(propertyName), // kebab-case
                ConvertToSnakeCase(propertyName).ToUpperInvariant() // UPPER_SNAKE_CASE
            };

            return possible.Distinct().ToList();
        }

        /// <summary>
        /// 找到最佳列名匹配
        /// </summary>
        private string? FindBestColumnMatch(string propertyName, List<string> sqlColumns)
        {
            // 简单的相似度匹配算法
            var bestMatch = sqlColumns
                .Select(col => new { Column = col, Similarity = CalculateSimilarity(propertyName, col) })
                .Where(x => x.Similarity > 0.6) // 相似度阈值
                .OrderByDescending(x => x.Similarity)
                .FirstOrDefault();

            return bestMatch?.Column;
        }

        /// <summary>
        /// 计算字符串相似度
        /// </summary>
        private double CalculateSimilarity(string source, string target)
        {
            if (source == target) return 1.0;
            
            var sourceNormalized = source.ToLowerInvariant().Replace("_", "").Replace("-", "");
            var targetNormalized = target.ToLowerInvariant().Replace("_", "").Replace("-", "");
            
            if (sourceNormalized == targetNormalized) return 0.9;
            
            // 简单的编辑距离相似度
            var distance = LevenshteinDistance(sourceNormalized, targetNormalized);
            var maxLength = Math.Max(sourceNormalized.Length, targetNormalized.Length);
            
            return 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// 计算编辑距离
        /// </summary>
        private int LevenshteinDistance(string source, string target)
        {
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            var matrix = new int[source.Length + 1, target.Length + 1];

            for (var i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;

            for (var j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (var i = 1; i <= source.Length; i++)
            {
                for (var j = 1; j <= target.Length; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source.Length, target.Length];
        }

        /// <summary>
        /// 从SQL中提取列名
        /// </summary>
        private List<string> ExtractColumnNamesFromSql(string sql)
        {
            var columns = new List<string>();
            
            // 简单的SELECT列提取（可以改进为更完善的SQL解析）
            var selectMatch = Regex.Match(sql, @"SELECT\s+(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (selectMatch.Success)
            {
                var columnsPart = selectMatch.Groups[1].Value;
                if (columnsPart.Trim() != "*")
                {
                    var columnMatches = Regex.Matches(columnsPart, @"(\w+)(?:\s+AS\s+\w+)?", RegexOptions.IgnoreCase);
                    columns.AddRange(columnMatches.Cast<Match>().Select(m => m.Groups[1].Value));
                }
            }

            return columns.Distinct().ToList();
        }

        /// <summary>
        /// 提取WHERE子句中的列
        /// </summary>
        private List<string> ExtractWhereColumns(string sql)
        {
            var columns = new List<string>();
            
            var whereMatch = Regex.Match(sql, @"WHERE\s+(.*?)(?:\s+ORDER\s+BY|\s+GROUP\s+BY|\s+LIMIT|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (whereMatch.Success)
            {
                var whereClause = whereMatch.Groups[1].Value;
                var columnMatches = Regex.Matches(whereClause, @"(\w+)\s*[=<>!]", RegexOptions.IgnoreCase);
                columns.AddRange(columnMatches.Cast<Match>().Select(m => m.Groups[1].Value));
            }

            return columns.Distinct().ToList();
        }

        /// <summary>
        /// 获取实体属性
        /// </summary>
        private List<IPropertySymbol> GetEntityProperties(INamedTypeSymbol entityType)
        {
            return entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.CanBeReferencedByName && p.DeclaredAccessibility == Accessibility.Public)
                .ToList();
        }

        /// <summary>
        /// 检查是否为大型文本属性
        /// </summary>
        private bool IsLargeTextProperty(IPropertySymbol property)
        {
            var typeName = property.Type.Name.ToLowerInvariant();
            var propertyName = property.Name.ToLowerInvariant();
            
            return typeName == "string" && (
                propertyName.Contains("description") ||
                propertyName.Contains("content") ||
                propertyName.Contains("text") ||
                propertyName.Contains("body") ||
                propertyName.Contains("comment"));
        }

        /// <summary>
        /// 检测命名策略
        /// </summary>
        private string? DetectNamingStrategy(string sql, INamedTypeSymbol entityType)
        {
            var sqlColumns = ExtractColumnNamesFromSql(sql);
            if (!sqlColumns.Any()) return null;

            var snakeCaseCount = sqlColumns.Count(col => col.Contains("_"));
            var kebabCaseCount = sqlColumns.Count(col => col.Contains("-"));
            var upperCaseCount = sqlColumns.Count(col => col.All(char.IsUpper));

            var total = sqlColumns.Count;
            if (snakeCaseCount > total * 0.5) return "SnakeCase";
            if (kebabCaseCount > total * 0.5) return "KebabCase";
            if (upperCaseCount > total * 0.5) return "ScreamingSnake";

            return null;
        }

        /// <summary>
        /// 检测业务模式
        /// </summary>
        private string? DetectBusinessPattern(string methodName, string sql)
        {
            var lowerName = methodName.ToLowerInvariant();
            var lowerSql = sql.ToLowerInvariant();

            if (lowerName.Contains("page") || lowerSql.Contains("limit") || lowerSql.Contains("offset"))
                return "分页查询";
            
            if (lowerName.Contains("search") || lowerSql.Contains("like"))
                return "搜索查询";
            
            if (lowerName.Contains("audit") || lowerName.Contains("log"))
                return "审计日志";
            
            if (lowerName.Contains("report") || lowerSql.Contains("group by"))
                return "报表统计";

            return null;
        }

        /// <summary>
        /// 检查是否应该建议智能模板
        /// </summary>
        private bool ShouldSuggestSmartTemplate(string sql, INamedTypeSymbol entityType)
        {
            // 如果SQL比较简单且没有使用模板语法，建议使用智能模板
            return !sql.Contains("{{") && 
                   Regex.IsMatch(sql, @"SELECT\s+\*\s+FROM\s+\w+", RegexOptions.IgnoreCase) &&
                   !sql.Contains("JOIN");
        }

        /// <summary>
        /// 检查安全隐患
        /// </summary>
        private bool HasSecurityConcerns(string sql)
        {
            // 检测可能的字符串拼接
            return sql.Contains("\"") || sql.Contains("'") && !Regex.IsMatch(sql, @"@\w+");
        }

        /// <summary>
        /// 转换为snake_case
        /// </summary>
        private string ConvertToSnakeCase(string input)
        {
            return Regex.Replace(input, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
        }

        /// <summary>
        /// 转换为kebab-case
        /// </summary>
        private string ConvertToKebabCase(string input)
        {
            return Regex.Replace(input, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
        }

        /// <summary>
        /// 报告诊断
        /// </summary>
        private void ReportDiagnostic(string id, string message, Location? location, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
        {
            if (_diagnostics.TryGetValue(id, out var descriptor))
            {
                var diagnostic = Diagnostic.Create(descriptor, location, message);
                _context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 创建诊断描述符
        /// </summary>
        private static Dictionary<string, DiagnosticDescriptor> CreateDiagnostics()
        {
            return new Dictionary<string, DiagnosticDescriptor>
            {
                ["SQLX5001"] = new DiagnosticDescriptor(
                    "SQLX5001",
                    "属性列名映射问题",
                    "{0}",
                    "Mapping",
                    DiagnosticSeverity.Warning,
                    true,
                    "检测到实体属性可能无法正确映射到数据库列"),

                ["SQLX5002"] = new DiagnosticDescriptor(
                    "SQLX5002",
                    "列名映射建议",
                    "{0}",
                    "Mapping",
                    DiagnosticSeverity.Info,
                    true,
                    "提供智能的列名映射建议"),

                ["SQLX5003"] = new DiagnosticDescriptor(
                    "SQLX5003",
                    "SELECT * 使用建议",
                    "{0}",
                    "Performance",
                    DiagnosticSeverity.Warning,
                    true,
                    "建议避免使用 SELECT * 并明确指定需要的列"),

                ["SQLX5004"] = new DiagnosticDescriptor(
                    "SQLX5004",
                    "大型字段性能建议",
                    "{0}",
                    "Performance",
                    DiagnosticSeverity.Info,
                    true,
                    "建议排除不必要的大型字段以提高性能"),

                ["SQLX5005"] = new DiagnosticDescriptor(
                    "SQLX5005",
                    "性能优化建议",
                    "{0}",
                    "Performance",
                    DiagnosticSeverity.Info,
                    true,
                    "提供查询性能优化建议"),

                ["SQLX5006"] = new DiagnosticDescriptor(
                    "SQLX5006",
                    "开发效率提升建议",
                    "{0}",
                    "Productivity",
                    DiagnosticSeverity.Info,
                    true,
                    "提供开发效率提升的实用建议")
            };
        }
    }
}

