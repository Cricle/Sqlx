// -----------------------------------------------------------------------
// <copyright file="GenerateContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen
{
    using Microsoft.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;

    internal abstract record GenerateContext(MethodGenerationContext Context, string TableName)
    {
        public static string GetColumnName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Handle different patterns:
            // "UPPER_CASE" -> "upper_case" (already has underscores)
            // "PascalCase" -> "pascal_case" (PascalCase)
            // "camelCase" -> "camel_case" (camelCase)

            // If already contains underscores and is all caps, just convert to lowercase
            if (name.Contains("_") && name.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)))
            {
                return name.ToLower();
            }

            // Otherwise use the existing camelCase/PascalCase conversion
            return Regex.Replace(name, "[A-Z]", x => $"_{x.Value.ToLower()}").TrimStart('_');
        }

        internal static string GetParamterName(string prefx, string name) => $"{prefx}{GetColumnName(name)}";

        protected static string GetColumnName(IPropertySymbol symbol)
        {
            var colAttr = symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbColumnAttribute" && x.ConstructorArguments.Length == 1);
            return colAttr == null ? GetColumnName(symbol.Name) : colAttr.ConstructorArguments[0].Value!.ToString();
        }
    }

    internal sealed record InsertGenerateContext(MethodGenerationContext Context, string TableName, ObjectMap Entry) : GenerateContext(Context, TableName)
    {
        public string GetParamterNames(string prefx) => string.Join(", ", Entry.Properties.Select(x => GetParamterName(prefx, x.Name)));

        public string GetColumnNames() => string.Join(", ", Entry.Properties.Select(GetColumnName));
    }

    internal sealed record SelectGenerateContext(MethodGenerationContext Context, string TableName, ObjectMap Entry) : GenerateContext(Context, TableName)
    {
        public string GetColumnNames() => string.Join(", ", Entry.Properties.Select(property =>
            $"{GetColumnName(property)} AS {property.Name}"));
    }

    internal sealed record UpdateGenerateContext(MethodGenerationContext Context, string TableName, ObjectMap Entry) : GenerateContext(Context, TableName)
    {
        public string GetUpdateSet(string prefx) => string.Join(", ", Entry.Properties.Select(property =>
            $"{GetColumnName(property)} = {GetParamterName(prefx, property.Name)}"));
    }

    internal sealed record DeleteGenerateContext(MethodGenerationContext Context, string TableName, ObjectMap Entry) : GenerateContext(Context, TableName)
    {
        // DELETE operations typically don't need specific context, just table name
    }
}
