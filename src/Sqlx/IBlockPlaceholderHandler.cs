// <copyright file="IBlockPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;

/// <summary>
/// Defines a block-level placeholder handler that processes content between opening and closing tags.
/// Block placeholders have opening and closing tags (e.g., {{if ...}}content{{/if}}, {{foreach ...}}content{{/foreach}}).
/// </summary>
/// <remarks>
/// <para>
/// Block handlers process the content between opening and closing tags at render time.
/// They can conditionally include content, repeat content (loops), or transform content in other ways.
/// </para>
/// <para>
/// To create a custom block handler:
/// 1. Implement this interface
/// 2. Register the handler with <see cref="PlaceholderProcessor.RegisterHandler"/>
/// 3. The closing tag is automatically registered based on <see cref="ClosingTagName"/>
/// </para>
/// <para>
/// Examples of block handlers:
/// - Conditional ({{if}}): Include content once or not at all
/// - Loop ({{foreach}}): Repeat content multiple times
/// - Context ({{with}}): Transform content with different context
/// </para>
/// </remarks>
public interface IBlockPlaceholderHandler : IPlaceholderHandler
{
    /// <summary>
    /// Gets the closing tag name for this block (e.g., "/if" for "if" blocks).
    /// </summary>
    string ClosingTagName { get; }

    /// <summary>
    /// Processes the block content and returns the rendered result.
    /// </summary>
    /// <param name="options">The options string from the opening tag.</param>
    /// <param name="blockContent">The content between opening and closing tags.</param>
    /// <param name="parameters">The dynamic parameters provided at render time.</param>
    /// <returns>The rendered content. Return empty string to exclude the block, or the processed content (possibly repeated multiple times for loops).</returns>
    /// <remarks>
    /// <para>
    /// This method gives the handler full control over how to process the block content:
    /// </para>
    /// <list type="bullet">
    /// <item><description>For conditional blocks ({{if}}): Return blockContent if condition is true, empty string otherwise.</description></item>
    /// <item><description>For loop blocks ({{foreach}}): Parse blockContent, render it multiple times with different contexts, and concatenate results.</description></item>
    /// <item><description>For transform blocks: Modify and return the transformed blockContent.</description></item>
    /// </list>
    /// </remarks>
    string ProcessBlock(string options, string blockContent, IReadOnlyDictionary<string, object?>? parameters);
}
