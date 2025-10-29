// -----------------------------------------------------------------------
// <copyright file="RepositoryExplorerWindow.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// Repository Explorer tool window for browsing all Sqlx repositories in the project.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000004")]
    public class RepositoryExplorerWindow : ToolWindowPane
    {
        private RepositoryExplorerControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryExplorerWindow"/> class.
        /// </summary>
        public RepositoryExplorerWindow() : base(null)
        {
            this.Caption = "Sqlx Repository Explorer";
            this.control = new RepositoryExplorerControl();
            this.Content = this.control;
        }
    }

    /// <summary>
    /// Repository information.
    /// </summary>
    public class RepositoryInfo
    {
        public string Name { get; set; }
        public string Database { get; set; }
        public List<MethodInfo> Methods { get; set; }
    }

    /// <summary>
    /// Method information.
    /// </summary>
    public class MethodInfo
    {
        public string Name { get; set; }
        public string Type { get; set; } // Select, Insert, Update, Delete, etc.
        public string Icon { get; set; }
    }

    /// <summary>
    /// User control for repository explorer content.
    /// </summary>
    public class RepositoryExplorerControl : UserControl
    {
        private TextBox searchBox;
        private TextBlock statsText;
        private TreeView repositoryTreeView;
        private List<RepositoryInfo> repositories;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryExplorerControl"/> class.
        /// </summary>
        public RepositoryExplorerControl()
        {
            this.repositories = new List<RepositoryInfo>();
            InitializeComponent();
            LoadSampleData();
        }

        private void InitializeComponent()
        {
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Title
            var titleLabel = new TextBlock
            {
                Text = "Sqlx Repository Explorer",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(titleLabel);

            // Search box
            var searchPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var searchLabel = new TextBlock
            {
                Text = "🔍",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            searchPanel.Children.Add(searchLabel);

            this.searchBox = new TextBox
            {
                Width = 300
            };
            this.searchBox.TextChanged += OnSearchTextChanged;
            searchPanel.Children.Add(this.searchBox);

            mainPanel.Children.Add(searchPanel);

            // Statistics
            this.statsText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.Bold
            };
            mainPanel.Children.Add(this.statsText);

            // Repository tree
            var treeLabel = new TextBlock
            {
                Text = "📁 Repositories:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(treeLabel);

            this.repositoryTreeView = new TreeView
            {
                Height = 500
            };
            mainPanel.Children.Add(this.repositoryTreeView);

            // Action buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var refreshButton = new Button
            {
                Content = "🔄 Refresh",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            refreshButton.Click += OnRefreshClick;
            buttonPanel.Children.Add(refreshButton);

            var expandButton = new Button
            {
                Content = "▼ Expand All",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            expandButton.Click += OnExpandAllClick;
            buttonPanel.Children.Add(expandButton);

            var collapseButton = new Button
            {
                Content = "▲ Collapse All",
                Padding = new Thickness(10, 5, 10, 5)
            };
            collapseButton.Click += OnCollapseAllClick;
            buttonPanel.Children.Add(collapseButton);

            mainPanel.Children.Add(buttonPanel);

            // Scroll viewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = mainPanel
            };

            this.Content = scrollViewer;
        }

        private void LoadSampleData()
        {
            // Sample data for demonstration
            this.repositories.Add(new RepositoryInfo
            {
                Name = "IUserRepository",
                Database = "SQLite",
                Methods = new List<MethodInfo>
                {
                    new MethodInfo { Name = "GetByIdAsync", Type = "Select", Icon = "🔍" },
                    new MethodInfo { Name = "GetAllAsync", Type = "Select", Icon = "📝" },
                    new MethodInfo { Name = "InsertAsync", Type = "Insert", Icon = "➕" },
                    new MethodInfo { Name = "UpdateAsync", Type = "Update", Icon = "✏️" },
                    new MethodInfo { Name = "DeleteAsync", Type = "Delete", Icon = "❌" }
                }
            });

            this.repositories.Add(new RepositoryInfo
            {
                Name = "IProductRepository",
                Database = "MySQL",
                Methods = new List<MethodInfo>
                {
                    new MethodInfo { Name = "GetByIdAsync", Type = "Select", Icon = "🔍" },
                    new MethodInfo { Name = "GetByCategoryAsync", Type = "Select", Icon = "📋" },
                    new MethodInfo { Name = "GetByPriceRangeAsync", Type = "Select", Icon = "💰" }
                }
            });

            this.repositories.Add(new RepositoryInfo
            {
                Name = "IOrderRepository",
                Database = "PostgreSQL",
                Methods = new List<MethodInfo>
                {
                    new MethodInfo { Name = "GetByIdAsync", Type = "Select", Icon = "🔍" },
                    new MethodInfo { Name = "GetByUserIdAsync", Type = "Select", Icon = "👤" }
                }
            });

            UpdateTreeView();
            UpdateStatistics();
        }

        private void UpdateTreeView()
        {
            this.repositoryTreeView.Items.Clear();

            string searchText = this.searchBox?.Text?.ToLower() ?? "";
            var filteredRepos = string.IsNullOrEmpty(searchText)
                ? this.repositories
                : this.repositories.Where(r =>
                    r.Name.ToLower().Contains(searchText) ||
                    r.Methods.Any(m => m.Name.ToLower().Contains(searchText))).ToList();

            foreach (var repo in filteredRepos)
            {
                var repoNode = new TreeViewItem
                {
                    Header = $"👤 {repo.Name} ({repo.Database})",
                    IsExpanded = true
                };

                foreach (var method in repo.Methods)
                {
                    var methodNode = new TreeViewItem
                    {
                        Header = $"{method.Icon} {method.Name}",
                        Tag = new Tuple<RepositoryInfo, MethodInfo>(repo, method)
                    };

                    // Add context menu
                    var contextMenu = new ContextMenu();

                    var gotoDefItem = new MenuItem { Header = "📂 Go to Definition" };
                    gotoDefItem.Click += (s, e) => OnGoToDefinition(repo, method);
                    contextMenu.Items.Add(gotoDefItem);

                    var viewCodeItem = new MenuItem { Header = "🔬 View Generated Code" };
                    viewCodeItem.Click += (s, e) => OnViewGeneratedCode(repo, method);
                    contextMenu.Items.Add(viewCodeItem);

                    var viewSqlItem = new MenuItem { Header = "📊 View SQL Preview" };
                    viewSqlItem.Click += (s, e) => OnViewSqlPreview(repo, method);
                    contextMenu.Items.Add(contextMenu.Items);

                    var testItem = new MenuItem { Header = "🧪 Test Query" };
                    testItem.Click += (s, e) => OnTestQuery(repo, method);
                    contextMenu.Items.Add(testItem);

                    contextMenu.Items.Add(new Separator());

                    var addCrudItem = new MenuItem { Header = "➕ Add CRUD Methods" };
                    addCrudItem.Click += (s, e) => OnAddCrudMethods(repo);
                    contextMenu.Items.Add(addCrudItem);

                    methodNode.ContextMenu = contextMenu;
                    repoNode.Items.Add(methodNode);
                }

                this.repositoryTreeView.Items.Add(repoNode);
            }
        }

        private void UpdateStatistics()
        {
            int totalMethods = this.repositories.Sum(r => r.Methods.Count);
            var databases = this.repositories.Select(r => r.Database).Distinct().Count();

            this.statsText.Text = $"📊 Statistics: {this.repositories.Count} Repositories • {totalMethods} Methods • {databases} Database Types";
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTreeView();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            // TODO: Re-scan project for repositories
            MessageBox.Show("Scanning project for repositories...", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnExpandAllClick(object sender, RoutedEventArgs e)
        {
            ExpandOrCollapseAll(true);
        }

        private void OnCollapseAllClick(object sender, RoutedEventArgs e)
        {
            ExpandOrCollapseAll(false);
        }

        private void ExpandOrCollapseAll(bool expand)
        {
            foreach (var item in this.repositoryTreeView.Items)
            {
                if (item is TreeViewItem treeItem)
                {
                    treeItem.IsExpanded = expand;
                }
            }
        }

        private void OnGoToDefinition(RepositoryInfo repo, MethodInfo method)
        {
            MessageBox.Show($"Navigating to {repo.Name}.{method.Name}...", "Go to Definition", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnViewGeneratedCode(RepositoryInfo repo, MethodInfo method)
        {
            MessageBox.Show($"Opening generated code for {repo.Name}.{method.Name}...", "Generated Code", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnViewSqlPreview(RepositoryInfo repo, MethodInfo method)
        {
            MessageBox.Show($"Opening SQL preview for {repo.Name}.{method.Name}...", "SQL Preview", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnTestQuery(RepositoryInfo repo, MethodInfo method)
        {
            MessageBox.Show($"Opening query tester for {repo.Name}.{method.Name}...", "Query Tester", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnAddCrudMethods(RepositoryInfo repo)
        {
            MessageBox.Show($"Adding CRUD methods to {repo.Name}...", "Add CRUD", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

