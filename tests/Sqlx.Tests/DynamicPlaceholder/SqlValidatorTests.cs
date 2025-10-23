using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Validation;
using System;

namespace Sqlx.Tests.DynamicPlaceholder;

/// <summary>
/// 测试 SqlValidator 的所有验证方法
/// </summary>
[TestClass]
public class SqlValidatorTests
{
    #region IsValidIdentifier Tests

    [TestMethod]
    public void IsValidIdentifier_ValidIdentifier_ReturnsTrue()
    {
        // Arrange
        var validIdentifiers = new[]
        {
            "users",
            "UserTable",
            "user_table",
            "_users",
            "users123",
            "table1",
            "a"
        };

        // Act & Assert
        foreach (var identifier in validIdentifiers)
        {
            var result = SqlValidator.IsValidIdentifier(identifier.AsSpan());
            Assert.IsTrue(result, $"Expected '{identifier}' to be valid");
        }
    }

    [TestMethod]
    public void IsValidIdentifier_InvalidIdentifier_ReturnsFalse()
    {
        // Arrange
        var invalidIdentifiers = new[]
        {
            "",                     // 空字符串
            "123users",             // 数字开头
            "user-table",           // 包含连字符
            "user table",           // 包含空格
            "user@table",           // 包含特殊字符
            "user.table",           // 包含点
            "user;table",           // 包含分号
            new string('a', 129)    // 超过128字符
        };

        // Act & Assert
        foreach (var identifier in invalidIdentifiers)
        {
            var result = SqlValidator.IsValidIdentifier(identifier.AsSpan());
            Assert.IsFalse(result, $"Expected '{identifier}' to be invalid");
        }
    }

