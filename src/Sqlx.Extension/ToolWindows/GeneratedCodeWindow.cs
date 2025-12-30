// -----------------------------------------------------------------------
// <copyright file="GeneratedCodeWindow.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// Generated Code Viewer tool window for displaying Roslyn generated code.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000002")]
    public class GeneratedCodeWindow : ToolWindowPane
    {
        private GeneratedCodeControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedCodeWindow"/> class.
        /// </summary>
        public GeneratedCodeWindow() : base(null)
        {
            this.Caption = "Sqlx Generated Code";
            this.control = new GeneratedCodeControl();
            this.Content = this.control;
        }

        /// <summary>
        /// Updates the generated code view.
        /// </summary>
        /// <param name="files">List of generated files</param>
        public void UpdateGeneratedCode(List<GeneratedFileInfo> files)
        {
            this.control?.UpdateGeneratedCode(files);
        }
    }

    /// <summary>
    /// Information about a generated file.
    /// </summary>
    public class GeneratedFileInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Content { get; set; }
        public string Category { get; set; } // Repository, Method, etc.
    }

    /// <summary>
    /// User control for generated code content.
    /// </summary>
    public class GeneratedCodeControl : UserControl
    {
        private TreeView fileTreeView;
        private TextBox codeTextBox;
        private TextBlock fileNameLabel;
        private List<GeneratedFileInfo> generatedFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedCodeControl"/> class.
        /// </summary>
        public GeneratedCodeControl()
        {
            this.generatedFiles = new List<GeneratedFileInfo>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left panel - File tree
            var leftPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var treeLabel = new TextBlock
            {
                Text = "📁 Generated Files",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            leftPanel.Children.Add(treeLabel);

            this.fileTreeView = new TreeView
            {
                Height = 500
            };
            this.fileTreeView.SelectedItemChanged += OnTreeViewSelectionChanged;
            leftPanel.Children.Add(this.fileTreeView);

            var refreshButton = new Button
            {
                Content = "🔄 Refresh",
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            refreshButton.Click += OnRefreshClick;
            leftPanel.Children.Add(refreshButton);

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            // Splitter
            var splitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = System.Windows.Media.Brushes.LightGray
            };
            Grid.SetColumn(splitter, 1);
            grid.Children.Add(splitter);

            // Right panel - Code viewer
            var rightPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            this.fileNameLabel = new TextBlock
            {
                Text = "Select a file to view code",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            rightPanel.Children.Add(this.fileNameLabel);

            this.codeTextBox = new TextBox
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Height = 500
            };
            rightPanel.Children.Add(this.codeTextBox);

            // Action buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var copyButton = new Button
            {
                Content = "📋 Copy Code",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            copyButton.Click += OnCopyClick;
            buttonPanel.Children.Add(copyButton);

            var openButton = new Button
            {
                Content = "📂 Open in Editor",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            openButton.Click += OnOpenClick;
            buttonPanel.Children.Add(openButton);

            var saveButton = new Button
            {
                Content = "💾 Save",
                Padding = new Thickness(10, 5, 10, 5)
            };
            saveButton.Click += OnSaveClick;
            buttonPanel.Children.Add(saveButton);

            rightPanel.Children.Add(buttonPanel);

            Grid.SetColumn(rightPanel, 2);
            grid.Children.Add(rightPanel);

            this.Content = grid;
        }

        /// <summary>
        /// Updates the generated code view with new files.
        /// </summary>
        public void UpdateGeneratedCode(List<GeneratedFileInfo> files)
        {
            this.generatedFiles = files ?? new List<GeneratedFileInfo>();
            PopulateTreeView();
        }

        private void PopulateTreeView()
        {
            this.fileTreeView.Items.Clear();

            // Group files by repository
            var repositoryGroups = new Dictionary<string, List<GeneratedFileInfo>>();

            foreach (var file in this.generatedFiles)
            {
                string repoName = ExtractRepositoryName(file.FileName);
                if (!repositoryGroups.ContainsKey(repoName))
                {
                    repositoryGroups[repoName] = new List<GeneratedFileInfo>();
                }
                repositoryGroups[repoName].Add(file);
            }

            // Create tree nodes
            foreach (var group in repositoryGroups)
            {
                var repoNode = new TreeViewItem
                {
                    Header = $"📁 {group.Key}",
                    IsExpanded = true
                };

                foreach (var file in group.Value)
                {
                    var fileNode = new TreeViewItem
                    {
                        Header = $"📄 {file.FileName}",
                        Tag = file
                    };
                    repoNode.Items.Add(fileNode);
                }

                this.fileTreeView.Items.Add(repoNode);
            }
        }

        private string ExtractRepositoryName(string fileName)
        {
            // Extract repository name from generated file name
            // e.g., "UserRepository.g.cs" -> "UserRepository"
            if (fileName.Contains("."))
            {
                return fileName.Substring(0, fileName.IndexOf('.'));
            }
            return fileName;
        }

        private void OnTreeViewSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is GeneratedFileInfo file)
            {
                this.fileNameLabel.Text = $"📝 {file.FileName}";
                this.codeTextBox.Text = file.Content;
            }
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            // TODO: Re-scan generated files from obj/Debug/generated folder
            MessageBox.Show("Scanning for generated files...", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(this.codeTextBox.Text);
                MessageBox.Show("Code copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenClick(object sender, RoutedEventArgs e)
        {
            // TODO: Open file in Visual Studio editor
            MessageBox.Show("Opening in editor...", "Open", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "C# Files (*.cs)|*.cs|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".cs",
                FileName = this.fileNameLabel.Text.Replace("📝 ", "")
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    System.IO.File.WriteAllText(dialog.FileName, this.codeTextBox.Text);
                    MessageBox.Show($"Code saved to {dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

