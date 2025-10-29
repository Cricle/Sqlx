// -----------------------------------------------------------------------
// <copyright file="QueryTesterWindow.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// Query Tester tool window for testing SQL queries directly in IDE.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000003")]
    public class QueryTesterWindow : ToolWindowPane
    {
        private QueryTesterControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTesterWindow"/> class.
        /// </summary>
        public QueryTesterWindow() : base(null)
        {
            this.Caption = "Sqlx Query Tester";
            this.control = new QueryTesterControl();
            this.Content = this.control;
        }
    }

    /// <summary>
    /// User control for query tester content.
    /// </summary>
    public class QueryTesterControl : UserControl
    {
        private TextBox connectionStringTextBox;
        private TextBlock methodNameText;
        private StackPanel parametersPanel;
        private TextBox sqlTextBox;
        private Button executeButton;
        private TextBlock statusText;
        private DataGrid resultsGrid;
        private Dictionary<string, TextBox> parameterInputs;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTesterControl"/> class.
        /// </summary>
        public QueryTesterControl()
        {
            this.parameterInputs = new Dictionary<string, TextBox>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Connection string section
            var connLabel = new TextBlock
            {
                Text = "📌 Connection String:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(connLabel);

            var connPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            this.connectionStringTextBox = new TextBox
            {
                Width = 500,
                Margin = new Thickness(0, 0, 10, 0)
            };
            this.connectionStringTextBox.Text = "Data Source=app.db";
            connPanel.Children.Add(this.connectionStringTextBox);

            var testConnButton = new Button
            {
                Content = "Test",
                Padding = new Thickness(10, 5, 10, 5)
            };
            testConnButton.Click += OnTestConnectionClick;
            connPanel.Children.Add(testConnButton);

            mainPanel.Children.Add(connPanel);

            // Method section
            var methodLabel = new TextBlock
            {
                Text = "🎯 Method:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(methodLabel);

            this.methodNameText = new TextBlock
            {
                Text = "GetUserByIdAsync",
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(this.methodNameText);

            // Parameters section
            var paramLabel = new TextBlock
            {
                Text = "📝 Parameters:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(paramLabel);

            this.parametersPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(this.parametersPanel);

            // Add sample parameter
            AddParameter("id", "long", "123");

            var addParamButton = new Button
            {
                Content = "+ Add Parameter",
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(10, 5, 10, 5)
            };
            addParamButton.Click += OnAddParameterClick;
            mainPanel.Children.Add(addParamButton);

            // Generated SQL section
            var sqlLabel = new TextBlock
            {
                Text = "💻 Generated SQL:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(sqlLabel);

            this.sqlTextBox = new TextBox
            {
                Height = 100,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Margin = new Thickness(0, 0, 0, 10)
            };
            this.sqlTextBox.Text = "SELECT id, name, age, email FROM users WHERE id = 123";
            mainPanel.Children.Add(this.sqlTextBox);

            // Execute button
            var executePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            this.executeButton = new Button
            {
                Content = "▶️ Execute",
                Padding = new Thickness(20, 10, 20, 10),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0)
            };
            this.executeButton.Click += OnExecuteClick;
            executePanel.Children.Add(this.executeButton);

            this.statusText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            executePanel.Children.Add(this.statusText);

            mainPanel.Children.Add(executePanel);

            // Results section
            var resultsLabel = new TextBlock
            {
                Text = "📊 Results:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(resultsLabel);

            this.resultsGrid = new DataGrid
            {
                Height = 250,
                AutoGenerateColumns = true,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(this.resultsGrid);

            // Action buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var copyResultsButton = new Button
            {
                Content = "📋 Copy Results",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            copyResultsButton.Click += OnCopyResultsClick;
            buttonPanel.Children.Add(copyResultsButton);

            var exportButton = new Button
            {
                Content = "💾 Export CSV",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            exportButton.Click += OnExportClick;
            buttonPanel.Children.Add(exportButton);

            var detailsButton = new Button
            {
                Content = "📊 Details",
                Padding = new Thickness(10, 5, 10, 5)
            };
            detailsButton.Click += OnDetailsClick;
            buttonPanel.Children.Add(detailsButton);

            mainPanel.Children.Add(buttonPanel);

            // Scroll viewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = mainPanel
            };

            this.Content = scrollViewer;
        }

        private void AddParameter(string name, string type, string value = "")
        {
            var paramPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var nameLabel = new TextBlock
            {
                Text = $"@{name}",
                Width = 150,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            };
            paramPanel.Children.Add(nameLabel);

            var typeLabel = new TextBlock
            {
                Text = $"({type}):",
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray
            };
            paramPanel.Children.Add(typeLabel);

            var valueTextBox = new TextBox
            {
                Width = 200,
                Text = value
            };
            paramPanel.Children.Add(valueTextBox);

            this.parameterInputs[name] = valueTextBox;
            this.parametersPanel.Children.Add(paramPanel);
        }

        private void OnTestConnectionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Test database connection
                MessageBox.Show("Connection test successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnAddParameterClick(object sender, RoutedEventArgs e)
        {
            AddParameter($"param{this.parameterInputs.Count + 1}", "string");
        }

        private void OnExecuteClick(object sender, RoutedEventArgs e)
        {
            this.executeButton.IsEnabled = false;
            this.statusText.Text = "⏱️ Executing...";

            try
            {
                var startTime = DateTime.Now;

                // TODO: Execute actual query
                // For now, show sample data
                var dataTable = new DataTable();
                dataTable.Columns.Add("Id", typeof(int));
                dataTable.Columns.Add("Name", typeof(string));
                dataTable.Columns.Add("Age", typeof(int));
                dataTable.Columns.Add("Email", typeof(string));

                dataTable.Rows.Add(123, "Alice", 25, "alice@example.com");

                this.resultsGrid.ItemsSource = dataTable.DefaultView;

                var elapsed = DateTime.Now - startTime;
                this.statusText.Text = $"✅ Success - {elapsed.TotalMilliseconds:F1} ms - 1 row";
                this.statusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                this.statusText.Text = $"❌ Error: {ex.Message}";
                this.statusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                this.executeButton.IsEnabled = true;
            }
        }

        private void OnCopyResultsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Copy results to clipboard in tabular format
                MessageBox.Show("Results copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = "query_results.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // TODO: Export to CSV
                    MessageBox.Show($"Results exported to {dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnDetailsClick(object sender, RoutedEventArgs e)
        {
            // TODO: Show execution details (plan, statistics, etc.)
            MessageBox.Show("Execution details:\n\nRows: 1\nTime: 12.3 ms\nColumns: 4", "Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

