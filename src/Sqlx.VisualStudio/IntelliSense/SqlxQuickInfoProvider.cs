using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Sqlx.VisualStudio.IntelliSense
{
    /// <summary>
    /// Sqlx QuickInfo源提供器 - 简化独立版本
    /// 为鼠标悬浮在Sqlx方法上时提供SQL查询信息
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType("CSharp")]
    [Name("Sqlx QuickInfo Provider")]
    internal class SqlxQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        private IComponentModel ComponentModel { get; set; } = null!;

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new SqlxQuickInfoSource(textBuffer, ComponentModel);
        }
    }

    /// <summary>
    /// Sqlx QuickInfo源 - 简化独立版本
    /// </summary>
    internal class SqlxQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly VisualStudioWorkspace? _workspace;
        private bool _isDisposed;

        public SqlxQuickInfoSource(ITextBuffer textBuffer, IComponentModel componentModel)
        {
            _textBuffer = textBuffer;
            _workspace = componentModel.GetService<VisualStudioWorkspace>();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        /// <summary>
        /// 获取QuickInfo项（异步）- 简化版本
        /// </summary>
        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            try
            {
                if (_isDisposed || _workspace == null)
                    return null!;

                // 获取触发点
                var triggerPoint = session.GetTriggerPoint(_textBuffer);
                if (triggerPoint == null)
                    return null!;

                // 创建简单的跟踪范围
                var currentPosition = triggerPoint.GetPosition(_textBuffer.CurrentSnapshot);
                var trackingSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
                    currentPosition,
                    Math.Min(50, _textBuffer.CurrentSnapshot.Length - currentPosition),
                    SpanTrackingMode.EdgeInclusive);

                // 尝试找到Sqlx相关的方法
                var sqlInfo = await TryExtractSqlInfoAsync(_workspace, triggerPoint, cancellationToken);
                if (string.IsNullOrEmpty(sqlInfo))
                    return null!;

                // 生成简单的QuickInfo内容
                var content = $"🔍 Sqlx Method\n📝 SQL: {sqlInfo}";

                return new QuickInfoItem(trackingSpan, content);
            }
            catch (Exception)
            {
                // 静默处理错误，避免影响IDE
                return null!;
            }
        }

        /// <summary>
        /// 尝试提取SQL信息 - 简化版本
        /// </summary>
        private async Task<string?> TryExtractSqlInfoAsync(VisualStudioWorkspace workspace, ITrackingPoint triggerPoint, CancellationToken cancellationToken)
        {
            try
            {
                // 获取当前文档
                var documents = workspace.CurrentSolution.Projects.SelectMany(p => p.Documents);
                var document = documents.FirstOrDefault();

                if (document == null)
                    return null;

                // 获取语法树
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
                if (syntaxTree == null)
                    return null;

                var root = await syntaxTree.GetRootAsync(cancellationToken);
                var position = triggerPoint.GetPosition(_textBuffer.CurrentSnapshot);

                // 查找包含当前位置的方法声明
                var methodDeclaration = root.FindToken(position).Parent?
                    .AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

                if (methodDeclaration == null)
                    return null;

                // 获取语义模型
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                if (semanticModel == null)
                    return null;

                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
                if (methodSymbol == null)
                    return null;

                // 检查Sqlx特性
                foreach (var attribute in methodSymbol.GetAttributes())
                {
                    var attributeName = attribute.AttributeClass?.Name;
                    if (attributeName != null && IsSqlxAttribute(attributeName))
                    {
                        var sqlValue = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
                        if (!string.IsNullOrEmpty(sqlValue))
                        {
                            // 限制显示长度，避免过长的SQL查询
                            return sqlValue.Length > 200
                                ? sqlValue.Substring(0, 200) + "..."
                                : sqlValue;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 判断是否为Sqlx相关特性
        /// </summary>
        private static bool IsSqlxAttribute(string attributeName)
        {
            return attributeName switch
            {
                "SqlxAttribute" or "Sqlx" => true,
                "SqlTemplateAttribute" or "SqlTemplate" => true,
                "ExpressionToSqlAttribute" or "ExpressionToSql" => true,
                _ => false
            };
        }
    }
}
