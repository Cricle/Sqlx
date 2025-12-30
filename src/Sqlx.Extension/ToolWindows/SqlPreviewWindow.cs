// -----------------------------------------------------------------------
// <copyright file="SqlPreviewWindow.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// SQL Preview tool window for displaying generated SQL in real-time.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000001")]
    public class SqlPreviewWindow : ToolWindowPane
    {
        private SqlPreviewControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlPreviewWindow"/> class.
        /// </summary>
        public SqlPreviewWindow() : base(null)
        {
            this.Caption = "Sqlx SQL Preview";
            this.control = new SqlPreviewControl();
            this.Content = this.control;
        }

        /// <summary>
        /// Updates the SQL preview with new content.
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="template">SQL template</param>
        /// <param name="dialect">Database dialect</param>
        /// <param name="generatedSql">Generated SQL</param>
        public void UpdatePreview(string methodName, string template, string dialect, string generatedSql)
        {
            this.control?.UpdatePreview(methodName, template, dialect, generatedSql);
        }
    }

    /// <summary>
    /// User control for SQL preview content.
    /// </summary>
    public class SqlPreviewControl : UserControl
    {
        private TextBlock methodNameText;
        private TextBlock templateText;
        private ComboBox dialectCombo;
        private TextBox sqlTextBox;
        private StackPanel mainPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlPreviewControl"/> class.
        /// </summary>
        public SqlPreviewControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.mainPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Method name section
            var methodLabel = new TextBlock
            {
                Text = "Method:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            this.mainPanel.Children.Add(methodLabel);

            this.methodNameText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            };
            this.mainPanel.Children.Add(this.methodNameText);

            // Template section
            var templateLabel = new TextBlock
            {
                Text = "Template:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            this.mainPanel.Children.Add(templateLabel);

            this.templateText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap
            };
            this.mainPanel.Children.Add(this.templateText);

            // Database dialect selector
            var dialectPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var dialectLabel = new TextBlock
            {
                Text = "🎯 Database:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            dialectPanel.Children.Add(dialectLabel);

            this.dialectCombo = new ComboBox
            {
                Width = 150
            };
            this.dialectCombo.Items.Add("SQLite");
            this.dialectCombo.Items.Add("MySQL");
            this.dialectCombo.Items.Add("PostgreSQL");
            this.dialectCombo.Items.Add("SQL Server");
            this.dialectCombo.Items.Add("Oracle");
            this.dialectCombo.SelectedIndex = 0;
            this.dialectCombo.SelectionChanged += OnDialectChanged;
            dialectPanel.Children.Add(this.dialectCombo);

            this.mainPanel.Children.Add(dialectPanel);

            // Generated SQL section
            var sqlLabel = new TextBlock
            {
                Text = "Generated SQL:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            this.mainPanel.Children.Add(sqlLabel);

            this.sqlTextBox = new TextBox
            {
                Height = 200,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Margin = new Thickness(0, 0, 0, 10)
            };
            this.mainPanel.Children.Add(this.sqlTextBox);

            // Action buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var copyButton = new Button
            {
                Content = "📋 Copy SQL",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            copyButton.Click += OnCopyClick;
            buttonPanel.Children.Add(copyButton);

            var refreshButton = new Button
            {
                Content = "🔄 Refresh",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            refreshButton.Click += OnRefreshClick;
            buttonPanel.Children.Add(refreshButton);

            var exportButton = new Button
            {
                Content = "💾 Export",
                Padding = new Thickness(10, 5, 10, 5)
            };
            exportButton.Click += OnExportClick;
            buttonPanel.Children.Add(exportButton);

            this.mainPanel.Children.Add(buttonPanel);

            // Scroll viewer for main panel
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = this.mainPanel
            };

            this.Content = scrollViewer;
        }

        /// <summary>
        /// Updates the preview with new SQL content.
        /// </summary>
        public void UpdatePreview(string methodName, string template, string dialect, string generatedSql)
        {
            this.methodNameText.Text = methodName;
            this.templateText.Text = template;
            this.sqlTextBox.Text = generatedSql;

            // Select dialect
            int dialectIndex = this.dialectCombo.Items.IndexOf(dialect);
            if (dialectIndex >= 0)
            {
                this.dialectCombo.SelectedIndex = dialectIndex;
            }
        }

        private void OnDialectChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Re-generate SQL for selected dialect
            // This would call the template engine with the new dialect
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(this.sqlTextBox.Text);
                MessageBox.Show("SQL copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            // TODO: Refresh preview by re-parsing current editor context
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            // TODO: Export SQL to file
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".sql",
                FileName = $"{this.methodNameText.Text}.sql"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    System.IO.File.WriteAllText(dialog.FileName, this.sqlTextBox.Text);
                    MessageBox.Show($"SQL exported to {dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

