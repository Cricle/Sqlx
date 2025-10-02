// -----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

/// <summary>
/// Define constants used in the Sqlx framework
/// </summary>
internal static class Constants
{
    /// <summary>
    /// Variable names in generated code
    /// </summary>
    public static class GeneratedVariables
    {
        public const string Connection = "__conn__";
        public const string Command = "__cmd__";
        public const string Reader = "__reader__";
        public const string Result = "__result__";
        public const string Data = "__data__";
        public const string StartTime = "__startTime__";
        public const string Exception = "__exception__";
        public const string Elapsed = "__elapsed__";
    }

    /// <summary>
    /// Type name constants
    /// </summary>
    public static class TypeNames
    {
        public const string IAsyncEnumerable = "IAsyncEnumerable";
    }
}
