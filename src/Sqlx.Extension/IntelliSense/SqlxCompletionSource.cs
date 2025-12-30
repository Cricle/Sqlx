// -----------------------------------------------------------------------
// <copyright file="SqlxCompletionSource.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Sqlx.Extension.IntelliSense
{
    /// <summary>
    /// Provides IntelliSense completion for SqlTemplate attributes.
    /// </summary>
    internal class SqlxCompletionSource : ICompletionSource
    {
        private ITextBuffer _textBuffer;
        private ITextStructureNavigatorSelectorService _navigator;
        private bool _isDisposed = false;

        public SqlxCompletionSource(ITextBuffer textBuffer, ITextStructureNavigatorSelectorService navigator)
        {
            _textBuffer = textBuffer;
            _navigator = navigator;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_isDisposed)
                return;

            try
            {
                ITextSnapshot snapshot = _textBuffer.CurrentSnapshot;
                var triggerPoint = session.GetTriggerPoint(snapshot);

                if (triggerPoint == null)
                    return;

                var line = triggerPoint.Value.GetContainingLine();
                var lineText = line.GetText();

                // Check if we're in a SqlTemplate attribute
                if (!IsSqlTemplateContext(lineText))
                    return;

                // Get the text before the trigger point
                int position = triggerPoint.Value.Position;
                int lineStart = line.Start.Position;
                int relativePosition = position - lineStart;
                string textBeforeCursor = lineText.Substring(0, relativePosition);

                // Determine what kind of completion we need
                CompletionContext context = DetermineCompletionContext(textBeforeCursor);

                if (context.Type == CompletionType.None)
                    return;

                // Get completions based on context
                var completions = GetCompletions(context, textBeforeCursor);

                if (completions.Count == 0)
                    return;

                // Find the span to replace
                var applicableTo = GetApplicableSpan(snapshot, triggerPoint.Value, context);

                // Create completion set
                var completionSet = new CompletionSet(
                    "Sqlx",
                    "Sqlx Completions",
                    applicableTo,
                    completions,
                    null);

                completionSets.Add(completionSet);
            }
            catch
            {
                // Silently ignore errors
            }
        }

        private bool IsSqlTemplateContext(string lineText)
        {
            return lineText.Contains("[SqlTemplate") || lineText.Contains("SqlTemplate(");
        }

        private CompletionContext DetermineCompletionContext(string textBeforeCursor)
        {
            // Check for placeholder: {{
            if (textBeforeCursor.EndsWith("{{") || 
                (textBeforeCursor.Contains("{{") && !textBeforeCursor.EndsWith("}}")))
            {
                // Check if we're after a space (modifier context)
                var lastBraceIndex = textBeforeCursor.LastIndexOf("{{");
                var textAfterBrace = textBeforeCursor.Substring(lastBraceIndex + 2);
                
                if (textAfterBrace.Contains(" ") && !textAfterBrace.TrimEnd().EndsWith("--"))
                {
                    return new CompletionContext { Type = CompletionType.Modifier };
                }
                
                return new CompletionContext { Type = CompletionType.Placeholder };
            }

            // Check for parameter: @
            if (textBeforeCursor.TrimEnd().EndsWith("@"))
            {
                return new CompletionContext { Type = CompletionType.Parameter };
            }

            // Check for SQL keywords (after space or at start)
            if (IsKeywordContext(textBeforeCursor))
            {
                return new CompletionContext { Type = CompletionType.Keyword };
            }

            return new CompletionContext { Type = CompletionType.None };
        }

        private bool IsKeywordContext(string textBeforeCursor)
        {
            // Check if we're in a good position for SQL keyword
            if (textBeforeCursor.Length == 0)
                return false;

            var lastChar = textBeforeCursor[textBeforeCursor.Length - 1];
            
            // After space, comma, or opening parenthesis
            return char.IsWhiteSpace(lastChar) || lastChar == ',' || lastChar == '(';
        }

        private List<Completion> GetCompletions(CompletionContext context, string textBeforeCursor)
        {
            switch (context.Type)
            {
                case CompletionType.Placeholder:
                    return GetPlaceholderCompletions();

                case CompletionType.Modifier:
                    return GetModifierCompletions();

                case CompletionType.Parameter:
                    return GetParameterCompletions();

                case CompletionType.Keyword:
                    return GetKeywordCompletions(textBeforeCursor);

                default:
                    return new List<Completion>();
            }
        }

        private List<Completion> GetPlaceholderCompletions()
        {
            var completions = new List<Completion>
            {
                new Completion("columns", "columns}}", "All columns from the entity", null, null),
                new Completion("table", "table}}", "Table name for the entity", null, null),
                new Completion("values", "values}}", "All values for INSERT", null, null),
                new Completion("set", "set}}", "SET clause for UPDATE", null, null),
                new Completion("where", "where}}", "WHERE clause from expression", null, null),
                new Completion("limit", "limit}}", "LIMIT clause", null, null),
                new Completion("offset", "offset}}", "OFFSET clause", null, null),
                new Completion("orderby", "orderby}}", "ORDER BY clause", null, null),
                new Completion("batch_values", "batch_values}}", "Batch values for bulk INSERT", null, null),
            };

            return completions;
        }

        private List<Completion> GetModifierCompletions()
        {
            var completions = new List<Completion>
            {
                new Completion("--exclude", "--exclude ", "Exclude specific columns", null, null),
                new Completion("--param", "--param ", "Use parameter value", null, null),
                new Completion("--value", "--value ", "Use fixed value", null, null),
                new Completion("--from", "--from ", "Use value from object", null, null),
                new Completion("--desc", "--desc", "Descending order", null, null),
            };

            return completions;
        }

        private List<Completion> GetParameterCompletions()
        {
            // TODO: Extract from method signature using Roslyn
            var completions = new List<Completion>
            {
                new Completion("@id", "id", "Entity ID parameter", null, null),
                new Completion("@name", "name", "Name parameter", null, null),
                new Completion("@limit", "limit", "Limit parameter", null, null),
                new Completion("@offset", "offset", "Offset parameter", null, null),
            };

            return completions;
        }

        private List<Completion> GetKeywordCompletions(string textBeforeCursor)
        {
            var keywords = new[]
            {
                // Core operations
                "SELECT", "INSERT", "UPDATE", "DELETE",
                // Clauses
                "FROM", "WHERE", "JOIN", "ON", "GROUP BY", "ORDER BY", "HAVING",
                // Join types
                "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "OUTER JOIN", "CROSS JOIN",
                // Operators
                "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN", "IS NULL", "IS NOT NULL",
                // Aggregate functions
                "COUNT", "SUM", "AVG", "MIN", "MAX",
                // Others
                "DISTINCT", "AS", "LIMIT", "OFFSET", "UNION", "ALL", "EXISTS",
                "CASE", "WHEN", "THEN", "ELSE", "END",
                "ASC", "DESC"
            };

            var completions = keywords
                .Select(k => new Completion(k, k + " ", $"SQL keyword: {k}", null, null))
                .ToList();

            return completions;
        }

        private ITrackingSpan GetApplicableSpan(ITextSnapshot snapshot, SnapshotPoint triggerPoint, CompletionContext context)
        {
            var line = triggerPoint.GetContainingLine();
            var lineText = line.GetText();
            int position = triggerPoint.Position;
            int lineStart = line.Start.Position;
            int relativePosition = position - lineStart;

            int start = relativePosition;
            int end = relativePosition;

            // Find the start of the current word
            if (context.Type == CompletionType.Placeholder)
            {
                // Go back to {{
                while (start > 0 && lineText[start - 1] != '{')
                {
                    start--;
                }
                // Skip the {{
                if (start >= 2 && lineText.Substring(start - 2, 2) == "{{")
                {
                    start -= 2;
                }
            }
            else if (context.Type == CompletionType.Parameter)
            {
                // Go back to @
                while (start > 0 && lineText[start - 1] != '@')
                {
                    start--;
                }
                if (start > 0)
                {
                    start--;
                }
            }
            else
            {
                // Go back to start of word
                while (start > 0 && char.IsLetterOrDigit(lineText[start - 1]))
                {
                    start--;
                }
            }

            // Find the end of the current word
            while (end < lineText.Length && char.IsLetterOrDigit(lineText[end]))
            {
                end++;
            }

            // Create tracking span
            var span = new SnapshotSpan(snapshot, lineStart + start, end - start);
            return snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }

        private class CompletionContext
        {
            public CompletionType Type { get; set; }
        }

        private enum CompletionType
        {
            None,
            Placeholder,
            Modifier,
            Parameter,
            Keyword
        }
    }
}

