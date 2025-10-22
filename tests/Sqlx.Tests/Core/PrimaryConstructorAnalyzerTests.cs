// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorAnalyzerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// PrimaryConstructorAnalyzer的单元测试
/// 验证主构造函数分析逻辑
/// </summary>
[TestClass]
public class PrimaryConstructorAnalyzerTests : TestBase
{
    /// <summary>
    /// 测试：识别主构造函数（C# 12新特性）
    /// </summary>
    [TestMethod]
    public void ShouldRecognize_PrimaryConstructor()
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
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    public partial class UserRepository(IDbConnection connection) : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证生成的代码使用了connection参数
        Assert.IsTrue(generatedCode.Contains("connection") || generatedCode.Contains("Connection"));
    }

    /// <summary>
    /// 测试：传统构造函数
    /// </summary>
    [TestMethod]
    public void ShouldWork_WithTraditionalConstructor()
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
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    public partial class UserRepository : IUserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
        }
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：主构造函数参数名为connection
    /// </summary>
    [TestMethod]
    public void PrimaryConstructor_WithConnectionParameter()
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
    public partial class UserRepository(IDbConnection connection) : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        Assert.IsTrue(generatedCode.Contains("GetAsync"));
    }

    /// <summary>
    /// 测试：主构造函数多个参数
    /// </summary>
    [TestMethod]
    public void PrimaryConstructor_WithMultipleParameters()
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
    public partial class UserRepository(IDbConnection connection, string tableName) : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：无构造函数时的行为
    /// </summary>
    [TestMethod]
    public void NoConstructor_ShouldStillGenerate()
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
        Assert.IsTrue(generatedCode.Contains("GetAsync"));
    }

    /// <summary>
    /// 测试：主构造函数record类型
    /// </summary>
    [TestMethod]
    public void PrimaryConstructor_RecordType()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public record User(int Id, string Name);

    public interface IUserRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    public partial class UserRepository(IDbConnection connection) : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        // Record类型应该正常工作
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：主构造函数 + 接口实现
    /// </summary>
    [TestMethod]
    public void PrimaryConstructor_WithInterfaceImplementation()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<List<User>> GetAllAsync();

        [Sqlx(""SELECT {{columns}} FROM {{table}} WHERE Id = @id"")]
        Task<User> GetByIdAsync(int id);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    public partial class UserRepository(IDbConnection connection) : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        Assert.IsTrue(generatedCode.Contains("GetAllAsync"));
        Assert.IsTrue(generatedCode.Contains("GetByIdAsync"));
    }
}

