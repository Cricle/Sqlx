// -----------------------------------------------------------------------
// <copyright file="TestCategories.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

/// <summary>
/// 测试类别常量，用于 [TestCategory] 特性
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// SQLite数据库测试 - 使用内存数据库，本地和CI都运行
    /// </summary>
    public const string SQLite = "SQLite";

    /// <summary>
    /// PostgreSQL数据库测试 - 需要真实数据库连接，仅在CI运行
    /// </summary>
    public const string PostgreSQL = "PostgreSQL";

    /// <summary>
    /// MySQL数据库测试 - 需要真实数据库连接，仅在CI运行
    /// </summary>
    public const string MySQL = "MySQL";

    /// <summary>
    /// SQL Server数据库测试 - 需要真实数据库连接，仅在CI运行
    /// </summary>
    public const string SqlServer = "SqlServer";

    /// <summary>
    /// Oracle数据库测试 - 需要真实数据库连接，仅在CI运行
    /// </summary>
    public const string Oracle = "Oracle";

    /// <summary>
    /// 需要真实数据库连接的测试（不包括SQLite）
    /// </summary>
    public const string RequiresDatabase = "RequiresDatabase";

    /// <summary>
    /// 性能测试 - 默认跳过，需要手动运行
    /// </summary>
    public const string Performance = "Performance";

    /// <summary>
    /// 集成测试
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// 单元测试
    /// </summary>
    public const string Unit = "Unit";
}

