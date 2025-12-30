// -----------------------------------------------------------------------
// <copyright file="TDD_Phase1_SoftDelete_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.SoftDelete;

/// <summary>
/// TDD Phase 1: Red Tests for SoftDelete feature.
/// Goal: Auto-filter deleted records in SELECT, convert DELETE to UPDATE.
/// </summary>
[TestClass]
public class TDD_Phase1_SoftDelete_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// SELECT without WHERE should add "WHERE is_deleted = false"
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("SoftDelete")]
    public void SoftDelete_SELECT_Without_WHERE_Should_Add_Filter()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[SoftDelete]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public bool IsDeleted { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // 应该在生成的SQL中添加 WHERE is_deleted = false
        Assert.IsTrue(
            generatedCode.Contains("is_deleted = false") ||
            generatedCode.Contains("is_deleted = 0") ||
            generatedCode.Contains("IsDeleted = false"),
            "应该自动添加软删除过滤条件");
    }

    /// <summary>
    /// SELECT with existing WHERE should add "AND is_deleted = false"
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("SoftDelete")]
    public void SoftDelete_SELECT_With_WHERE_Should_Add_AND_Filter()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[SoftDelete]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public int Age { get; set; }
    public bool IsDeleted { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE age > @age"")]
    Task<List<User>> GetActiveUsersAsync(int age);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // 应该同时包含原有条件和软删除过滤
        StringAssert.Contains(generatedCode, "age > @age", "应该保留原有WHERE条件");
        Assert.IsTrue(
            generatedCode.Contains("is_deleted") || generatedCode.Contains("IsDeleted"),
            "应该添加软删除过滤条件");
        Assert.IsTrue(
            generatedCode.Contains("AND") || generatedCode.Contains("and"),
            "应该使用AND连接多个条件");
    }

    /// <summary>
    /// [IncludeDeleted] should bypass soft delete filter
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("SoftDelete")]
    public void IncludeDeleted_Should_Not_Filter()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[SoftDelete]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public bool IsDeleted { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    [IncludeDeleted]
    Task<List<User>> GetAllIncludingDeletedAsync();
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // CommandText应该不包含is_deleted过滤（因为有[IncludeDeleted]）
        // 但实体类定义中会有IsDeleted属性，所以我们检查SQL部分
        var commandTextIndex = generatedCode.IndexOf("CommandText =");
        if (commandTextIndex > 0)
        {
            var sqlPart = generatedCode.Substring(commandTextIndex,
                Math.Min(200, generatedCode.Length - commandTextIndex));

            Assert.IsFalse(
                sqlPart.Contains("is_deleted = false") || sqlPart.Contains("is_deleted = 0"),
                "[IncludeDeleted]应该绕过软删除过滤");
        }
    }

    /// <summary>
    /// DELETE should convert to UPDATE with is_deleted = true
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("SoftDelete")]
    public void SoftDelete_DELETE_Should_Convert_To_UPDATE()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[SoftDelete]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public bool IsDeleted { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 添加此方法以便推断实体类型

    [SqlTemplate(""DELETE FROM {{table}} WHERE id = @id"")]
    Task<int> DeleteAsync(long id);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // DELETE应该转换为UPDATE - 查找DeleteAsync方法的CommandText
        var deleteMethodIndex = generatedCode.IndexOf("public async System.Threading.Tasks.Task<int> DeleteAsync");
        Assert.IsTrue(deleteMethodIndex > 0, "应该找到DeleteAsync方法");

        // 从DeleteAsync方法开始往后查找CommandText
        var commandTextIndex = generatedCode.IndexOf("CommandText =", deleteMethodIndex);
        if (commandTextIndex > 0)
        {
            var sqlPart = generatedCode.Substring(commandTextIndex,
                Math.Min(300, generatedCode.Length - commandTextIndex));

            Assert.IsTrue(
                sqlPart.Contains("UPDATE") || sqlPart.Contains("update"),
                "DELETE应该转换为UPDATE");
            Assert.IsTrue(
                sqlPart.Contains("is_deleted = true") ||
                sqlPart.Contains("is_deleted = 1") ||
                sqlPart.Contains("SET") || sqlPart.Contains("set"),
                "应该设置is_deleted标志");
            Assert.IsFalse(
                sqlPart.Contains("DELETE FROM"),
                "不应该包含DELETE FROM语句");
        }
    }

    /// <summary>
    /// DELETE with TimestampColumn should set deleted_at
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("SoftDelete")]
    public void SoftDelete_DELETE_With_Timestamp_Should_Set_DeletedAt()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[SoftDelete(FlagColumn = ""IsDeleted"", TimestampColumn = ""DeletedAt"")]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 添加此方法以便推断实体类型

    [SqlTemplate(""DELETE FROM {{table}} WHERE id = @id"")]
    Task<int> DeleteAsync(long id);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert - 查找DeleteAsync方法的CommandText
        var deleteMethodIndex = generatedCode.IndexOf("public async System.Threading.Tasks.Task<int> DeleteAsync");
        Assert.IsTrue(deleteMethodIndex > 0, "应该找到DeleteAsync方法");

        var commandTextIndex = generatedCode.IndexOf("CommandText =", deleteMethodIndex);
        if (commandTextIndex > 0)
        {
            var sqlPart = generatedCode.Substring(commandTextIndex,
                Math.Min(400, generatedCode.Length - commandTextIndex));

            // 应该设置is_deleted和deleted_at
            Assert.IsTrue(
                sqlPart.Contains("is_deleted") || sqlPart.Contains("IsDeleted"),
                "应该设置is_deleted");
            Assert.IsTrue(
                sqlPart.Contains("deleted_at") || sqlPart.Contains("DeletedAt"),
                "应该设置deleted_at时间戳");
            // PostgreSQL使用NOW()
            Assert.IsTrue(
                sqlPart.Contains("NOW()") || sqlPart.Contains("CURRENT_TIMESTAMP") ||
                sqlPart.Contains("@") || // 参数化
                sqlPart.Contains("GetDate") || sqlPart.Contains("datetime"), // 其他数据库
                "应该使用数据库时间函数或参数");
        }
    }
}

