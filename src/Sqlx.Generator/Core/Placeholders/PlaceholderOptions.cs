// -----------------------------------------------------------------------
// <copyright file="PlaceholderOptions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Sqlx.Generator.Placeholders;

/// <summary>
/// 占位符选项解析器 - 统一处理命令行风格和管道风格的选项
/// </summary>
public sealed class PlaceholderOptions
{
    private readonly Dictionary<string, string> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _rawOptions;

    public PlaceholderOptions(string options)
    {
        _rawOptions = options ?? "";
        Parse(options);
    }

    /// <summary>原始选项字符串</summary>
    public string Raw => _rawOptions;

    /// <summary>获取选项值，如果不存在返回默认值</summary>
    public string Get(string key, string defaultValue = "") =>
        _options.TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>检查选项是否存在</summary>
    public bool Has(string key) => _options.ContainsKey(key);

    /// <summary>获取第一个非标志参数（用于简单格式如 {{limit 10}}）</summary>
    public string FirstArg { get; private set; } = "";

    private void Parse(string options)
    {
        if (string.IsNullOrWhiteSpace(options)) return;

        // 命令行风格: --exclude Id CreatedAt --param count
        if (options.Contains("--"))
        {
            ParseCommandLineStyle(options);
        }
        // 管道风格: exclude=Id,CreatedAt|param=count
        else if (options.Contains("="))
        {
            ParsePipeStyle(options);
        }
        // 简单格式: 直接值（如 {{limit 10}}）
        else
        {
            FirstArg = options.Trim();
        }
    }

    private void ParseCommandLineStyle(string options)
    {
        var parts = options.Split(new[] { " --" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.TrimStart('-').Trim();
            var spaceIndex = trimmed.IndexOf(' ');
            
            if (spaceIndex > 0)
            {
                var key = trimmed.Substring(0, spaceIndex);
                var value = trimmed.Substring(spaceIndex + 1).Trim();
                _options[key] = value;
            }
            else if (!string.IsNullOrEmpty(trimmed))
            {
                // 布尔标志
                _options[trimmed] = "true";
            }
        }
    }

    private void ParsePipeStyle(string options)
    {
        foreach (var pair in options.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = pair.Split(new[] { '=' }, 2);
            if (keyValue.Length == 2)
            {
                _options[keyValue[0].Trim()] = keyValue[1].Trim();
            }
        }
    }



    /// <summary>获取排除列列表</summary>
    public HashSet<string> GetExcludeSet()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excludeValue = Get("exclude");
        
        if (!string.IsNullOrEmpty(excludeValue))
        {
            foreach (var item in excludeValue.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(item.Trim());
            }
        }
        
        return result;
    }

    /// <summary>获取仅包含列列表</summary>
    public HashSet<string> GetOnlySet()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var onlyValue = Get("only");
        
        if (!string.IsNullOrEmpty(onlyValue))
        {
            foreach (var item in onlyValue.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(item.Trim());
            }
        }
        
        return result;
    }
}
