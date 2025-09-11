using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Sqlx.Core
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
            INamedTypeSymbol type, 
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX1001",
                "Primary Constructor Issue",
                "Primary Constructor analysis failed for type '{0}': {1}",
                DiagnosticSeverity.Warning,
                location,
                type.Name, issue);
        }

        /// <summary>
        /// 创建 Record 类型相关的诊断信息
        /// </summary>
        public static Diagnostic CreateRecordTypeDiagnostic(
            string issue, 
            INamedTypeSymbol type, 
            Location? location = null)
        {
            return CreateDiagnostic(
                "SQLX1002",
                "Record Type Issue",
                "Record type analysis failed for type '{0}': {1}",
                DiagnosticSeverity.Warning,
                location,
                type.Name, issue);
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

            // 检查是否有可访问的构造函数
            var accessibleConstructors = entityType.Constructors
                .Where(c => !c.IsImplicitlyDeclared && c.DeclaredAccessibility == Accessibility.Public)
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
                    .FirstOrDefault(c => c.Parameters.Length > 0);
                
                if (primaryConstructor == null)
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

            // 检查是否应该使用 Record
            if (!PrimaryConstructorAnalyzer.IsRecord(entityType))
            {
                var properties = PrimaryConstructorAnalyzer.GetAccessibleMembers(entityType).ToList();
                var readOnlyProperties = properties.Count(p => !p.CanWrite);
                
                if (readOnlyProperties > properties.Count * 0.7) // 70% 以上只读属性
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
            foreach (var prop in memberProperties)
            {
                if (prop.Type.SpecialType == SpecialType.System_String && 
                    prop is IPropertySymbol propSymbol && propSymbol.SetMethod != null)
                {
                    suggestions.Add($"属性 {prop.Name}: 考虑添加非空约束以提高性能");
                }
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
    }

    /// <summary>
    /// 诊断 ID 常量定义
    /// </summary>
    internal static class DiagnosticIds
    {
        public const string PrimaryConstructorIssue = "SQLX1001";
        public const string RecordTypeIssue = "SQLX1002";
        public const string EntityInferenceIssue = "SQLX1003";
        public const string PerformanceSuggestion = "SQLX2001";
        public const string CodeQualityWarning = "SQLX2002";
        public const string GenerationError = "SQLX3001";
    }
}
