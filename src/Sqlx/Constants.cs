// -----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

/// <summary>
/// 定义 Sqlx 框架中使用的常量
/// </summary>
internal static class Constants
{
    /// <summary>
    /// SQL 操作类型枚举值
    /// </summary>
    public static class SqlExecuteTypeValues
    {
        public const int Select = 0;
        public const int Update = 1;
        public const int Insert = 2;
        public const int Delete = 3;
        public const int BatchInsert = 4;
        public const int BatchUpdate = 5;
        public const int BatchDelete = 6;
        public const int BatchCommand = 7;
    }

    /// <summary>
    /// 生成的代码中的变量名
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
}
