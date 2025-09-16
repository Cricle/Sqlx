using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Sqlx
{
    /// <summary>
    /// 诊断和错误处理辅助类
    /// 提供详细的错误信息和调试支持
    /// </summary>
    internal static class DiagnosticHelper
    {
        /// <summary>
        /// 创建详细的编译诊断信息
        /// </summary>
        public static Diagnostic CreateDiagnostic(
            string id,
            string title,
            string messageFormat,
            DiagnosticSeverity severity,
            Location? location = null,
            params object[] messageArgs)
        {
            // Handle edge cases for diagnostic parameters
            if (string.IsNullOrWhiteSpace(id))
                id = "SQLX_UNKNOWN";
            if (string.IsNullOrWhiteSpace(title))
                title = "Unknown Diagnostic";
            if (string.IsNullOrWhiteSpace(messageFormat))
                messageFormat = "No message provided";

            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: "Sqlx",
                defaultSeverity: severity,
                isEnabledByDefault: true,
                description: title);

            return Diagnostic.Create(descriptor, location, messageArgs);
        }

        /// <summary>
        /// 创建 Primary Constructor 相关的诊断信息
        /// </summary>
        public static Diagnostic CreatePrimaryConstructorDiagnostic(
            string issue,
            INamedTypeSymbol? type,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX1001",
                "Primary Constructor Issue",
                "Primary Constructor analysis failed for type '{0}': {1}",
                DiagnosticSeverity.Warning,
                location,
                type?.Name ?? "Unknown", issue);
        }

        /// <summary>
        /// 创建 Record 类型相关的诊断信息
        /// </summary>
        public static Diagnostic CreateRecordTypeDiagnostic(
            string issue,
            INamedTypeSymbol? type,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX1002",
                "Record Type Issue",
                "Record type analysis failed for type '{0}': {1}",
                DiagnosticSeverity.Warning,
                location,
                type?.Name ?? "Unknown", issue);
        }

        /// <summary>
        /// 创建实体类型推断相关的诊断信息
        /// </summary>
        public static Diagnostic CreateEntityInferenceDiagnostic(
            string issue,
            string methodName,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX1003",
                "Entity Type Inference Issue",
                "Entity type inference failed for method '{0}': {1}",
                DiagnosticSeverity.Info,
                location,
                methodName, issue);
        }

        /// <summary>
        /// 创建性能优化建议诊断信息
        /// </summary>
        public static Diagnostic CreatePerformanceSuggestion(
            string suggestion,
            string context,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX2001",
                "Performance Suggestion",
                "Performance suggestion for {0}: {1}",
                DiagnosticSeverity.Info,
                location,
                context, suggestion);
        }

        /// <summary>
        /// 创建使用方式指导诊断信息
        /// </summary>
        public static Diagnostic CreateUsageGuidanceDiagnostic(
            string issue,
            string suggestion,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX3001",
                "Usage Guidance",
                "Usage issue detected: {0}. Suggestion: {1}",
                DiagnosticSeverity.Info,
                location,
                issue, suggestion);
        }

        /// <summary>
        /// 创建SQL质量检查诊断信息
        /// </summary>
        public static Diagnostic CreateSqlQualityDiagnostic(
            string issue,
            string sql,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX3002",
                "SQL Quality Warning",
                "SQL quality issue in '{0}': {1}",
                DiagnosticSeverity.Warning,
                location,
                sql, issue);
        }

        /// <summary>
        /// 创建安全性警告诊断信息
        /// </summary>
        public static Diagnostic CreateSecurityWarningDiagnostic(
            string issue,
            string context,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX3003",
                "Security Warning",
                "Security concern in {0}: {1}",
                DiagnosticSeverity.Warning,
                location,
                context, issue);
        }

        /// <summary>
        /// 创建最佳实践建议诊断信息
        /// </summary>
        public static Diagnostic CreateBestPracticeDiagnostic(
            string practice,
            string context,
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX3004",
                "Best Practice Suggestion",
                "Best practice suggestion for {0}: {1}",
                DiagnosticSeverity.Info,
                location,
                context, practice);
        }

        /// <summary>
        /// 生成详细的类型分析报告
        /// </summary>
        public static string GenerateTypeAnalysisReport(INamedTypeSymbol type)
        {
            var report = new StringBuilder();
            report.AppendLine($"=== 类型分析报告: {type.Name} ===");
            report.AppendLine($"完全限定名: {type.ToDisplayString()}");
            report.AppendLine($"类型种类: {type.TypeKind}");
            report.AppendLine($"是否为 Record: {PrimaryConstructorAnalyzer.IsRecord(type)}");
            report.AppendLine($"是否有主构造函数: {PrimaryConstructorAnalyzer.HasPrimaryConstructor(type)}");

            // 构造函数信息
            var constructors = type.Constructors.Where(c => !c.IsImplicitlyDeclared).ToList();
            report.AppendLine($"构造函数数量: {constructors.Count}");
            foreach (var ctor in constructors)
            {
                report.AppendLine($"  - {ctor.ToDisplayString()}");
            }

            // 属性信息
            var properties = PrimaryConstructorAnalyzer.GetAccessibleMembers(type).ToList();
            report.AppendLine($"可访问属性数量: {properties.Count}");
            foreach (var prop in properties)
            {
                var canRead = prop is IPropertySymbol propSymbol ? propSymbol.GetMethod != null : true;
                var canWrite = prop is IPropertySymbol propSymbol2 ? propSymbol2.SetMethod != null : false;
                report.AppendLine($"  - {prop.Name}: {prop.Type.ToDisplayString()} " +
                    $"(可读: {canRead}, 可写: {canWrite})");
            }

            report.AppendLine("=== 分析完成 ===");
            return report.ToString();
        }

        /// <summary>
        /// 验证实体类型的完整性
        /// </summary>
        public static List<string> ValidateEntityType(INamedTypeSymbol entityType)
        {
            var issues = new List<string>();

            // 检查是否为抽象类
            if (entityType.IsAbstract)
            {
                issues.Add("实体类型不能是抽象类");
            }

            // 检查是否有可访问的构造函数（包括隐式构造函数）
            var accessibleConstructors = entityType.Constructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            if (!accessibleConstructors.Any())
            {
                issues.Add("实体类型必须有至少一个公共构造函数");
            }

            // 检查是否有可映射的属性
            var mappableProperties = PrimaryConstructorAnalyzer.GetAccessibleMembers(entityType).ToList();
            if (!mappableProperties.Any())
            {
                issues.Add("实体类型必须有至少一个可映射的属性");
            }

            // 检查 Record 类型的特殊情况
            if (PrimaryConstructorAnalyzer.IsRecord(entityType))
            {
                var primaryConstructor = entityType.Constructors
                    .FirstOrDefault(c => c.Parameters.Length > 0 && !c.IsImplicitlyDeclared);

                if (primaryConstructor == null)
                {
                    issues.Add("Record 类型应该有主构造函数");
                }
            }
            else if (entityType.TypeKind == TypeKind.Class && entityType.BaseType?.Name == "Record")
            {
                // 备用检查：如果类型名包含record关键字或继承自Record基类
                var hasExplicitConstructor = entityType.Constructors
                    .Any(c => c.Parameters.Length > 0 && !c.IsImplicitlyDeclared);
                
                if (!hasExplicitConstructor)
                {
                    issues.Add("Record 类型应该有主构造函数");
                }
            }

            return issues;
        }

        /// <summary>
        /// 生成性能优化建议
        /// </summary>
        public static List<string> GeneratePerformanceSuggestions(INamedTypeSymbol entityType)
        {
            var suggestions = new List<string>();

            // 基础性能建议 - 确保至少有一个建议
            suggestions.Add("考虑使用索引优化数据库查询性能");

            // 检查是否应该使用 Record
            if (!PrimaryConstructorAnalyzer.IsRecord(entityType))
            {
                var properties = PrimaryConstructorAnalyzer.GetAccessibleMembers(entityType).ToList();
                var readOnlyProperties = properties.Count(p => !p.CanWrite);

                if (readOnlyProperties > properties.Count * 0.5) // 50% 以上只读属性（降低阈值）
                {
                    suggestions.Add("考虑使用 Record 类型以获得更好的不可变性和性能");
                }
            }

            // 检查是否应该使用主构造函数
            if (!PrimaryConstructorAnalyzer.HasPrimaryConstructor(entityType) &&
                !PrimaryConstructorAnalyzer.IsRecord(entityType))
            {
                var constructors = entityType.Constructors
                    .Where(c => !c.IsImplicitlyDeclared)
                    .ToList();

                if (constructors.Count == 1 && constructors[0].Parameters.Length > 2)
                {
                    suggestions.Add("考虑使用主构造函数语法简化代码");
                }
            }

            // 检查属性类型优化
            var memberProperties = PrimaryConstructorAnalyzer.GetAccessibleMembers(entityType).ToList();
            var stringPropertyCount = 0;
            
            foreach (var prop in memberProperties)
            {
                if (prop.Type.SpecialType == SpecialType.System_String &&
                    prop is IPropertySymbol propSymbol && propSymbol.SetMethod != null)
                {
                    stringPropertyCount++;
                    suggestions.Add($"属性 {prop.Name}: 考虑添加非空约束以提高性能");
                }
            }

            // 如果有很多字符串属性，建议考虑字符串优化
            if (stringPropertyCount >= 3)
            {
                suggestions.Add("实体包含多个字符串属性，考虑使用字符串池或其他优化策略");
            }

            // 如果属性很多，建议考虑拆分
            if (memberProperties.Count >= 8)
            {
                suggestions.Add("实体包含较多属性，考虑按功能拆分为多个更小的实体");
            }

            return suggestions;
        }

        /// <summary>
        /// 创建代码生成上下文诊断
        /// </summary>
        public static void LogCodeGenerationContext(
            string context,
            INamedTypeSymbol entityType,
            string methodName)
        {
            System.Diagnostics.Debug.WriteLine($"[Sqlx CodeGen] {context}");
            System.Diagnostics.Debug.WriteLine($"  实体类型: {entityType.ToDisplayString()}");
            System.Diagnostics.Debug.WriteLine($"  方法名称: {methodName}");
            System.Diagnostics.Debug.WriteLine($"  是否为 Record: {PrimaryConstructorAnalyzer.IsRecord(entityType)}");
            System.Diagnostics.Debug.WriteLine($"  是否有主构造函数: {PrimaryConstructorAnalyzer.HasPrimaryConstructor(entityType)}");
        }

        /// <summary>
        /// 验证生成的代码质量
        /// </summary>
        public static List<string> ValidateGeneratedCode(string generatedCode, INamedTypeSymbol entityType)
        {
            var issues = new List<string>();

            // 检查是否包含不安全的类型转换
            if (generatedCode.Contains("(DateTime)") && !generatedCode.Contains("GetDateTime"))
            {
                issues.Add("检测到不安全的 DateTime 类型转换，应使用 GetDateTime() 方法");
            }

            // 检查是否正确使用了实体类型
            var expectedTypeName = entityType.Name;
            if (!generatedCode.Contains($"new {entityType.ContainingNamespace}.{expectedTypeName}"))
            {
                issues.Add($"生成的代码未正确使用实体类型 {expectedTypeName}");
            }

            // 检查是否有适当的 null 检查
            if (generatedCode.Contains("reader.Get") && !generatedCode.Contains("IsDBNull"))
            {
                issues.Add("生成的代码缺少必要的 null 检查");
            }

            return issues;
        }

        /// <summary>
        /// 分析SQL质量并提供改进建议
        /// </summary>
        public static List<Diagnostic> AnalyzeSqlQuality(string sql, string methodName, Location? location = null)
        {
            var diagnostics = new List<Diagnostic>();

            if (string.IsNullOrWhiteSpace(sql))
                return diagnostics;

            var sqlLower = sql.ToLowerInvariant();

            // 检查SELECT * 使用
            if (sqlLower.Contains("select *"))
            {
                diagnostics.Add(CreateSqlQualityDiagnostic(
                    "避免使用 SELECT *，明确指定需要的列可以提高性能和维护性",
                    sql.Length > 50 ? sql.Substring(0, 50) + "..." : sql,
                    location));
            }

            // 检查缺少WHERE子句的UPDATE/DELETE
            if ((sqlLower.StartsWith("update") || sqlLower.StartsWith("delete")) && !sqlLower.Contains("where"))
            {
                diagnostics.Add(CreateSecurityWarningDiagnostic(
                    "UPDATE/DELETE 语句缺少 WHERE 子句，可能会影响所有记录",
                    methodName,
                    location));
            }

            // 检查SQL注入风险
            if (sql.Contains("'") && !sql.Contains("@") && !sql.Contains("$"))
            {
                diagnostics.Add(CreateSecurityWarningDiagnostic(
                    "检测到硬编码字符串值，可能存在SQL注入风险，建议使用参数化查询",
                    methodName,
                    location));
            }

            // 检查性能问题
            if (sqlLower.Contains("select") && sqlLower.Contains("order by") && !sqlLower.Contains("limit") && !sqlLower.Contains("top"))
            {
                diagnostics.Add(CreatePerformanceSuggestion(
                    "ORDER BY 查询建议添加 LIMIT/TOP 限制返回行数，避免大结果集性能问题",
                    methodName,
                    location));
            }

            // 检查JOIN性能
            var joinCount = 0;
            var joinKeywords = new[] { " inner join ", " left join ", " right join ", " full join ", " join " };
            // 计算时要避免重复计算，先处理具体的join类型，最后处理通用的join
            foreach (var keyword in joinKeywords)
            {
                var matches = (sqlLower.Length - sqlLower.Replace(keyword, "").Length) / keyword.Length;
                joinCount += matches;
                // 移除已匹配的部分，避免重复计算
                sqlLower = sqlLower.Replace(keyword, " ");
            }

            if (joinCount >= 3)
            {
                diagnostics.Add(CreatePerformanceSuggestion(
                    $"检测到 {joinCount} 个 JOIN 操作，考虑优化查询或添加适当的索引",
                    methodName,
                    location));
            }

            // 检查子查询 - 修复逻辑，检查是否有嵌套的SELECT
            var originalSqlLower = sql.ToLowerInvariant(); // 使用原始SQL检查子查询
            if (originalSqlLower.Contains("select") && originalSqlLower.Count(c => c == '(') >= 1 && 
                originalSqlLower.IndexOf("select", originalSqlLower.IndexOf("select") + 1) > 0)
            {
                diagnostics.Add(CreatePerformanceSuggestion(
                    "检测到子查询，考虑使用 JOIN 或 CTE 来提高性能",
                    methodName,
                    location));
            }

            return diagnostics;
        }

        /// <summary>
        /// 分析方法使用模式并提供指导
        /// </summary>
        public static List<Diagnostic> AnalyzeUsagePattern(IMethodSymbol method, string sql, Location? location = null)
        {
            var diagnostics = new List<Diagnostic>();
            var methodName = method.Name;

            // 检查异步方法最佳实践
            var isAsyncMethod = method.IsAsync || method.ReturnType.ToDisplayString().Contains("Task");
            if (isAsyncMethod)
            {
                if (!method.Parameters.Any(p => p.Type.Name == "CancellationToken"))
                {
                    diagnostics.Add(CreateBestPracticeDiagnostic(
                        "异步方法建议添加 CancellationToken 参数以支持取消操作",
                        methodName,
                        location));
                }

                if (!methodName.EndsWith("Async"))
                {
                    diagnostics.Add(CreateUsageGuidanceDiagnostic(
                        "异步方法名称不符合约定",
                        "异步方法建议以 'Async' 结尾",
                        location));
                }
            }

            // 检查方法命名约定
            var sqlLower = sql.ToLowerInvariant();
            if (sqlLower.StartsWith("select"))
            {
                if (!methodName.ToLowerInvariant().Contains("get") && !methodName.ToLowerInvariant().Contains("query") && !methodName.ToLowerInvariant().Contains("find"))
                {
                    diagnostics.Add(CreateUsageGuidanceDiagnostic(
                        "SELECT 查询方法命名不够清晰",
                        "建议使用 Get/Query/Find 前缀，如 GetUser、QueryUsers、FindByName",
                        location));
                }
            }
            else if (sqlLower.StartsWith("insert"))
            {
                if (!methodName.ToLowerInvariant().Contains("create") && !methodName.ToLowerInvariant().Contains("add") && !methodName.ToLowerInvariant().Contains("insert"))
                {
                    diagnostics.Add(CreateUsageGuidanceDiagnostic(
                        "INSERT 操作方法命名不够清晰",
                        "建议使用 Create/Add/Insert 前缀，如 CreateUser、AddUser",
                        location));
                }
            }
            else if (sqlLower.StartsWith("update"))
            {
                if (!methodName.ToLowerInvariant().Contains("update") && !methodName.ToLowerInvariant().Contains("modify"))
                {
                    diagnostics.Add(CreateUsageGuidanceDiagnostic(
                        "UPDATE 操作方法命名不够清晰",
                        "建议使用 Update/Modify 前缀，如 UpdateUser、ModifyUser",
                        location));
                }
            }
            else if (sqlLower.StartsWith("delete"))
            {
                if (!methodName.ToLowerInvariant().Contains("delete") && !methodName.ToLowerInvariant().Contains("remove"))
                {
                    diagnostics.Add(CreateUsageGuidanceDiagnostic(
                        "DELETE 操作方法命名不够清晰",
                        "建议使用 Delete/Remove 前缀，如 DeleteUser、RemoveUser",
                        location));
                }
            }

            // 检查返回类型约定
            var returnType = method.ReturnType.ToDisplayString();
            if (sqlLower.StartsWith("select"))
            {
                if (returnType == "void" || returnType == "Task" || returnType == "System.Threading.Tasks.Task")
                {
                    diagnostics.Add(CreateUsageGuidanceDiagnostic(
                        "SELECT 查询应该有返回值",
                        "SELECT 查询建议返回实体类型、集合或基础类型",
                        location));
                }
            }
            else if (sqlLower.StartsWith("insert") || sqlLower.StartsWith("update") || sqlLower.StartsWith("delete"))
            {
                if (!returnType.Contains("int") && returnType != "void" && returnType != "Task")
                {
                    diagnostics.Add(CreateUsageGuidanceDiagnostic(
                        "数据修改操作的返回类型可能不合适",
                        "INSERT/UPDATE/DELETE 操作建议返回 int（受影响行数）或 void",
                        location));
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// 分析性能优化机会
        /// </summary>
        public static List<Diagnostic> AnalyzePerformanceOpportunities(IMethodSymbol method, INamedTypeSymbol? entityType, string sql, Location? location = null)
        {
            var diagnostics = new List<Diagnostic>();
            var methodName = method.Name;

            // 检查批量操作机会
            if (method.Parameters.Any(p => TypeAnalyzer.IsCollectionType(p.Type)))
            {
                if (!sql.ToLowerInvariant().Contains("batch") && (sql.ToLowerInvariant().StartsWith("insert") || sql.ToLowerInvariant().StartsWith("update") || sql.ToLowerInvariant().StartsWith("delete")))
                {
                    diagnostics.Add(CreatePerformanceSuggestion(
                        "检测到集合参数，考虑使用批量操作以提高性能",
                        methodName,
                        location));
                }
            }

            // 检查分页查询
            if (sql.ToLowerInvariant().Contains("select") && !sql.ToLowerInvariant().Contains("top") && !sql.ToLowerInvariant().Contains("limit") && !sql.ToLowerInvariant().Contains("offset"))
            {
                if (method.ReturnType.ToDisplayString().Contains("List") || method.ReturnType.ToDisplayString().Contains("IEnumerable"))
                {
                    diagnostics.Add(CreatePerformanceSuggestion(
                        "返回集合的查询建议添加分页支持（LIMIT/OFFSET 或 TOP）",
                        methodName,
                        location));
                }
            }

            // 检查实体大小
            if (entityType != null)
            {
                var properties = entityType.GetMembers().OfType<IPropertySymbol>().ToList();
                if (properties.Count > 15)
                {
                    diagnostics.Add(CreatePerformanceSuggestion(
                        $"实体 {entityType.Name} 有 {properties.Count} 个属性，考虑使用投影查询只选择需要的字段",
                        methodName,
                        location));
                }

                var stringProperties = properties.Where(p => p.Type.SpecialType == SpecialType.System_String).ToList();
                if (stringProperties.Count > 5)
                {
                    diagnostics.Add(CreatePerformanceSuggestion(
                        $"实体包含 {stringProperties.Count} 个字符串属性，考虑字符串池优化或分离大文本字段",
                        methodName,
                        location));
                }
            }

            // 检查数据库连接管理 - 检查是否为异步模式（返回Task或有async修饰符）
            var isAsyncMethod = method.IsAsync || method.ReturnType.ToDisplayString().Contains("Task");
            if (!isAsyncMethod && sql.ToLowerInvariant().Contains("select"))
            {
                diagnostics.Add(CreateBestPracticeDiagnostic(
                    "数据库查询建议使用异步方法以避免阻塞线程",
                    methodName,
                    location));
            }

            return diagnostics;
        }
    }

    /// <summary>
    /// 诊断 ID 常量定义
    /// </summary>
    internal static class DiagnosticIds
    {
        // 原有诊断ID
        public const string PrimaryConstructorIssue = "SQLX1001";
        public const string RecordTypeIssue = "SQLX1002";
        public const string EntityInferenceIssue = "SQLX1003";
        public const string PerformanceSuggestion = "SQLX2001";
        public const string CodeQualityWarning = "SQLX2002";
        public const string GenerationError = "SQLX2003";

        // 新增诊断ID
        public const string UsageGuidance = "SQLX3001";
        public const string SqlQualityWarning = "SQLX3002";
        public const string SecurityWarning = "SQLX3003";
        public const string BestPracticeSuggestion = "SQLX3004";
        public const string PerformanceOptimization = "SQLX3005";
        public const string NamingConvention = "SQLX3006";
        public const string MethodSignatureGuidance = "SQLX3007";
    }
}
