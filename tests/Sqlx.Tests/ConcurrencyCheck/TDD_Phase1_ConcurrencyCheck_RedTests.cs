// -----------------------------------------------------------------------
// <copyright file="TDD_Phase1_ConcurrencyCheck_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.ConcurrencyCheck;

/// <summary>
/// TDD Phase 1: Red Tests for ConcurrencyCheck feature.
/// Goal: Auto-add optimistic locking to UPDATE operations.
/// </summary>
[TestClass]
public class TDD_Phase1_ConcurrencyCheck_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// UPDATE should automatically increment version
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ConcurrencyCheck")]
    [TestCategory("UPDATE")]
    public void ConcurrencyCheck_UPDATE_Should_Increment_Version()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";

    [ConcurrencyCheck]
    public int Version { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<Product>> GetAllAsync();  // 帮助推断实体类型

    [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
    Task<int> UpdateAsync(Product entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var updateMethodIndex = generatedCode.IndexOf("public async System.Threading.Tasks.Task<int> UpdateAsync");
        Assert.IsTrue(updateMethodIndex > 0, "应该找到UpdateAsync方法");

        var commandTextIndex = generatedCode.IndexOf("CommandText =", updateMethodIndex);
        Assert.IsTrue(commandTextIndex > 0, "应该找到CommandText");

        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));

        // 应该包含version递增
        Assert.IsTrue(
            sqlPart.Contains("version = version + 1") || sqlPart.Contains("Version = Version + 1"),
            "应该包含version递增语句");
    }

    /// <summary>
    /// UPDATE should check version in WHERE clause
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ConcurrencyCheck")]
    [TestCategory("UPDATE")]
    public void ConcurrencyCheck_UPDATE_Should_Check_Version_In_WHERE()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";

    [ConcurrencyCheck]
    public int Version { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<Product>> GetAllAsync();  // 帮助推断实体类型

    [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
    Task<int> UpdateAsync(Product entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var updateMethodIndex = generatedCode.IndexOf("public async System.Threading.Tasks.Task<int> UpdateAsync");
        Assert.IsTrue(updateMethodIndex > 0, "应该找到UpdateAsync方法");

        var commandTextIndex = generatedCode.IndexOf("CommandText =", updateMethodIndex);
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));

        // 应该在WHERE子句检查version
        Assert.IsTrue(
            sqlPart.Contains("version = @") || sqlPart.Contains("Version = @"),
            "应该在WHERE子句检查version");

        // 应该包含AND（因为已有WHERE id = @id）
        Assert.IsTrue(
            sqlPart.Contains(" AND ") || sqlPart.Contains(" and "),
            "应该使用AND连接version条件");
    }

    /// <summary>
    /// UPDATE without WHERE should add WHERE version = @version
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ConcurrencyCheck")]
    [TestCategory("UPDATE")]
    public void ConcurrencyCheck_UPDATE_Without_WHERE_Should_Add_WHERE()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public string Name { get; set; } = """";

    [ConcurrencyCheck]
    public int Version { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<Product>> GetAllAsync();  // 帮助推断实体类型

    [SqlTemplate(""UPDATE {{table}} SET name = @name"")]
    Task<int> UpdateAsync(Product entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var updateMethodIndex = generatedCode.IndexOf("public async System.Threading.Tasks.Task<int> UpdateAsync");
        Assert.IsTrue(updateMethodIndex > 0, "应该找到UpdateAsync方法");

        var commandTextIndex = generatedCode.IndexOf("CommandText =", updateMethodIndex);
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));

        // 应该添加WHERE子句
        Assert.IsTrue(
            sqlPart.Contains("WHERE") || sqlPart.Contains("where"),
            "应该添加WHERE子句");

        // 应该包含version检查
        Assert.IsTrue(
            sqlPart.Contains("version = @") || sqlPart.Contains("Version = @"),
            "应该检查version");
    }

    /// <summary>
    /// Should return affected rows count (0 indicates conflict)
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ConcurrencyCheck")]
    [TestCategory("Return")]
    public void ConcurrencyCheck_Should_Return_Affected_Rows()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public long Id { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<Product>> GetAllAsync();  // 帮助推断实体类型

    [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
    Task<int> UpdateAsync(Product entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var updateMethodIndex = generatedCode.IndexOf("public async System.Threading.Tasks.Task<int> UpdateAsync");
        Assert.IsTrue(updateMethodIndex > 0, "应该找到UpdateAsync方法");

        // 方法应该返回Task<int>（返回受影响行数）
        Assert.IsTrue(
            generatedCode.Contains("Task<int> UpdateAsync"),
            "方法应该返回Task<int>");

        // 应该有result变量用于存储受影响行数
        Assert.IsTrue(
            generatedCode.Contains("int __result__"),
            "应该有result变量存储受影响行数");
    }

    /// <summary>
    /// ConcurrencyCheck should work together with AuditFields
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("ConcurrencyCheck")]
    [TestCategory("Integration")]
    public void ConcurrencyCheck_Should_Work_With_AuditFields()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";

    [ConcurrencyCheck]
    public int Version { get; set; }

    public DateTime UpdatedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<Product>> GetAllAsync();  // 帮助推断实体类型

    [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
    Task<int> UpdateAsync(Product entity);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var updateMethodIndex = generatedCode.IndexOf("public async System.Threading.Tasks.Task<int> UpdateAsync");
        Assert.IsTrue(updateMethodIndex > 0, "应该找到UpdateAsync方法");

        var commandTextIndex = generatedCode.IndexOf("CommandText =", updateMethodIndex);
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(600, generatedCode.Length - commandTextIndex));

        // 应该同时包含audit fields和concurrency check
        Assert.IsTrue(
            sqlPart.Contains("updated_at") || sqlPart.Contains("UpdatedAt"),
            "应该包含审计字段updated_at");

        Assert.IsTrue(
            sqlPart.Contains("version = version + 1") || sqlPart.Contains("Version = Version + 1"),
            "应该包含version递增");

        Assert.IsTrue(
            sqlPart.Contains("version = @") || sqlPart.Contains("Version = @"),
            "应该检查version");
    }
}

