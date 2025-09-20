// -----------------------------------------------------------------------
// <copyright file="TemplateEngineFactory.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

/// <summary>
/// Factory for creating SQL template engines.
/// </summary>
public static class TemplateEngineFactory
{
    /// <summary>
    /// Gets a new default template engine instance.
    /// </summary>
    public static ISqlTemplateEngine Default => new SqlTemplateEngine();
}

