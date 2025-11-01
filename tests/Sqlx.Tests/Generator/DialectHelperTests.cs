// -----------------------------------------------------------------------
// <copyright file="DialectHelperTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using Sqlx.Generator.Core;
using System.Linq;

namespace Sqlx.Tests.Generator;

[TestClass]
public class DialectHelperTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetDialectFromRepositoryFor_WithDialectSet_ShouldReturnDialect()
    {
        // Arrange
        var code = @"
using Sqlx.Annotations;

namespace Test
{
    public class User { public int Id { get; set; } }
    
    public interface IUserRepository { }
    
    [RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.PostgreSql)]
    public partial class UserRepository : IUserRepository { }
}
";
        var (_, repositoryClass) = CompileAndGetSymbol(code, "UserRepository");

        // Act
        var dialect = DialectHelper.GetDialectFromRepositoryFor(repositoryClass);

        // Assert
        Assert.AreEqual(SqlDefineTypes.PostgreSql, dialect);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetDialectFromRepositoryFor_NoDialectSet_ShouldReturnDefault()
    {
        // Arrange
        var code = @"
using Sqlx.Annotations;

namespace Test
{
    public class User { public int Id { get; set; } }
    
    public interface IUserRepository { }
    
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository : IUserRepository { }
}
";
        var (_, repositoryClass) = CompileAndGetSymbol(code, "UserRepository");

        // Act
        var dialect = DialectHelper.GetDialectFromRepositoryFor(repositoryClass);

        // Assert
        Assert.AreEqual(SqlDefineTypes.SQLite, dialect);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetTableNameFromRepositoryFor_WithTableNameSet_ShouldReturnTableName()
    {
        // Arrange
        var code = @"
using Sqlx.Annotations;

namespace Test
{
    public class User { public int Id { get; set; } }
    
    public interface IUserRepository { }
    
    [RepositoryFor(typeof(IUserRepository), TableName = ""custom_users"")]
    public partial class UserRepository : IUserRepository { }
}
";
        var (_, repositoryClass) = CompileAndGetSymbol(code, "UserRepository");

        // Act
        var tableName = DialectHelper.GetTableNameFromRepositoryFor(repositoryClass, null);

        // Assert
        Assert.AreEqual("custom_users", tableName);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetTableNameFromRepositoryFor_NoTableNameSet_ShouldReturnInferred()
    {
        // Arrange
        var code = @"
using Sqlx.Annotations;

namespace Test
{
    public class User { public int Id { get; set; } }
    
    public interface IUserRepository { }
    
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository : IUserRepository { }
}
";
        var (compilation, repositoryClass) = CompileAndGetSymbol(code, "UserRepository");
        var userType = compilation.GetTypeByMetadataName("Test.User");

        // Act
        var tableName = DialectHelper.GetTableNameFromRepositoryFor(repositoryClass, userType);

        // Assert
        Assert.AreEqual("user", tableName);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetDialectProvider_PostgreSQL_ShouldReturnCorrectProvider()
    {
        // Act
        var provider = DialectHelper.GetDialectProvider(SqlDefineTypes.PostgreSql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetDialectProvider_MySQL_ShouldReturnCorrectProvider()
    {
        // Act
        var provider = DialectHelper.GetDialectProvider(SqlDefineTypes.MySql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetDialectProvider_SqlServer_ShouldReturnCorrectProvider()
    {
        // Act
        var provider = DialectHelper.GetDialectProvider(SqlDefineTypes.SqlServer);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetDialectProvider_SQLite_ShouldReturnCorrectProvider()
    {
        // Act
        var provider = DialectHelper.GetDialectProvider(SqlDefineTypes.SQLite);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ShouldUseTemplateInheritance_WithPlaceholders_ShouldReturnTrue()
    {
        // Arrange
        var code = @"
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class User { public int Id { get; set; } }
    
    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM {{table}} WHERE id = @id"")]
        Task<User?> GetByIdAsync(int id, CancellationToken ct);
    }
}
";
        var (_, interfaceSymbol) = CompileAndGetSymbol(code, "IUserRepository");

        // Act
        var result = DialectHelper.ShouldUseTemplateInheritance(interfaceSymbol);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ShouldUseTemplateInheritance_WithoutPlaceholders_ShouldReturnFalse()
    {
        // Arrange
        var code = @"
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class User { public int Id { get; set; } }
    
    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = @id"")]
        Task<User?> GetByIdAsync(int id, CancellationToken ct);
    }
}
";
        var (_, interfaceSymbol) = CompileAndGetSymbol(code, "IUserRepository");

        // Act
        var result = DialectHelper.ShouldUseTemplateInheritance(interfaceSymbol);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CombinedScenario_PostgreSQLWithCustomTable_ShouldWorkCorrectly()
    {
        // Arrange
        var code = @"
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class User { public int Id { get; set; } }
    
    public interface IUserRepositoryBase
    {
        [SqlTemplate(""SELECT * FROM {{table}} WHERE id = @id"")]
        Task<User?> GetByIdAsync(int id, CancellationToken ct);
    }
    
    [RepositoryFor(typeof(IUserRepositoryBase), Dialect = SqlDefineTypes.PostgreSql, TableName = ""pg_users"")]
    public partial class PostgreSQLUserRepository : IUserRepositoryBase { }
}
";
        var (compilation, repositoryClass) = CompileAndGetSymbol(code, "PostgreSQLUserRepository");
        var interfaceSymbol = compilation.GetTypeByMetadataName("Test.IUserRepositoryBase");

        // Act
        var dialect = DialectHelper.GetDialectFromRepositoryFor(repositoryClass);
        var tableName = DialectHelper.GetTableNameFromRepositoryFor(repositoryClass, null);
        var shouldUseInheritance = DialectHelper.ShouldUseTemplateInheritance(interfaceSymbol!);
        var provider = DialectHelper.GetDialectProvider(dialect);

        // Assert
        Assert.AreEqual(SqlDefineTypes.PostgreSql, dialect);
        Assert.AreEqual("pg_users", tableName);
        Assert.IsTrue(shouldUseInheritance);
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
    }

    private (Compilation compilation, INamedTypeSymbol symbol) CompileAndGetSymbol(string code, string typeName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        
        var refPaths = new[]
        {
            typeof(object).Assembly.Location,
            typeof(System.Threading.Tasks.Task).Assembly.Location,
            typeof(System.Linq.Enumerable).Assembly.Location,
            typeof(Sqlx.Annotations.SqlTemplateAttribute).Assembly.Location,
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"),
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll")
        };

        var references = refPaths.Where(System.IO.File.Exists).Select(r => MetadataReference.CreateFromFile(r)).ToArray();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var symbol = compilation.GetTypeByMetadataName($"Test.{typeName}");

        return (compilation, symbol!);
    }
}

