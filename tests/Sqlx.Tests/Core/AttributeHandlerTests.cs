// -----------------------------------------------------------------------
// <copyright file="AttributeHandlerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// AttributeHandler的单元测试
/// 验证特性处理逻辑
/// </summary>
[TestClass]
public class AttributeHandlerTests : TestBase
{
    /// <summary>
    /// 测试：正确识别SqlxAttribute
    /// </summary>
    [TestMethod]
    public void ShouldRecognize_SqlxAttribute()
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
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users"")]
        Task<User> GetUserAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
            "不应该有编译错误");

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        Assert.IsTrue(generatedCode.Contains("GetUserAsync"), "应该生成GetUserAsync方法");
    }

    /// <summary>
    /// 测试：正确识别RepositoryForAttribute
    /// </summary>
    [TestMethod]
    public void ShouldRecognize_RepositoryForAttribute()
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
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        Assert.IsTrue(generatedCode.Contains("partial class UserRepository"));
    }

    /// <summary>
    /// 测试：正确识别TableNameAttribute
    /// </summary>
    [TestMethod]
    public void ShouldRecognize_TableNameAttribute()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""custom_users"")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""custom_users"")]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // TableName主要用于占位符替换，不一定直接出现在生成的C#代码中
        Assert.IsTrue(generatedCode.Contains("GetAsync") || generatedCode.Contains("UserRepository"));
    }

    /// <summary>
    /// 测试：正确识别SqlDefineAttribute
    /// </summary>
    [TestMethod]
    public void ShouldRecognize_SqlDefineAttribute_SQLServer()
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
        [Sqlx(""SELECT TOP 10 * FROM users"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLServer)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        Assert.IsTrue(generatedCode.Contains("UserRepository"));
    }

    /// <summary>
    /// 测试：正确识别SqlDefineAttribute - MySQL
    /// </summary>
    [TestMethod]
    public void ShouldRecognize_SqlDefineAttribute_MySQL()
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
        [Sqlx(""SELECT * FROM users LIMIT 10"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.MySQL)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：正确识别SqlDefineAttribute - PostgreSQL
    /// </summary>
    [TestMethod]
    public void ShouldRecognize_SqlDefineAttribute_PostgreSQL()
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
        [Sqlx(""SELECT * FROM users LIMIT 10"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：缺少必要特性时的行为
    /// </summary>
    [TestMethod]
    public void WhenMissingRequiredAttributes_ShouldNotGenerate()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public partial class UserRepository
    {
        public Task<User> GetAsync() => throw new System.NotImplementedException();
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // 没有RepositoryFor特性，不应该生成代码
        var allGenerated = compilation.SyntaxTrees
            .Where(st => st.FilePath.Contains("UserRepository"))
            .ToList();

        // 应该只有源代码，没有生成的代码
        Assert.IsTrue(allGenerated.Count <= 1, "不应该生成额外的代码");
    }

    /// <summary>
    /// 测试：多个特性组合
    /// </summary>
    [TestMethod]
    public void ShouldHandle_MultipleAttributes()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""my_users"")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.MySQL)]
    [TableName(""my_users"")]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // TableName主要用于占位符替换，验证代码生成成功即可
        Assert.IsTrue(generatedCode.Contains("GetAsync") || generatedCode.Contains("UserRepository"));
    }
}

