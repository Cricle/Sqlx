// -----------------------------------------------------------------------
// <copyright file="IntegrationTestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 集成测试基类
/// 提供通用的测试初始化和清理逻辑
/// </summary>
public abstract class IntegrationTestBase
{
    protected static DatabaseFixture _fixture = null!;
    protected bool _needsSeedData = false;  // 子类可以设置是否需要预置数据

    [TestInitialize]
    public virtual void TestInitialize()
    {
        // 确保 fixture 已初始化
        if (_fixture == null)
        {
            _fixture = new DatabaseFixture();
        }
        
        // 每个测试前清理数据
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        
        // 如果需要，插入测试数据
        if (_needsSeedData)
        {
            _fixture.SeedTestData(SqlDefineTypes.SQLite);
        }
    }
}