    [TestMethod]
    public void IsValidIdentifier_EmptyString_ReturnsFalse()
    {
        // Act
        var result = SqlValidator.IsValidIdentifier("".AsSpan());

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidIdentifier_MaxLength_ReturnsTrue()
    {
        // Arrange
        var identifier = new string('a', 128);

        // Act
        var result = SqlValidator.IsValidIdentifier(identifier.AsSpan());

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidIdentifier_TooLong_ReturnsFalse()
    {
        // Arrange
        var identifier = new string('a', 129);

        // Act
        var result = SqlValidator.IsValidIdentifier(identifier.AsSpan());

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region ContainsDangerousKeyword Tests

    [TestMethod]
    public void ContainsDangerousKeyword_DangerousKeyword_ReturnsTrue()
    {
        // Arrange
        var dangerousInputs = new[]
        {
            "DROP TABLE users",
            "TRUNCATE TABLE users",
            "ALTER TABLE users",
            "EXEC sp_executesql",
            "SELECT * FROM users --",
            "SELECT * FROM users /*",
            "SELECT * FROM users; DROP TABLE users"
        };

        // Act & Assert
        foreach (var input in dangerousInputs)
        {
            var result = SqlValidator.ContainsDangerousKeyword(input.AsSpan());
            Assert.IsTrue(result, $"Expected '{input}' to contain dangerous keyword");
        }
    }

    [TestMethod]
    public void ContainsDangerousKeyword_SafeInput_ReturnsFalse()
    {
        // Arrange
        var safeInputs = new[]
        {
            "SELECT * FROM users",
            "age > 18",
            "name = 'John'",
            "status = 'active' AND age > 18",
            "ORDER BY created_at DESC"
        };

        // Act & Assert
        foreach (var input in safeInputs)
        {
            var result = SqlValidator.ContainsDangerousKeyword(input.AsSpan());
            Assert.IsFalse(result, $"Expected '{input}' to be safe");
        }
    }

    [TestMethod]
    public void ContainsDangerousKeyword_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var inputs = new[]
        {
            "drop table users",
            "DROP TABLE users",
            "DrOp TaBLe users"
        };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = SqlValidator.ContainsDangerousKeyword(input.AsSpan());
            Assert.IsTrue(result, $"Expected '{input}' to be dangerous (case insensitive)");
        }
    }

    #endregion

    #region IsValidFragment Tests

    [TestMethod]
    public void IsValidFragment_ValidFragment_ReturnsTrue()
    {
        // Arrange
        var validFragments = new[]
        {
            "age > 18",
            "status = 'active'",
            "age > 18 AND status = 'active'",
            "name LIKE '%John%'",
            "created_at >= '2024-01-01'",
            "id IN (1, 2, 3)"
        };

        // Act & Assert
        foreach (var fragment in validFragments)
        {
            var result = SqlValidator.IsValidFragment(fragment.AsSpan());
            Assert.IsTrue(result, $"Expected '{fragment}' to be valid");
        }
    }

    [TestMethod]
    public void IsValidFragment_InvalidFragment_ReturnsFalse()
    {
        // Arrange
        var invalidFragments = new[]
        {
            "",                                     // 空字符串
            "age > 18; DROP TABLE users",           // 包含分号
            "age > 18 --",                          // 包含注释
            "age > 18 /* comment */",               // 包含块注释
            "DROP TABLE users",                     // DDL 操作
            "TRUNCATE TABLE users",                 // DDL 操作
            "ALTER TABLE users ADD COLUMN name",    // DDL 操作
            new string('a', 4097)                   // 超过4096字符
        };

        // Act & Assert
        foreach (var fragment in invalidFragments)
        {
            var result = SqlValidator.IsValidFragment(fragment.AsSpan());
            Assert.IsFalse(result, $"Expected '{fragment}' to be invalid");
        }
    }

    [TestMethod]
    public void IsValidFragment_MaxLength_ReturnsTrue()
    {
        // Arrange
        var fragment = new string('a', 4096);

        // Act
        var result = SqlValidator.IsValidFragment(fragment.AsSpan());

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidFragment_TooLong_ReturnsFalse()
    {
        // Arrange
        var fragment = new string('a', 4097);

        // Act
        var result = SqlValidator.IsValidFragment(fragment.AsSpan());

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region IsValidTablePart Tests

    [TestMethod]
    public void IsValidTablePart_ValidTablePart_ReturnsTrue()
    {
        // Arrange
        var validParts = new[]
        {
            "202410",
            "tenant1",
            "shard001",
            "2024",
            "abc123"
        };

        // Act & Assert
        foreach (var part in validParts)
        {
            var result = SqlValidator.IsValidTablePart(part.AsSpan());
            Assert.IsTrue(result, $"Expected '{part}' to be valid");
        }
    }

    [TestMethod]
    public void IsValidTablePart_InvalidTablePart_ReturnsFalse()
    {
        // Arrange
        var invalidParts = new[]
        {
            "",                     // 空字符串
            "2024_10",              // 包含下划线
            "tenant-1",             // 包含连字符
            "shard 001",            // 包含空格
            "2024.10",              // 包含点
            "tenant@1",             // 包含特殊字符
            new string('a', 65)     // 超过64字符
        };

        // Act & Assert
        foreach (var part in invalidParts)
        {
            var result = SqlValidator.IsValidTablePart(part.AsSpan());
            Assert.IsFalse(result, $"Expected '{part}' to be invalid");
        }
    }

    [TestMethod]
    public void IsValidTablePart_MaxLength_ReturnsTrue()
    {
        // Arrange
        var part = new string('a', 64);

        // Act
        var result = SqlValidator.IsValidTablePart(part.AsSpan());

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidTablePart_TooLong_ReturnsFalse()
    {
        // Arrange
        var part = new string('a', 65);

        // Act
        var result = SqlValidator.IsValidTablePart(part.AsSpan());

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region Validate Tests (通用方法)

    [TestMethod]
    public void Validate_Identifier_Valid_ReturnsTrue()
    {
        // Act
        var result = SqlValidator.Validate("users".AsSpan(), DynamicSqlType.Identifier);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Validate_Identifier_Invalid_ReturnsFalse()
    {
        // Act
        var result = SqlValidator.Validate("DROP TABLE".AsSpan(), DynamicSqlType.Identifier);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Validate_Fragment_Valid_ReturnsTrue()
    {
        // Act
        var result = SqlValidator.Validate("age > 18".AsSpan(), DynamicSqlType.Fragment);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Validate_Fragment_Invalid_ReturnsFalse()
    {
        // Act
        var result = SqlValidator.Validate("age > 18; DROP TABLE users".AsSpan(), DynamicSqlType.Fragment);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Validate_TablePart_Valid_ReturnsTrue()
    {
        // Act
        var result = SqlValidator.Validate("202410".AsSpan(), DynamicSqlType.TablePart);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Validate_TablePart_Invalid_ReturnsFalse()
    {
        // Act
        var result = SqlValidator.Validate("2024_10".AsSpan(), DynamicSqlType.TablePart);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region SQL注入测试

    [TestMethod]
    public void IsValidIdentifier_SqlInjectionAttempt_ReturnsFalse()
    {
        // Arrange
        var maliciousInputs = new[]
        {
            "users'; DROP TABLE users--",
            "users' OR '1'='1",
            "users; EXEC sp_executesql",
            "users/**/",
            "users--"
        };

        // Act & Assert
        foreach (var input in maliciousInputs)
        {
            var result = SqlValidator.IsValidIdentifier(input.AsSpan());
            Assert.IsFalse(result, $"Expected SQL injection attempt '{input}' to be rejected");
        }
    }

    [TestMethod]
    public void IsValidFragment_SqlInjectionAttempt_ReturnsFalse()
    {
        // Arrange
        var maliciousInputs = new[]
        {
            "1=1; DROP TABLE users",
            "1=1--",
            "1=1/* comment */",
            "EXEC sp_executesql @sql"
        };

        // Act & Assert
        foreach (var input in maliciousInputs)
        {
            var result = SqlValidator.IsValidFragment(input.AsSpan());
            Assert.IsFalse(result, $"Expected SQL injection attempt '{input}' to be rejected");
        }
    }

    #endregion
}

