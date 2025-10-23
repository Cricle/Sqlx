using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Sqlx.Generator.Analyzers;

/// <summary>
/// 动态SQL分析器 - 检测动态占位符的不安全使用
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DynamicSqlAnalyzer : DiagnosticAnalyzer
{
    // SQLX2001: 使用{{@}}但参数未标记[DynamicSql]
    private static readonly DiagnosticDescriptor Rule2001 = new(
        id: "SQLX2001",
        title: "Dynamic placeholder requires [DynamicSql] attribute",
        messageFormat: "Parameter '{0}' is used in dynamic placeholder '{{{{@{0}}}}}' but is not marked with [DynamicSql] attribute. This is required for security.",
        category: "Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Dynamic placeholders ({{@paramName}}) must have corresponding parameters marked with [DynamicSql] attribute to ensure proper validation.");

    // SQLX2006: 动态参数类型必须是string
    private static readonly DiagnosticDescriptor Rule2006 = new(
        id: "SQLX2006",
        title: "Dynamic SQL parameter must be of type string",
        messageFormat: "Parameter '{0}' is marked with [DynamicSql] but has type '{1}'. Dynamic SQL parameters must be of type 'string'.",
        category: "Type Safety",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Parameters marked with [DynamicSql] attribute must be of type string for safe validation.");

    // SQLX2007: SQL模板包含危险操作
    private static readonly DiagnosticDescriptor Rule2007 = new(
        id: "SQLX2007",
        title: "SQL template contains potentially dangerous operations",
        messageFormat: "SQL template contains '{0}' which may be dangerous. Consider using parameterized queries instead.",
        category: "Security",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "SQL templates should avoid DDL operations and potentially dangerous keywords.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        Rule2001,
        Rule2006,
        Rule2007);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // 分析方法声明（检查[Sqlx]特性和参数）
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // 获取方法符号
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null)
            return;

        // 检查是否有[Sqlx]或[SqlxAttribute]特性
        var sqlxAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlxAttribute" ||
                                   attr.AttributeClass?.Name == "Sqlx");

        if (sqlxAttribute == null)
            return;

        // 获取SQL模板
        string? sqlTemplate = null;
        if (sqlxAttribute.ConstructorArguments.Length > 0)
        {
            sqlTemplate = sqlxAttribute.ConstructorArguments[0].Value as string;
        }

        if (string.IsNullOrEmpty(sqlTemplate))
            return;

        // 检查SQLX2007: SQL模板包含危险操作
        CheckDangerousOperations(context, methodDeclaration, sqlTemplate);

        // 提取动态占位符（{{@paramName}}）
        var dynamicPlaceholders = ExtractDynamicPlaceholders(sqlTemplate);

        if (dynamicPlaceholders.Count == 0)
            return;

        // 获取方法参数
        var parameters = methodSymbol.Parameters;

        // 检查SQLX2001和SQLX2006
        foreach (var placeholderName in dynamicPlaceholders)
        {
            var parameter = parameters.FirstOrDefault(p =>
                p.Name.Equals(placeholderName, System.StringComparison.Ordinal));

            if (parameter == null)
                continue;

            // 检查是否有[DynamicSql]特性
            var hasDynamicSqlAttribute = parameter.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "DynamicSqlAttribute" ||
                            attr.AttributeClass?.Name == "DynamicSql");

            if (!hasDynamicSqlAttribute)
            {
                // SQLX2001: 缺少[DynamicSql]特性
                var parameterSyntax = methodDeclaration.ParameterList.Parameters
                    .FirstOrDefault(p => p.Identifier.Text == placeholderName);

                if (parameterSyntax != null)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule2001,
                        parameterSyntax.GetLocation(),
                        placeholderName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else
            {
                // SQLX2006: 检查参数类型是否是string
                if (parameter.Type.SpecialType != SpecialType.System_String)
                {
                    var parameterSyntax = methodDeclaration.ParameterList.Parameters
                        .FirstOrDefault(p => p.Identifier.Text == placeholderName);

                    if (parameterSyntax != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule2006,
                            parameterSyntax.GetLocation(),
                            placeholderName,
                            parameter.Type.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private void CheckDangerousOperations(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, string sqlTemplate)
    {
        var dangerousKeywords = new[]
        {
            ("DROP", "DDL operation"),
            ("TRUNCATE", "DDL operation"),
            ("ALTER", "DDL operation"),
            ("EXEC", "command execution"),
            ("EXECUTE", "command execution")
        };

        foreach (var (keyword, description) in dangerousKeywords)
        {
            if (sqlTemplate.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var diagnostic = Diagnostic.Create(
                    Rule2007,
                    method.Identifier.GetLocation(),
                    $"{keyword} ({description})");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// 从SQL模板中提取动态占位符名称
    /// </summary>
    private static System.Collections.Generic.HashSet<string> ExtractDynamicPlaceholders(string sqlTemplate)
    {
        var placeholders = new System.Collections.Generic.HashSet<string>();

        // 简单的正则匹配 {{@paramName}}
        var pattern = @"\{\{@(\w+)\}\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(sqlTemplate, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                placeholders.Add(match.Groups[1].Value);
            }
        }

        return placeholders;
    }
}

