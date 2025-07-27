// -----------------------------------------------------------------------
// <copyright file="ISqlxSyntaxReceiver.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
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
}
