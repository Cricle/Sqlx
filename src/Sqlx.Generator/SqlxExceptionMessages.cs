// -----------------------------------------------------------------------
// <copyright file="SqlxExceptionMessages.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator;

/// <summary>
/// 统一的异常消息常量，减少重复代码
/// </summary>
internal static class SqlxExceptionMessages
{
    // ArgumentNullException 消息
    internal const string CollectionParameterNull = "Collection parameter cannot be null for batch INSERT";
    internal const string DbConnectionNull = "Database connection cannot be null";

    // InvalidOperationException 消息
    internal const string DeleteRequiresWhere = "DELETE operation requires WHERE clause for safety. Use Delete(predicate) or call Where() before Delete().";
    internal const string EmptyCollection = "Cannot perform batch INSERT with empty collection";
    internal const string NoSetProperties = "No properties marked with [Set] attribute or eligible for SET clause in batch update";
    internal const string NoWherePropertiesUpdate = "No properties marked with [Where] attribute or eligible for WHERE clause in batch update";
    internal const string NoWherePropertiesDelete = "No properties marked with [Where] attribute or eligible for WHERE clause in batch delete";
    internal const string SequenceEmpty = "Sequence contains no elements";
    internal const string InvalidDepthLevel = "Cannot pop at depthlevel 0";

    // ArgumentException 消息
    internal const string LegacyBatchSql = "Legacy BATCH SQL syntax detected. Please use Constants.SqlExecuteTypeValues.BatchCommand for proper batch operations.";
    internal const string BatchCommandRequiresCollection = "BatchCommand requires a collection parameter";

    /// <summary>生成ArgumentNullException代码字符串</summary>
    internal static string GenerateArgumentNullCheck(string paramName, string message) =>
        $"throw new global::System.ArgumentNullException(nameof({paramName}), \"{message}\");";

    /// <summary>生成InvalidOperationException代码字符串</summary>
    internal static string GenerateInvalidOperationThrow(string message) =>
        $"throw new global::System.InvalidOperationException(\"{message}\");";

    /// <summary>生成ArgumentException代码字符串</summary>
    internal static string GenerateArgumentExceptionThrow(string message) =>
        $"throw new global::System.ArgumentException(\"{message}\");";
}

