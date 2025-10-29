// -----------------------------------------------------------------------
// <copyright file="TemplateVisualizerWindow.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;

namespace Sqlx.Extension.ToolWindows
{
    /// <summary>
    /// SQL Template Visualizer tool window for visual SQL template design.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000007")]
    public class TemplateVisualizerWindow : ToolWindowPane
    {
        private TemplateVisualizerControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateVisualizerWindow"/> class.
        /// </summary>
        public TemplateVisualizerWindow() : base(null)
        {
            this.Caption = "Sqlx Template Visualizer";
            this.control = new TemplateVisualizerControl();
            this.Content = this.control;
        }
    }

    /// <summary>
    /// SQL template model for visual editor.
    /// </summary>
    public class SqlTemplateModel
    {
        public string Operation { get; set; } = "SELECT";
        public ObservableCollection<PlaceholderInfo> Placeholders { get; set; } = new ObservableCollection<PlaceholderInfo>();
        public ObservableCollection<ParameterInfo> Parameters { get; set; } = new ObservableCollection<ParameterInfo>();
        public ObservableCollection<ClauseInfo> Clauses { get; set; } = new ObservableCollection<ClauseInfo>();

        public string GenerateCode()
        {
            var sb = new StringBuilder();
            sb.Append("[SqlTemplate(\"");
            sb.Append(GenerateSql());
            sb.Append("\")]");
            return sb.ToString();
        }

        public string GenerateSql()
        {
            var sb = new StringBuilder();

            switch (Operation)
            {
                case "SELECT":
                    sb.Append("SELECT ");
                    sb.Append(string.Join(", ", Placeholders.Where(p => p.IsEnabled).Select(p => p.ToSqlString())));
                    sb.Append(" FROM {{table}}");
                    break;

                case "INSERT":
                    sb.Append("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})");
                    break;

                case "UPDATE":
                    sb.Append("UPDATE {{table}} SET {{set --exclude Id}}");
                    break;

                case "DELETE":
                    sb.Append("DELETE FROM {{table}}");
                    break;
            }

            foreach (var clause in Clauses.Where(c => c.IsEnabled).OrderBy(c => c.Order))
            {
                sb.Append(" ");
                sb.Append(clause.ToSqlString());
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Placeholder information.
    /// </summary>
    public class PlaceholderInfo
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Modifier { get; set; }

        public string ToSqlString()
        {
            var result = "{{" + Name;
            if (!string.IsNullOrEmpty(Modifier))
            {
                result += " " + Modifier;
            }
            result += "}}";
            return result;
        }
    }

    /// <summary>
    /// Parameter information.
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; } = "string";
        public bool IsEnabled { get; set; } = true;

        public string ToSqlString()
        {
            return "@" + Name;
        }
    }

    /// <summary>
    /// Clause information.
    /// </summary>
    public class ClauseInfo
    {
        public string Type { get; set; } // WHERE, JOIN, ORDER BY, etc.
        public string Content { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int Order { get; set; }

        public string ToSqlString()
        {
            return Type + " " + Content;
        }
    }

    /// <summary>
    /// User control for template visualizer content.
    /// </summary>
    public class TemplateVisualizerControl : UserControl
    {
        private SqlTemplateModel model;
        private ComboBox operationCombo;
        private ListBox placeholderList;
        private ListBox parameterList;
        private ListBox clauseList;
        private TextBox codePreview;

        public TemplateVisualizerControl()
        {
            model = new SqlTemplateModel();
            InitializeComponent();
            InitializeDefaultModel();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Title
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };

            var titleText = new TextBlock
            {
                Text = "SQL Template Visual Editor",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 20, 0)
            };
            titlePanel.Children.Add(titleText);

