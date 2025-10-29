// -----------------------------------------------------------------------
// <copyright file="SqlExecutionLogWindow.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// SQL Execution Log tool window for tracking SQL query execution.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000006")]
    public class SqlExecutionLogWindow : ToolWindowPane
    {
        private SqlExecutionLogControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecutionLogWindow"/> class.
        /// </summary>
        public SqlExecutionLogWindow() : base(null)
        {
            this.Caption = "Sqlx SQL Execution Log";
            this.control = new SqlExecutionLogControl();
            this.Content = this.control;
        }

        /// <summary>
        /// Adds a log entry.
        /// </summary>
        public void AddLog(SqlExecutionLogEntry log)
        {
            this.control?.AddLog(log);
        }
    }

    /// <summary>
    /// SQL execution log entry.
    /// </summary>
    public class SqlExecutionLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Method { get; set; }
        public string Sql { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public long ExecutionTimeMs { get; set; }
        public int RowsAffected { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Database { get; set; }

        public string Status => Success ? "✅" : "❌";
        public string TimeDisplay => $"{ExecutionTimeMs}ms";
        public Brush StatusBrush
        {
            get
            {
                if (!Success) return Brushes.Red;
                if (ExecutionTimeMs > 500) return Brushes.Orange;
                if (ExecutionTimeMs > 100) return Brushes.Yellow;
                return Brushes.Green;
            }
        }
    }

    /// <summary>
    /// User control for SQL execution log content.
    /// </summary>
    public class SqlExecutionLogControl : UserControl
    {
        private TextBox searchBox;
        private ListBox logListBox;
        private TextBlock detailsText;
        private TextBlock statsText;
        private ObservableCollection<SqlExecutionLogEntry> logs;
        private ObservableCollection<SqlExecutionLogEntry> filteredLogs;
        private bool isPaused = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecutionLogControl"/> class.
        /// </summary>
        public SqlExecutionLogControl()
        {
            logs = new ObservableCollection<SqlExecutionLogEntry>();
            filteredLogs = new ObservableCollection<SqlExecutionLogEntry>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Title and stats
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };

            var titleText = new TextBlock
            {
                Text = "SQL Execution Log",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 20, 0)
            };
            titlePanel.Children.Add(titleText);

            this.statsText = new TextBlock
            {
                Text = "0 executions",
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(this.statsText);

            Grid.SetRow(titlePanel, 0);
            mainGrid.Children.Add(titlePanel);

            // Toolbar
            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 10, 10)
            };

            var searchLabel = new TextBlock
            {
                Text = "🔍",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            toolbarPanel.Children.Add(searchLabel);

            this.searchBox = new TextBox
            {
                Width = 200,
                Margin = new Thickness(0, 0, 10, 0)
            };
            this.searchBox.TextChanged += OnSearchTextChanged;
            toolbarPanel.Children.Add(this.searchBox);

            var pauseButton = new Button
            {
                Content = "⏸️ Pause",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            pauseButton.Click += OnPauseClick;
            toolbarPanel.Children.Add(pauseButton);

            var clearButton = new Button
            {
                Content = "🗑️ Clear",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            clearButton.Click += OnClearClick;
            toolbarPanel.Children.Add(clearButton);

            var exportButton = new Button
            {
                Content = "💾 Export",
                Padding = new Thickness(10, 5, 10, 5)
            };
            exportButton.Click += OnExportClick;
            toolbarPanel.Children.Add(exportButton);

            Grid.SetRow(toolbarPanel, 1);
            mainGrid.Children.Add(toolbarPanel);

            // Log list
            this.logListBox = new ListBox
            {
                Margin = new Thickness(10),
                ItemsSource = filteredLogs
            };
            this.logListBox.SelectionChanged += OnLogSelectionChanged;

            // Custom item template
            var itemTemplate = new DataTemplate();
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var timeFactory = new FrameworkElementFactory(typeof(TextBlock));
            timeFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Timestamp") { StringFormat = "HH:mm:ss" });
            timeFactory.SetValue(TextBlock.WidthProperty, 80.0);
            timeFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 10, 0));
            stackPanelFactory.AppendChild(timeFactory);

            var methodFactory = new FrameworkElementFactory(typeof(TextBlock));
            methodFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Method"));
            methodFactory.SetValue(TextBlock.WidthProperty, 150.0);
            methodFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 10, 0));
            stackPanelFactory.AppendChild(methodFactory);

            var sqlFactory = new FrameworkElementFactory(typeof(TextBlock));
            sqlFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Sql"));
            sqlFactory.SetValue(TextBlock.WidthProperty, 300.0);
            sqlFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            sqlFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 10, 0));
            stackPanelFactory.AppendChild(sqlFactory);

            var timeDisplayFactory = new FrameworkElementFactory(typeof(TextBlock));
            timeDisplayFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("TimeDisplay"));
            timeDisplayFactory.SetValue(TextBlock.WidthProperty, 60.0);
            timeDisplayFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 10, 0));
            stackPanelFactory.AppendChild(timeDisplayFactory);

            var statusFactory = new FrameworkElementFactory(typeof(TextBlock));
            statusFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Status"));
            statusFactory.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("StatusBrush"));
            statusFactory.SetValue(TextBlock.FontSizeProperty, 16.0);
            stackPanelFactory.AppendChild(statusFactory);

            itemTemplate.VisualTree = stackPanelFactory;
            this.logListBox.ItemTemplate = itemTemplate;

            Grid.SetRow(this.logListBox, 2);
            mainGrid.Children.Add(this.logListBox);

            // Details panel
            var detailsPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var detailsLabel = new TextBlock
            {
                Text = "Details:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            detailsPanel.Children.Add(detailsLabel);

            this.detailsText = new TextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap
            };
            detailsPanel.Children.Add(this.detailsText);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = detailsPanel
            };

            Grid.SetRow(scrollViewer, 3);
            mainGrid.Children.Add(scrollViewer);

            this.Content = mainGrid;
        }

        public void AddLog(SqlExecutionLogEntry log)
        {
            if (isPaused)
                return;

            logs.Add(log);
            ApplyFilter();
            UpdateStatistics();
        }

        private void ApplyFilter()
        {
            var searchText = this.searchBox?.Text?.ToLower() ?? "";

            filteredLogs.Clear();
            var filtered = string.IsNullOrEmpty(searchText)
                ? logs
                : logs.Where(l => 
                    l.Method.ToLower().Contains(searchText) ||
                    l.Sql.ToLower().Contains(searchText));

            foreach (var log in filtered.OrderByDescending(l => l.Timestamp).Take(1000))
            {
                filteredLogs.Add(log);
            }
        }

        private void UpdateStatistics()
        {
            var total = logs.Count;
            var success = logs.Count(l => l.Success);
            var failed = logs.Count(l => !l.Success);
            var avgTime = logs.Any() ? logs.Average(l => l.ExecutionTimeMs) : 0;

            this.statsText.Text = $"📊 {total} executions | ✅ {success} success | ❌ {failed} failed | ⏱️ avg {avgTime:F1}ms";
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnPauseClick(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
            var button = sender as Button;
            if (button != null)
            {
                button.Content = isPaused ? "▶️ Resume" : "⏸️ Pause";
            }
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            logs.Clear();
            filteredLogs.Clear();
            this.detailsText.Text = string.Empty;
            UpdateStatistics();
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = $"sql_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ExportLogs(dialog.FileName);
                    MessageBox.Show($"Logs exported to {dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportLogs(string fileName)
        {
            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                var lines = new List<string>
                {
                    "Timestamp,Method,SQL,ExecutionTime(ms),RowsAffected,Success,Error"
                };

                foreach (var log in logs)
                {
                    var sql = log.Sql.Replace("\"", "\"\"");
                    var error = log.Error?.Replace("\"", "\"\"") ?? "";
                    lines.Add($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Method}\",\"{sql}\",{log.ExecutionTimeMs},{log.RowsAffected},{log.Success},\"{error}\"");
                }

                System.IO.File.WriteAllLines(fileName, lines);
            }
            else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Implement JSON export
                MessageBox.Show("JSON export not implemented yet", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnLogSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.logListBox.SelectedItem is SqlExecutionLogEntry log)
            {
                var details = new System.Text.StringBuilder();
                details.AppendLine($"Timestamp: {log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                details.AppendLine($"Method: {log.Method}");
                details.AppendLine($"Database: {log.Database ?? "N/A"}");
                details.AppendLine();
                details.AppendLine("SQL:");
                details.AppendLine(log.Sql);
                details.AppendLine();

                if (log.Parameters != null && log.Parameters.Any())
                {
                    details.AppendLine("Parameters:");
                    foreach (var param in log.Parameters)
                    {
                        details.AppendLine($"  {param.Key} = {param.Value}");
                    }
                    details.AppendLine();
                }

                details.AppendLine($"Execution Time: {log.ExecutionTimeMs} ms");
                details.AppendLine($"Rows Affected: {log.RowsAffected}");
                details.AppendLine($"Status: {(log.Success ? "Success ✅" : "Failed ❌")}");

                if (!log.Success && !string.IsNullOrEmpty(log.Error))
                {
                    details.AppendLine();
                    details.AppendLine("Error:");
                    details.AppendLine(log.Error);
                }

                this.detailsText.Text = details.ToString();
            }
        }
    }
}

