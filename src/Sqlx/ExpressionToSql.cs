// -----------------------------------------------------------------------
// <copyright file="ExpressionToSql.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx
{
    /// <summary>
    /// Placeholder values for dynamic SQL generation.
    /// </summary>
    public static class Any
    {
        /// <summary>Gets a placeholder value of the specified type.</summary>
        public static TValue Value<TValue>() => default!;

        /// <summary>Gets a placeholder value of the specified type with a parameter name.</summary>
        public static TValue Value<TValue>(string parameterName) => default!;

        /// <summary>Gets a string placeholder.</summary>
        public static string String() => default!;

        /// <summary>Gets a string placeholder with a parameter name.</summary>
        public static string String(string parameterName) => default!;

        /// <summary>Gets an integer placeholder.</summary>
        public static int Int() => default;

        /// <summary>Gets an integer placeholder with a parameter name.</summary>
        public static int Int(string parameterName) => default;

        /// <summary>Gets a boolean placeholder.</summary>
        public static bool Bool() => default;

        /// <summary>Gets a boolean placeholder with a parameter name.</summary>
        public static bool Bool(string parameterName) => default;

        /// <summary>Gets a DateTime placeholder.</summary>
        public static DateTime DateTime() => default;

        /// <summary>Gets a DateTime placeholder with a parameter name.</summary>
        public static DateTime DateTime(string parameterName) => default;

        /// <summary>Gets a Guid placeholder.</summary>
        public static Guid Guid() => default;

        /// <summary>Gets a Guid placeholder with a parameter name.</summary>
        public static Guid Guid(string parameterName) => default;
    }
}
