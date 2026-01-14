// <copyright file="IBlockPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;

/// <summary>
/// Defines a block-level placeholder handler that controls content inclusion.
/// Block placeholders have opening and closing tags (e.g., {{if ...}}content{{/if}}).
/// </summary>
/// <remarks>
/// <para>
/// Block handlers evaluate conditions at render time and control whether
/// the content between opening and closing tags should be included.
/// </para>
/// <para>
/// To create a custom block handler:
/// 1. Implement this interface
/// 2. Register both the opening handler and closing handler with <see cref="PlaceholderProcessor.RegisterHandler"/>
/// 3. Register the closing tag name with <see cref="PlaceholderProcessor.RegisterBlockClosingTag"/>
/// </para>
/// </remarks>
public interface IBlockPlaceholderHandler : IPlaceholderHandler
{
    /// <summary>
    /// Gets the closing tag name for this block (e.g., "/if" for "if" blocks).
    /// </summary>
    string ClosingTagName { get; }

    /// <summary>
    /// Evaluates whether the block content should be included.
    /// </summary>
    /// <param name="options">The options string from the opening tag.</param>
    /// <param name="parameters">The dynamic parameters provided at render time.</param>
    /// <returns><c>true</c> if the block content should be included; otherwise, <c>false</c>.</returns>
    bool ShouldInclude(string options, IReadOnlyDictionary<string, object?>? parameters);
}
