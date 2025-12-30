// -----------------------------------------------------------------------
// <copyright file="CodeGenerationServiceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// CodeGenerationService的单元测试
/// 验证代码生成服务的核心功能
/// </summary>
[TestClass]
public class CodeGenerationServiceTests : TestBase
{
    /// <summary>
    /// 测试：生成完整的Repository类
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_CompleteRepositoryClass()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Todo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
    }

    public interface ITodoRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<List<Todo>> GetAllAsync();

        [Sqlx(""SELECT {{columns}} FROM {{table}} WHERE Id = @id"")]
        Task<Todo?> GetByIdAsync(int id);

        [Sqlx(""INSERT INTO {{table}} (Title, IsCompleted) VALUES (@title, @isCompleted)"")]
        Task<int> CreateAsync(string title, bool isCompleted);

        [Sqlx(""UPDATE {{table}} SET Title = @title, IsCompleted = @isCompleted WHERE Id = @id"")]
        Task<int> UpdateAsync(int id, string title, bool isCompleted);

        [Sqlx(""DELETE FROM {{table}} WHERE Id = @id"")]
        Task<int> DeleteAsync(int id);
    }

    [RepositoryFor(typeof(ITodoRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""todos"")]
    public partial class TodoRepository : ITodoRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "TodoRepository");

        // 验证所有CRUD方法都生成了
        Assert.IsTrue(generatedCode.Contains("GetAllAsync"));
        Assert.IsTrue(generatedCode.Contains("GetByIdAsync"));
        Assert.IsTrue(generatedCode.Contains("CreateAsync"));
        Assert.IsTrue(generatedCode.Contains("UpdateAsync"));
        Assert.IsTrue(generatedCode.Contains("DeleteAsync"));

        // 验证类结构正确
        Assert.IsTrue(generatedCode.Contains("partial class TodoRepository"));
    }

    /// <summary>
    /// 测试：生成正确的命名空间
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_CorrectNamespace()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace MyCompany.Data.Repositories
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
        Assert.IsTrue(generatedCode.Contains("namespace MyCompany.Data.Repositories"));
    }

    /// <summary>
    /// 测试：生成正确的using指令
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_CorrectUsings()
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

        // 验证必要的using指令
        Assert.IsTrue(generatedCode.Contains("using System"));
        Assert.IsTrue(generatedCode.Contains("using System.Data"));
    }

    /// <summary>
    /// 测试：生成正确的Partial方法
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_PartialMethods()
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

        // 验证三个partial方法声明
        Assert.IsTrue(generatedCode.Contains("partial void OnExecuting"));
        Assert.IsTrue(generatedCode.Contains("partial void OnExecuted"));
        Assert.IsTrue(generatedCode.Contains("partial void OnExecuteFail"));
    }

    /// <summary>
    /// 测试：生成正确的Activity追踪代码
    /// </summary>
    [TestMethod]
    public void ShouldGenerate_ActivityTracingCode()
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

        // 验证Activity追踪代码
        Assert.IsTrue(generatedCode.Contains("Activity.Current"));
        Assert.IsTrue(generatedCode.Contains("Stopwatch.GetTimestamp()"));
    }

    /// <summary>
    /// 测试：正确处理多个接口方法
    /// </summary>
    [TestMethod]
    public void ShouldHandle_MultipleInterfaceMethods()
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
        public int Age { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<List<User>> GetAllAsync();

        [Sqlx(""SELECT {{columns}} FROM {{table}} WHERE Id = @id"")]
        Task<User?> GetByIdAsync(int id);

        [Sqlx(""SELECT {{columns}} FROM {{table}} WHERE Age > @age"")]
        Task<List<User>> GetByAgeAsync(int age);

        [Sqlx(""SELECT COUNT(*) FROM {{table}}"")]
        Task<int> GetCountAsync();
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

        // 验证所有方法都生成了
        Assert.IsTrue(generatedCode.Contains("GetAllAsync"));
        Assert.IsTrue(generatedCode.Contains("GetByIdAsync"));
        Assert.IsTrue(generatedCode.Contains("GetByAgeAsync"));
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"));
    }

    /// <summary>
    /// 测试：正确处理泛型返回类型
    /// </summary>
    [TestMethod]
    public void ShouldHandle_GenericReturnTypes()
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
        Task<List<User>> GetListAsync();

        [Sqlx(""SELECT * FROM users"")]
        Task<IEnumerable<User>> GetEnumerableAsync();

        [Sqlx(""SELECT * FROM users WHERE Id = @id"")]
        Task<User?> GetSingleAsync(int id);
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
        Assert.IsTrue(generatedCode.Contains("GetListAsync"));
        Assert.IsTrue(generatedCode.Contains("GetEnumerableAsync"));
        Assert.IsTrue(generatedCode.Contains("GetSingleAsync"));
    }

    /// <summary>
    /// 测试：正确处理复杂参数类型
    /// </summary>
    [TestMethod]
    public void ShouldHandle_ComplexParameterTypes()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } }

    public interface IUserRepository
    {
        [Sqlx(""SELECT * FROM users WHERE CreatedAt > @date AND IsActive = @isActive AND Score >= @score"")]
        Task<User?> GetAsync(DateTime date, bool isActive, decimal score);
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
    /// 测试：生成nullable引用类型的支持
    /// </summary>
    [TestMethod]
    public void ShouldSupport_NullableReferenceTypes()
    {
        var source = @"
#nullable enable
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
        public string? Email { get; set; }
    }

    public interface IUserRepository
    {
        [Sqlx(""SELECT {{columns}} FROM {{table}}"")]
        Task<User?> GetAsync();
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
        Assert.IsTrue(generatedCode.Contains("GetAsync"));
    }
}