            var newButton = new Button
            {
                Content = "➕ New",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            newButton.Click += OnNewClick;
            titlePanel.Children.Add(newButton);

            var copyButton = new Button
            {
                Content = "📋 Copy Code",
                Padding = new Thickness(10, 5, 10, 5)
            };
            copyButton.Click += OnCopyClick;
            titlePanel.Children.Add(copyButton);

            Grid.SetRow(titlePanel, 0);
            mainGrid.Children.Add(titlePanel);

            // Main content area
            var contentGrid = new Grid
            {
                Margin = new Thickness(10, 0, 10, 10)
            };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            // Left panel - Components
            var leftPanel = new StackPanel();

            var operationLabel = new TextBlock
            {
                Text = "Operation:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            leftPanel.Children.Add(operationLabel);

            this.operationCombo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            this.operationCombo.Items.Add("SELECT");
            this.operationCombo.Items.Add("INSERT");
            this.operationCombo.Items.Add("UPDATE");
            this.operationCombo.Items.Add("DELETE");
            this.operationCombo.SelectedIndex = 0;
            this.operationCombo.SelectionChanged += OnOperationChanged;
            leftPanel.Children.Add(this.operationCombo);

            var placeholderLabel = new TextBlock
            {
                Text = "Placeholders:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            leftPanel.Children.Add(placeholderLabel);

            this.placeholderList = new ListBox
            {
                Height = 150,
                Margin = new Thickness(0, 0, 0, 10)
            };
            leftPanel.Children.Add(this.placeholderList);

            var addPlaceholderButton = new Button
            {
                Content = "➕ Add Placeholder",
                Margin = new Thickness(0, 0, 0, 10)
            };
            addPlaceholderButton.Click += OnAddPlaceholderClick;
            leftPanel.Children.Add(addPlaceholderButton);

            var parameterLabel = new TextBlock
            {
                Text = "Parameters:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            leftPanel.Children.Add(parameterLabel);

            this.parameterList = new ListBox
            {
                Height = 100
            };
            leftPanel.Children.Add(this.parameterList);

            var addParameterButton = new Button
            {
                Content = "➕ Add Parameter",
                Margin = new Thickness(0, 5, 0, 0)
            };
            addParameterButton.Click += OnAddParameterClick;
            leftPanel.Children.Add(addParameterButton);

            Grid.SetColumn(leftPanel, 0);
            contentGrid.Children.Add(leftPanel);

            // Right panel - Clauses
            var rightPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 0)
            };

            var clauseLabel = new TextBlock
            {
                Text = "Clauses:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            rightPanel.Children.Add(clauseLabel);

            this.clauseList = new ListBox
            {
                Height = 300
            };
            rightPanel.Children.Add(this.clauseList);

            var clauseButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var addWhereButton = new Button
            {
                Content = "WHERE",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0)
            };
            addWhereButton.Click += (s, e) => AddClause("WHERE", "id = @id");
            clauseButtonPanel.Children.Add(addWhereButton);

            var addOrderButton = new Button
            {
                Content = "ORDER BY",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0)
            };
            addOrderButton.Click += (s, e) => AddClause("ORDER BY", "id ASC");
            clauseButtonPanel.Children.Add(addOrderButton);

            var addLimitButton = new Button
            {
                Content = "LIMIT",
                Padding = new Thickness(10, 5, 10, 5)
            };
            addLimitButton.Click += (s, e) => AddClause("LIMIT", "{{limit --param limit}}");
            clauseButtonPanel.Children.Add(addLimitButton);

            rightPanel.Children.Add(clauseButtonPanel);

            Grid.SetColumn(rightPanel, 1);
            contentGrid.Children.Add(rightPanel);

            Grid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            // Separator
            var separator = new Separator
            {
                Margin = new Thickness(10, 10, 10, 10)
            };
            Grid.SetRow(separator, 2);
            mainGrid.Children.Add(separator);

            // Code preview
            var previewPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 10, 10)
            };

            var previewLabel = new TextBlock
            {
                Text = "Generated Code:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            previewPanel.Children.Add(previewLabel);

            this.codePreview = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(5)
            };
            previewPanel.Children.Add(this.codePreview);

            Grid.SetRow(previewPanel, 3);
            mainGrid.Children.Add(previewPanel);

