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

    internal abstract record GenerateContext(MethodGenerationContext Context)
    {
        public static string GetColumnName(string name)
        {
            return Regex.Replace(name, "[A-Z]", x => $"{x.Value.ToLower()}_").TrimEnd('_');
        }
    }

    internal sealed record InsertGenerateContext(MethodGenerationContext Context, string TableName, IParameterSymbol VisitorSymbol, ObjectMap Entry) : GenerateContext(Context)
    {
        private string GetParamterName(string prefx, string name) => $"{prefx}p{name}";

        public string GetParamterNames(string prefx) => string.Join(", ", Entry.Properties.Select(x => GetParamterName(prefx, x.Name)));

        public string GetColumnNames() => string.Join(", ", Entry.Properties.Select(x =>
        {
            var colAttr = x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbColumnAttribute" && x.ConstructorArguments.Length == 1);
            if (colAttr == null)
            {
                return GetColumnName(x.Name);
            }

            return colAttr.ConstructorArguments[0].Value!.ToString();
        }));
    }
}
