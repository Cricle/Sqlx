// -----------------------------------------------------------------------
// <copyright file="TDD_Phase1_INQuery_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CollectionSupport;

/// <summary>
/// TDD Phase 1: Red Tests for IN Query support.
/// Goal: Support IEnumerable parameters that expand to IN (param0, param1, ...) clauses.
/// </summary>
[TestClass]
public class TDD_Phase1_INQuery_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// Array parameter should expand to multiple parameters in IN clause
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("IN-Query")]
    public void IN_Query_Array_Parameter_Should_Expand_To_Multiple_Parameters()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE id IN (@ids)"")]
    Task<List<User>> GetByIdsAsync(long[] ids);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var getByIdsMethodIndex = generatedCode.IndexOf("GetByIdsAsync");
        Assert.IsTrue(getByIdsMethodIndex > 0, "应该找到GetByIdsAsync方法");
        
        // 应该展开集合参数（使用foreach遍历）
        Assert.IsTrue(
            generatedCode.Contains("foreach") && generatedCode.Contains("ids"),
            "应该使用foreach遍历ids参数");
        
        // 应该生成动态IN子句
        Assert.IsTrue(
            generatedCode.Contains("IN (") || generatedCode.Contains("IN("),
            "应该包含IN子句");
        
        // 应该有空集合检查
        Assert.IsTrue(
            generatedCode.Contains(".Any()"),
            "应该检查集合是否为空");
    }

    /// <summary>
    /// IEnumerable parameter should work the same as array
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("IN-Query")]
    public void IN_Query_IEnumerable_Parameter_Should_Work()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Status { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE status IN (@statuses)"")]
    Task<List<User>> GetByStatusesAsync(IEnumerable<string> statuses);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var getByStatusesMethodIndex = generatedCode.IndexOf("GetByStatusesAsync");
        Assert.IsTrue(getByStatusesMethodIndex > 0, "应该找到GetByStatusesAsync方法");
        
        // 应该处理IEnumerable参数 - 展开为多个参数
        Assert.IsTrue(
            generatedCode.Contains("foreach") && generatedCode.Contains("statuses"),
            "应该使用foreach遍历statuses参数");
        
        // 应该有动态IN子句替换
        Assert.IsTrue(
            generatedCode.Contains("__inClause_statuses__"),
            "应该有动态IN子句变量");
    }

    /// <summary>
    /// String parameter should NOT be treated as collection (even though it's IEnumerable<char>)
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("IN-Query")]
    public void String_Parameter_Should_Not_Be_Treated_As_Collection()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE name = @name"")]
    Task<User?> GetByNameAsync(string name);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var getByNameMethodIndex = generatedCode.IndexOf("GetByNameAsync");
        Assert.IsTrue(getByNameMethodIndex > 0, "应该找到GetByNameAsync方法");
        
        var methodSection = generatedCode.Substring(getByNameMethodIndex, Math.Min(800, generatedCode.Length - getByNameMethodIndex));
        
        // string参数应该按普通参数处理，不展开
        Assert.IsFalse(
            methodSection.Contains("foreach"),
            "string参数不应该被展开（即使它是IEnumerable<char>）");
        
        // 应该直接绑定为单个参数
        Assert.IsTrue(
            methodSection.Contains("@name") || methodSection.Contains("ParameterName"),
            "应该有普通参数绑定");
    }

    /// <summary>
    /// Empty collection should be handled gracefully (avoid SQL error: WHERE id IN ())
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("IN-Query")]
    public void Empty_Collection_Should_Handle_Gracefully()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE id IN (@ids)"")]
    Task<List<User>> GetByIdsAsync(long[] ids);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // 应该检查集合是否为空（避免生成非法SQL: WHERE id IN ()）
        Assert.IsTrue(
            generatedCode.Contains("Any()") || generatedCode.Contains("Count") || generatedCode.Contains("Length"),
            "应该检查集合是否为空");
    }

    /// <summary>
    /// List<T> parameter should work
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("IN-Query")]
    public void IN_Query_List_Parameter_Should_Work()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE id IN (@ids)"")]
    Task<List<User>> GetByIdsAsync(List<long> ids);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var getByIdsMethodIndex = generatedCode.IndexOf("GetByIdsAsync");
        Assert.IsTrue(getByIdsMethodIndex > 0, "应该找到GetByIdsAsync方法");
        
        // List<T>应该被识别为集合参数
        Assert.IsTrue(
            generatedCode.Contains("foreach") && generatedCode.Contains("ids"),
            "List<T>应该被展开");
    }
}

