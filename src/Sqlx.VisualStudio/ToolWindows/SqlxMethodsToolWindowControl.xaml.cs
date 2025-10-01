using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Sqlx.VisualStudio.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Sqlx.VisualStudio.ToolWindows
{
    /// <summary>
    /// Sqlx方法工具窗口控件
    /// </summary>
    public partial class SqlxMethodsToolWindowControl : UserControl
    {
        private readonly ISqlxLanguageService _languageService;
        private List<SqlxMethodInfoViewModel> _allMethods = new();
        private bool _isLoading = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SqlxMethodsToolWindowControl()
        {
            InitializeComponent();

            // 获取语言服务
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            _languageService = componentModel?.GetService<ISqlxLanguageService>();

            Loaded += OnLoaded;
        }

        /// <summary>
        /// 控件加载事件
        /// </summary>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await RefreshMethodsAsync();
        }

        /// <summary>
        /// 搜索框文本变化事件
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterMethods();
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshMethodsAsync();
        }

        /// <summary>
        /// 方法列表选择变化事件
        /// </summary>
        private void MethodsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMethod = MethodsListView.SelectedItem as SqlxMethodInfoViewModel;
            ShowMethodDetails(selectedMethod);
        }

        /// <summary>
        /// 方法列表双击事件
        /// </summary>
        private void MethodsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedMethod = MethodsListView.SelectedItem as SqlxMethodInfoViewModel;
            if (selectedMethod != null)
            {
                NavigateToMethod(selectedMethod);
            }
        }

        /// <summary>
        /// 刷新方法列表
        /// </summary>
        private async Task RefreshMethodsAsync()
        {
            if (_isLoading || _languageService == null)
                return;

            _isLoading = true;
            RefreshButton.IsEnabled = false;
            RefreshButton.Content = "🔄 正在加载...";

            try
            {
                await _languageService.RefreshCacheAsync();
                var methods = await _languageService.GetSqlxMethodsAsync();

                _allMethods = methods.Select(m => new SqlxMethodInfoViewModel(m)).ToList();
                FilterMethods();
            }
            catch (Exception ex)
            {
                // 显示错误信息
                MessageBox.Show($"加载Sqlx方法时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _isLoading = false;
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "🔄 刷新";
            }
        }

        /// <summary>
        /// 过滤方法列表
        /// </summary>
        private void FilterMethods()
        {
            var searchText = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";

            var filteredMethods = string.IsNullOrEmpty(searchText)
                ? _allMethods
                : _allMethods.Where(m =>
                    m.MethodName.ToLowerInvariant().Contains(searchText) ||
                    m.ClassName.ToLowerInvariant().Contains(searchText) ||
                    m.SqlQuery.ToLowerInvariant().Contains(searchText) ||
                    m.Namespace.ToLowerInvariant().Contains(searchText)
                ).ToList();

            MethodsListView.ItemsSource = filteredMethods;
        }

        /// <summary>
        /// 显示方法详情
        /// </summary>
        private void ShowMethodDetails(SqlxMethodInfoViewModel method)
        {
            if (method == null)
            {
                DetailsExpander.IsExpanded = false;
                return;
            }

            MethodSignatureText.Text = $"{method.MethodSignature}";
            SqlQueryText.Text = FormatSql(method.SqlQuery);

            var parametersText = method.Parameters.Any()
                ? $"Parameters: {string.Join(", ", method.Parameters.Select(p => $"{p.Name}: {p.Type}"))}"
                : "No parameters";
            ParametersText.Text = parametersText;

            DetailsExpander.IsExpanded = true;
        }

        /// <summary>
        /// 导航到方法定义
        /// </summary>
        private void NavigateToMethod(SqlxMethodInfoViewModel method)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                if (dte == null) return;

                // 打开文件
                if (!string.IsNullOrEmpty(method.FilePath) && File.Exists(method.FilePath))
                {
                    dte.ItemOperations.OpenFile(method.FilePath);

                    // 跳转到指定行
                    var textSelection = (EnvDTE.TextSelection)dte.ActiveDocument.Selection;
                    textSelection.GotoLine(method.LineNumber, false);
                    textSelection.SelectLine();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到方法时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 格式化SQL查询
        /// </summary>
        private static string FormatSql(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            return sql.Replace("SELECT ", "SELECT\n    ")
                     .Replace(" FROM ", "\nFROM ")
                     .Replace(" WHERE ", "\nWHERE ")
                     .Replace(" AND ", "\n  AND ")
                     .Replace(" OR ", "\n  OR ")
                     .Replace(" ORDER BY ", "\nORDER BY ")
                     .Replace(" GROUP BY ", "\nGROUP BY ")
                     .Replace(" HAVING ", "\nHAVING ")
                     .Replace(" INNER JOIN ", "\nINNER JOIN ")
                     .Replace(" LEFT JOIN ", "\nLEFT JOIN ")
                     .Replace(" RIGHT JOIN ", "\nRIGHT JOIN ")
                     .Trim();
        }
    }

    /// <summary>
    /// Sqlx方法信息视图模型
    /// </summary>
    public class SqlxMethodInfoViewModel
    {
        private readonly SqlxMethodInfo _methodInfo;

        public SqlxMethodInfoViewModel(SqlxMethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        public string MethodName => _methodInfo.MethodName;
        public string ClassName => _methodInfo.ClassName;
        public string Namespace => _methodInfo.Namespace;
        public string SqlQuery => _methodInfo.SqlQuery;
        public string SqlPreview => SqlQuery.Length > 100 ? SqlQuery.Substring(0, 97) + "..." : SqlQuery;
        public string FilePath => _methodInfo.FilePath;
        public string FileName => Path.GetFileName(_methodInfo.FilePath);
        public int LineNumber => _methodInfo.LineNumber;
        public int ColumnNumber => _methodInfo.ColumnNumber;
        public string MethodSignature => _methodInfo.MethodSignature;
        public IReadOnlyList<SqlxParameterInfo> Parameters => _methodInfo.Parameters;
        public SqlxAttributeType AttributeType => _methodInfo.AttributeType;
    }
}
