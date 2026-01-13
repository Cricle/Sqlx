// -----------------------------------------------------------------------
// <copyright file="TDD_ArgPlaceholder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Tests for {{arg --param paramName}} placeholder that generates dialect-specific parameter prefixes.
/// </summary>
[TestClass]
[TestCategory("Placeholders")]
[TestCategory("Arg")]
public class TDD_ArgPlaceholder : CodeGenerationTestBase
{
    [TestMethod]
    [Description("{{arg --param id}} should generate @id for SQLite in SQL statement")]
    public void ArgPlaceholder_SQLite_ShouldUseAtPrefix()
    {
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public long Id { get; set; } public string Name { get; set; } }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = {{arg --param id}}"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        public UserRepository(System.Data.Common.DbConnection connection) => _connection = connection;
    }
}";
        var generatedCode = GetCSharpGeneratedOutput(source);
        StringAssert.Contains(generatedCode, "CommandText = @\"SELECT * FROM users WHERE id = @id\"");
    }

    [TestMethod]
    [Description("{{arg --param id}} should generate $id for PostgreSQL in SQL statement")]
    public void ArgPlaceholder_PostgreSQL_ShouldUseDollarPrefix()
    {
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public long Id { get; set; } public string Name { get; set; } }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = {{arg --param id}}"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        public UserRepository(System.Data.Common.DbConnection connection) => _connection = connection;
    }
}";
        var generatedCode = GetCSharpGeneratedOutput(source);
        // Check that the SQL statement contains $id (PostgreSQL parameter prefix)
        StringAssert.Contains(generatedCode, "CommandText = @\"SELECT * FROM users WHERE id = $id\"");
    }

    [TestMethod]
    [Description("{{arg --param id}} should generate :id for Oracle in SQL statement")]
    public void ArgPlaceholder_Oracle_ShouldUseColonPrefix()
    {
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public long Id { get; set; } public string Name { get; set; } }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = {{arg --param id}}"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.Oracle)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        public UserRepository(System.Data.Common.DbConnection connection) => _connection = connection;
    }
}";
        var generatedCode = GetCSharpGeneratedOutput(source);
        // Check that the SQL statement contains :id (Oracle parameter prefix)
        // Note: ADO.NET parameter binding still uses @id, but the SQL statement should use :id
        StringAssert.Contains(generatedCode, "CommandText = @\"SELECT * FROM users WHERE id = :id\"");
    }

    [TestMethod]
    [Description("{{arg --param id}} should generate @id for MySQL in SQL statement")]
    public void ArgPlaceholder_MySQL_ShouldUseAtPrefix()
    {
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public long Id { get; set; } public string Name { get; set; } }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = {{arg --param id}}"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.MySql)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        public UserRepository(System.Data.Common.DbConnection connection) => _connection = connection;
    }
}";
        var generatedCode = GetCSharpGeneratedOutput(source);
        StringAssert.Contains(generatedCode, "CommandText = @\"SELECT * FROM users WHERE id = @id\"");
    }

    [TestMethod]
    [Description("{{arg --param id}} should generate @id for SqlServer in SQL statement")]
    public void ArgPlaceholder_SqlServer_ShouldUseAtPrefix()
    {
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public long Id { get; set; } public string Name { get; set; } }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = {{arg --param id}}"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.SqlServer)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        public UserRepository(System.Data.Common.DbConnection connection) => _connection = connection;
    }
}";
        var generatedCode = GetCSharpGeneratedOutput(source);
        StringAssert.Contains(generatedCode, "CommandText = @\"SELECT * FROM users WHERE id = @id\"");
    }

    [TestMethod]
    [Description("{{arg paramName}} shorthand syntax should work")]
    public void ArgPlaceholder_ShorthandSyntax_ShouldWork()
    {
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public long Id { get; set; } public string Name { get; set; } }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = {{arg id}}"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        public UserRepository(System.Data.Common.DbConnection connection) => _connection = connection;
    }
}";
        var generatedCode = GetCSharpGeneratedOutput(source);
        StringAssert.Contains(generatedCode, "CommandText = @\"SELECT * FROM users WHERE id = $id\"");
    }

    [TestMethod]
    [Description("Multiple {{arg}} placeholders should work correctly")]
    public void ArgPlaceholder_MultipleArgs_ShouldWorkCorrectly()
    {
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public long Id { get; set; } public string Name { get; set; } }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = {{arg --param id}} AND name = {{arg --param name}}"")]
        Task<User?> GetByIdAndNameAsync(long id, string name);
    }

    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        public UserRepository(System.Data.Common.DbConnection connection) => _connection = connection;
    }
}";
        var generatedCode = GetCSharpGeneratedOutput(source);
        StringAssert.Contains(generatedCode, "$id");
        StringAssert.Contains(generatedCode, "$name");
    }
}
