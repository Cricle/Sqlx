// <copyright file="SqlTemplate.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

#if NET7_0_OR_GREATER
public sealed partial class SqlTemplate
#else
public sealed class SqlTemplate
#endif
{
    private readonly TemplateSegment[] _segments;
    private readonly bool _hasBlocks;

    private SqlTemplate(string sql, TemplateSegment[] segments, bool hasBlocks)
    {
        Sql = sql;
        _segments = segments;
        _hasBlocks = hasBlocks;
    }

    public string Sql { get; }
    public bool HasDynamicPlaceholders => _segments.Length > 1 || _hasBlocks;

    public static SqlTemplate Prepare(string template, PlaceholderContext context)
    {
        var segments = new List<TemplateSegment>();
        var lastIndex = 0;
        var hasBlocks = false;

        foreach (Match match in PlaceholderRegex().Matches(template))
        {
            var name = match.Groups[1].Value;
            var options = match.Groups[2].Value;

            if (PlaceholderProcessor.IsBlockClosingTag(name))
            {
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(before)) segments.Add(TemplateSegment.Static(before));
                segments.Add(TemplateSegment.BlockEnd(name));
                lastIndex = match.Index + match.Length;
                hasBlocks = true;
                continue;
            }

            if (!PlaceholderProcessor.TryGetHandler(name, out var handler))
                throw new InvalidOperationException($"Unknown placeholder: {{{{{name}}}}}");

            if (handler is IBlockPlaceholderHandler blockHandler)
            {
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(before)) segments.Add(TemplateSegment.Static(before));
                segments.Add(TemplateSegment.BlockStart(blockHandler, options));
                lastIndex = match.Index + match.Length;
                hasBlocks = true;
                continue;
            }

            var type = handler.GetType(options);
            if (type == PlaceholderType.Static)
            {
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                var replacement = handler.Process(context, options);
                segments.Add(TemplateSegment.Static(before + replacement));
            }
            else
            {
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(before)) segments.Add(TemplateSegment.Static(before));
                segments.Add(TemplateSegment.Dynamic(handler, options, context));
            }
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < template.Length) segments.Add(TemplateSegment.Static(template.Substring(lastIndex)));
        var mergedSegments = MergeStaticSegments(segments);
        var sql = BuildStaticSql(mergedSegments);
        return new SqlTemplate(sql, mergedSegments, hasBlocks);
    }

    public string Render(IReadOnlyDictionary<string, object?>? dynamicParameters)
    {
        if (_segments.Length == 0) return Sql;
        if (_segments.Length == 1 && _segments[0].Type == SegmentType.Static && !_hasBlocks) return Sql;

        var sb = new StringBuilder();
        var skipDepth = 0;

        for (var i = 0; i < _segments.Length; i++)
        {
            var seg = _segments[i];
            switch (seg.Type)
            {
                case SegmentType.Static:
                    if (skipDepth == 0) sb.Append(seg.Text);
                    break;
                case SegmentType.Dynamic:
                    if (skipDepth == 0) sb.Append(seg.Handler!.Render(seg.Context!, seg.Options!, dynamicParameters));
                    break;
                case SegmentType.BlockStart:
                    if (skipDepth > 0) skipDepth++;
                    else if (!seg.BlockHandler!.ShouldInclude(seg.Options!, dynamicParameters)) skipDepth = 1;
                    break;
                case SegmentType.BlockEnd:
                    if (skipDepth > 0) skipDepth--;
                    break;
            }
        }
        return sb.ToString();
    }

    private static string BuildStaticSql(TemplateSegment[] segments)
    {
        var sb = new StringBuilder();
        foreach (var seg in segments)
            if (seg.Type == SegmentType.Static) sb.Append(seg.Text);
        return sb.ToString();
    }

    private static TemplateSegment[] MergeStaticSegments(List<TemplateSegment> segments)
    {
        if (segments.Count == 0) return Array.Empty<TemplateSegment>();
        var result = new List<TemplateSegment>();
        var currentStatic = new StringBuilder();
        foreach (var seg in segments)
        {
            if (seg.Type == SegmentType.Static) currentStatic.Append(seg.Text);
            else
            {
                if (currentStatic.Length > 0)
                {
                    result.Add(TemplateSegment.Static(currentStatic.ToString()));
                    currentStatic.Clear();
                }
                result.Add(seg);
            }
        }
        if (currentStatic.Length > 0) result.Add(TemplateSegment.Static(currentStatic.ToString()));
        return result.ToArray();
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"\{\{(/?\w+)(?:\s+([^}]+))?\}\}")]
    private static partial Regex PlaceholderRegex();
#else
    private static readonly Regex PlaceholderRegexInstance = new(@"\{\{(/?\w+)(?:\s+([^}]+))?\}\}", RegexOptions.Compiled);
    private static Regex PlaceholderRegex() => PlaceholderRegexInstance;
#endif

    private enum SegmentType : byte { Static, Dynamic, BlockStart, BlockEnd }

    private readonly struct TemplateSegment
    {
        public readonly SegmentType Type;
        public readonly string? Text;
        public readonly IPlaceholderHandler? Handler;
        public readonly IBlockPlaceholderHandler? BlockHandler;
        public readonly string? Options;
        public readonly PlaceholderContext? Context;

        private TemplateSegment(SegmentType type, string? text, IPlaceholderHandler? handler,
            IBlockPlaceholderHandler? blockHandler, string? options, PlaceholderContext? context)
        {
            Type = type; Text = text; Handler = handler;
            BlockHandler = blockHandler; Options = options; Context = context;
        }

        public static TemplateSegment Static(string text) => new(SegmentType.Static, text, null, null, null, null);
        public static TemplateSegment Dynamic(IPlaceholderHandler handler, string options, PlaceholderContext context)
            => new(SegmentType.Dynamic, null, handler, null, options, context);
        public static TemplateSegment BlockStart(IBlockPlaceholderHandler handler, string options)
            => new(SegmentType.BlockStart, null, null, handler, options, null);
        public static TemplateSegment BlockEnd(string closingTagName)
            => new(SegmentType.BlockEnd, null, null, null, null, null);
    }
}
