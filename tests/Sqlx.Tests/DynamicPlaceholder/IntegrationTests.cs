using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Validation;
using System;

namespace Sqlx.Tests.DynamicPlaceholder;

/// <summary>
/// 集成测试：验证动态占位符完整工作流程
/// </summary>
[TestClass]
public class IntegrationTests
{
    #region 模拟生成的验证代码

    /// <summary>
    /// 模拟生成器生成的验证代码（Identifier 类型）
    /// </summary>
    private void ValidateDynamicParameter_Identifier(string tableName)
    {
        // 这是 CodeGenerationService 生成的验证代码的精确副本
        if (!SqlValidator.IsValidIdentifier(tableName.AsSpan()))
        {
            throw new ArgumentException($"Invalid identifier: {tableName}. Only letters, digits, and underscores are allowed.", nameof(tableName));
        }
    }

    /// <summary>
    /// 模拟生成器生成的验证代码（Fragment 类型）
    /// </summary>
    private void ValidateDynamicParameter_Fragment(string whereClause)
    {
        if (!SqlValidator.IsValidFragment(whereClause.AsSpan()))
        {
            throw new ArgumentException($"Invalid SQL fragment: {whereClause}. Contains dangerous keywords or operations.", nameof(whereClause));
        }
    }

    /// <summary>
    /// 模拟生成器生成的验证代码（TablePart 类型）
    /// </summary>
    private void ValidateDynamicParameter_TablePart(string suffix)
    {
        if (!SqlValidator.IsValidTablePart(suffix.AsSpan()))
        {
            throw new ArgumentException($"Invalid table part: {suffix}. Only letters and digits are allowed.", nameof(suffix));
        }
    }

    #endregion

    #region Identifier 验证测试

    [TestMethod]
    public void GeneratedValidation_Identifier_ValidInput_ShouldNotThrow()
    {
        // Arrange
        var validInputs = new[] { "users", "tenant1_users", "_table", "Table123" };

        // Act & Assert - 不应抛出异常
        foreach (var input in validInputs)
        {
            ValidateDynamicParameter_Identifier(input);
        }
    }

    [TestMethod]
    public void GeneratedValidation_Identifier_SqlInjection_ShouldThrow()
    {
        // Arrange
        var maliciousInput = "users'; DROP TABLE users--";

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => ValidateDynamicParameter_Identifier(maliciousInput));

