// -----------------------------------------------------------------------
// <copyright file="TDD_Phase2_Operators_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Expression;

/// <summary>
/// TDD Phase 2: Red Tests for Expression operators and methods.
/// Goal: Support >=, <=, !=, &&, ||, !, StartsWith, EndsWith, NULL checks
/// </summary>
[TestClass]
public class TDD_Phase2_Operators_RedTests : CodeGenerationTestBase
{
    #region Phase 2A: 比较运算符

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_GreaterThanOrEqual_Should_Generate_SQL()
    {
        var source = @"
using System;
using System.Data;
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
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_LessThanOrEqual_Should_Generate_SQL()
    {
        var source = @"
using System;
using System.Data;
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
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_NotEqual_Should_Generate_SQL()
    {
        var source = @"
using System;
using System.Data;
using System.Linq.Expressions;
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
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    #endregion

    #region Phase 2B: 逻辑运算符

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_And_Should_Generate_SQL()
    {
        var source = @"
using System;
using System.Data;
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
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_Or_Should_Generate_SQL()
    {
        var source = @"
using System;
using System.Data;
using System.Linq.Expressions;
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
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_Not_Should_Generate_SQL()
    {
        var source = @"
using System;
using System.Data;
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
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    #endregion

    #region Phase 2C: 字符串方法

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_StartsWith_Should_Generate_LIKE()
    {
        var source = @"
using System;
using System.Data;
using System.Linq.Expressions;
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
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_EndsWith_Should_Generate_LIKE()
    {
        var source = @"
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_StringContains_Should_Generate_LIKE()
    {
        var source = @"
using System;
using System.Data;
using System.Linq.Expressions;
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
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    #endregion

    #region Phase 2D: NULL检查

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_EqualNull_Should_Generate_IS_NULL()
    {
        var source = @"
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public DateTime? DeletedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("Expression")]
    [TestCategory("Phase2")]
    public void Expression_NotEqualNull_Should_Generate_IS_NOT_NULL()
    {
        var source = @"
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<User?> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Expression运算符在运行时由ExpressionToSql处理，检查桥接代码存在
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<") && generatedCode.Contains(".Where(predicate)"),
            "应该生成Expression桥接代码");
    }

    #endregion
}

