// -----------------------------------------------------------------------
// <copyright file="SqlxClassifier.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.VisualStudio.Highlighting
{
    /// <summary>
    /// Classifier provider for Sqlx SQL syntax highlighting.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType("CSharp")]
    internal class SqlxClassifierProvider : IClassifierProvider
    {
        [Import]
        private IClassificationTypeRegistryService ClassificationTypeRegistry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() =>
                new SqlxClassifier(ClassificationTypeRegistry));
        }
    }

    /// <summary>
    /// Classification type definitions for Sqlx SQL syntax.
    /// </summary>
    internal static class SqlxClassificationTypes
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sqlx.sql.keyword")]
        internal static ClassificationTypeDefinition SqlKeyword = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sqlx.sql.string")]
        internal static ClassificationTypeDefinition SqlString = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sqlx.sql.comment")]
        internal static ClassificationTypeDefinition SqlComment = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sqlx.sql.parameter")]
        internal static ClassificationTypeDefinition SqlParameter = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sqlx.sql.table")]
        internal static ClassificationTypeDefinition SqlTable = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sqlx.sql.column")]
        internal static ClassificationTypeDefinition SqlColumn = null;
    }

    /// <summary>
    /// Classification format definitions for Sqlx SQL syntax.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "sqlx.sql.keyword")]
    [Name("sqlx.sql.keyword")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlKeywordFormat : ClassificationFormatDefinition
    {
        public SqlKeywordFormat()
        {
            DisplayName = "Sqlx SQL Keyword";
            ForegroundColor = System.Windows.Media.Colors.Blue;
            IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "sqlx.sql.string")]
    [Name("sqlx.sql.string")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlStringFormat : ClassificationFormatDefinition
    {
        public SqlStringFormat()
        {
            DisplayName = "Sqlx SQL String";
            ForegroundColor = System.Windows.Media.Colors.DarkRed;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "sqlx.sql.parameter")]
    [Name("sqlx.sql.parameter")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlParameterFormat : ClassificationFormatDefinition
    {
        public SqlParameterFormat()
        {
            DisplayName = "Sqlx SQL Parameter";
            ForegroundColor = System.Windows.Media.Colors.DarkMagenta;
            IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "sqlx.sql.table")]
    [Name("sqlx.sql.table")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlTableFormat : ClassificationFormatDefinition
    {
        public SqlTableFormat()
        {
            DisplayName = "Sqlx SQL Table";
            ForegroundColor = System.Windows.Media.Colors.DarkGreen;
            IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "sqlx.sql.column")]
    [Name("sqlx.sql.column")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlColumnFormat : ClassificationFormatDefinition
    {
        public SqlColumnFormat()
        {
            DisplayName = "Sqlx SQL Column";
            ForegroundColor = System.Windows.Media.Colors.DarkCyan;
        }
    }

    /// <summary>
    /// Classifier for SQL syntax within Sqlx attributes.
    /// </summary>
    internal class SqlxClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _typeRegistry;
        private readonly IClassificationType _sqlKeywordType;
        private readonly IClassificationType _sqlStringType;
        private readonly IClassificationType _sqlParameterType;
        private readonly IClassificationType _sqlTableType;
        private readonly IClassificationType _sqlColumnType;

        private static readonly Regex SqlxAttributeRegex = new Regex(
            @"\[Sqlx\(\s*""([^""]*)""\s*\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SqlKeywordRegex = new Regex(
            @"\b(SELECT|FROM|WHERE|INSERT|UPDATE|DELETE|JOIN|INNER|LEFT|RIGHT|FULL|ON|AS|ORDER|BY|GROUP|HAVING|UNION|DISTINCT|TOP|LIMIT|OFFSET|COUNT|SUM|AVG|MIN|MAX|AND|OR|NOT|IN|LIKE|BETWEEN|IS|NULL)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SqlParameterRegex = new Regex(
            @"[@:$]\w+|\?\d*",
            RegexOptions.Compiled);

        private static readonly Regex SqlStringRegex = new Regex(
            @"'([^']|'')*'",
            RegexOptions.Compiled);

        private static readonly string[] KnownTables =
        {
            "Users", "Orders", "Products", "Categories", "OrderItems", "Employees"
        };

        private static readonly string[] KnownColumns =
        {
            "Id", "FirstName", "LastName", "Email", "Age", "IsActive", "CreatedDate",
            "UserId", "OrderDate", "TotalAmount", "Status", "ShippingAddress",
            "Name", "Price", "CategoryId", "Description", "InStock", "ParentId",
            "OrderId", "ProductId", "Quantity", "UnitPrice", "Salary", "Department", "HireDate"
        };

        public SqlxClassifier(IClassificationTypeRegistryService typeRegistry)
        {
            _typeRegistry = typeRegistry;
            _sqlKeywordType = typeRegistry.GetClassificationType("sqlx.sql.keyword");
            _sqlStringType = typeRegistry.GetClassificationType("sqlx.sql.string");
            _sqlParameterType = typeRegistry.GetClassificationType("sqlx.sql.parameter");
            _sqlTableType = typeRegistry.GetClassificationType("sqlx.sql.table");
            _sqlColumnType = typeRegistry.GetClassificationType("sqlx.sql.column");
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var classifications = new List<ClassificationSpan>();
            var text = span.GetText();

            // Find Sqlx attributes
            foreach (Match attributeMatch in SqlxAttributeRegex.Matches(text))
            {
                if (attributeMatch.Groups.Count > 1)
                {
                    var sqlContent = attributeMatch.Groups[1].Value;
                    var sqlStartIndex = attributeMatch.Groups[1].Index;

                    // Classify SQL content within the attribute
                    ClassifySqlContent(sqlContent, sqlStartIndex, span.Start, classifications);
                }
            }

            return classifications;
        }

        private void ClassifySqlContent(string sqlContent, int sqlStartIndex, SnapshotPoint spanStart, List<ClassificationSpan> classifications)
        {
            // Classify SQL keywords
            foreach (Match match in SqlKeywordRegex.Matches(sqlContent))
            {
                var start = spanStart + sqlStartIndex + match.Index;
                var classificationSpan = new ClassificationSpan(
                    new SnapshotSpan(start, match.Length),
                    _sqlKeywordType);
                classifications.Add(classificationSpan);
            }

            // Classify SQL parameters
            foreach (Match match in SqlParameterRegex.Matches(sqlContent))
            {
                var start = spanStart + sqlStartIndex + match.Index;
                var classificationSpan = new ClassificationSpan(
                    new SnapshotSpan(start, match.Length),
                    _sqlParameterType);
                classifications.Add(classificationSpan);
            }

            // Classify SQL strings
            foreach (Match match in SqlStringRegex.Matches(sqlContent))
            {
                var start = spanStart + sqlStartIndex + match.Index;
                var classificationSpan = new ClassificationSpan(
                    new SnapshotSpan(start, match.Length),
                    _sqlStringType);
                classifications.Add(classificationSpan);
            }

            // Classify known tables
            foreach (var table in KnownTables)
            {
                var tableRegex = new Regex(@"\b" + Regex.Escape(table) + @"\b", RegexOptions.IgnoreCase);
                foreach (Match match in tableRegex.Matches(sqlContent))
                {
                    var start = spanStart + sqlStartIndex + match.Index;
                    var classificationSpan = new ClassificationSpan(
                        new SnapshotSpan(start, match.Length),
                        _sqlTableType);
                    classifications.Add(classificationSpan);
                }
            }

            // Classify known columns
            foreach (var column in KnownColumns)
            {
                var columnRegex = new Regex(@"\b" + Regex.Escape(column) + @"\b", RegexOptions.IgnoreCase);
                foreach (Match match in columnRegex.Matches(sqlContent))
                {
                    var start = spanStart + sqlStartIndex + match.Index;
                    var classificationSpan = new ClassificationSpan(
                        new SnapshotSpan(start, match.Length),
                        _sqlColumnType);
                    classifications.Add(classificationSpan);
                }
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        protected virtual void OnClassificationChanged(SnapshotSpan span)
        {
            ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));
        }
    }
}