            this.Content = mainGrid;
        }

        private void InitializeDefaultModel()
        {
            model.Placeholders.Add(new PlaceholderInfo { Name = "columns" });
            model.Parameters.Add(new ParameterInfo { Name = "id", Type = "long" });

            UpdateLists();
        }

        private void UpdateLists()
        {
            this.placeholderList.Items.Clear();
            foreach (var placeholder in model.Placeholders)
            {
                this.placeholderList.Items.Add($"✓ {{{{{placeholder.Name}}}}}");
            }

            this.parameterList.Items.Clear();
            foreach (var param in model.Parameters)
            {
                this.parameterList.Items.Add($"✓ @{param.Name} ({param.Type})");
            }

            this.clauseList.Items.Clear();
            foreach (var clause in model.Clauses)
            {
                this.clauseList.Items.Add($"✓ {clause.Type} {clause.Content}");
            }
        }

        private void UpdatePreview()
        {
            model.Operation = this.operationCombo.SelectedItem?.ToString() ?? "SELECT";
            
            var code = model.GenerateCode();
            var sql = model.GenerateSql();

            this.codePreview.Text = $"{code}\n\n// Generated SQL:\n// {sql}";
        }

        private void OnOperationChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void OnAddPlaceholderClick(object sender, RoutedEventArgs e)
        {
            var dialog = new PlaceholderDialog();
            if (dialog.ShowDialog() == true)
            {
                model.Placeholders.Add(new PlaceholderInfo
                {
                    Name = dialog.PlaceholderName,
                    Modifier = dialog.Modifier
                });
                UpdateLists();
                UpdatePreview();
            }
        }

        private void OnAddParameterClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ParameterDialog();
            if (dialog.ShowDialog() == true)
            {
                model.Parameters.Add(new ParameterInfo
                {
                    Name = dialog.ParameterName,
                    Type = dialog.ParameterType
                });
                UpdateLists();
                UpdatePreview();
            }
        }

        private void AddClause(string type, string content)
        {
            model.Clauses.Add(new ClauseInfo
            {
                Type = type,
                Content = content,
                Order = model.Clauses.Count
            });
            UpdateLists();
            UpdatePreview();
        }

        private void OnNewClick(object sender, RoutedEventArgs e)
        {
            model = new SqlTemplateModel();
            InitializeDefaultModel();
            UpdatePreview();
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(model.GenerateCode());
                MessageBox.Show("Code copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Dialog for adding placeholders.
    /// </summary>
    public class PlaceholderDialog : Window
    {
        private TextBox nameBox;
        private ComboBox modifierCombo;

        public string PlaceholderName { get; private set; }
        public string Modifier { get; private set; }

        public PlaceholderDialog()
        {
            Title = "Add Placeholder";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid
            {
                Margin = new Thickness(20)
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new TextBlock { Text = "Placeholder Name:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            this.nameBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(this.nameBox, 1);
            grid.Children.Add(this.nameBox);

            var modifierLabel = new TextBlock { Text = "Modifier (optional):", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(modifierLabel, 2);
            grid.Children.Add(modifierLabel);

            this.modifierCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            this.modifierCombo.Items.Add("");
            this.modifierCombo.Items.Add("--exclude Id");
            this.modifierCombo.Items.Add("--param");
            this.modifierCombo.Items.Add("--value");
            this.modifierCombo.SelectedIndex = 0;
            Grid.SetRow(this.modifierCombo, 3);
            grid.Children.Add(this.modifierCombo);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += OnOkClick;
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5),
                IsCancel = true
            };
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 4);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            PlaceholderName = nameBox.Text.Trim();
            Modifier = modifierCombo.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(PlaceholderName))
            {
                MessageBox.Show("Please enter a placeholder name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Dialog for adding parameters.
    /// </summary>
    public class ParameterDialog : Window
    {
        private TextBox nameBox;
        private ComboBox typeCombo;

        public string ParameterName { get; private set; }
        public string ParameterType { get; private set; }

        public ParameterDialog()
        {
            Title = "Add Parameter";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid
            {
                Margin = new Thickness(20)
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new TextBlock { Text = "Parameter Name:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            this.nameBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(this.nameBox, 1);
            grid.Children.Add(this.nameBox);

            var typeLabel = new TextBlock { Text = "Parameter Type:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(typeLabel, 2);
            grid.Children.Add(typeLabel);

            this.typeCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            this.typeCombo.Items.Add("string");
            this.typeCombo.Items.Add("int");
            this.typeCombo.Items.Add("long");
            this.typeCombo.Items.Add("bool");
            this.typeCombo.Items.Add("DateTime");
            this.typeCombo.SelectedIndex = 0;
            Grid.SetRow(this.typeCombo, 3);
            grid.Children.Add(this.typeCombo);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += OnOkClick;
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5),
                IsCancel = true
            };
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 4);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            ParameterName = nameBox.Text.Trim();
            ParameterType = typeCombo.SelectedItem?.ToString() ?? "string";

            if (string.IsNullOrEmpty(ParameterName))
            {
                MessageBox.Show("Please enter a parameter name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}

