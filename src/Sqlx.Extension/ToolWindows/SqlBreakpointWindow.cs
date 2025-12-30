using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sqlx.Extension.Debugging;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// SQL断点管理器窗口
    /// </summary>
    [Guid("1C7E8A3F-4D5B-4E6F-8A9B-1C2D3E4F5A6B")]
    public class SqlBreakpointWindow : ToolWindowPane
    {
        private DataGrid breakpointGrid;
        private TextBlock summaryText;
        private ObservableCollection<BreakpointViewModel> breakpoints;

        public SqlBreakpointWindow() : base(null)
        {
            Caption = "Sqlx SQL Breakpoints";
            BitmapResourceID = 301;
            BitmapIndex = 1;

            breakpoints = new ObservableCollection<BreakpointViewModel>();
            Content = CreateUI();

            // 订阅断点管理器事件
            var manager = SqlBreakpointManager.Instance;
            manager.BreakpointAdded += OnBreakpointAdded;
            manager.BreakpointRemoved += OnBreakpointRemoved;
            manager.BreakpointUpdated += OnBreakpointUpdated;
            manager.BreakpointHit += OnBreakpointHit;

            // 加载现有断点
            LoadBreakpoints();
        }

        private UIElement CreateUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

            // 标题和工具栏
            var headerPanel = new DockPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                LastChildFill = true
            };

            var titleText = new TextBlock
            {
                Text = "SQL Breakpoints",
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
                Content = "➕ Add",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            addButton.Click += AddButton_Click;
            toolbar.Children.Add(addButton);

            var removeButton = new Button
            {
                Content = "❌ Remove",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            removeButton.Click += RemoveButton_Click;
            toolbar.Children.Add(removeButton);

            var clearButton = new Button
            {
                Content = "🗑️ Clear All",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            clearButton.Click += ClearButton_Click;
            toolbar.Children.Add(clearButton);

            var refreshButton = new Button
            {
                Content = "🔄 Refresh",
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            refreshButton.Click += RefreshButton_Click;
            toolbar.Children.Add(refreshButton);

            headerPanel.Children.Add(toolbar);

            Grid.SetRow(headerPanel, 0);
            grid.Children.Add(headerPanel);

            // 断点列表
            breakpointGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = true,
                ItemsSource = breakpoints,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                RowBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(35, 35, 35))
            };

            // 列定义
            var enabledColumn = new DataGridCheckBoxColumn
            {
                Header = "Enabled",
                Binding = new System.Windows.Data.Binding("IsEnabled"),
                Width = 70
            };
            breakpointGrid.Columns.Add(enabledColumn);

            var methodColumn = new DataGridTextColumn
            {
                Header = "Method",
                Binding = new System.Windows.Data.Binding("MethodName"),
                Width = 150
            };
            breakpointGrid.Columns.Add(methodColumn);

            var sqlColumn = new DataGridTextColumn
            {
                Header = "SQL Template",
                Binding = new System.Windows.Data.Binding("SqlTemplate"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };
            breakpointGrid.Columns.Add(sqlColumn);

            var conditionColumn = new DataGridTextColumn
            {
                Header = "Condition",
                Binding = new System.Windows.Data.Binding("Condition"),
                Width = 120
            };
            breakpointGrid.Columns.Add(conditionColumn);

            var hitCountColumn = new DataGridTextColumn
            {
                Header = "Hit Count",
                Binding = new System.Windows.Data.Binding("HitCount"),
                Width = 80
            };
            breakpointGrid.Columns.Add(hitCountColumn);

            var typeColumn = new DataGridTextColumn
            {
                Header = "Type",
                Binding = new System.Windows.Data.Binding("TypeDisplay"),
                Width = 100
            };
            breakpointGrid.Columns.Add(typeColumn);

            Grid.SetRow(breakpointGrid, 1);
            grid.Children.Add(breakpointGrid);

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

        private void LoadBreakpoints()
        {
            breakpoints.Clear();
            var allBreakpoints = SqlBreakpointManager.Instance.GetAllBreakpoints();
            foreach (var bp in allBreakpoints)
            {
                breakpoints.Add(new BreakpointViewModel(bp));
            }
            UpdateSummary();
        }

        private void OnBreakpointAdded(object sender, SqlBreakpointInfo e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                breakpoints.Add(new BreakpointViewModel(e));
                UpdateSummary();
            });
        }

        private void OnBreakpointRemoved(object sender, int breakpointId)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var item = breakpoints.FirstOrDefault(b => b.Id == breakpointId);
                if (item != null)
                {
                    breakpoints.Remove(item);
                    UpdateSummary();
                }
            });
        }

        private void OnBreakpointUpdated(object sender, SqlBreakpointInfo e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var item = breakpoints.FirstOrDefault(b => b.Id == e.Id);
                if (item != null)
                {
                    item.Update(e);
                    UpdateSummary();
                }
            });
        }

        private void OnBreakpointHit(object sender, SqlBreakpointHitEventArgs e)
        {
            // 断点命中时的处理（显示对话框或更新UI）
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                if (e.ShouldBreak)
                {
                    // 显示断点命中对话框
                    ShowBreakpointHitDialog(e.Breakpoint);
                }
            });
        }

        private void ShowBreakpointHitDialog(SqlBreakpointInfo breakpoint)
        {
            var dialog = new Window
            {
                Title = "🔴 SQL Breakpoint Hit",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var content = new StackPanel { Margin = new Thickness(15) };

            content.Children.Add(new TextBlock
            {
                Text = $"Method: {breakpoint.MethodName}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            content.Children.Add(new TextBlock
            {
                Text = "SQL Template:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            });
            content.Children.Add(new TextBox
            {
                Text = breakpoint.SqlTemplate,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 100,
                Margin = new Thickness(0, 0, 0, 10)
            });

            content.Children.Add(new TextBlock
            {
                Text = "Generated SQL:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            });
            content.Children.Add(new TextBox
            {
                Text = breakpoint.GeneratedSql,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 100,
                Margin = new Thickness(0, 0, 0, 10)
            });

            content.Children.Add(new TextBlock
            {
                Text = "Parameters:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            });
            var paramText = string.Join("\n", breakpoint.Parameters.Select(p => $"{p.Key} = {p.Value} ({p.Value?.GetType().Name ?? "null"})"));
            content.Children.Add(new TextBox
            {
                Text = paramText,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 100,
                Margin = new Thickness(0, 0, 0, 15)
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var continueButton = new Button
            {
                Content = "▶️ Continue",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0)
            };
            continueButton.Click += (s, e) => dialog.Close();
            buttonPanel.Children.Add(continueButton);

            var stopButton = new Button
            {
                Content = "⏹️ Stop",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0)
            };
            stopButton.Click += (s, e) => dialog.Close();
            buttonPanel.Children.Add(stopButton);

            content.Children.Add(buttonPanel);

            dialog.Content = new ScrollViewer { Content = content };
            dialog.ShowDialog();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please set breakpoints directly in the code editor by clicking on the left margin.",
                "Add Breakpoint", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = breakpointGrid.SelectedItem as BreakpointViewModel;
            if (selected != null)
            {
                SqlBreakpointManager.Instance.RemoveBreakpoint(selected.Id);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all breakpoints?",
                "Clear All Breakpoints", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SqlBreakpointManager.Instance.ClearAllBreakpoints();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadBreakpoints();
        }

        private void UpdateSummary()
        {
            var total = breakpoints.Count;
            var enabled = breakpoints.Count(b => b.IsEnabled);
            var hitCount = breakpoints.Sum(b => b.HitCount);

            summaryText.Text = $"Total: {total} breakpoints | Enabled: {enabled} | Total Hits: {hitCount}";
        }
    }

    /// <summary>
    /// 断点视图模型
    /// </summary>
    public class BreakpointViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private SqlBreakpointInfo _breakpoint;

        public int Id => _breakpoint.Id;
        public string MethodName => _breakpoint.MethodName;
        public string SqlTemplate => _breakpoint.SqlTemplate.Length > 50 
            ? _breakpoint.SqlTemplate.Substring(0, 47) + "..." 
            : _breakpoint.SqlTemplate;
        public string Condition => _breakpoint.Condition ?? "-";
        public int HitCount => _breakpoint.HitCount;
        public string TypeDisplay => GetTypeDisplay();
        public bool IsEnabled
        {
            get => _breakpoint.IsEnabled;
            set
            {
                if (_breakpoint.IsEnabled != value)
                {
                    SqlBreakpointManager.Instance.SetBreakpointEnabled(_breakpoint.Id, value);
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public BreakpointViewModel(SqlBreakpointInfo breakpoint)
        {
            _breakpoint = breakpoint;
        }

        public void Update(SqlBreakpointInfo breakpoint)
        {
            _breakpoint = breakpoint;
            OnPropertyChanged(string.Empty);
        }

        private string GetTypeDisplay()
        {
            if (_breakpoint.IsLogPoint)
                return "🟡 LogPoint";
            
            switch (_breakpoint.Type)
            {
                case BreakpointType.Conditional:
                    return "🔵 Conditional";
                case BreakpointType.HitCount:
                    return "🟣 HitCount";
                default:
                    return "🔴 Line";
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

