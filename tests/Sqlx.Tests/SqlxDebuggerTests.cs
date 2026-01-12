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

            // Verify the SQL getter method is generated
            StringAssert.Contains(generatedCode, "GetGetByIdAsyncSql");
            StringAssert.Contains(generatedCode, "public string GetGetByIdAsyncSql");
            StringAssert.Contains(generatedCode, "long id");
            StringAssert.Contains(generatedCode, "global::Sqlx.Generator.SqlDefine? dialect = null");
        }

        [TestMethod]
        public async Task SqlxDebugger_WithMultipleParameters_ShouldIncludeAllParameters()
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

            // Verify method signature includes all parameters except CancellationToken
            StringAssert.Contains(generatedCode, "GetGetUsersByAgeAsyncSql");
            StringAssert.Contains(generatedCode, "int minAge");
            StringAssert.Contains(generatedCode, "string? orderBy = default");
            StringAssert.Contains(generatedCode, "int? limit = default");
            
            // Verify CancellationToken is NOT included
            Assert.IsFalse(generatedCode.Contains("GetGetUsersByAgeAsyncSql(int minAge, string? orderBy = default, int? limit = default, CancellationToken"));
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
        public async Task SqlxDebugger_WithDialectParameter_ShouldAllowDialectSelection()
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

            // Verify dialect parameter is included
            StringAssert.Contains(generatedCode, "global::Sqlx.Generator.SqlDefine? dialect = null");
            StringAssert.Contains(generatedCode, "var actualDialect = dialect ?? global::Sqlx.Generator.SqlDefine.PostgreSql");
        }

        [TestMethod]
        public async Task SqlxDebugger_WithPlaceholders_ShouldProcessTemplateCorrectly()
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

            // Verify SQL debugger method is generated with dialect-specific SQL
            StringAssert.Contains(generatedCode, "GetGetByIdAsyncSql");
            StringAssert.Contains(generatedCode, "global::Sqlx.Generator.SqlDefine.SQLite");
            StringAssert.Contains(generatedCode, "global::Sqlx.Generator.SqlDefine.PostgreSql");
            StringAssert.Contains(generatedCode, "global::Sqlx.Generator.SqlDefine.MySql");
            StringAssert.Contains(generatedCode, "global::Sqlx.Generator.SqlDefine.SqlServer");
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
            StringAssert.Contains(generatedCode, "/// Gets the processed SQL template for");
            StringAssert.Contains(generatedCode, "/// <param name=\"id\">");
            StringAssert.Contains(generatedCode, "/// <param name=\"dialect\">");
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
            StringAssert.Contains(generatedCode, "GetGetByIdAsyncSql");
            StringAssert.Contains(generatedCode, "GetInsertAsyncSql");
            StringAssert.Contains(generatedCode, "GetDeleteAsyncSql");
            
            // Verify they all have the dialect parameter
            var getByIdCount = System.Text.RegularExpressions.Regex.Matches(generatedCode, "GetGetByIdAsyncSql").Count;
            var insertCount = System.Text.RegularExpressions.Regex.Matches(generatedCode, "GetInsertAsyncSql").Count;
            var deleteCount = System.Text.RegularExpressions.Regex.Matches(generatedCode, "GetDeleteAsyncSql").Count;
            
            Assert.IsTrue(getByIdCount >= 1, "GetGetByIdAsyncSql should be generated");
            Assert.IsTrue(insertCount >= 1, "GetInsertAsyncSql should be generated");
            Assert.IsTrue(deleteCount >= 1, "GetDeleteAsyncSql should be generated");
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
            var matches = System.Text.RegularExpressions.Regex.Matches(generatedCode, @"public string GetGetByIdAsyncSql\(");
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
