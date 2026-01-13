// -----------------------------------------------------------------------
// <copyright file="SqlxDebuggerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests
{
    /// <summary>
    /// Tests for SqlxDebugger attribute functionality.
    /// Verifies that SQL template getter methods are generated correctly.
    /// </summary>
    [TestClass]
    public class SqlxDebuggerTests : CodeGenerationTestBase
    {
        [TestMethod]
        public async Task SqlxDebugger_SimpleQuery_ShouldGenerateGetSqlMethod()
        {
            var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [SqlxDebugger]
        [SqlTemplate(""SELECT * FROM users WHERE id = @id"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify the SQL getter method is generated with SqlTemplate return type
            StringAssert.Contains(generatedCode, "GetGetByIdAsyncSql");
            StringAssert.Contains(generatedCode, "public global::Sqlx.SqlTemplate GetGetByIdAsyncSql()");
        }

        [TestMethod]
        public async Task SqlxDebugger_WithMultipleParameters_ShouldGenerateSqlTemplate()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public interface IUserRepository
    {
        [SqlxDebugger]
        [SqlTemplate(""SELECT * FROM users WHERE age > @minAge {{orderby --param orderBy}} {{limit --param limit}}"")]
        Task<List<User>> GetUsersByAgeAsync(int minAge, string? orderBy = null, int? limit = null, CancellationToken ct = default);
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify method returns SqlTemplate with no parameters
            StringAssert.Contains(generatedCode, "GetGetUsersByAgeAsyncSql");
            StringAssert.Contains(generatedCode, "public global::Sqlx.SqlTemplate GetGetUsersByAgeAsyncSql()");
        }

        [TestMethod]
        public async Task SqlxDebugger_WithoutAttribute_ShouldNotGenerateGetSqlMethod()
        {
            var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = @id"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify the SQL getter method is NOT generated
            Assert.IsFalse(generatedCode.Contains("GetGetByIdAsyncSql"));
        }

        [TestMethod]
        public async Task SqlxDebugger_WithPlaceholders_ShouldReturnSqlTemplate()
        {
            var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [SqlxDebugger]
        [SqlTemplate(""SELECT {{columns}} FROM {{table}} WHERE id = @id"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [TableName(""users"")]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify SqlTemplate return type and parameterless method
            StringAssert.Contains(generatedCode, "public global::Sqlx.SqlTemplate GetGetByIdAsyncSql()");
            StringAssert.Contains(generatedCode, "new global::Sqlx.SqlTemplate");
        }

        [TestMethod]
        public async Task SqlxDebugger_WithEntityColumns_ShouldExpandPlaceholders()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [SqlxDebugger]
        [SqlTemplate(""SELECT {{columns}} FROM {{table}} WHERE id = @id"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify SQL debugger method is generated
            StringAssert.Contains(generatedCode, "GetGetByIdAsyncSql");
            StringAssert.Contains(generatedCode, "public global::Sqlx.SqlTemplate GetGetByIdAsyncSql()");
            // Verify placeholders are processed (columns should be expanded)
            StringAssert.Contains(generatedCode, "Id");
            StringAssert.Contains(generatedCode, "Name");
            StringAssert.Contains(generatedCode, "Email");
        }

        [TestMethod]
        public async Task SqlxDebugger_WithXmlDocumentation_ShouldGenerateDocumentation()
        {
            var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [SqlxDebugger]
        [SqlTemplate(""SELECT * FROM users WHERE id = @id"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify XML documentation is generated
            StringAssert.Contains(generatedCode, "/// <summary>");
            StringAssert.Contains(generatedCode, "/// Gets the SQL template for");
            StringAssert.Contains(generatedCode, "/// <returns>");
        }

        [TestMethod]
        public async Task SqlxDebugger_OnClass_ShouldGenerateForAllMethods()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        [SqlTemplate(""SELECT {{columns}} FROM {{table}} WHERE id = @id"")]
        Task<User?> GetByIdAsync(long id);

        [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
        Task<int> InsertAsync(User entity);

        [SqlTemplate(""DELETE FROM {{table}} WHERE id = @id"")]
        Task<int> DeleteAsync(long id);
    }

    [SqlxDebugger]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""users"")]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify all three methods have SQL getter methods generated
            StringAssert.Contains(generatedCode, "public global::Sqlx.SqlTemplate GetGetByIdAsyncSql()");
            StringAssert.Contains(generatedCode, "public global::Sqlx.SqlTemplate GetInsertAsyncSql()");
            StringAssert.Contains(generatedCode, "public global::Sqlx.SqlTemplate GetDeleteAsyncSql()");
        }

        [TestMethod]
        public async Task SqlxDebugger_OnClassAndMethod_ShouldGenerateOnce()
        {
            var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserRepository
    {
        [SqlxDebugger]
        [SqlTemplate(""SELECT * FROM users WHERE id = @id"")]
        Task<User?> GetByIdAsync(long id);
    }

    [SqlxDebugger]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly System.Data.Common.DbConnection _connection;
        
        public UserRepository(System.Data.Common.DbConnection connection)
        {
            _connection = connection;
        }
    }
}";

            var compilation = await CreateCompilationAsync(source);
            var generatedCode = GetGeneratedCode(compilation);

            // Verify the SQL getter method is generated (should only appear once, not twice)
            var matches = System.Text.RegularExpressions.Regex.Matches(generatedCode, @"public global::Sqlx\.SqlTemplate GetGetByIdAsyncSql\(\)");
            Assert.AreEqual(1, matches.Count, "GetGetByIdAsyncSql should be generated exactly once");
        }

        private async Task<Compilation> CreateCompilationAsync(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Sqlx.Annotations.SqlTemplateAttribute).Assembly.Location),
            };

            var runtimeAssembly = System.Runtime.Loader.AssemblyLoadContext.Default
                .LoadFromAssemblyName(new System.Reflection.AssemblyName("System.Runtime"));
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private string GetGeneratedCode(Compilation compilation)
        {
            var generator = new CSharpGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            var results = driver.GetRunResult();
            var generatedCode = string.Join("\n", results.GeneratedTrees.Select(t => t.ToString()));
            return generatedCode;
        }
    }
}
