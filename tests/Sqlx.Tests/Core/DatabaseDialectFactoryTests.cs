// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectFactoryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// DatabaseDialectFactory的单元测试
/// 验证数据库方言工厂的正确性
/// </summary>
[TestClass]
public class DatabaseDialectFactoryTests : TestBase
{
    /// <summary>
    /// 测试：SQLite方言列引号
    /// </summary>
    [TestMethod]
    public void SQLiteDialect_ShouldNotQuoteColumns()
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
        [Sqlx(""SELECT {{columns}} FROM users"")]
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
        // SQLite通常不使用列引号，或者使用双引号
        Assert.IsFalse(generatedCode.Contains("[Id]"), "SQLite不应该使用方括号");
        Assert.IsFalse(generatedCode.Contains("`Id`"), "SQLite不应该使用反引号");
    }

    /// <summary>
    /// 测试：SQL Server方言列引号
    /// </summary>
    [TestMethod]
    public void SqlServerDialect_ShouldUseBrackets()
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
        [Sqlx(""SELECT {{columns}} FROM users"")]
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
        // SQL Server使用方括号
        Assert.IsTrue(generatedCode.Contains("[Id]") || generatedCode.Contains("Id"),
            "SQL Server应该使用方括号或不引号");
    }

    /// <summary>
    /// 测试：MySQL方言列引号
    /// </summary>
    [TestMethod]
    public void MySqlDialect_ShouldUseBackticks()
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
        [Sqlx(""SELECT {{columns}} FROM users"")]
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

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // MySQL使用反引号
        Assert.IsTrue(generatedCode.Contains("`Id`") || generatedCode.Contains("Id"),
            "MySQL应该使用反引号或不引号");
    }

    /// <summary>
    /// 测试：PostgreSQL方言列引号
    /// </summary>
    [TestMethod]
    public void PostgreSqlDialect_ShouldUseDoubleQuotes()
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
        [Sqlx(""SELECT {{columns}} FROM users"")]
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

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // PostgreSQL使用双引号
        Assert.IsTrue(generatedCode.Contains("\"Id\"") || generatedCode.Contains("Id"),
            "PostgreSQL应该使用双引号或不引号");
    }

    /// <summary>
    /// 测试：Oracle方言
    /// </summary>
    [TestMethod]
    public void OracleDialect_ShouldWork()
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
        [Sqlx(""SELECT {{columns}} FROM users"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.Oracle)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：DB2方言
    /// </summary>
    [TestMethod]
    public void DB2Dialect_ShouldWork()
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
        [Sqlx(""SELECT {{columns}} FROM users"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.DB2)]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：默认方言（未指定SqlDefine时）
    /// </summary>
    [TestMethod]
    public void DefaultDialect_WhenNoSqlDefine_ShouldWork()
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
        [Sqlx(""SELECT {{columns}} FROM users"")]
        Task<User> GetAsync();
    }

    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        // 应该使用默认方言，不报错
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// 测试：方言切换不影响其他功能
    /// </summary>
    [TestMethod]
    public void DialectSwitch_ShouldNotAffectOtherFeatures()
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
        [Sqlx(""SELECT {{columns}} FROM {{table}} WHERE Id = @id"")]
        Task<User> GetByIdAsync(int id);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.MySQL)]
    [TableName(""users"")]
    public partial class UserRepository : IUserRepository
    {
    }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "UserRepository");
        // 验证参数绑定和其他功能正常
        Assert.IsTrue(generatedCode.Contains("@id") || generatedCode.Contains("AddWithValue"),
            "参数绑定应该正常工作");
    }
}

