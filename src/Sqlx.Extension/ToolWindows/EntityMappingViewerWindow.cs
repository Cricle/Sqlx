// -----------------------------------------------------------------------
// <copyright file="EntityMappingViewerWindow.cs" company="Cricle">
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
    /// Entity Mapping Viewer tool window for visualizing ORM mappings.
    /// </summary>
    [Guid("A1B2C3D4-5E6F-7890-ABCD-000000000009")]
    public class EntityMappingViewerWindow : ToolWindowPane
    {
        private EntityMappingViewerControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityMappingViewerWindow"/> class.
        /// </summary>
        public EntityMappingViewerWindow() : base(null)
        {
            this.Caption = "Sqlx Entity Mapping Viewer";
            this.control = new EntityMappingViewerControl();
            this.Content = this.control;
        }
    }

    /// <summary>
    /// Entity mapping information.
    /// </summary>
    public class EntityMappingInfo
    {
        public string EntityName { get; set; }
        public string TableName { get; set; }
        public ObservableCollection<PropertyMapping> PropertyMappings { get; set; } = new ObservableCollection<PropertyMapping>();
    }

    /// <summary>
    /// Property to column mapping.
    /// </summary>
    public class PropertyMapping
    {
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public string ColumnName { get; set; }
        public string SqlType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public string DefaultValue { get; set; }

        public string Status
        {
            get
            {
                if (IsPrimaryKey) return "🔑";
                if (IsForeignKey) return "🔗";
                return "✓";
            }
        }
    }

    /// <summary>
    /// Validation result.
    /// </summary>
    public class MappingValidationResult
    {
        public string Type { get; set; } // Success, Warning, Error
        public string Message { get; set; }
        public string Icon => Type == "Success" ? "✅" : Type == "Warning" ? "⚠️" : "❌";
    }

    /// <summary>
    /// User control for entity mapping viewer content.
    /// </summary>
    public class EntityMappingViewerControl : UserControl
    {
        private ListBox entityList;
        private Canvas mappingCanvas;
        private ListBox propertyList;
        private TextBox detailsText;
        private ListBox validationList;
        private ObservableCollection<EntityMappingInfo> entities;
        private EntityMappingInfo selectedEntity;

        public EntityMappingViewerControl()
        {
            entities = new ObservableCollection<EntityMappingInfo>();
            InitializeComponent();
            GenerateSampleData(); // For demo purposes
        }

        private void InitializeComponent()
        {
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });

            // Left panel - Entity list
            var leftPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var titleText = new TextBlock
            {
                Text = "Entities",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            leftPanel.Children.Add(titleText);

            this.entityList = new ListBox
            {
                ItemsSource = entities
            };
            this.entityList.SelectionChanged += OnEntitySelectionChanged;
            leftPanel.Children.Add(this.entityList);

            Grid.SetColumn(leftPanel, 0);
            mainGrid.Children.Add(leftPanel);

            // Middle panel - Mapping visualization
            var middlePanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var mappingTitle = new TextBlock
            {
                Text = "Entity-Table Mapping",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            middlePanel.Children.Add(mappingTitle);

            var mappingBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Height = 300,
                Margin = new Thickness(0, 0, 0, 10)
            };

            this.mappingCanvas = new Canvas();
            var scrollViewer = new ScrollViewer
            {
                Content = this.mappingCanvas,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            mappingBorder.Child = scrollViewer;
            middlePanel.Children.Add(mappingBorder);

            var propertyTitle = new TextBlock
            {
                Text = "Property Mappings",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            middlePanel.Children.Add(propertyTitle);

            this.propertyList = new ListBox();
            this.propertyList.SelectionChanged += OnPropertySelectionChanged;
            middlePanel.Children.Add(this.propertyList);

            Grid.SetColumn(middlePanel, 1);
            mainGrid.Children.Add(middlePanel);

            // Right panel - Details and validation
            var rightPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var detailsTitle = new TextBlock
            {
                Text = "Mapping Details",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            rightPanel.Children.Add(detailsTitle);

            this.detailsText = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                Height = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10)
            };
            rightPanel.Children.Add(this.detailsText);

            var validationTitle = new TextBlock
            {
                Text = "Validation Results",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            rightPanel.Children.Add(validationTitle);

            this.validationList = new ListBox();
            rightPanel.Children.Add(this.validationList);

            Grid.SetColumn(rightPanel, 2);
            mainGrid.Children.Add(rightPanel);

            this.Content = mainGrid;
        }

        private void GenerateSampleData()
        {
            // Sample User entity
            var userMapping = new EntityMappingInfo
            {
                EntityName = "User",
                TableName = "users"
            };
            userMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "Id",
                PropertyType = "long",
                ColumnName = "id",
                SqlType = "BIGINT",
                IsPrimaryKey = true,
                IsNullable = false
            });
            userMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "Name",
                PropertyType = "string",
                ColumnName = "name",
                SqlType = "VARCHAR(50)",
                IsNullable = false
            });
            userMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "Email",
                PropertyType = "string",
                ColumnName = "email",
                SqlType = "VARCHAR(100)",
                IsNullable = false
            });
            userMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "Age",
                PropertyType = "int",
                ColumnName = "age",
                SqlType = "INT",
                IsNullable = true
            });
            userMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "IsActive",
                PropertyType = "bool",
                ColumnName = "is_active",
                SqlType = "BIT",
                IsNullable = false,
                DefaultValue = "1"
            });
            entities.Add(userMapping);

            // Sample Order entity
            var orderMapping = new EntityMappingInfo
            {
                EntityName = "Order",
                TableName = "orders"
            };
            orderMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "Id",
                PropertyType = "long",
                ColumnName = "id",
                SqlType = "BIGINT",
                IsPrimaryKey = true,
                IsNullable = false
            });
            orderMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "UserId",
                PropertyType = "long",
                ColumnName = "user_id",
                SqlType = "BIGINT",
                IsForeignKey = true,
                IsNullable = false
            });
            orderMapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = "Total",
                PropertyType = "decimal",
                ColumnName = "total",
                SqlType = "DECIMAL(10,2)",
                IsNullable = false
            });
            entities.Add(orderMapping);
        }

        private void OnEntitySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.entityList.SelectedItem is EntityMappingInfo entity)
            {
                selectedEntity = entity;
                UpdateMappingVisualization();
                UpdatePropertyList();
                UpdateValidation();
            }
        }

        private void UpdateMappingVisualization()
        {
            this.mappingCanvas.Children.Clear();

            if (selectedEntity == null) return;

            // Draw entity box (left)
            var entityBox = DrawBox(20, 20, 180, 200, selectedEntity.EntityName + " (C#)", Brushes.LightBlue);
            this.mappingCanvas.Children.Add(entityBox);

            // Draw table box (right)
            var tableBox = DrawBox(400, 20, 180, 200, selectedEntity.TableName + " (Table)", Brushes.LightGreen);
            this.mappingCanvas.Children.Add(tableBox);

            // Draw properties and mappings
            var yEntity = 50;
            var yTable = 50;
            foreach (var mapping in selectedEntity.PropertyMappings)
            {
                // Property text
                var propText = new TextBlock
                {
                    Text = $"{mapping.Status} {mapping.PropertyName}",
                    FontSize = 10
                };
                Canvas.SetLeft(propText, 30);
                Canvas.SetTop(propText, yEntity);
                this.mappingCanvas.Children.Add(propText);

                // Column text
                var colText = new TextBlock
                {
                    Text = $"{mapping.Status} {mapping.ColumnName}",
                    FontSize = 10
                };
                Canvas.SetLeft(colText, 410);
                Canvas.SetTop(colText, yTable);
                this.mappingCanvas.Children.Add(colText);

                // Draw connection line
                var line = new Line
                {
                    X1 = 200,
                    Y1 = yEntity + 7,
                    X2 = 400,
                    Y2 = yTable + 7,
                    Stroke = mapping.IsPrimaryKey ? Brushes.Blue : Brushes.Gray,
                    StrokeThickness = mapping.IsPrimaryKey ? 2 : 1
                };
                if (mapping.IsPrimaryKey || mapping.IsForeignKey)
                {
                    line.StrokeDashArray = new DoubleCollection { 2, 2 };
                }
                this.mappingCanvas.Children.Add(line);

                yEntity += 20;
                yTable += 20;
            }

            this.mappingCanvas.Height = Math.Max(yEntity, yTable) + 40;
        }

        private StackPanel DrawBox(double x, double y, double width, double height, string title, Brush fill)
        {
            var panel = new StackPanel();

            var rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = fill,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            panel.Children.Add(rect);

            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                FontSize = 12
            };
            Canvas.SetLeft(titleText, x);
            Canvas.SetTop(titleText, y);

            var container = new StackPanel();
            container.Children.Add(rect);
            
            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);

            // Add title separately
            this.mappingCanvas.Children.Add(titleText);

            return container;
        }

        private void UpdatePropertyList()
        {
            this.propertyList.Items.Clear();

            if (selectedEntity == null) return;

            foreach (var mapping in selectedEntity.PropertyMappings)
            {
                var displayText = $"{mapping.Status} {mapping.PropertyName} → {mapping.ColumnName} " +
                                $"({mapping.PropertyType} → {mapping.SqlType})";
                this.propertyList.Items.Add(displayText);
            }
        }

        private void OnPropertySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.propertyList.SelectedIndex >= 0 && selectedEntity != null)
            {
                var mapping = selectedEntity.PropertyMappings[this.propertyList.SelectedIndex];
                UpdateDetails(mapping);
            }
        }

        private void UpdateDetails(PropertyMapping mapping)
        {
            var details = $"Property: {mapping.PropertyName}\n" +
                         $"Type: {mapping.PropertyType}\n" +
                         $"\n" +
                         $"Column: {mapping.ColumnName}\n" +
                         $"SQL Type: {mapping.SqlType}\n" +
                         $"\n" +
                         $"Nullable: {(mapping.IsNullable ? "Yes" : "No")}\n" +
                         $"Primary Key: {(mapping.IsPrimaryKey ? "Yes" : "No")}\n" +
                         $"Foreign Key: {(mapping.IsForeignKey ? "Yes" : "No")}\n";

            if (!string.IsNullOrEmpty(mapping.DefaultValue))
            {
                details += $"Default Value: {mapping.DefaultValue}\n";
            }

            this.detailsText.Text = details;
        }

        private void UpdateValidation()
        {
            this.validationList.Items.Clear();

            if (selectedEntity == null) return;

            var results = ValidateMapping(selectedEntity);

            foreach (var result in results)
            {
                this.validationList.Items.Add($"{result.Icon} {result.Message}");
            }

            if (!results.Any())
            {
                this.validationList.Items.Add("✅ No validation issues found");
            }
        }

        private List<MappingValidationResult> ValidateMapping(EntityMappingInfo entity)
        {
            var results = new List<MappingValidationResult>();

            // Check if primary key exists
            if (!entity.PropertyMappings.Any(p => p.IsPrimaryKey))
            {
                results.Add(new MappingValidationResult
                {
                    Type = "Warning",
                    Message = "No primary key defined"
                });
            }

            // Check for nullable primary key
            var pkMapping = entity.PropertyMappings.FirstOrDefault(p => p.IsPrimaryKey);
            if (pkMapping != null && pkMapping.IsNullable)
            {
                results.Add(new MappingValidationResult
                {
                    Type = "Error",
                    Message = "Primary key should not be nullable"
                });
            }

            // Check for properties without column mapping
            var unmappedProperties = entity.PropertyMappings.Where(p => string.IsNullOrEmpty(p.ColumnName));
            if (unmappedProperties.Any())
            {
                results.Add(new MappingValidationResult
                {
                    Type = "Error",
                    Message = $"{unmappedProperties.Count()} properties without column mapping"
                });
            }

            // Check for naming convention
            foreach (var mapping in entity.PropertyMappings)
            {
                if (mapping.ColumnName != mapping.PropertyName.ToLower() && 
                    mapping.ColumnName != ToSnakeCase(mapping.PropertyName))
                {
                    results.Add(new MappingValidationResult
                    {
                        Type = "Warning",
                        Message = $"{mapping.PropertyName}: Naming convention mismatch"
                    });
                }
            }

            // All checks passed
            if (!results.Any())
            {
                results.Add(new MappingValidationResult
                {
                    Type = "Success",
                    Message = "All mappings are valid"
                });
            }

            return results;
        }

        private string ToSnakeCase(string input)
        {
            return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
        }
    }
}

