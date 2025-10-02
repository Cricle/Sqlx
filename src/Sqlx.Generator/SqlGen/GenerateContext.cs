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

    internal abstract class GenerateContext
    {
        // 性能优化：预编译正则表达式
        private static readonly Regex CapitalLetterRegex = new("[A-Z]", RegexOptions.Compiled);

        protected GenerateContext(MethodGenerationContext context, string tableName)
        {
            Context = context;
            TableName = tableName;
        }

        public MethodGenerationContext Context { get; }
        public string TableName { get; }
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
            return CapitalLetterRegex.Replace(name, x => $"_{x.Value.ToLower()}").TrimStart('_');
        }

        internal static string GetParamterName(string prefx, string name) => $"{prefx}{GetColumnName(name)}";

        protected static string GetColumnName(IPropertySymbol symbol)
        {
            var colAttr = symbol.GetDbColumnAttribute();  // 使用扩展方法简化代码
            return colAttr == null || colAttr.ConstructorArguments.Length != 1
                ? GetColumnName(symbol.Name)
                : colAttr.ConstructorArguments[0].Value!.ToString();
        }
    }

    internal sealed class InsertGenerateContext : GenerateContext
    {
        public InsertGenerateContext(MethodGenerationContext context, string tableName, ObjectMap entry) : base(context, tableName)
        {
            Entry = entry;
        }

        public ObjectMap Entry { get; }

        public string GetParamterNames(string prefx) => string.Join(", ", Entry.Properties.Select(x => GetParamterName(prefx, x.Name)));

        public string GetColumnNames() => string.Join(", ", Entry.Properties.Select(GetColumnName));
    }

    internal sealed class SelectGenerateContext : GenerateContext
    {
        public SelectGenerateContext(MethodGenerationContext context, string tableName, ObjectMap entry) : base(context, tableName)
        {
            Entry = entry;
        }

        public ObjectMap Entry { get; }

        public string GetColumnNames() => string.Join(", ", Entry.Properties.Select(property =>
            $"{GetColumnName(property)} AS {property.Name}"));
    }

    internal sealed class UpdateGenerateContext : GenerateContext
    {
        public UpdateGenerateContext(MethodGenerationContext context, string tableName, ObjectMap entry) : base(context, tableName)
        {
            Entry = entry;
        }

        public ObjectMap Entry { get; }

        public string GetUpdateSet(string prefx) => string.Join(", ", Entry.Properties.Select(property =>
            $"{GetColumnName(property)} = {GetParamterName(prefx, property.Name)}"));
    }

    internal sealed class DeleteGenerateContext : GenerateContext
    {
        public DeleteGenerateContext(MethodGenerationContext context, string tableName, ObjectMap entry) : base(context, tableName)
        {
            Entry = entry;
        }

        public ObjectMap Entry { get; }

        // DELETE operations typically don't need specific context, just table name
    }
}
