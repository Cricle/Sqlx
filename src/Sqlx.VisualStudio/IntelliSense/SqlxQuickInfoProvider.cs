using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using Sqlx.VisualStudio.Services;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Sqlx.VisualStudio.IntelliSense
{
    /// <summary>
    /// Sqlx QuickInfo源提供器
    /// 为鼠标悬浮在Sqlx方法上时提供SQL查询和方法详情信息
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType("CSharp")]
    [Name("Sqlx QuickInfo Provider")]
    internal class SqlxQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        private IComponentModel ComponentModel { get; set; }

        /// <summary>
        /// 尝试创建QuickInfo源
        /// </summary>
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new SqlxQuickInfoSource(textBuffer, ComponentModel);
        }
    }

    /// <summary>
    /// Sqlx QuickInfo源实现
    /// </summary>
    internal class SqlxQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ISqlxLanguageService _languageService;
        private readonly VisualStudioWorkspace _workspace;
        private bool _isDisposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SqlxQuickInfoSource(ITextBuffer textBuffer, IComponentModel componentModel)
        {
            _textBuffer = textBuffer;
            _languageService = componentModel.GetService<ISqlxLanguageService>();
            _workspace = componentModel.GetService<VisualStudioWorkspace>();
        }

        /// <summary>
        /// 获取QuickInfo项（异步）
        /// </summary>
        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            try
            {
                if (_isDisposed || _workspace == null || _languageService == null)
                    return null;

                // 获取触发点
                var triggerPoint = session.GetTriggerPoint(_textBuffer);
                if (!triggerPoint.HasValue)
                    return null;

                // 获取当前文档
                var document = _textBuffer.GetRelatedDocuments().FirstOrDefault();
                if (document == null)
                    return null;

                // 获取语义模型和语法树
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
                if (semanticModel == null || syntaxTree == null)
                    return null;

                var root = await syntaxTree.GetRootAsync(cancellationToken);
                var position = triggerPoint.Value.Position;

                // 查找包含当前位置的方法声明
                var methodDeclaration = root.FindToken(position).Parent?
                    .AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

                if (methodDeclaration == null)
                    return null;

                // 检查是否是Sqlx方法
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) as IMethodSymbol;
                if (methodSymbol == null)
                    return null;

                // 查找Sqlx特性
                var sqlxAttribute = methodSymbol.GetAttributes().FirstOrDefault(attr =>
                {
                    var fullName = attr.AttributeClass?.ToDisplayString() ?? "";
                    return fullName.Contains("Sqlx.Annotations.SqlxAttribute") ||
                           fullName.Contains("Sqlx.Annotations.SqlTemplateAttribute") ||
                           fullName.Contains("Sqlx.Annotations.ExpressionToSqlAttribute") ||
                           fullName.EndsWith("SqlxAttribute") ||
                           fullName.EndsWith("SqlTemplateAttribute") ||
                           fullName.EndsWith("ExpressionToSqlAttribute");
                });

                if (sqlxAttribute == null)
                    return null;

                // 提取SQL查询
                var sqlQuery = ExtractSqlQuery(sqlxAttribute);
                if (string.IsNullOrEmpty(sqlQuery))
                    return null;

                // 创建跟踪范围
                var line = triggerPoint.Value.GetContainingLine();
                var trackingSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
                    line.Start,
                    line.Length,
                    SpanTrackingMode.EdgeInclusive);

                // 构建方法信息
                var methodInfo = new SqlxMethodInfo(
                    methodSymbol.Name,
                    methodSymbol.ContainingType.Name,
                    methodSymbol.ContainingNamespace.ToDisplayString(),
                    sqlQuery,
                    document.FilePath ?? document.Name,
                    methodDeclaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    methodDeclaration.GetLocation().GetLineSpan().StartLinePosition.Character + 1,
                    methodSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    methodSymbol.Parameters.Select(p => new SqlxParameterInfo(p.Name, p.Type.ToDisplayString())).ToList(),
                    GetSqlxAttributeType(sqlxAttribute.AttributeClass)
                );

                // 生成QuickInfo内容
                var content = await GenerateQuickInfoContentAsync(methodInfo);
                return new QuickInfoItem(trackingSpan, content);
            }
            catch
            {
                // 静默处理异常，避免影响IDE稳定性
                return null;
            }
        }

        /// <summary>
        /// 提取SQL查询字符串
        /// </summary>
        private static string ExtractSqlQuery(AttributeData attribute)
        {
            // 尝试获取第一个构造函数参数
            if (attribute.ConstructorArguments.Length > 0)
            {
                var firstArg = attribute.ConstructorArguments[0];
                if (firstArg.Kind == TypedConstantKind.Primitive && firstArg.Value is string sqlQuery)
                {
                    return sqlQuery;
                }
            }

            // 尝试获取命名参数
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "Sql" || namedArg.Key == "Query")
                {
                    if (namedArg.Value.Kind == TypedConstantKind.Primitive && namedArg.Value.Value is string sqlValue)
                    {
                        return sqlValue;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取Sqlx特性类型
        /// </summary>
        private static SqlxAttributeType GetSqlxAttributeType(INamedTypeSymbol attributeClass)
        {
            if (attributeClass == null) return SqlxAttributeType.Sqlx;

            var fullName = attributeClass.ToDisplayString();
            if (fullName.Contains("SqlTemplateAttribute")) return SqlxAttributeType.SqlTemplate;
            if (fullName.Contains("ExpressionToSqlAttribute")) return SqlxAttributeType.ExpressionToSql;
            return SqlxAttributeType.Sqlx;
        }

        /// <summary>
        /// 生成QuickInfo内容
        /// </summary>
        private static async Task<object> GenerateQuickInfoContentAsync(SqlxMethodInfo methodInfo)
        {
            await Task.Yield();

            var textBlock = new TextBlock { TextWrapping = System.Windows.TextWrapping.Wrap };

            // 标题
            textBlock.Inlines.Add(new Run("🔍 Sqlx Method: ")
            {
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue)
            });
            textBlock.Inlines.Add(new Run(methodInfo.MethodName + Environment.NewLine)
            {
                FontWeight = System.Windows.FontWeights.Bold
            });

            // 方法签名
            textBlock.Inlines.Add(new Run("📝 Signature: ") { FontWeight = System.Windows.FontWeights.Bold });
            textBlock.Inlines.Add(new Run(methodInfo.MethodSignature + Environment.NewLine));

            // 位置信息
            textBlock.Inlines.Add(new Run("📂 Location: ") { FontWeight = System.Windows.FontWeights.Bold });
            textBlock.Inlines.Add(new Run($"{methodInfo.Namespace}.{methodInfo.ClassName}" + Environment.NewLine));

            // 特性类型
            textBlock.Inlines.Add(new Run("🏷️ Attribute: ") { FontWeight = System.Windows.FontWeights.Bold });
            textBlock.Inlines.Add(new Run($"[{methodInfo.AttributeType}]" + Environment.NewLine));

            // SQL查询
            textBlock.Inlines.Add(new Run("🗄️ SQL Query:" + Environment.NewLine)
            {
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green)
            });

            var formattedSql = FormatSql(methodInfo.SqlQuery);
            textBlock.Inlines.Add(new Run(formattedSql + Environment.NewLine)
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkSlateGray)
            });

            // 参数列表
            if (methodInfo.Parameters.Any())
            {
                textBlock.Inlines.Add(new Run("📋 Parameters:" + Environment.NewLine) { FontWeight = System.Windows.FontWeights.Bold });
                foreach (var param in methodInfo.Parameters)
                {
                    textBlock.Inlines.Add(new Run($"  • {param.Name}: {param.Type}" + Environment.NewLine)
                    {
                        FontFamily = new System.Windows.Media.FontFamily("Consolas")
                    });
                }
            }

            return textBlock;
        }

        /// <summary>
        /// 格式化SQL查询
        /// </summary>
        private static string FormatSql(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            // 简单的SQL格式化
            return sql.Replace("SELECT ", "SELECT \n    ")
                     .Replace(" FROM ", "\nFROM ")
                     .Replace(" WHERE ", "\nWHERE ")
                     .Replace(" AND ", "\n  AND ")
                     .Replace(" OR ", "\n  OR ")
                     .Replace(" ORDER BY ", "\nORDER BY ")
                     .Replace(" GROUP BY ", "\nGROUP BY ")
                     .Replace(" HAVING ", "\nHAVING ")
                     .Trim();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
