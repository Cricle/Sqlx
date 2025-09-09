// -----------------------------------------------------------------------
// <copyright file="SqlxCompletionSource.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;

namespace Sqlx.VisualStudio.IntelliSense
{
    /// <summary>
    /// Provides IntelliSense completion for Sqlx SQL strings.
    /// </summary>
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Sqlx SQL Completion")]
    [ContentType("CSharp")]
    internal class SqlxCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        [Import]
        private ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        public IAsyncCompletionSource GetOrCreateCompletionSource(ITextView textView)
        {
            return new SqlxCompletionSource(TextStructureNavigatorSelector.GetTextStructureNavigator(textView.TextBuffer));
        }
    }

    /// <summary>
    /// Completion source for Sqlx SQL IntelliSense.
    /// </summary>
    internal class SqlxCompletionSource : IAsyncCompletionSource
    {
        private readonly ITextStructureNavigator _navigator;
        private static readonly Dictionary<string, string[]> DatabaseTables = new Dictionary<string, string[]>
        {
            ["Users"] = new[] { "Id", "FirstName", "LastName", "Email", "Age", "IsActive", "CreatedDate" },
            ["Orders"] = new[] { "Id", "UserId", "OrderDate", "TotalAmount", "Status", "ShippingAddress" },
            ["Products"] = new[] { "Id", "Name", "Price", "CategoryId", "Description", "InStock" },
            ["Categories"] = new[] { "Id", "Name", "Description", "ParentId" },
            ["OrderItems"] = new[] { "Id", "OrderId", "ProductId", "Quantity", "UnitPrice" },
            ["Employees"] = new[] { "Id", "FirstName", "LastName", "Email", "Salary", "Department", "HireDate" }
        };

        private static readonly string[] SqlKeywords = new[]
        {
            "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL",
            "ON", "AS", "ORDER", "BY", "GROUP", "HAVING", "UNION", "DISTINCT", "TOP", "LIMIT", "OFFSET",
            "COUNT", "SUM", "AVG", "MIN", "MAX", "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN", "IS", "NULL",
            "CREATE", "TABLE", "ALTER", "DROP", "INDEX", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER",
            "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "UNIQUE", "CHECK", "DEFAULT", "CONSTRAINT"
        };

        private static readonly string[] SqlxAttributes = new[]
        {
            "[Sqlx(\"", "[SqlExecuteType(SqlExecuteTypes.", "[TableName(\"", "[RepositoryFor(typeof("
        };

        public SqlxCompletionSource(ITextStructureNavigator navigator)
        {
            _navigator = navigator;
        }

        public async Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            await Task.Yield(); // Make this method async

            var line = triggerLocation.GetContainingLine();
            var lineText = line.GetText();

            // Check if we're inside a Sqlx attribute
            if (!IsSqlxContext(lineText))
            {
                return CompletionContext.Empty;
            }

            var completionItems = new List<CompletionItem>();

            // Add SQL keywords
            foreach (var keyword in SqlKeywords)
            {
                completionItems.Add(new CompletionItem(
                    displayText: keyword,
                    source: this,
                    icon: KnownMonikers.Keyword.ToImageElement(),
                    filters: ImmutableArray.Create(CompletionFilters.Keyword),
                    suffix: " ",
                    insertText: keyword,
                    sortText: "1" + keyword,
                    filterText: keyword,
                    attributeIcons: ImmutableArray<ImageElement>.Empty));
            }

            // Add table names
            foreach (var table in DatabaseTables.Keys)
            {
                completionItems.Add(new CompletionItem(
                    displayText: table,
                    source: this,
                    icon: KnownMonikers.Table.ToImageElement(),
                    filters: ImmutableArray.Create(CompletionFilters.Member),
                    suffix: "",
                    insertText: table,
                    sortText: "2" + table,
                    filterText: table,
                    attributeIcons: ImmutableArray<ImageElement>.Empty));
            }

            // Add column names based on context
            var currentTable = ExtractTableFromContext(lineText);
            if (!string.IsNullOrEmpty(currentTable) && DatabaseTables.TryGetValue(currentTable, out var columns))
            {
                foreach (var column in columns)
                {
                    completionItems.Add(new CompletionItem(
                        displayText: column,
                        source: this,
                        icon: KnownMonikers.Field.ToImageElement(),
                        filters: ImmutableArray.Create(CompletionFilters.Property),
                        suffix: "",
                        insertText: column,
                        sortText: "3" + column,
                        filterText: column,
                        attributeIcons: ImmutableArray<ImageElement>.Empty));
                }
            }

            // Add Sqlx-specific snippets
            completionItems.AddRange(GetSqlxSnippets());

            return new CompletionContext(completionItems.ToImmutableArray());
        }

        public async Task<object> GetDescriptionAsync(
            IAsyncCompletionSession session,
            CompletionItem item,
            CancellationToken token)
        {
            await Task.Yield();

            return item.DisplayText switch
            {
                var keyword when SqlKeywords.Contains(keyword) => $"SQL Keyword: {keyword}",
                var table when DatabaseTables.ContainsKey(table) => $"Table: {table}\nColumns: {string.Join(", ", DatabaseTables[table])}",
                _ => $"Sqlx completion item: {item.DisplayText}"
            };
        }

        private static bool IsSqlxContext(string lineText)
        {
            return SqlxAttributes.Any(attr => lineText.Contains(attr, StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractTableFromContext(string lineText)
        {
            // Simple extraction - look for "FROM tablename" pattern
            var fromIndex = lineText.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
            if (fromIndex >= 0)
            {
                var afterFrom = lineText.Substring(fromIndex + 4).Trim();
                var tableName = afterFrom.Split(' ', '\t')[0];
                return tableName;
            }

            return string.Empty;
        }

        private static IEnumerable<CompletionItem> GetSqlxSnippets()
        {
            var snippets = new[]
            {
                new { Display = "SELECT * FROM", Insert = "SELECT * FROM ", Description = "Basic SELECT statement" },
                new { Display = "INSERT INTO", Insert = "INSERT INTO ", Description = "INSERT statement" },
                new { Display = "UPDATE SET", Insert = "UPDATE  SET ", Description = "UPDATE statement" },
                new { Display = "DELETE FROM", Insert = "DELETE FROM ", Description = "DELETE statement" },
                new { Display = "WHERE condition", Insert = "WHERE ", Description = "WHERE clause" },
                new { Display = "ORDER BY", Insert = "ORDER BY ", Description = "ORDER BY clause" },
                new { Display = "GROUP BY", Insert = "GROUP BY ", Description = "GROUP BY clause" },
                new { Display = "INNER JOIN", Insert = "INNER JOIN  ON ", Description = "INNER JOIN clause" },
                new { Display = "LEFT JOIN", Insert = "LEFT JOIN  ON ", Description = "LEFT JOIN clause" }
            };

            return snippets.Select(snippet => new CompletionItem(
                displayText: snippet.Display,
                source: null,
                icon: KnownMonikers.Snippet.ToImageElement(),
                filters: ImmutableArray.Create(CompletionFilters.Snippet),
                suffix: "",
                insertText: snippet.Insert,
                sortText: "0" + snippet.Display,
                filterText: snippet.Display,
                attributeIcons: ImmutableArray<ImageElement>.Empty));
        }
    }
}

