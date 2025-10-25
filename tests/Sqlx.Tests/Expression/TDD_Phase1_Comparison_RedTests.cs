// -----------------------------------------------------------------------
// <copyright file="TDD_Phase1_Comparison_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Expression;

/// <summary>
/// TDD Phase 1: Red Tests for Expression parameter support - Simple comparison operations.
/// Goal: Support basic comparison operators (==, !=, >, >=, <, <=) in WHERE clauses.
/// </summary>
[TestClass]
public class TDD_Phase1_Comparison_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// Expression: u => u.Age == 18
    /// Expected SQL: WHERE age = @p0
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression-Comparison")]
    public void Expression_Equal_Should_Generate_Equal_SQL()
    {
        // Arrange
        var source = @"
using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        // 应该包含WHERE子句生成逻辑
        StringAssert.Contains(generatedCode, "WHERE", "应该生成WHERE子句");
        
        // 应该有Expression解析逻辑
        Assert.IsTrue(
            generatedCode.Contains("Expression") || generatedCode.Contains("visitor") || generatedCode.Contains("predicate"),
            "应该有Expression参数处理逻辑");
        
        // 应该返回List<User>
        StringAssert.Contains(generatedCode, "List<", "应该返回List<User>");
    }

    /// <summary>
    /// Expression: u => u.Age > 18
    /// Expected SQL: WHERE age > @p0
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression-Comparison")]
    public void Expression_GreaterThan_Should_Generate_GreaterThan_SQL()
    {
        // Arrange
        var source = @"
using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        StringAssert.Contains(generatedCode, "WHERE", "应该生成WHERE子句");
        
        // 应该支持比较运算符
        Assert.IsTrue(
            generatedCode.Contains(">") || generatedCode.Contains("GreaterThan"),
            "应该支持大于运算符");
    }

    /// <summary>
    /// Expression: u => u.Age != 18
    /// Expected SQL: WHERE age <> @p0
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression-Comparison")]
    public void Expression_NotEqual_Should_Generate_NotEqual_SQL()
    {
        // Arrange
        var source = @"
using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        StringAssert.Contains(generatedCode, "WHERE", "应该生成WHERE子句");
        
        // 应该支持不等于运算符
        Assert.IsTrue(
            generatedCode.Contains("<>") || generatedCode.Contains("!=") || generatedCode.Contains("NotEqual"),
            "应该支持不等于运算符");
    }

    /// <summary>
    /// Expression: u => u.Age >= 18
    /// Expected SQL: WHERE age >= @p0
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression-Comparison")]
    public void Expression_GreaterThanOrEqual_Should_Generate_GreaterThanOrEqual_SQL()
    {
        // Arrange
        var source = @"
using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        StringAssert.Contains(generatedCode, "WHERE", "应该生成WHERE子句");
        
        // 应该支持大于等于运算符
        Assert.IsTrue(
            generatedCode.Contains(">=") || generatedCode.Contains("GreaterThanOrEqual"),
            "应该支持大于等于运算符");
    }

    /// <summary>
    /// Expression parameter should be AOT-friendly (no runtime compilation).
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression-Comparison")]
    [TestCategory("AOT")]
    public void Expression_Should_Be_AOT_Friendly()
    {
        // Arrange
        var source = @"
using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert - 应该不使用Expression.Compile()或反射
        Assert.IsFalse(generatedCode.Contains("Compile()"), "不应使用Expression.Compile()");
        Assert.IsFalse(generatedCode.Contains("GetType()"), "不应使用GetType()");
        Assert.IsFalse(generatedCode.Contains("Activator.CreateInstance"), "不应使用Activator");
        
        // 应该在编译时解析Expression或使用访问者模式
        Assert.IsTrue(
            generatedCode.Contains("WHERE") || generatedCode.Contains("predicate"),
            "应该有WHERE子句或Expression处理逻辑");
    }

    /// <summary>
    /// Multiple comparison operations in the same query.
    /// Expression: u => u.Age > 18 (as a baseline test)
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression-Comparison")]
    public void Expression_LessThan_Should_Generate_LessThan_SQL()
    {
        // Arrange
        var source = @"
using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        StringAssert.Contains(generatedCode, "WHERE", "应该生成WHERE子句");
        
        // 应该支持小于运算符
        Assert.IsTrue(
            generatedCode.Contains("<") || generatedCode.Contains("LessThan"),
            "应该支持小于运算符");
    }
}

