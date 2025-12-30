using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// SQL监视窗口
    /// </summary>
    [Guid("2D8F9B4E-5C6D-4F7E-9A8B-2C3D4E5F6A7B")]
    public class SqlWatchWindow : ToolWindowPane
    {
        private DataGrid watchGrid;
        private TextBlock summaryText;
        private ObservableCollection<WatchItemViewModel> watchItems;

        public SqlWatchWindow() : base(null)
        {
            Caption = "Sqlx SQL Watch";
            BitmapResourceID = 301;
            BitmapIndex = 2;

            watchItems = new ObservableCollection<WatchItemViewModel>();
            Content = CreateUI();

            // 添加示例监视项
            AddSampleWatchItems();
        }

        private UIElement CreateUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

            // 标题栏
            var headerPanel = new DockPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                LastChildFill = true
            };

            var titleText = new TextBlock
            {
                Text = "SQL Watch",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(titleText, Dock.Left);
            headerPanel.Children.Add(titleText);

            // 工具栏
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(toolbar, Dock.Right);

            var addButton = new Button
            {
                Content = "➕ Add Watch",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            addButton.Click += AddWatch_Click;
            toolbar.Children.Add(addButton);

            var removeButton = new Button
            {
                Content = "❌ Remove",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            removeButton.Click += RemoveWatch_Click;
            toolbar.Children.Add(removeButton);

            var clearButton = new Button
            {
                Content = "🗑️ Clear All",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            clearButton.Click += ClearAll_Click;
            toolbar.Children.Add(clearButton);

            var refreshButton = new Button
            {
                Content = "🔄 Refresh",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            refreshButton.Click += Refresh_Click;
            toolbar.Children.Add(refreshButton);

            headerPanel.Children.Add(toolbar);

            Grid.SetRow(headerPanel, 0);
            grid.Children.Add(headerPanel);

            // 监视项列表
            watchGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = true,
                CanUserDeleteRows = true,
                ItemsSource = watchItems,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                RowBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(35, 35, 35))
            };

            // 列定义
            var nameColumn = new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = 200
            };
            watchGrid.Columns.Add(nameColumn);

            var valueColumn = new DataGridTextColumn
            {
                Header = "Value",
                Binding = new System.Windows.Data.Binding("Value"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };
            watchGrid.Columns.Add(valueColumn);

            var typeColumn = new DataGridTextColumn
            {
                Header = "Type",
                Binding = new System.Windows.Data.Binding("Type"),
                Width = 150
            };
            watchGrid.Columns.Add(typeColumn);

            Grid.SetRow(watchGrid, 1);
            grid.Children.Add(watchGrid);

            // 摘要栏
            summaryText = new TextBlock
            {
                Margin = new Thickness(10, 5, 10, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                VerticalAlignment = VerticalAlignment.Center
            };
            UpdateSummary();

            var summaryPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Child = summaryText
            };

            Grid.SetRow(summaryPanel, 2);
            grid.Children.Add(summaryPanel);

            return grid;
        }

        private void AddSampleWatchItems()
        {
            watchItems.Add(new WatchItemViewModel
            {
                Name = "@id",
                Value = "123",
                Type = "long"
            });

            watchItems.Add(new WatchItemViewModel
            {
                Name = "@name",
                Value = "\"John Doe\"",
                Type = "string"
            });

            watchItems.Add(new WatchItemViewModel
            {
                Name = "generatedSql",
                Value = "SELECT id, name, email FROM users WHERE id = 123",
                Type = "string"
            });

            watchItems.Add(new WatchItemViewModel
            {
                Name = "executionTime",
                Value = "45ms",
                Type = "TimeSpan"
            });

            watchItems.Add(new WatchItemViewModel
            {
                Name = "rowsAffected",
                Value = "1",
                Type = "int"
            });

            watchItems.Add(new WatchItemViewModel
            {
                Name = "result",
                Value = "User { Id = 123, Name = \"John Doe\", Email = \"john@example.com\" }",
                Type = "User"
            });

            watchItems.Add(new WatchItemViewModel
            {
                Name = "result.Id",
                Value = "123",
                Type = "long"
            });

            watchItems.Add(new WatchItemViewModel
            {
                Name = "result.Name",
                Value = "\"John Doe\"",
                Type = "string"
            });

            UpdateSummary();
        }

        private void AddWatch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Add Watch",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var panel = new StackPanel { Margin = new Thickness(15) };

            panel.Children.Add(new TextBlock
            {
                Text = "Expression:",
                Margin = new Thickness(0, 0, 0, 5)
            });

            var expressionBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15)
            };
            panel.Children.Add(expressionBox);

            panel.Children.Add(new TextBlock
            {
                Text = "Examples:",
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 0, 0, 5)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "• @id\n• result.Name\n• executionTime.TotalMilliseconds\n• parameters.Count",
                Margin = new Thickness(15, 0, 0, 15),
                FontSize = 11
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 25,
                Margin = new Thickness(5, 0, 0, 0),
                IsDefault = true
            };
            okButton.Click += (s, ev) =>
            {
                var expression = expressionBox.Text.Trim();
                if (!string.IsNullOrEmpty(expression))
                {
                    watchItems.Add(new WatchItemViewModel
                    {
                        Name = expression,
                        Value = "Evaluating...",
                        Type = "Unknown"
                    });
                    UpdateSummary();
                }
                dialog.Close();
            };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 25,
                Margin = new Thickness(5, 0, 0, 0),
                IsCancel = true
            };
            cancelButton.Click += (s, ev) => dialog.Close();
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private void RemoveWatch_Click(object sender, RoutedEventArgs e)
        {
            var selected = watchGrid.SelectedItem as WatchItemViewModel;
            if (selected != null)
            {
                watchItems.Remove(selected);
                UpdateSummary();
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all watch items?",
                "Clear All", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                watchItems.Clear();
                UpdateSummary();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // 刷新所有监视项的值
            foreach (var item in watchItems)
            {
                item.Value = EvaluateExpression(item.Name);
            }
            UpdateSummary();
        }

        private string EvaluateExpression(string expression)
        {
            // TODO: 实现真实的表达式求值
            // 这里返回模拟值
            return $"<Evaluated: {expression}>";
        }

        private void UpdateSummary()
        {
            summaryText.Text = $"Total watch items: {watchItems.Count}";
        }
    }

    /// <summary>
    /// 监视项视图模型
    /// </summary>
    public class WatchItemViewModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
    }
}

