// -----------------------------------------------------------------------
// <copyright file="PropertyOrderAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Sqlx.Generator.Analyzers
{
    /// <summary>
    /// 分析器：检测C#属性顺序与SQL列顺序不一致
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PropertyOrderAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SQLX001";

        private static readonly LocalizableString Title = "属性顺序与SQL列顺序不匹配";
        private static readonly LocalizableString MessageFormat = "实体类型 '{0}' 的属性顺序与SQL列顺序不匹配。期望顺序: {1}";
        private static readonly LocalizableString Description =
            "为了使用硬编码索引访问（极致性能），C#属性顺序必须与SQL模板中的列顺序一致。" +
            "当前使用 reader.GetInt32(0) 等硬编码索引访问，如果顺序不匹配会导致运行时错误。";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // 注册类声明分析
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null)
                return;

            // 检查是否有 [TableName] 或 [RepositoryFor] 特性
            bool hasTableNameAttribute = classSymbol.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "TableNameAttribute" || a.AttributeClass?.Name == "TableName");

            bool hasRepositoryForAttribute = classSymbol.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "RepositoryForAttribute" || a.AttributeClass?.Name == "RepositoryFor");

            // 如果是实体类（有TableName）或Repository类（有RepositoryFor），进行检查
            if (hasTableNameAttribute)
            {
                // 实体类：检查属性顺序
                AnalyzeEntityClass(context, classSymbol, classDeclaration);
            }
            else if (hasRepositoryForAttribute)
            {
                // Repository类：检查接口方法返回的实体类型
                AnalyzeRepositoryClass(context, classSymbol);
            }
        }

        private void AnalyzeEntityClass(SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDeclaration)
        {
            // 获取所有可映射的属性（有setter的）
            var properties = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.CanBeReferencedByName && p.SetMethod != null)
                .ToList();

            if (properties.Count == 0)
                return;

            // 获取属性的声明顺序
            var propertyDeclarations = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .ToList();

            // 转换为snake_case列名
            var expectedColumnOrder = properties
                .Select(p => ConvertToSnakeCase(p.Name))
                .ToList();

            // 这里简单示例：如果第一个属性不是id，发出警告
            // 实际应该与SQL模板比对，但在Analyzer中无法获取SQL模板，所以采用启发式规则

            // 启发式规则1：如果类有Id属性，它应该是第一个
            var idProperty = properties.FirstOrDefault(p =>
                p.Name.Equals("Id", System.StringComparison.OrdinalIgnoreCase));

            if (idProperty != null && properties.IndexOf(idProperty) != 0)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    classDeclaration.Identifier.GetLocation(),
                    classSymbol.Name,
                    "Id 属性应该是第一个属性（对应 SQL 中的主键列）");

                context.ReportDiagnostic(diagnostic);
            }

            // 启发式规则2：属性应该按字母顺序排列（snake_case）以匹配常见的SQL列顺序
            // 注意：这只是一个简化的示例，实际可能需要更复杂的逻辑
        }

        private void AnalyzeRepositoryClass(SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol)
        {
            // Repository类分析：检查接口方法中使用的实体类型
            // 这里可以扩展以检查方法参数和返回类型

            // 获取 RepositoryFor 特性的类型参数
            var repositoryForAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryForAttribute" ||
                                   a.AttributeClass?.Name == "RepositoryFor");

            if (repositoryForAttr == null || repositoryForAttr.ConstructorArguments.Length == 0)
                return;

            // 获取接口类型
            if (repositoryForAttr.ConstructorArguments[0].Value is INamedTypeSymbol interfaceSymbol)
            {
                // 分析接口方法的返回类型和参数
                foreach (var member in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    // 检查返回类型
                    var returnType = UnwrapTaskType(member.ReturnType);
                    if (returnType is INamedTypeSymbol entityType && IsEntityType(entityType))
                    {
                        // 这里可以添加更多检查
                    }
                }
            }
        }

        private ITypeSymbol UnwrapTaskType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.Name == "Task" && namedType.TypeArguments.Length > 0)
                {
                    return namedType.TypeArguments[0];
                }
                if (namedType.Name == "List" || namedType.Name == "IEnumerable")
                {
                    if (namedType.TypeArguments.Length > 0)
                    {
                        return namedType.TypeArguments[0];
                    }
                }
            }
            return type;
        }

        private bool IsEntityType(INamedTypeSymbol type)
        {
            // 检查是否是实体类型（有TableName特性）
            return type.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "TableNameAttribute" ||
                a.AttributeClass?.Name == "TableName");
        }

        private string ConvertToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLowerInvariant(name[0]));

            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLowerInvariant(name[i]));
                }
                else
                {
                    result.Append(name[i]);
                }
            }

            return result.ToString();
        }
    }
}

