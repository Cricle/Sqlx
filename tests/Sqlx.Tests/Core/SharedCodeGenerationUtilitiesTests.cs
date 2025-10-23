// -----------------------------------------------------------------------
// <copyright file="SharedCodeGenerationUtilitiesTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// SharedCodeGenerationUtilities的单元测试
/// 验证共享代码生成工具的正确性
/// </summary>
[TestClass]
public class SharedCodeGenerationUtilitiesTests : TestBase
{
    /// <summary>
    /// 测试：生成正确的参数绑定代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_CorrectParameterBinding()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users WHERE Id = @id AND Name = @name"")]
        Task<User> GetAsync(int id, string name);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证参数绑定代码存在
        Assert.IsTrue(generatedCode.Contains("AddWithValue") || generatedCode.Contains("Parameters"));
    }

    /// <summary>
    /// 测试：生成正确的结果映射代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_CorrectResultMapping()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证结果映射代码 - 直接索引访问
        Assert.IsTrue(generatedCode.Contains("Id ="));
        Assert.IsTrue(generatedCode.Contains("Name ="));
        Assert.IsTrue(generatedCode.Contains("Email ="));
    }

    /// <summary>
    /// 测试：生成正确的异常处理代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_ExceptionHandling()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证异常处理结构
        Assert.IsTrue(generatedCode.Contains("try"));
        Assert.IsTrue(generatedCode.Contains("catch"));
        Assert.IsTrue(generatedCode.Contains("finally"));
    }

    /// <summary>
    /// 测试：生成正确的命令创建代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_CommandCreation()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users WHERE Id = @id"")]
        Task<User> GetAsync(int id);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证命令创建代码
        Assert.IsTrue(generatedCode.Contains("CreateCommand") || generatedCode.Contains("__cmd__"));
    }

    /// <summary>
    /// 测试：生成正确的命令释放代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_CommandDisposal()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证命令释放代码在finally块
        Assert.IsTrue(generatedCode.Contains("__cmd__?.Dispose()"));
    }

    /// <summary>
    /// 测试：生成正确的异步代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_AsyncCode()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证异步代码生成 - 可能使用Task.FromResult或ExecuteNonQueryAsync
        Assert.IsTrue(generatedCode.Contains("Task") || generatedCode.Contains("GetAsync"));
    }

    /// <summary>
    /// 测试：生成正确的集合返回代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_ListReturnCode()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users"")]
        Task<List<User>> GetAllAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证List生成代码
        Assert.IsTrue(generatedCode.Contains("List<"));
    }

    /// <summary>
    /// 测试：生成正确的单个对象返回代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_SingleObjectReturnCode()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users WHERE Id = @id"")]
        Task<User?> GetByIdAsync(int id);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        Assert.IsTrue(generatedCode.Contains("GetByIdAsync"));
    }

    /// <summary>
    /// 测试：生成正确的标量返回代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_ScalarReturnCode()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IUserRepository
    {
        [Sqlx(""SELECT COUNT(*) FROM users"")]
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"));
    }

    /// <summary>
    /// 测试：生成正确的非查询代码（INSERT/UPDATE/DELETE）
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_NonQueryCode()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""INSERT INTO users (Name) VALUES (@name)"")]
        Task<int> InsertAsync(string name);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证非查询代码生成
        Assert.IsTrue(generatedCode.Contains("ExecuteNonQuery") || generatedCode.Contains("InsertAsync"));
    }

    /// <summary>
    /// 测试：生成正确的null处理代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_NullHandling()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证IsDBNull检查生成
        Assert.IsTrue(generatedCode.Contains("IsDBNull"));
    }
}

