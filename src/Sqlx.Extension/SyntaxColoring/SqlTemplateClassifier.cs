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
            
            try
            {
                // Get the full line text
                var line = span.Start.GetContainingLine();
                var lineText = line.GetText();
                
                // Check if this line contains SqlTemplate attribute
                if (!lineText.Contains("SqlTemplate"))
                {
                    return classifications;
                }

                // Find the SQL string content
                var sqlRegex = new Regex(@"\[SqlTemplate\s*\(\s*(?:@)?""([^""]*)""\s*\)", RegexOptions.IgnoreCase);
                var match = sqlRegex.Match(lineText);
                
                if (!match.Success)
                {
                    return classifications;
                }

                var sqlContent = match.Groups[1].Value;
                if (string.IsNullOrEmpty(sqlContent))
                {
                    return classifications;
                }

                // Calculate the start position of SQL content in the line
                var sqlStartInLine = match.Groups[1].Index;
                var sqlStartPosition = line.Start.Position + sqlStartInLine;

                // Create a list to track already classified regions
                var classifiedRanges = new List<Tuple<int, int>>();

                // 1. Classify comments first
                foreach (Match commentMatch in CommentRegex.Matches(sqlContent))
                {
                    var start = sqlStartPosition + commentMatch.Index;
                    if (start >= span.Start && start < span.End)
                    {
                        var end = Math.Min(start + commentMatch.Length, span.End);
                        var matchSpan = new SnapshotSpan(span.Snapshot, start, end - start);
                        classifications.Add(new ClassificationSpan(matchSpan, _sqlCommentType));
                        classifiedRanges.Add(Tuple.Create(commentMatch.Index, commentMatch.Index + commentMatch.Length));
                    }
                }

                // 2. Classify string literals
                foreach (Match stringMatch in StringLiteralRegex.Matches(sqlContent))
                {
                    if (IsAlreadyClassified(stringMatch.Index, stringMatch.Length, classifiedRanges))
                        continue;

                    var start = sqlStartPosition + stringMatch.Index;
                    if (start >= span.Start && start < span.End)
                    {
                        var end = Math.Min(start + stringMatch.Length, span.End);
                        var matchSpan = new SnapshotSpan(span.Snapshot, start, end - start);
                        classifications.Add(new ClassificationSpan(matchSpan, _sqlStringType));
                        classifiedRanges.Add(Tuple.Create(stringMatch.Index, stringMatch.Index + stringMatch.Length));
                    }
                }

                // 3. Classify placeholders
                foreach (Match placeholderMatch in PlaceholderRegex.Matches(sqlContent))
                {
                    if (IsAlreadyClassified(placeholderMatch.Index, placeholderMatch.Length, classifiedRanges))
                        continue;

                    var start = sqlStartPosition + placeholderMatch.Index;
                    if (start >= span.Start && start < span.End)
                    {
                        var end = Math.Min(start + placeholderMatch.Length, span.End);
                        var matchSpan = new SnapshotSpan(span.Snapshot, start, end - start);
                        classifications.Add(new ClassificationSpan(matchSpan, _sqlPlaceholderType));
                        classifiedRanges.Add(Tuple.Create(placeholderMatch.Index, placeholderMatch.Index + placeholderMatch.Length));
                    }
                }

                // 4. Classify parameters
                foreach (Match paramMatch in ParameterRegex.Matches(sqlContent))
                {
                    if (IsAlreadyClassified(paramMatch.Index, paramMatch.Length, classifiedRanges))
                        continue;

                    var start = sqlStartPosition + paramMatch.Index;
                    if (start >= span.Start && start < span.End)
                    {
                        var end = Math.Min(start + paramMatch.Length, span.End);
                        var matchSpan = new SnapshotSpan(span.Snapshot, start, end - start);
                        classifications.Add(new ClassificationSpan(matchSpan, _sqlParameterType));
                        classifiedRanges.Add(Tuple.Create(paramMatch.Index, paramMatch.Index + paramMatch.Length));
                    }
                }

                // 5. Classify SQL keywords
                foreach (Match keywordMatch in SqlKeywordRegex.Matches(sqlContent))
                {
                    if (IsAlreadyClassified(keywordMatch.Index, keywordMatch.Length, classifiedRanges))
                        continue;

                    var start = sqlStartPosition + keywordMatch.Index;
                    if (start >= span.Start && start < span.End)
                    {
                        var end = Math.Min(start + keywordMatch.Length, span.End);
                        var matchSpan = new SnapshotSpan(span.Snapshot, start, end - start);
                        classifications.Add(new ClassificationSpan(matchSpan, _sqlKeywordType));
                    }
                }
            }
            catch
            {
                // Silently ignore errors
            }

            return classifications;
        }

        /// <summary>
        /// Check if we're in a SqlTemplate attribute context
        /// </summary>
        private bool IsSqlTemplateContext(SnapshotSpan span)
        {
            try
            {
                // Get a larger context (up to 500 characters before and after)
                var snapshot = span.Snapshot;
                var start = Math.Max(0, span.Start - 500);
                var end = Math.Min(snapshot.Length, span.End + 500);
                var contextSpan = new SnapshotSpan(snapshot, start, end - start);
                var contextText = contextSpan.GetText();

                // Look for SqlTemplate attribute pattern
                var sqlTemplatePattern = new Regex(@"\[SqlTemplate\s*\(\s*[""@]", RegexOptions.IgnoreCase);
                return sqlTemplatePattern.IsMatch(contextText);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extract SQL content from the attribute string
        /// </summary>
        private string ExtractSqlContent(string text)
        {
            try
            {
                // Handle verbatim string @"..."
                if (text.Contains("@\""))
                {
                    var startIndex = text.IndexOf("@\"") + 2;
                    var endIndex = text.LastIndexOf('"');
                    if (endIndex > startIndex)
                    {
                        return text.Substring(startIndex, endIndex - startIndex);
                    }
                }

                // Handle regular string "..."
                var firstQuote = text.IndexOf('"');
                if (firstQuote >= 0)
                {
                    var lastQuote = text.LastIndexOf('"');
                    if (lastQuote > firstQuote)
                    {
                        return text.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
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

