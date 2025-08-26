// -----------------------------------------------------------------------
// <copyright file="GenerateContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;

    internal abstract record GenerateContext(MethodGenerationContext Context, string TableName)
    {
        public static string GetColumnName(string name)
            => Regex.Replace(name, "[A-Z]", x => $"_{x.Value.ToLower()}").TrimStart('_');

        protected static string GetParamterName(string prefx, string name) => $"{prefx}{GetColumnName(name)}";

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
}
