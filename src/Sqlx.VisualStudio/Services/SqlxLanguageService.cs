using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.VisualStudio.Services
{
    /// <summary>
    /// Sqlx语言服务实现
    /// 负责分析C#代码中的Sqlx特性并提取相关信息
    /// </summary>
    [Export(typeof(ISqlxLanguageService))]
    internal class SqlxLanguageService : ISqlxLanguageService
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly ConcurrentDictionary<DocumentId, IReadOnlyList<SqlxMethodInfo>> _documentCache = new();
        private readonly ConcurrentDictionary<ProjectId, IReadOnlyList<SqlxMethodInfo>> _projectCache = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="componentModel">组件模型服务</param>
        [ImportingConstructor]
        public SqlxLanguageService([Import] IComponentModel componentModel)
        {
            _workspace = componentModel.GetService<VisualStudioWorkspace>();
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize()
        {
            if (_workspace != null)
            {
                _workspace.WorkspaceChanged += OnWorkspaceChanged;
            }
        }

        /// <summary>
        /// 工作空间变化处理
        /// </summary>
        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            // 在工作空间发生重要变化时清除缓存
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionRemoved:
                    _documentCache.Clear();
                    _projectCache.Clear();
                    break;

                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectRemoved:
                    if (e.ProjectId != null)
                    {
                        _projectCache.TryRemove(e.ProjectId, out _);
                        // 清除项目下所有文档的缓存
                        var project = _workspace.CurrentSolution.GetProject(e.ProjectId);
                        if (project != null)
                        {
                            foreach (var docId in project.DocumentIds)
                            {
                                _documentCache.TryRemove(docId, out _);
                            }
                        }
                    }
                    break;

                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.DocumentReloaded:
                case WorkspaceChangeKind.DocumentRemoved:
                    if (e.DocumentId != null)
                    {
                        _documentCache.TryRemove(e.DocumentId, out _);
                        if (e.DocumentId.ProjectId != null)
                        {
                            _projectCache.TryRemove(e.DocumentId.ProjectId, out _);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取解决方案中所有带有Sqlx特性的方法信息
        /// </summary>
        public async Task<IReadOnlyList<SqlxMethodInfo>> GetSqlxMethodsAsync()
        {
            if (_workspace?.CurrentSolution == null)
                return Array.Empty<SqlxMethodInfo>();

            await _semaphore.WaitAsync();
            try
            {
                var allMethods = new List<SqlxMethodInfo>();
                foreach (var project in _workspace.CurrentSolution.Projects)
                {
                    if (!_projectCache.TryGetValue(project.Id, out var projectMethods))
                    {
                        projectMethods = await AnalyzeProjectAsync(project);
                        _projectCache.TryAdd(project.Id, projectMethods);
                    }
                    allMethods.AddRange(projectMethods);
                }
                return allMethods;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 获取指定文档中所有带有Sqlx特性的方法信息
        /// </summary>
        public async Task<IReadOnlyList<SqlxMethodInfo>> GetSqlxMethodsInDocumentAsync(Document document)
        {
            if (document == null)
                return Array.Empty<SqlxMethodInfo>();

            await _semaphore.WaitAsync();
            try
            {
                if (_documentCache.TryGetValue(document.Id, out var methods))
                {
                    return methods;
                }

                methods = await AnalyzeDocumentAsync(document);
                _documentCache.TryAdd(document.Id, methods);
                return methods;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 刷新语言服务缓存
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _documentCache.Clear();
                _projectCache.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 分析项目中的所有文档
        /// </summary>
        private async Task<IReadOnlyList<SqlxMethodInfo>> AnalyzeProjectAsync(Project project)
        {
            var projectMethods = new List<SqlxMethodInfo>();

            foreach (var document in project.Documents)
            {
                if (document.SupportsSyntaxTree && document.SupportsSemanticModel)
                {
                    var documentMethods = await AnalyzeDocumentAsync(document);
                    projectMethods.AddRange(documentMethods);
                }
            }

            return projectMethods;
        }

        /// <summary>
        /// 分析单个文档
        /// </summary>
        private async Task<IReadOnlyList<SqlxMethodInfo>> AnalyzeDocumentAsync(Document document)
        {
            try
            {
                var methods = new List<SqlxMethodInfo>();
                var semanticModel = await document.GetSemanticModelAsync();
                var syntaxTree = await document.GetSyntaxTreeAsync();

                if (semanticModel == null || syntaxTree == null)
                    return methods;

                var root = await syntaxTree.GetRootAsync();

                // 查找所有方法声明
                foreach (var methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
                    if (methodSymbol == null) continue;

                    // 检查方法的特性
                    foreach (var attribute in methodSymbol.GetAttributes())
                    {
                        var attributeType = GetSqlxAttributeType(attribute.AttributeClass);
                        if (attributeType.HasValue)
                        {
                            // 获取SQL查询参数
                            var sqlQuery = ExtractSqlQuery(attribute);
                            if (!string.IsNullOrEmpty(sqlQuery))
                            {
                                var location = methodDeclaration.GetLocation().GetLineSpan();
                                var filePath = document.FilePath ?? document.Name;
                                var lineNumber = location.StartLinePosition.Line + 1;
                                var columnNumber = location.StartLinePosition.Character + 1;

                                var parameters = methodSymbol.Parameters
                                    .Select(p => new SqlxParameterInfo(p.Name, p.Type.ToDisplayString()))
                                    .ToList();

                                methods.Add(new SqlxMethodInfo(
                                    methodSymbol.Name,
                                    methodSymbol.ContainingType.Name,
                                    methodSymbol.ContainingNamespace.ToDisplayString(),
                                    sqlQuery,
                                    filePath,
                                    lineNumber,
                                    columnNumber,
                                    methodSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                    parameters,
                                    attributeType.Value));
                            }
                        }
                    }
                }

                return methods;
            }
            catch
            {
                // 静默处理分析异常
                return Array.Empty<SqlxMethodInfo>();
            }
        }

        /// <summary>
        /// 获取Sqlx特性类型
        /// </summary>
        private static SqlxAttributeType? GetSqlxAttributeType(INamedTypeSymbol attributeClass)
        {
            if (attributeClass == null) return null;

            var fullName = attributeClass.ToDisplayString();
            if (fullName.Contains("Sqlx.Annotations.SqlxAttribute") || fullName.EndsWith("SqlxAttribute"))
                return SqlxAttributeType.Sqlx;
            if (fullName.Contains("Sqlx.Annotations.SqlTemplateAttribute") || fullName.EndsWith("SqlTemplateAttribute"))
                return SqlxAttributeType.SqlTemplate;
            if (fullName.Contains("Sqlx.Annotations.ExpressionToSqlAttribute") || fullName.EndsWith("ExpressionToSqlAttribute"))
                return SqlxAttributeType.ExpressionToSql;

            return null;
        }

        /// <summary>
        /// 提取SQL查询字符串
        /// </summary>
        private static string ExtractSqlQuery(AttributeData attribute)
        {
            // 尝试获取第一个构造函数参数作为SQL查询
            if (attribute.ConstructorArguments.Length > 0)
            {
                var firstArg = attribute.ConstructorArguments[0];
                if (firstArg.Kind == TypedConstantKind.Primitive && firstArg.Value is string sqlQuery)
                {
                    return sqlQuery;
                }
            }

            // 如果没有构造函数参数，尝试获取命名参数
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
    }
}
