// <copyright file="SqlTemplate.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Represents a prepared SQL template with efficient rendering for dynamic placeholders.
/// </summary>
/// <remarks>
/// <para>
/// SqlTemplate processes SQL templates containing placeholders (e.g., {{columns}}, {{where --param predicate}})
/// and provides efficient rendering with support for both static and dynamic placeholders.
/// </para>
/// <para>
/// Static placeholders are resolved once during <see cref="Prepare"/> and cached.
/// Dynamic placeholders are resolved each time during <see cref="Render(IReadOnlyDictionary{string, object})"/>.
/// </para>
/// </remarks>
#if NET7_0_OR_GREATER
public sealed partial class SqlTemplate
#else
public sealed class SqlTemplate
#endif
{
    /// <summary>
    /// The parsed template segments containing static text, dynamic placeholders, and block markers.
    /// </summary>
    private readonly TemplateSegment[] _segments;

    /// <summary>
    /// Indicates whether the template contains conditional blocks ({{if}}/{{/if}}).
    /// </summary>
    private readonly bool _hasBlocks;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTemplate"/> class.
    /// </summary>
    /// <param name="sql">The pre-rendered SQL with static placeholders resolved.</param>
    /// <param name="segments">The parsed template segments.</param>
    /// <param name="hasBlocks">Whether the template contains conditional blocks.</param>
    private SqlTemplate(string sql, TemplateSegment[] segments, bool hasBlocks)
    {
        Sql = sql;
        _segments = segments;
        _hasBlocks = hasBlocks;
    }

    /// <summary>
    /// Gets the prepared SQL string with static placeholders resolved.
    /// </summary>
    /// <remarks>
    /// This property returns the SQL with all static placeholders (like {{columns}}, {{table}})
    /// already replaced. Dynamic placeholders are represented as empty strings in this output.
    /// Use <see cref="Render(IReadOnlyDictionary{string, object})"/> to get the fully rendered SQL with dynamic values.
    /// </remarks>
    public string Sql { get; }

    /// <summary>
    /// Gets a value indicating whether this template contains dynamic placeholders or conditional blocks.
    /// </summary>
    /// <remarks>
    /// When this is false, the <see cref="Sql"/> property contains the final SQL and
    /// calling <see cref="Render(IReadOnlyDictionary{string, object})"/> is not necessary.
    /// </remarks>
    public bool HasDynamicPlaceholders => _segments.Length > 1 || _hasBlocks;

    /// <summary>
    /// Prepares a SQL template by resolving static placeholders and recording dynamic placeholder positions.
    /// </summary>
    /// <param name="template">The SQL template string containing placeholders.</param>
    /// <param name="context">The context providing dialect, table name, and column metadata.</param>
    /// <returns>A prepared <see cref="SqlTemplate"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unknown placeholder is encountered.</exception>
    /// <remarks>
    /// <para>
    /// This method parses the template and categorizes each placeholder:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Static placeholders ({{columns}}, {{table}}) are resolved immediately.</description></item>
    /// <item><description>Dynamic placeholders ({{where --param predicate}}) are recorded for later rendering.</description></item>
    /// <item><description>Block placeholders ({{if --param condition}}) are recorded for conditional rendering.</description></item>
    /// </list>
    /// </remarks>
    public static SqlTemplate Prepare(string template, PlaceholderContext context)
    {
        var segments = new List<TemplateSegment>();
        var lastIndex = 0;
        var hasBlocks = false;

        // Iterate through all placeholder matches in the template
        foreach (Match match in PlaceholderRegex().Matches(template))
        {
            var name = match.Groups[1].Value;      // Placeholder name (e.g., "columns", "if", "/if")
            var options = match.Groups[2].Value;   // Placeholder options (e.g., "--param predicate")
            var beforeText = template.Substring(lastIndex, match.Index - lastIndex);

            void AddBefore() { if (!string.IsNullOrEmpty(beforeText)) segments.Add(TemplateSegment.Static(beforeText)); }

            // Handle block closing tags (e.g., {{/if}})
            if (PlaceholderProcessor.IsBlockClosingTag(name))
            {
                AddBefore();
                segments.Add(TemplateSegment.BlockEnd(name));
                hasBlocks = true;
            }
            // Look up and handle block opening tags (e.g., {{if --param condition}})
            else if (PlaceholderProcessor.TryGetHandler(name, out var handler) && handler is IBlockPlaceholderHandler blockHandler)
            {
                AddBefore();
                segments.Add(TemplateSegment.BlockStart(blockHandler, options));
                hasBlocks = true;
            }
            // Handle regular placeholders
            else if (handler != null)
            {
                if (handler.GetType(options) == PlaceholderType.Static)
                {
                    // Static placeholder: resolve immediately and merge with preceding text
                    segments.Add(TemplateSegment.Static(beforeText + handler.Process(context, options)));
                }
                else
                {
                    // Dynamic placeholder: record for later rendering
                    AddBefore();
                    segments.Add(TemplateSegment.Dynamic(handler, options, context));
                }
            }
            else
            {
                throw new InvalidOperationException($"Unknown placeholder: {{{{{name}}}}}");
            }

            lastIndex = match.Index + match.Length;
        }

        // Add any remaining text after the last placeholder
        if (lastIndex < template.Length) segments.Add(TemplateSegment.Static(template.Substring(lastIndex)));

        // Merge consecutive static segments for efficiency
        var mergedSegments = MergeStaticSegments(segments);

        // Build the static SQL (with dynamic placeholders as empty strings)
        var sb = new StringBuilder();
        foreach (var seg in mergedSegments)
            if (seg.Type == SegmentType.Static) sb.Append(seg.Text);

        return new SqlTemplate(sb.ToString(), mergedSegments, hasBlocks);
    }

    /// <summary>
    /// Renders the template with dynamic parameters, resolving dynamic placeholders and block handlers.
    /// </summary>
    /// <param name="dynamicParameters">A dictionary of parameter names and values for dynamic placeholders.</param>
    /// <returns>The fully rendered SQL string.</returns>
    /// <remarks>
    /// <para>
    /// This method processes all segments in order:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Static segments are appended directly.</description></item>
    /// <item><description>Dynamic segments are rendered using the provided parameters.</description></item>
    /// <item><description>Block segments are collected and processed by their handlers (e.g., conditionals, loops).</description></item>
    /// </list>
    /// <para>
    /// Block handlers receive the complete block content and can return empty string (exclude),
    /// the original content (include once), or repeated content (for loops).
    /// </para>
    /// </remarks>
    public string Render(IReadOnlyDictionary<string, object?>? dynamicParameters)
    {
        // Fast path: no segments means empty template
        if (_segments.Length == 0) return Sql;

        // Fast path: single static segment with no blocks means no dynamic content
        if (_segments.Length == 1 && _segments[0].Type == SegmentType.Static && !_hasBlocks) return Sql;

        return RenderSegments(_segments, 0, _segments.Length, dynamicParameters);
    }

    /// <summary>
    /// Renders a range of segments, handling nested blocks recursively.
    /// </summary>
    /// <param name="segments">The array of all segments.</param>
    /// <param name="start">The starting index in the segments array.</param>
    /// <param name="end">The ending index (exclusive) in the segments array.</param>
    /// <param name="dynamicParameters">The dynamic parameters for rendering.</param>
    /// <returns>The rendered content for this segment range.</returns>
    private string RenderSegments(TemplateSegment[] segments, int start, int end, IReadOnlyDictionary<string, object?>? dynamicParameters)
    {
        var sb = new StringBuilder();

        for (var i = start; i < end; i++)
        {
            var seg = segments[i];
            switch (seg.Type)
            {
                case SegmentType.Static:
                    sb.Append(seg.Text);
                    break;

                case SegmentType.Dynamic:
                    sb.Append(seg.Handler!.Render(seg.Context!, seg.Options!, dynamicParameters));
                    break;

                case SegmentType.BlockStart:
                    var blockEnd = FindMatchingBlockEnd(segments, i + 1, end);
                    if (blockEnd == -1) throw new InvalidOperationException($"Unmatched block opening tag at segment {i}");
                    var blockContent = RenderSegments(segments, i + 1, blockEnd, dynamicParameters);
                    sb.Append(seg.BlockHandler!.ProcessBlock(seg.Options!, blockContent, dynamicParameters));
                    i = blockEnd;
                    break;

                case SegmentType.BlockEnd:
                    throw new InvalidOperationException($"Unmatched block closing tag at segment {i}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Finds the matching BlockEnd for a BlockStart, handling nested blocks.
    /// </summary>
    /// <param name="segments">The array of all segments.</param>
    /// <param name="start">The starting index to search from (after the BlockStart).</param>
    /// <param name="end">The ending index (exclusive) to search within.</param>
    /// <returns>The index of the matching BlockEnd, or -1 if not found.</returns>
    private static int FindMatchingBlockEnd(TemplateSegment[] segments, int start, int end)
    {
        var depth = 1;
        for (var i = start; i < end; i++)
        {
            if (segments[i].Type == SegmentType.BlockStart) depth++;
            else if (segments[i].Type == SegmentType.BlockEnd && --depth == 0) return i;
        }
        return -1;
    }

    /// <summary>
    /// Thread-local cached dictionary for single-parameter rendering to avoid allocations.
    /// </summary>
    [ThreadStatic]
    private static Dictionary<string, object?>? _singleParamCache;

    /// <summary>
    /// Renders the template with a single dynamic parameter.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The fully rendered SQL string.</returns>
    /// <remarks>
    /// This overload is optimized for the common case of a single dynamic parameter,
    /// using a thread-local cached dictionary to avoid allocations.
    /// </remarks>
    public string Render(string key, object? value) => RenderWithCache((key, value));

    /// <summary>
    /// Renders the template with two dynamic parameters.
    /// </summary>
    /// <param name="key1">The first parameter name.</param>
    /// <param name="value1">The first parameter value.</param>
    /// <param name="key2">The second parameter name.</param>
    /// <param name="value2">The second parameter value.</param>
    /// <returns>The fully rendered SQL string.</returns>
    public string Render(string key1, object? value1, string key2, object? value2) => RenderWithCache((key1, value1), (key2, value2));

    /// <summary>
    /// Helper method to render with cached dictionary.
    /// </summary>
    private string RenderWithCache(params (string key, object? value)[] parameters)
    {
        var cache = _singleParamCache ??= new Dictionary<string, object?>(parameters.Length);
        cache.Clear();
        foreach (var (key, value) in parameters)
            cache[key] = value;
        return Render(cache);
    }

    /// <summary>
    /// Merges consecutive static segments into single segments for efficiency.
    /// </summary>
    /// <param name="segments">The list of parsed segments.</param>
    /// <returns>An array of segments with consecutive static segments merged.</returns>
    /// <remarks>
    /// This optimization reduces the number of segments to iterate during rendering
    /// and reduces memory allocations.
    /// </remarks>
    private static TemplateSegment[] MergeStaticSegments(List<TemplateSegment> segments)
    {
        if (segments.Count == 0) return Array.Empty<TemplateSegment>();

        var result = new List<TemplateSegment>();
        var currentStatic = new StringBuilder();

        void FlushStatic()
        {
            if (currentStatic.Length > 0)
            {
                result.Add(TemplateSegment.Static(currentStatic.ToString()));
                currentStatic.Clear();
            }
        }

        foreach (var seg in segments)
        {
            if (seg.Type == SegmentType.Static)
                currentStatic.Append(seg.Text);
            else
            {
                FlushStatic();
                result.Add(seg);
            }
        }

        FlushStatic();
        return result.ToArray();
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Gets the compiled regex for matching placeholders in templates.
    /// </summary>
    /// <returns>A regex that matches {{name}} or {{name options}} patterns.</returns>
    /// <remarks>
    /// Pattern breakdown:
    /// - \{\{ - Opening braces
    /// - (/?\w+) - Capture group 1: optional "/" followed by word characters (placeholder name)
    /// - (?:\s+([^}]+))? - Optional non-capturing group with capture group 2: whitespace followed by non-brace characters (options)
    /// - \}\} - Closing braces
    /// </remarks>
    [GeneratedRegex(@"\{\{(/?\w+)(?:\s+([^}]+))?\}\}")]
    private static partial Regex PlaceholderRegex();
#else
    /// <summary>
    /// The compiled regex instance for matching placeholders (used in older .NET versions).
    /// </summary>
    private static readonly Regex PlaceholderRegexInstance = new(@"\{\{(/?\w+)(?:\s+([^}]+))?\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Gets the compiled regex for matching placeholders in templates.
    /// </summary>
    /// <returns>A regex that matches {{name}} or {{name options}} patterns.</returns>
    private static Regex PlaceholderRegex() => PlaceholderRegexInstance;
#endif

    /// <summary>
    /// Defines the type of a template segment.
    /// </summary>
    private enum SegmentType : byte
    {
        Static,      // A static text segment that is output directly
        Dynamic,     // A dynamic placeholder that is rendered at runtime with parameters
        BlockStart,  // The start of a conditional block (e.g., {{if}})
        BlockEnd     // The end of a conditional block (e.g., {{/if}})
    }

    /// <summary>
    /// Represents a single segment of a parsed template.
    /// </summary>
    /// <remarks>
    /// This is a readonly struct for performance - it avoids heap allocations
    /// when stored in arrays and passed by value.
    /// </remarks>
    private readonly struct TemplateSegment
    {
        /// <summary>
        /// The type of this segment.
        /// </summary>
        public readonly SegmentType Type;

        /// <summary>
        /// The static text content (only used for Static segments).
        /// </summary>
        public readonly string? Text;

        /// <summary>
        /// The placeholder handler for dynamic rendering (only used for Dynamic segments).
        /// </summary>
        public readonly IPlaceholderHandler? Handler;

        /// <summary>
        /// The block placeholder handler for conditional logic (only used for BlockStart segments).
        /// </summary>
        public readonly IBlockPlaceholderHandler? BlockHandler;

        /// <summary>
        /// The placeholder options string (e.g., "--param predicate").
        /// </summary>
        public readonly string? Options;

        /// <summary>
        /// The placeholder context for rendering (only used for Dynamic segments).
        /// </summary>
        public readonly PlaceholderContext? Context;

        private TemplateSegment(SegmentType type, string? text = null, IPlaceholderHandler? handler = null,
            IBlockPlaceholderHandler? blockHandler = null, string? options = null, PlaceholderContext? context = null)
        {
            Type = type;
            Text = text;
            Handler = handler;
            BlockHandler = blockHandler;
            Options = options;
            Context = context;
        }

        /// <summary>
        /// Creates a static text segment.
        /// </summary>
        /// <param name="text">The static text content.</param>
        /// <returns>A new static segment.</returns>
        public static TemplateSegment Static(string text) => new(SegmentType.Static, text);

        /// <summary>
        /// Creates a dynamic placeholder segment.
        /// </summary>
        /// <param name="handler">The handler for rendering this placeholder.</param>
        /// <param name="options">The placeholder options.</param>
        /// <param name="context">The placeholder context.</param>
        /// <returns>A new dynamic segment.</returns>
        public static TemplateSegment Dynamic(IPlaceholderHandler handler, string options, PlaceholderContext context)
            => new(SegmentType.Dynamic, handler: handler, options: options, context: context);

        /// <summary>
        /// Creates a block start segment for conditional blocks.
        /// </summary>
        /// <param name="handler">The block handler for evaluating the condition.</param>
        /// <param name="options">The block options (e.g., "--param condition").</param>
        /// <returns>A new block start segment.</returns>
        public static TemplateSegment BlockStart(IBlockPlaceholderHandler handler, string options)
            => new(SegmentType.BlockStart, blockHandler: handler, options: options);

        /// <summary>
        /// Creates a block end segment for closing conditional blocks.
        /// </summary>
        /// <param name="closingTagName">The closing tag name (e.g., "/if").</param>
        /// <returns>A new block end segment.</returns>
        public static TemplateSegment BlockEnd(string closingTagName) => new(SegmentType.BlockEnd);
    }
}
