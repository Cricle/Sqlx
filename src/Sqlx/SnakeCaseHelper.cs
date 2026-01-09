// -----------------------------------------------------------------------
// <copyright file="SnakeCaseHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;

namespace Sqlx
{
    /// <summary>
    /// Helper class for converting property names to snake_case column names.
    /// Used by generated code for AOT-compatible SET clause generation.
    /// </summary>
    public static class SnakeCaseHelper
    {
        /// <summary>
        /// Converts PascalCase/camelCase to snake_case for database column names.
        /// </summary>
        /// <param name="name">The property name to convert.</param>
        /// <returns>The snake_case column name.</returns>
        /// <example>
        /// SnakeCaseHelper.ToSnakeCase("FirstName") returns "first_name"
        /// SnakeCaseHelper.ToSnakeCase("ID") returns "id"
        /// SnakeCaseHelper.ToSnakeCase("createdAt") returns "created_at"
        /// </example>
        public static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Fast path for already lowercase names
            var hasUpper = false;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    hasUpper = true;
                    break;
                }
            }
            if (!hasUpper) return name;

            // Convert PascalCase/camelCase to snake_case
            var result = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) result.Append('_');
                    result.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }
    }
}
