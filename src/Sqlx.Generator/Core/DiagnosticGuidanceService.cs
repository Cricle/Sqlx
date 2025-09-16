using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sqlx
{
    /// <summary>
    /// 诊断指导服务，为用户提供全面的使用指导、SQL质量检查和性能优化建议
    /// </summary>
    internal class DiagnosticGuidanceService
    {
        private readonly GeneratorExecutionContext _context;

        public DiagnosticGuidanceService(GeneratorExecutionContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 执行完整的诊断分析并报告所有发现的问题
        /// </summary>
        public void PerformComprehensiveAnalysis(IMethodSymbol method, string sql, INamedTypeSymbol? entityType = null)
        {
            var location = method.Locations.FirstOrDefault();
            var allDiagnostics = new List<Diagnostic>();

            try
            {
                // 1. SQL质量检查
                allDiagnostics.AddRange(DiagnosticHelper.AnalyzeSqlQuality(sql, method.Name, location));

                // 2. 使用模式分析
                allDiagnostics.AddRange(DiagnosticHelper.AnalyzeUsagePattern(method, sql, location));

                // 3. 性能优化建议
                allDiagnostics.AddRange(DiagnosticHelper.AnalyzePerformanceOpportunities(method, entityType, sql, location));

                // 4. 额外的使用指导
                allDiagnostics.AddRange(AnalyzeAdditionalGuidance(method, sql, entityType, location));

                // 报告所有诊断信息
                foreach (var diagnostic in allDiagnostics)
                {
                    _context.ReportDiagnostic(diagnostic);
                }

                // 如果没有问题，给予正面反馈
                if (!allDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning || d.Severity == DiagnosticSeverity.Error))
                {
                    ReportPositiveFeedback(method, sql, location);
                }
            }
            catch (Exception ex)
            {
                // 如果诊断过程出错，报告但不影响代码生成
                var diagnostic = DiagnosticHelper.CreateDiagnostic(
                    "SQLX9998",
                    "诊断分析错误",
                    "诊断分析过程中发生错误: {0}",
                    DiagnosticSeverity.Info,
                    location,
                    ex.Message);
                _context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 分析额外的使用指导
        /// </summary>
        private List<Diagnostic> AnalyzeAdditionalGuidance(IMethodSymbol method, string sql, INamedTypeSymbol? entityType, Location? location)
        {
            var diagnostics = new List<Diagnostic>();
            var methodName = method.Name;

            // 检查参数数量
            if (method.Parameters.Length > 8)
            {
                diagnostics.Add(DiagnosticHelper.CreateUsageGuidanceDiagnostic(
                    $"方法有 {method.Parameters.Length} 个参数，可能过多",
                    "考虑使用对象参数或分解为多个更简单的方法",
                    location));
            }

            // 检查参数命名约定
            foreach (var param in method.Parameters)
            {
                if (!sql.Contains($"@{param.Name}") && !sql.Contains($"${param.Name}") && !sql.Contains($":{param.Name}"))
                {
                    diagnostics.Add(DiagnosticHelper.CreateUsageGuidanceDiagnostic(
                        $"参数 '{param.Name}' 在SQL中未使用",
                        "确保所有方法参数都在SQL中被使用，或考虑移除未使用的参数",
                        location));
                }
            }

            // 检查实体类型的最佳实践
            if (entityType != null)
            {
                var hasId = entityType.GetMembers().OfType<IPropertySymbol>()
                    .Any(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

                if (!hasId && sql.ToLowerInvariant().Contains("select"))
                {
                    diagnostics.Add(DiagnosticHelper.CreateBestPracticeDiagnostic(
                        "实体类型缺少Id属性，建议添加主键属性",
                        methodName,
                        location));
                }

                // 检查是否使用了 Record 类型的最佳实践
                if (PrimaryConstructorAnalyzer.IsRecord(entityType))
                {
                    if (sql.ToLowerInvariant().StartsWith("insert") || sql.ToLowerInvariant().StartsWith("update"))
                    {
                        diagnostics.Add(DiagnosticHelper.CreateBestPracticeDiagnostic(
                            "Record类型通常用于不可变数据，考虑在数据修改操作中使用普通类",
                            methodName,
                            location));
                    }
                }
            }

            // 检查数据库方言配置
            CheckDatabaseDialectConfiguration(method, diagnostics, location);

            // 检查并发和事务相关建议
            CheckConcurrencyAndTransactionGuidance(method, sql, diagnostics, location);

            return diagnostics;
        }

        /// <summary>
        /// 检查数据库方言配置
        /// </summary>
        private void CheckDatabaseDialectConfiguration(IMethodSymbol method, List<Diagnostic> diagnostics, Location? location)
        {
            var containingType = method.ContainingType;
            var sqlDefineAttr = containingType.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "SqlDefineAttribute");

            if (sqlDefineAttr == null)
            {
                diagnostics.Add(DiagnosticHelper.CreateBestPracticeDiagnostic(
                    "未指定数据库方言，将使用默认方言。建议显式指定 [SqlDefine] 特性",
                    method.Name,
                    location));
            }
        }

        /// <summary>
        /// 检查并发和事务相关指导
        /// </summary>
        private void CheckConcurrencyAndTransactionGuidance(IMethodSymbol method, string sql, List<Diagnostic> diagnostics, Location? location)
        {
            var sqlLower = sql.ToLowerInvariant();

            // 检查事务相关操作
            if (sqlLower.Contains("insert") || sqlLower.Contains("update") || sqlLower.Contains("delete"))
            {
                if (!method.Parameters.Any(p => p.Type.Name.Contains("Transaction")))
                {
                    diagnostics.Add(DiagnosticHelper.CreateBestPracticeDiagnostic(
                        "数据修改操作建议支持事务参数，以确保数据一致性",
                        method.Name,
                        location));
                }
            }

            // 检查乐观并发控制
            if (sqlLower.Contains("update") && !sqlLower.Contains("version") && !sqlLower.Contains("timestamp") && !sqlLower.Contains("rowversion"))
            {
                diagnostics.Add(DiagnosticHelper.CreateBestPracticeDiagnostic(
                    "UPDATE操作建议包含版本字段或时间戳以支持乐观并发控制",
                    method.Name,
                    location));
            }

            // 检查读取一致性
            if (sqlLower.Contains("select") && method.IsAsync)
            {
                if (sqlLower.Contains("join") && !sqlLower.Contains("with (nolock)") && !sqlLower.Contains("isolation"))
                {
                    diagnostics.Add(DiagnosticHelper.CreatePerformanceSuggestion(
                        "复杂查询考虑指定合适的隔离级别以平衡一致性和性能",
                        method.Name,
                        location));
                }
            }
        }

        /// <summary>
        /// 为良好的实践提供正面反馈
        /// </summary>
        private void ReportPositiveFeedback(IMethodSymbol method, string sql, Location? location)
        {
            var feedbacks = new List<string>();

            // 检查好的实践
            if (method.IsAsync && method.Name.EndsWith("Async"))
            {
                feedbacks.Add("✅ 异步方法命名规范");
            }

            if (method.IsAsync && method.Parameters.Any(p => p.Type.Name == "CancellationToken"))
            {
                feedbacks.Add("✅ 支持取消令牌");
            }

            if (sql.Contains("@") || sql.Contains("$") || sql.Contains(":"))
            {
                feedbacks.Add("✅ 使用参数化查询");
            }

            var sqlLower = sql.ToLowerInvariant();
            if (sqlLower.Contains("limit") || sqlLower.Contains("top") || sqlLower.Contains("offset"))
            {
                feedbacks.Add("✅ 使用分页限制");
            }

            if (feedbacks.Any())
            {
                var message = $"良好实践检测: {string.Join(", ", feedbacks)}";
                var diagnostic = DiagnosticHelper.CreateDiagnostic(
                    "SQLX9999",
                    "良好实践确认",
                    message,
                    DiagnosticSeverity.Info,
                    location);
                _context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 为新手用户提供快速入门指导
        /// </summary>
        public void ProvideQuickStartGuidance(IMethodSymbol method, Location? location)
        {
            var guidance = new List<string>
            {
                "💡 Sqlx快速提示:",
                "  • 使用 [Sqlx(\"SQL语句\")] 标记方法",
                "  • 异步方法建议添加 CancellationToken 参数",
                "  • 使用 @param、$param 或 :param 进行参数化查询",
                "  • 为大结果集添加 LIMIT/TOP 分页",
                "  • 考虑为修改操作返回受影响行数 (int)"
            };

            var message = string.Join("\n", guidance);
            var diagnostic = DiagnosticHelper.CreateDiagnostic(
                "SQLX0001",
                "Sqlx 使用指导",
                message,
                DiagnosticSeverity.Info,
                location);
            _context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// 生成诊断摘要报告
        /// </summary>
        public void GenerateDiagnosticSummary(IReadOnlyList<IMethodSymbol> analyzedMethods)
        {
            if (!analyzedMethods.Any()) return;

            var summary = new List<string>
            {
                $"📊 Sqlx 代码生成摘要:",
                $"  • 已分析 {analyzedMethods.Count} 个方法",
                $"  • 异步方法: {analyzedMethods.Count(m => m.IsAsync)}",
                $"  • 查询方法: {analyzedMethods.Count(m => m.ReturnType.ToString() != "void")}",
                "  • 建议查看诊断信息以获取优化建议"
            };

            var message = string.Join("\n", summary);
            var diagnostic = DiagnosticHelper.CreateDiagnostic(
                "SQLX0002",
                "代码生成摘要",
                message,
                DiagnosticSeverity.Info,
                null);
            _context.ReportDiagnostic(diagnostic);
        }
    }
}