        Assert.IsTrue(exception.Message.Contains("Invalid identifier"));
        Assert.AreEqual("tableName", exception.ParamName);
    }

    [TestMethod]
    public void GeneratedValidation_Identifier_SpecialCharacters_ShouldThrow()
    {
        // Arrange
        var invalidInputs = new[] { "users@table", "user.table", "user-table", "user table" };

        // Act & Assert
        foreach (var input in invalidInputs)
        {
            var exception = Assert.ThrowsException<ArgumentException>(
                () => ValidateDynamicParameter_Identifier(input),
                $"Should reject invalid input: {input}");

            Assert.IsTrue(exception.Message.Contains("Invalid identifier"));
        }
    }

    [TestMethod]
    public void GeneratedValidation_Identifier_EmptyString_ShouldThrow()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => ValidateDynamicParameter_Identifier(""));

        Assert.IsTrue(exception.Message.Contains("Invalid identifier"));
    }

    [TestMethod]
    public void GeneratedValidation_Identifier_TooLong_ShouldThrow()
    {
        // Arrange
        var tooLong = new string('a', 129);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => ValidateDynamicParameter_Identifier(tooLong));

        Assert.IsTrue(exception.Message.Contains("Invalid identifier"));
    }

    #endregion

    #region Fragment 验证测试

    [TestMethod]
    public void GeneratedValidation_Fragment_ValidInput_ShouldNotThrow()
    {
        // Arrange
        var validInputs = new[]
        {
            "age > 18",
            "status = 'active'",
            "age > 18 AND status = 'active'",
            "name LIKE '%John%'"
        };

        // Act & Assert
        foreach (var input in validInputs)
        {
            ValidateDynamicParameter_Fragment(input);
        }
    }

    [TestMethod]
    public void GeneratedValidation_Fragment_DangerousKeywords_ShouldThrow()
    {
        // Arrange
        var dangerousInputs = new[]
        {
            "age > 18; DROP TABLE users",
            "age > 18--",
            "age > 18/* comment */",
            "TRUNCATE TABLE users"
        };

        // Act & Assert
        foreach (var input in dangerousInputs)
        {
            var exception = Assert.ThrowsException<ArgumentException>(
                () => ValidateDynamicParameter_Fragment(input),
                $"Should reject dangerous input: {input}");

            Assert.IsTrue(exception.Message.Contains("Invalid SQL fragment"));
        }
    }

    #endregion

    #region TablePart 验证测试

    [TestMethod]
    public void GeneratedValidation_TablePart_ValidInput_ShouldNotThrow()
    {
        // Arrange
        var validInputs = new[] { "202410", "tenant1", "shard001", "2024", "abc123" };

        // Act & Assert
        foreach (var input in validInputs)
        {
            ValidateDynamicParameter_TablePart(input);
        }
    }

    [TestMethod]
    public void GeneratedValidation_TablePart_InvalidCharacters_ShouldThrow()
    {
        // Arrange
        var invalidInputs = new[] { "2024_10", "tenant-1", "shard.001", "table@1" };

        // Act & Assert
        foreach (var input in invalidInputs)
        {
            var exception = Assert.ThrowsException<ArgumentException>(
                () => ValidateDynamicParameter_TablePart(input),
                $"Should reject invalid input: {input}");

            Assert.IsTrue(exception.Message.Contains("Invalid table part"));
        }
    }

    #endregion

    #region 性能测试

    [TestMethod]
    public void GeneratedValidation_Performance_ShouldBeFast()
    {
        // Arrange
        var tableName = "users";
        var iterations = 10000;

        // Act - 预热
        ValidateDynamicParameter_Identifier(tableName);

        var startTime = DateTime.UtcNow;
        for (int i = 0; i < iterations; i++)
        {
            ValidateDynamicParameter_Identifier(tableName);
        }
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - 10000次验证应该在合理时间内完成
        var averageMicroseconds = elapsed.TotalMilliseconds * 1000 / iterations;
        Assert.IsTrue(averageMicroseconds < 10, 
            $"Average validation time ({averageMicroseconds:F2}μs) should be less than 10μs per call");
    }

    [TestMethod]
    public void GeneratedValidation_NoAllocation_ShouldBeZeroGC()
    {
        // Arrange
        var tableName = "users";
        var iterations = 1000;

        // Act - 预热
        ValidateDynamicParameter_Identifier(tableName);

        // 强制GC并记录初始值
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        // 执行验证
        for (int i = 0; i < iterations; i++)
        {
            ValidateDynamicParameter_Identifier(tableName);
        }

        // 记录结束值
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);

        // Assert - 应该没有触发任何GC
        Assert.AreEqual(gen0Before, gen0After, "Should not trigger Gen0 GC");
        Assert.AreEqual(gen1Before, gen1After, "Should not trigger Gen1 GC");
        Assert.AreEqual(gen2Before, gen2After, "Should not trigger Gen2 GC");
    }

    #endregion

    #region 错误消息测试

    [TestMethod]
    public void GeneratedValidation_ErrorMessage_ShouldBeClear()
    {
        // Arrange
        var invalidInput = "users'; DROP TABLE";

        // Act
        var exception = Assert.ThrowsException<ArgumentException>(
            () => ValidateDynamicParameter_Identifier(invalidInput));

        // Assert - 错误消息应该清晰明了
        Assert.IsTrue(exception.Message.Contains("Invalid identifier"));
        Assert.IsTrue(exception.Message.Contains(invalidInput));
        Assert.IsTrue(exception.Message.Contains("Only letters, digits, and underscores are allowed"));
        Assert.AreEqual("tableName", exception.ParamName);
    }

    #endregion
}

