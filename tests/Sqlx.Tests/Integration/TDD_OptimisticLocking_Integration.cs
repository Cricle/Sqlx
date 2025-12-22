// -----------------------------------------------------------------------
// <copyright file="TDD_OptimisticLocking_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using FullFeatureDemo;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 集成测试: 乐观锁
/// 测试版本号控制的并发更新
/// </summary>
[TestClass]
public class TDD_OptimisticLocking_Integration
{
    private static DatabaseFixture _fixture = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _fixture = new DatabaseFixture();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("OptimisticLocking")]
    public async Task OptimisticLocking_Insert_InitializesVersionToZero()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var accountRepo = new AccountRepository(connection);

        // Act - 插入新账户，version 应该初始化为 0
        var accountId = await accountRepo.InsertAsync("ACC001", 1000m);
        var account = await accountRepo.GetByIdAsync(accountId);

        // Assert
        Assert.IsNotNull(account);
        Assert.AreEqual("ACC001", account.AccountNo);
        Assert.AreEqual(1000m, account.Balance);
        Assert.AreEqual(0L, account.Version, "新插入的账户 version 应该为 0");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("OptimisticLocking")]
    public async Task OptimisticLocking_Update_IncrementsVersion()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var accountRepo = new AccountRepository(connection);
        
        var accountId = await accountRepo.InsertAsync("ACC002", 1000m);
        var account = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(account);
        Assert.AreEqual(0L, account.Version);

        // Act - 更新余额，version 应该递增
        var updateResult = await accountRepo.UpdateBalanceAsync(accountId, 1500m, account.Version);
        var updatedAccount = await accountRepo.GetByIdAsync(accountId);

        // Assert
        Assert.AreEqual(1, updateResult, "应该更新1条记录");
        Assert.IsNotNull(updatedAccount);
        Assert.AreEqual(1500m, updatedAccount.Balance);
        Assert.AreEqual(1L, updatedAccount.Version, "更新后 version 应该递增到 1");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("OptimisticLocking")]
    public async Task OptimisticLocking_Update_WithWrongVersion_Fails()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var accountRepo = new AccountRepository(connection);
        
        var accountId = await accountRepo.InsertAsync("ACC003", 1000m);
        var account = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(account);

        // Act - 使用错误的 version 更新
        var updateResult = await accountRepo.UpdateBalanceAsync(accountId, 1500m, 999L);

        // Assert
        Assert.AreEqual(0, updateResult, "使用错误的 version 应该更新失败（0条记录）");
        
        // 验证余额没有被修改
        var unchangedAccount = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(unchangedAccount);
        Assert.AreEqual(1000m, unchangedAccount.Balance, "余额不应该被修改");
        Assert.AreEqual(0L, unchangedAccount.Version, "version 不应该被修改");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("OptimisticLocking")]
    public async Task OptimisticLocking_ConcurrentUpdate_Simulation()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var accountRepo = new AccountRepository(connection);
        
        var accountId = await accountRepo.InsertAsync("ACC004", 1000m);
        
        // 模拟两个并发事务读取同一账户
        var account1 = await accountRepo.GetByIdAsync(accountId);
        var account2 = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(account1);
        Assert.IsNotNull(account2);
        Assert.AreEqual(account1.Version, account2.Version);

        // Act - 第一个事务更新成功
        var update1Result = await accountRepo.UpdateBalanceAsync(accountId, 1200m, account1.Version);
        
        // 第二个事务使用旧的 version 更新失败
        var update2Result = await accountRepo.UpdateBalanceAsync(accountId, 1300m, account2.Version);

        // Assert
        Assert.AreEqual(1, update1Result, "第一个更新应该成功");
        Assert.AreEqual(0, update2Result, "第二个更新应该失败（version 已过期）");
        
        // 验证最终状态
        var finalAccount = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(finalAccount);
        Assert.AreEqual(1200m, finalAccount.Balance, "余额应该是第一个事务的值");
        Assert.AreEqual(1L, finalAccount.Version, "version 应该递增到 1");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("OptimisticLocking")]
    public async Task OptimisticLocking_MultipleUpdates_VersionIncrementsCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var accountRepo = new AccountRepository(connection);
        
        var accountId = await accountRepo.InsertAsync("ACC005", 1000m);

        // Act - 执行多次更新
        var account = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(account);
        
        await accountRepo.UpdateBalanceAsync(accountId, 1100m, account.Version);
        account = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(account);
        
        await accountRepo.UpdateBalanceAsync(accountId, 1200m, account.Version);
        account = await accountRepo.GetByIdAsync(accountId);
        Assert.IsNotNull(account);
        
        await accountRepo.UpdateBalanceAsync(accountId, 1300m, account.Version);
        var finalAccount = await accountRepo.GetByIdAsync(accountId);

        // Assert
        Assert.IsNotNull(finalAccount);
        Assert.AreEqual(1300m, finalAccount.Balance);
        Assert.AreEqual(3L, finalAccount.Version, "经过3次更新，version 应该为 3");
    }
}
