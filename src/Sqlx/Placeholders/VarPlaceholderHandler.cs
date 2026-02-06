// <copyright file="VarPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;

/// <summary>
/// Handles the {{var}} placeholder for runtime variable resolution.
/// </summary>
/// <remarks>
/// <para>
/// This handler resolves variables declared with [SqlxVar] attributes at runtime.
/// Variables are resolved by calling the VarProvider function with the repository instance
/// and variable name. The returned value is inserted directly into the SQL as a literal.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--name varName</c> - The variable name to resolve (required)</description></item>
/// </list>
/// <para>
/// <strong>Security Warning:</strong> Variable values are inserted as literals into SQL.
/// Only use {{var}} for trusted, application-controlled values like tenant IDs or SQL keywords.
/// Never use {{var}} for user input - use SQL parameters instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Repository with SqlxVar methods
/// public partial class UserRepository
/// {
///     [SqlxVar("tenantId")]
///     private string GetTenantId() => TenantContext.Current;
/// }
/// 
/// // Template: SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}
/// // Generated SQL: SELECT * FROM users WHERE tenant_id = tenant-123
/// // Note: tenant-123 is inserted as a literal, not a parameter
/// </code>
/// </example>
public sealed class VarPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static VarPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "var";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options) => PlaceholderType.Dynamic;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        // Dynamic placeholder - should not be called during Prepare phase
        throw new InvalidOperationException(
            "{{var}} is a dynamic placeholder and cannot be processed during Prepare phase. " +
            "It must be resolved during Render phase with VarProvider.");
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Extract variable name from --name option
        var variableName = ParseOption(options, "name");
        if (string.IsNullOrEmpty(variableName))
        {
            throw new InvalidOperationException(
                "{{var}} placeholder requires --name option. " +
                "Usage: {{var --name variableName}}");
        }

        // Check if VarProvider is configured
        if (context.VarProvider == null)
        {
            throw new InvalidOperationException(
                $"VarProvider not configured for variable: {variableName}. " +
                "Ensure PlaceholderContext is created with VarProvider and Instance.");
        }

        // Call VarProvider to get the variable value
        // Note: Any exceptions from VarProvider (e.g., unknown variable) are propagated
        var value = context.VarProvider(context.Instance!, variableName);

        // Return the value directly as a literal
        return value;
    }
}
