using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sqlx.Extension.SyntaxColoring
{
    /// <summary>
    /// Classifies SqlTemplate attribute strings into different token types
    /// </summary>
    internal class SqlTemplateClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _classificationRegistry;
        private readonly IClassificationType _sqlKeywordType;
        private readonly IClassificationType _sqlPlaceholderType;
        private readonly IClassificationType _sqlParameterType;
        private readonly IClassificationType _sqlStringType;
        private readonly IClassificationType _sqlCommentType;

        // SQL Keywords regex (case-insensitive)
        private static readonly Regex SqlKeywordRegex = new Regex(
            @"\b(SELECT|INSERT|UPDATE|DELETE|FROM|WHERE|JOIN|INNER|LEFT|RIGHT|OUTER|ON|" +
            @"ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT|OFFSET|DISTINCT|AS|AND|OR|NOT|IN|LIKE|" +
            @"BETWEEN|IS|NULL|EXISTS|UNION|ALL|CREATE|ALTER|DROP|TABLE|INDEX|VALUES|SET|" +
            @"CASE|WHEN|THEN|ELSE|END|COUNT|SUM|AVG|MIN|MAX|ASC|DESC)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Placeholder regex: {{...}}
        private static readonly Regex PlaceholderRegex = new Regex(
            @"\{\{[a-zA-Z_][a-zA-Z0-9_]*(?:\s+[a-zA-Z_][a-zA-Z0-9_]*)*(?:\s+--desc)?\}\}",
            RegexOptions.Compiled);

        // Parameter regex: @paramName
        private static readonly Regex ParameterRegex = new Regex(
            @"@[a-zA-Z_][a-zA-Z0-9_]*",
            RegexOptions.Compiled);

        // String literal regex: 'text'
        private static readonly Regex StringLiteralRegex = new Regex(
            @"'(?:[^']|'')*'",
            RegexOptions.Compiled);

        // Comment regex: -- comment or /* comment */
        private static readonly Regex CommentRegex = new Regex(
            @"--[^\r\n]*|/\*.*?\*/",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public SqlTemplateClassifier(IClassificationTypeRegistryService registry)
        {
            _classificationRegistry = registry;

            // Get or create classification types
            _sqlKeywordType = registry.GetClassificationType("SqlKeyword");
            _sqlPlaceholderType = registry.GetClassificationType("SqlPlaceholder");
            _sqlParameterType = registry.GetClassificationType("SqlParameter");
            _sqlStringType = registry.GetClassificationType("SqlString");
            _sqlCommentType = registry.GetClassificationType("SqlComment");
        }

        /// <summary>
        /// Classify the given span of text
        /// </summary>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var classifications = new List<ClassificationSpan>();
            var text = span.GetText();

            // Check if this is inside a SqlTemplate attribute
            if (!IsSqlTemplateContext(span))
            {
                return classifications;
            }

            // Extract the SQL string content (between quotes)
            var sqlContent = ExtractSqlContent(text);
            if (string.IsNullOrEmpty(sqlContent))
            {
                return classifications;
            }

            // Find the start offset of the SQL content within the span
            var sqlStartOffset = text.IndexOf('"') + 1;
            if (sqlStartOffset <= 0)
            {
                return classifications;
            }

            // Create a list to track already classified regions
            var classifiedRanges = new List<Tuple<int, int>>();

            // 1. Classify comments first (highest priority to avoid conflicts)
            foreach (Match match in CommentRegex.Matches(sqlContent))
            {
                var start = span.Start + sqlStartOffset + match.Index;
                var matchSpan = new SnapshotSpan(span.Snapshot, start, match.Length);
                classifications.Add(new ClassificationSpan(matchSpan, _sqlCommentType));
                classifiedRanges.Add(Tuple.Create(match.Index, match.Index + match.Length));
            }

            // 2. Classify string literals (to avoid classifying keywords inside strings)
            foreach (Match match in StringLiteralRegex.Matches(sqlContent))
            {
                if (IsAlreadyClassified(match.Index, match.Length, classifiedRanges))
                    continue;

                var start = span.Start + sqlStartOffset + match.Index;
                var matchSpan = new SnapshotSpan(span.Snapshot, start, match.Length);
                classifications.Add(new ClassificationSpan(matchSpan, _sqlStringType));
                classifiedRanges.Add(Tuple.Create(match.Index, match.Index + match.Length));
            }

            // 3. Classify placeholders
            foreach (Match match in PlaceholderRegex.Matches(sqlContent))
            {
                if (IsAlreadyClassified(match.Index, match.Length, classifiedRanges))
                    continue;

                var start = span.Start + sqlStartOffset + match.Index;
                var matchSpan = new SnapshotSpan(span.Snapshot, start, match.Length);
                classifications.Add(new ClassificationSpan(matchSpan, _sqlPlaceholderType));
                classifiedRanges.Add(Tuple.Create(match.Index, match.Index + match.Length));
            }

            // 4. Classify parameters
            foreach (Match match in ParameterRegex.Matches(sqlContent))
            {
                if (IsAlreadyClassified(match.Index, match.Length, classifiedRanges))
                    continue;

                var start = span.Start + sqlStartOffset + match.Index;
                var matchSpan = new SnapshotSpan(span.Snapshot, start, match.Length);
                classifications.Add(new ClassificationSpan(matchSpan, _sqlParameterType));
                classifiedRanges.Add(Tuple.Create(match.Index, match.Index + match.Length));
            }

            // 5. Classify SQL keywords
            foreach (Match match in SqlKeywordRegex.Matches(sqlContent))
            {
                if (IsAlreadyClassified(match.Index, match.Length, classifiedRanges))
                    continue;

                var start = span.Start + sqlStartOffset + match.Index;
                var matchSpan = new SnapshotSpan(span.Snapshot, start, match.Length);
                classifications.Add(new ClassificationSpan(matchSpan, _sqlKeywordType));
            }

            return classifications;
        }

        /// <summary>
        /// Check if we're in a SqlTemplate attribute context
        /// </summary>
        private bool IsSqlTemplateContext(SnapshotSpan span)
        {
            var text = span.GetText();
            
            // Simple check: does the text contain [SqlTemplate(" pattern
            return text.Contains("[SqlTemplate(") || 
                   text.Contains("SqlTemplate(\"") ||
                   text.Contains("SqlTemplate(@\"");
        }

        /// <summary>
        /// Extract SQL content from the attribute string
        /// </summary>
        private string ExtractSqlContent(string text)
        {
            // Find content between quotes
            var startQuote = text.IndexOf('"');
            if (startQuote < 0) return null;

            var endQuote = text.LastIndexOf('"');
            if (endQuote <= startQuote) return null;

            return text.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        /// <summary>
        /// Check if a range is already classified
        /// </summary>
        private bool IsAlreadyClassified(int start, int length, List<Tuple<int, int>> classifiedRanges)
        {
            var end = start + length;
            foreach (var range in classifiedRanges)
            {
                // Check if there's any overlap
                if (start < range.Item2 && end > range.Item1)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

