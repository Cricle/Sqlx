// -----------------------------------------------------------------------
// <copyright file="PerformanceAnalyzerWindow.cs" company="Cricle">
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
using System.Windows.Shapes;
using Microsoft.VisualStudio.Shell;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// Performance Analyzer tool window for SQL query performance monitoring.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000008")]
    public class PerformanceAnalyzerWindow : ToolWindowPane
    {
        private PerformanceAnalyzerControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceAnalyzerWindow"/> class.
        /// </summary>
        public PerformanceAnalyzerWindow() : base(null)
        {
            this.Caption = "Sqlx Performance Analyzer";
            this.control = new PerformanceAnalyzerControl();
            this.Content = this.control;
        }

        /// <summary>
        /// Adds a performance metric.
        /// </summary>
        public void AddMetric(PerformanceMetric metric)
        {
            this.control?.AddMetric(metric);
        }
    }

    /// <summary>
    /// Performance metric for SQL execution.
    /// </summary>
    public class PerformanceMetric
    {
        public DateTime Timestamp { get; set; }
        public string Method { get; set; }
        public string Sql { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool Success { get; set; }
        public int RowsAffected { get; set; }
    }

    /// <summary>
    /// Performance statistics.
    /// </summary>
    public class PerformanceStats
    {
        public int TotalQueries { get; set; }
        public double AverageTime { get; set; }
        public long MaxTime { get; set; }
        public long MinTime { get; set; }
        public int SlowQueries { get; set; }
        public int FailedQueries { get; set; }
        public double QueriesPerSecond { get; set; }
    }

    /// <summary>
    /// Slow query information.
    /// </summary>
    public class SlowQueryInfo
    {
        public string Method { get; set; }
        public long ExecutionTime { get; set; }
        public DateTime Timestamp { get; set; }
        public string Sql { get; set; }
    }

    /// <summary>
    /// Optimization suggestion.
    /// </summary>
    public class OptimizationSuggestion
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string Method { get; set; }
        public int Priority { get; set; } // 1-5, 1 = highest

        public string PriorityIcon
        {
            get
            {
                return Priority <= 2 ? "🔴" : Priority == 3 ? "🟡" : "🟢";
            }
        }
    }

    /// <summary>
    /// User control for performance analyzer content.
    /// </summary>
    public class PerformanceAnalyzerControl : UserControl
    {
        private ObservableCollection<PerformanceMetric> metrics;
        private TextBlock summaryText;
        private Canvas chartCanvas;
        private ListBox slowQueriesList;
        private ListBox suggestionsList;
        private ComboBox timeRangeCombo;
        private int slowQueryThreshold = 500; // ms

        public PerformanceAnalyzerControl()
        {
            metrics = new ObservableCollection<PerformanceMetric>();
            InitializeComponent();
            GenerateSampleData(); // For demo purposes
            UpdateAnalysis();
        }

        private void InitializeComponent()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Title and controls
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };

            var titleText = new TextBlock
            {
                Text = "SQL Performance Analyzer",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 20, 0)
            };
            titlePanel.Children.Add(titleText);

            var timeLabel = new TextBlock
            {
                Text = "Time Range:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            titlePanel.Children.Add(timeLabel);

            this.timeRangeCombo = new ComboBox
            {
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0)
            };
            this.timeRangeCombo.Items.Add("Last 5 min");
            this.timeRangeCombo.Items.Add("Last 15 min");
            this.timeRangeCombo.Items.Add("Last 1 hour");
            this.timeRangeCombo.Items.Add("Last 24 hours");
            this.timeRangeCombo.SelectedIndex = 2;
            this.timeRangeCombo.SelectionChanged += OnTimeRangeChanged;
            titlePanel.Children.Add(this.timeRangeCombo);

            var refreshButton = new Button
            {
                Content = "🔄 Refresh",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            refreshButton.Click += OnRefreshClick;
            titlePanel.Children.Add(refreshButton);

            var clearButton = new Button
            {
                Content = "🗑️ Clear",
                Padding = new Thickness(10, 5, 10, 5)
            };
            clearButton.Click += OnClearClick;
            titlePanel.Children.Add(clearButton);

            Grid.SetRow(titlePanel, 0);
            mainGrid.Children.Add(titlePanel);

            // Summary statistics
            var summaryPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Padding = new Thickness(10),
                Margin = new Thickness(10, 0, 10, 10)
            };

            this.summaryText = new TextBlock
            {
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
            summaryPanel.Child = this.summaryText;

            Grid.SetRow(summaryPanel, 1);
            mainGrid.Children.Add(summaryPanel);

            // Performance chart
            var chartPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 10, 10)
            };

            var chartLabel = new TextBlock
            {
                Text = "📈 Execution Time Chart (Last 20 queries)",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            chartPanel.Children.Add(chartLabel);

            var chartBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };

            this.chartCanvas = new Canvas
            {
                Height = 150
            };
            chartBorder.Child = this.chartCanvas;
            chartPanel.Children.Add(chartBorder);

            Grid.SetRow(chartPanel, 2);
            mainGrid.Children.Add(chartPanel);

            // Slow queries list
            var slowQueriesPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 10, 10)
            };

            var slowLabel = new TextBlock
            {
                Text = $"⚠️ Slow Queries (>{slowQueryThreshold}ms)",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            slowQueriesPanel.Children.Add(slowLabel);

            this.slowQueriesList = new ListBox();
            slowQueriesPanel.Children.Add(this.slowQueriesList);

            Grid.SetRow(slowQueriesPanel, 3);
            mainGrid.Children.Add(slowQueriesPanel);

            // Optimization suggestions
            var suggestionsPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 10, 10)
            };

            var suggestionsLabel = new TextBlock
            {
                Text = "💡 Optimization Suggestions",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            suggestionsPanel.Children.Add(suggestionsLabel);

            this.suggestionsList = new ListBox();
            suggestionsPanel.Children.Add(this.suggestionsList);

            Grid.SetRow(suggestionsPanel, 4);
            mainGrid.Children.Add(suggestionsPanel);

            this.Content = mainGrid;
        }

        private void GenerateSampleData()
        {
            // Generate sample metrics for demonstration
            var random = new Random();
            var now = DateTime.Now;
            var methods = new[] { "GetByIdAsync", "GetAllAsync", "InsertAsync", "UpdateAsync", "DeleteAsync", "SearchAsync" };

            for (int i = 0; i < 50; i++)
            {
                metrics.Add(new PerformanceMetric
                {
                    Timestamp = now.AddMinutes(-i * 2),
                    Method = methods[random.Next(methods.Length)],
                    Sql = "SELECT * FROM users WHERE id = @id",
                    ExecutionTimeMs = random.Next(5, 1000),
                    Success = random.Next(100) > 5,
                    RowsAffected = random.Next(1, 100)
                });
            }
        }

        public void AddMetric(PerformanceMetric metric)
        {
            metrics.Add(metric);
            
            // Keep only recent metrics
            if (metrics.Count > 1000)
            {
                metrics.RemoveAt(0);
            }

            UpdateAnalysis();
        }

        private void UpdateAnalysis()
        {
            var stats = CalculateStats();
            UpdateSummary(stats);
            UpdateChart();
            UpdateSlowQueries();
            UpdateSuggestions();
        }

        private PerformanceStats CalculateStats()
        {
            if (!metrics.Any())
            {
                return new PerformanceStats();
            }

            var recentMetrics = GetRecentMetrics();

            return new PerformanceStats
            {
                TotalQueries = recentMetrics.Count,
                AverageTime = recentMetrics.Average(m => m.ExecutionTimeMs),
                MaxTime = recentMetrics.Max(m => m.ExecutionTimeMs),
                MinTime = recentMetrics.Min(m => m.ExecutionTimeMs),
                SlowQueries = recentMetrics.Count(m => m.ExecutionTimeMs > slowQueryThreshold),
                FailedQueries = recentMetrics.Count(m => !m.Success),
                QueriesPerSecond = CalculateQPS(recentMetrics)
            };
        }

        private List<PerformanceMetric> GetRecentMetrics()
        {
            var timeRange = GetSelectedTimeRange();
            var cutoff = DateTime.Now.AddMinutes(-timeRange);
            return metrics.Where(m => m.Timestamp >= cutoff).ToList();
        }

        private int GetSelectedTimeRange()
        {
            var selected = this.timeRangeCombo?.SelectedIndex ?? 2;
            return selected switch
            {
                0 => 5,
                1 => 15,
                2 => 60,
                3 => 1440,
                _ => 60
            };
        }

        private double CalculateQPS(List<PerformanceMetric> recentMetrics)
        {
            if (!recentMetrics.Any()) return 0;

            var timeSpan = DateTime.Now - recentMetrics.Min(m => m.Timestamp);
            return timeSpan.TotalSeconds > 0 ? recentMetrics.Count / timeSpan.TotalSeconds : 0;
        }

        private void UpdateSummary(PerformanceStats stats)
        {
            this.summaryText.Text = $"📊 Summary Statistics\n" +
                                   $"Total Queries: {stats.TotalQueries} | " +
                                   $"Avg Time: {stats.AverageTime:F1}ms | " +
                                   $"Max: {stats.MaxTime}ms | " +
                                   $"Min: {stats.MinTime}ms | " +
                                   $"Slow Queries: {stats.SlowQueries} ({stats.SlowQueries * 100.0 / Math.Max(stats.TotalQueries, 1):F1}%) | " +
                                   $"Failed: {stats.FailedQueries} | " +
                                   $"QPS: {stats.QueriesPerSecond:F2}";
        }

        private void UpdateChart()
        {
            this.chartCanvas.Children.Clear();

            var recentMetrics = GetRecentMetrics().OrderBy(m => m.Timestamp).TakeLast(20).ToList();
            if (!recentMetrics.Any()) return;

            var width = this.chartCanvas.ActualWidth > 0 ? this.chartCanvas.ActualWidth : 800;
            var height = this.chartCanvas.Height;
            var maxTime = Math.Max(recentMetrics.Max(m => m.ExecutionTimeMs), slowQueryThreshold);

            var barWidth = width / recentMetrics.Count - 2;
            var x = 0.0;

            for (int i = 0; i < recentMetrics.Count; i++)
            {
                var metric = recentMetrics[i];
                var barHeight = (metric.ExecutionTimeMs / (double)maxTime) * (height - 30);
                var y = height - barHeight - 20;

                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = metric.ExecutionTimeMs > slowQueryThreshold ? Brushes.Red :
                           metric.ExecutionTimeMs > 100 ? Brushes.Orange : Brushes.Green
                };

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                this.chartCanvas.Children.Add(bar);

                x += barWidth + 2;
            }

            // Add threshold line
            var thresholdY = height - (slowQueryThreshold / (double)maxTime) * (height - 30) - 20;
            var thresholdLine = new Line
            {
                X1 = 0,
                Y1 = thresholdY,
                X2 = width,
                Y2 = thresholdY,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            this.chartCanvas.Children.Add(thresholdLine);
        }

        private void UpdateSlowQueries()
        {
            this.slowQueriesList.Items.Clear();

            var slowQueries = GetRecentMetrics()
                .Where(m => m.ExecutionTimeMs > slowQueryThreshold)
                .OrderByDescending(m => m.ExecutionTimeMs)
                .Take(10);

            foreach (var query in slowQueries)
            {
                this.slowQueriesList.Items.Add(
                    $"{query.Method,-20} {query.ExecutionTimeMs,5}ms  {query.Timestamp:HH:mm:ss}  {TruncateSql(query.Sql)}");
            }

            if (!slowQueries.Any())
            {
                this.slowQueriesList.Items.Add("✅ No slow queries detected");
            }
        }

        private void UpdateSuggestions()
        {
            this.suggestionsList.Items.Clear();

            var suggestions = GenerateSuggestions();

            foreach (var suggestion in suggestions.OrderBy(s => s.Priority))
            {
                this.suggestionsList.Items.Add(
                    $"{suggestion.PriorityIcon} {suggestion.Message}");
            }

            if (!suggestions.Any())
            {
                this.suggestionsList.Items.Add("✅ No optimization suggestions at this time");
            }
        }

        private List<OptimizationSuggestion> GenerateSuggestions()
        {
            var suggestions = new List<OptimizationSuggestion>();
            var recentMetrics = GetRecentMetrics();

            // Check for slow queries
            var slowQueries = recentMetrics.Where(m => m.ExecutionTimeMs > slowQueryThreshold).ToList();
            if (slowQueries.Count > 5)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "SlowQuery",
                    Message = $"High number of slow queries detected ({slowQueries.Count}). Consider adding indexes.",
                    Priority = 1
                });
            }

            // Check for high average time
            var avgTime = recentMetrics.Any() ? recentMetrics.Average(m => m.ExecutionTimeMs) : 0;
            if (avgTime > 200)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "HighAverage",
                    Message = $"Average query time is high ({avgTime:F1}ms). Review query optimization.",
                    Priority = 2
                });
            }

            // Check for frequently called methods
            var methodFrequency = recentMetrics.GroupBy(m => m.Method)
                .Select(g => new { Method = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            if (methodFrequency != null && methodFrequency.Count > 20)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "HighFrequency",
                    Message = $"{methodFrequency.Method} called {methodFrequency.Count} times. Consider caching.",
                    Method = methodFrequency.Method,
                    Priority = 3
                });
            }

            // Check for failed queries
            var failedCount = recentMetrics.Count(m => !m.Success);
            if (failedCount > 0)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "Failed",
                    Message = $"{failedCount} queries failed. Check error logs for details.",
                    Priority = 1
                });
            }

            return suggestions;
        }

        private string TruncateSql(string sql)
        {
            if (sql.Length <= 50) return sql;
            return sql.Substring(0, 47) + "...";
        }

        private void OnTimeRangeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAnalysis();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            UpdateAnalysis();
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            metrics.Clear();
            UpdateAnalysis();
        }
    }
}

