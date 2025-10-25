// -----------------------------------------------------------------------
// <copyright file="TDD_Phase2_ReturnEntity_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.InsertReturning;

/// <summary>
/// TDD Phase 2: Red Tests for [ReturnInsertedEntity] attribute.
/// These tests define the expected behavior for returning the complete inserted entity.
/// </summary>
[TestClass]
public class TDD_Phase2_ReturnInsertedEntity_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// PostgreSQL should generate RETURNING * clause to return all columns.
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Insert-ReturnEntity")]
    [TestCategory("PostgreSQL")]
    public void PostgreSQL_InsertAndGetEntity_Should_Generate_RETURNING_Star()
    {
        // Arrange
        var source = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public string Email { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
    [ReturnInsertedEntity]
    Task<User> InsertAndGetEntityAsync(User entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // PostgreSQL应该生成RETURNING *子句
        StringAssert.Contains(generatedCode, "RETURNING *", "应该包含RETURNING *子句返回所有列");

        // 应该使用DataReader读取完整实体
        Assert.IsTrue(
            generatedCode.Contains("ExecuteReader") || generatedCode.Contains("ExecuteReaderAsync"),
            "应该使用ExecuteReader读取返回的实体");

        // 应该返回Task<User> (可能带命名空间前缀)
        Assert.IsTrue(
            generatedCode.Contains("Task<User>") || generatedCode.Contains("Task<Test.User>"),
            "方法签名应该返回Task<User> or Task<Test.User>");

        // 应该有实体映射代码（填充属性）
        // Note: Id可能不被映射，因为它在INSERT中被排除了
        StringAssert.Contains(generatedCode, "Name = reader.Get", "应该映射Name属性");
        StringAssert.Contains(generatedCode, "new Test.User", "应该创建新的User实例");
    }

    /// <summary>
    /// SQL Server should generate OUTPUT INSERTED.* clause.
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Insert-ReturnEntity")]
    [TestCategory("SqlServer")]
    public void SqlServer_InsertAndGetEntity_Should_Generate_OUTPUT_INSERTED_Star()
    {
        // Arrange
        var source = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
    [ReturnInsertedEntity]
    Task<User> InsertAndGetEntityAsync(User entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // SQL Server应该生成OUTPUT INSERTED.*
        StringAssert.Contains(generatedCode, "OUTPUT INSERTED.*", "应该包含OUTPUT INSERTED.*子句");

        // 应该使用ExecuteReader
        Assert.IsTrue(
            generatedCode.Contains("ExecuteReader"),
            "应该使用ExecuteReader");

        // 应该返回User实体 (可能带命名空间前缀)
        Assert.IsTrue(
            generatedCode.Contains("Task<User>") || generatedCode.Contains("Task<Test.User>"),
            "应该返回Task<User> or Task<Test.User>");
    }

    /// <summary>
    /// Should handle nullable columns correctly.
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Insert-ReturnEntity")]
    [TestCategory("Nullable")]
    public void ReturnInsertedEntity_Should_Handle_Nullable_Columns()
    {
        // Arrange
        var source = @"
using System;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public string? Email { get; set; }  // Nullable
    public DateTime? CreatedAt { get; set; }  // Nullable DateTime
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
    [ReturnInsertedEntity]
    Task<User> InsertAndGetEntityAsync(User entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // 应该包含RETURNING *
        StringAssert.Contains(generatedCode, "RETURNING *", "应该生成RETURNING *");

        // 应该检查DBNull for nullable columns
        Assert.IsTrue(
            generatedCode.Contains("DBNull") || generatedCode.Contains("IsDBNull"),
            "应该处理nullable列的DBNull检查");
    }

    /// <summary>
    /// Should be AOT-friendly with no reflection.
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Insert-ReturnEntity")]
    [TestCategory("AOT")]
    public void ReturnInsertedEntity_Should_Be_AOT_Friendly()
    {
        // Arrange
        var source = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[TableName(""users"")]
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
    [ReturnInsertedEntity]
    Task<User> InsertAndGetEntityAsync(User entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert - 应该不包含反射相关的代码（AOT友好）
        Assert.IsFalse(generatedCode.Contains("GetType()"), "不应使用GetType()");
        Assert.IsFalse(generatedCode.Contains("typeof(User).GetProperties"), "不应使用反射获取属性");
        Assert.IsFalse(generatedCode.Contains("Activator.CreateInstance"), "不应使用Activator");
        Assert.IsFalse(generatedCode.Contains("PropertyInfo"), "不应使用PropertyInfo");

        // 应该直接访问属性（编译时生成）
        // Note: Id可能不被映射，因为它在INSERT中被排除了
        StringAssert.Contains(generatedCode, "Name = reader.Get", "应该直接赋值Name属性");
        StringAssert.Contains(generatedCode, "new Test.User", "应该直接创建User实例（无反射）");
    }
}

