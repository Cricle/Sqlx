// -----------------------------------------------------------------------
// <copyright file="ISqlxSyntaxReceiver.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

/// <summary>
/// Language specific interface for syntax context receiver which is used to collect information about methods.
/// </summary>
internal interface ISqlxSyntaxReceiver : ISyntaxContextReceiver
{
    /// <summary>
    /// Gets list of collected methods.
    /// </summary>
    List<IMethodSymbol> Methods { get; }

    /// <summary>
    /// Called for every syntax node in the compilation.
    /// </summary>
    /// <param name="syntaxNode">The syntax node to visit.</param>
    void OnVisitSyntaxNode(SyntaxNode syntaxNode);
}
