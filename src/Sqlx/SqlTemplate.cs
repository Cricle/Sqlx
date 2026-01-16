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

            // Handle block closing tags (e.g., {{/if}})
            if (PlaceholderProcessor.IsBlockClosingTag(name))
            {
                // Add any text before this closing tag as a static segment
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(before)) segments.Add(TemplateSegment.Static(before));
                segments.Add(TemplateSegment.BlockEnd(name));
                lastIndex = match.Index + match.Length;
                hasBlocks = true;
                continue;
            }

            // Look up the handler for this placeholder name
            if (!PlaceholderProcessor.TryGetHandler(name, out var handler))
                throw new InvalidOperationException($"Unknown placeholder: {{{{{name}}}}}");

            // Handle block opening tags (e.g., {{if --param condition}})
            if (handler is IBlockPlaceholderHandler blockHandler)
            {
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(before)) segments.Add(TemplateSegment.Static(before));
                segments.Add(TemplateSegment.BlockStart(blockHandler, options));
                lastIndex = match.Index + match.Length;
                hasBlocks = true;
                continue;
            }

            // Determine if this is a static or dynamic placeholder
            var type = handler.GetType(options);
            if (type == PlaceholderType.Static)
            {
                // Static placeholder: resolve immediately and merge with preceding text
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                var replacement = handler.Process(context, options);
                segments.Add(TemplateSegment.Static(before + replacement));
            }
            else
            {
                // Dynamic placeholder: record for later rendering
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(before)) segments.Add(TemplateSegment.Static(before));
                segments.Add(TemplateSegment.Dynamic(handler, options, context));
            }
            lastIndex = match.Index + match.Length;
        }

        // Add any remaining text after the last placeholder
        if (lastIndex < template.Length) segments.Add(TemplateSegment.Static(template.Substring(lastIndex)));

        // Merge consecutive static segments for efficiency
        var mergedSegments = MergeStaticSegments(segments);

        // Build the static SQL (with dynamic placeholders as empty strings)
        var sql = BuildStaticSql(mergedSegments);

        return new SqlTemplate(sql, mergedSegments, hasBlocks);
    }

    /// <summary>
    /// Renders the template with dynamic parameters, resolving dynamic placeholders and conditional blocks.
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
    /// <item><description>Block segments control conditional inclusion based on parameter values.</description></item>
    /// </list>
    /// <para>
    /// The skipDepth counter tracks nested conditional blocks to properly handle
    /// nested {{if}} blocks when an outer condition is false.
    /// </para>
    /// </remarks>
    public string Render(IReadOnlyDictionary<string, object?>? dynamicParameters)
    {
        // Fast path: no segments means empty template
        if (_segments.Length == 0) return Sql;

        // Fast path: single static segment with no blocks means no dynamic content
        if (_segments.Length == 1 && _segments[0].Type == SegmentType.Static && !_hasBlocks) return Sql;

        var sb = new StringBuilder();

        // skipDepth tracks how many levels of false conditional blocks we're inside
        // When skipDepth > 0, we skip all content until we exit those blocks
        var skipDepth = 0;

        for (var i = 0; i < _segments.Length; i++)
        {
            var seg = _segments[i];
            switch (seg.Type)
            {
                case SegmentType.Static:
                    // Only append static text if we're not inside a skipped block
                    if (skipDepth == 0) sb.Append(seg.Text);
                    break;

                case SegmentType.Dynamic:
                    // Only render dynamic content if we're not inside a skipped block
                    if (skipDepth == 0) sb.Append(seg.Handler!.Render(seg.Context!, seg.Options!, dynamicParameters));
                    break;

                case SegmentType.BlockStart:
                    if (skipDepth > 0)
                    {
                        // Already skipping: increment depth for nested block tracking
                        skipDepth++;
                    }
                    else if (!seg.BlockHandler!.ShouldInclude(seg.Options!, dynamicParameters))
                    {
                        // Condition is false: start skipping this block's content
                        skipDepth = 1;
                    }
                    break;

                case SegmentType.BlockEnd:
                    // Decrement skip depth when exiting a block
                    if (skipDepth > 0) skipDepth--;
                    break;
            }
        }
        return sb.ToString();
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
    public string Render(string key, object? value)
    {
        var cache = _singleParamCache ??= new Dictionary<string, object?>(1);
        cache.Clear();
        cache[key] = value;
        return Render(cache);
    }

    /// <summary>
    /// Renders the template with two dynamic parameters.
    /// </summary>
    /// <param name="key1">The first parameter name.</param>
    /// <param name="value1">The first parameter value.</param>
    /// <param name="key2">The second parameter name.</param>
    /// <param name="value2">The second parameter value.</param>
    /// <returns>The fully rendered SQL string.</returns>
    public string Render(string key1, object? value1, string key2, object? value2)
    {
        var cache = _singleParamCache ??= new Dictionary<string, object?>(2);
        cache.Clear();
        cache[key1] = value1;
        cache[key2] = value2;
        return Render(cache);
    }

    /// <summary>
    /// Builds the static SQL string by concatenating all static segments.
    /// </summary>
    /// <param name="segments">The merged template segments.</param>
    /// <returns>A string containing only the static portions of the template.</returns>
    /// <remarks>
    /// Dynamic placeholders are not included in this output, resulting in a partial SQL
    /// that can be used when no dynamic parameters are needed.
    /// </remarks>
    private static string BuildStaticSql(TemplateSegment[] segments)
    {
        var sb = new StringBuilder();
        foreach (var seg in segments)
            if (seg.Type == SegmentType.Static) sb.Append(seg.Text);
        return sb.ToString();
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

        foreach (var seg in segments)
        {
            if (seg.Type == SegmentType.Static)
            {
                // Accumulate static text
                currentStatic.Append(seg.Text);
            }
            else
            {
                // Flush accumulated static text before adding non-static segment
                if (currentStatic.Length > 0)
                {
                    result.Add(TemplateSegment.Static(currentStatic.ToString()));
                    currentStatic.Clear();
                }
                result.Add(seg);
            }
        }

        // Flush any remaining static text
        if (currentStatic.Length > 0) result.Add(TemplateSegment.Static(currentStatic.ToString()));

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
        /// <summary>
        /// A static text segment that is output directly.
        /// </summary>
        Static,

        /// <summary>
        /// A dynamic placeholder that is rendered at runtime with parameters.
        /// </summary>
        Dynamic,

        /// <summary>
        /// The start of a conditional block (e.g., {{if}}).
        /// </summary>
        BlockStart,

        /// <summary>
        /// The end of a conditional block (e.g., {{/if}}).
        /// </summary>
        BlockEnd
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateSegment"/> struct.
        /// </summary>
        private TemplateSegment(SegmentType type, string? text, IPlaceholderHandler? handler,
            IBlockPlaceholderHandler? blockHandler, string? options, PlaceholderContext? context)
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
        public static TemplateSegment Static(string text) => new(SegmentType.Static, text, null, null, null, null);

        /// <summary>
        /// Creates a dynamic placeholder segment.
        /// </summary>
        /// <param name="handler">The handler for rendering this placeholder.</param>
        /// <param name="options">The placeholder options.</param>
        /// <param name="context">The placeholder context.</param>
        /// <returns>A new dynamic segment.</returns>
        public static TemplateSegment Dynamic(IPlaceholderHandler handler, string options, PlaceholderContext context)
            => new(SegmentType.Dynamic, null, handler, null, options, context);

        /// <summary>
        /// Creates a block start segment for conditional blocks.
        /// </summary>
        /// <param name="handler">The block handler for evaluating the condition.</param>
        /// <param name="options">The block options (e.g., "--param condition").</param>
        /// <returns>A new block start segment.</returns>
        public static TemplateSegment BlockStart(IBlockPlaceholderHandler handler, string options)
            => new(SegmentType.BlockStart, null, null, handler, options, null);

        /// <summary>
        /// Creates a block end segment for closing conditional blocks.
        /// </summary>
        /// <param name="closingTagName">The closing tag name (e.g., "/if").</param>
        /// <returns>A new block end segment.</returns>
        public static TemplateSegment BlockEnd(string closingTagName)
            => new(SegmentType.BlockEnd, null, null, null, null, null);
    }
}
