// -----------------------------------------------------------------------
// <copyright file="SqlxVarAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a method as a variable provider for SQL templates.
    /// The source generator will create a GetVar method and VarProvider property
    /// to enable dynamic variable substitution in SQL templates using {{var --name variableName}} syntax.
    /// </summary>
    /// <remarks>
    /// <para>Method requirements:</para>
    /// <list type="bullet">
    /// <item>Must return <see cref="string"/></item>
    /// <item>Must have zero parameters</item>
    /// <item>Can be instance or static</item>
    /// <item>Can have any accessibility (private, protected, public)</item>
    /// </list>
    /// <para>Security warning: Values are inserted as literals into SQL. Only use for trusted, application-controlled data.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [SqlxVar("tenantId")]
    /// private string GetTenantId() => TenantContext.Current;
    /// 
    /// [SqlxVar("timestamp")]
    /// private static string GetTimestamp() => "CURRENT_TIMESTAMP";
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SqlxVarAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxVarAttribute"/> class.
        /// </summary>
        /// <param name="variableName">The name of the variable to be used in SQL templates.</param>
        /// <exception cref="System.ArgumentException">Thrown when variableName is null, empty, or whitespace.</exception>
        public SqlxVarAttribute(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new System.ArgumentException(
                    "Variable name cannot be null, empty, or whitespace.",
                    nameof(variableName));
            }

            VariableName = variableName;
        }

        /// <summary>
        /// Gets the variable name used in SQL templates.
        /// This name is used with the {{var --name variableName}} placeholder syntax.
        /// </summary>
        public string VariableName { get; }
    }
}
