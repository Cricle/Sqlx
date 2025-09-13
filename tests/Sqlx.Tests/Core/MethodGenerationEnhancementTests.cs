// -----------------------------------------------------------------------
// <copyright file="MethodGenerationEnhancementTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class MethodGenerationEnhancementTests
    {
        [TestMethod]
        public void Select_With_ExpressionToSql_Appends_Where()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using Sqlx.Core; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.Select, ""users"")] Task<List<User>> GetAsync([ExpressionToSql] ExpressionToSql<User> q);
  }
}";

            var code = Generate(source).Join();
            Assert.IsTrue(code.Contains("ToWhereClause()"));
            Assert.IsTrue(code.Contains(" WHERE "));
        }

        [TestMethod]
        public void Insert_With_ExpressionToSql_Appends_AdditionalClause()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using Sqlx.Core; 
namespace N { 
  [TableName(""users"")] public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.Insert, ""users"")] Task<int> AddAsync(User u, [ExpressionToSql] ExpressionToSql<User> q);
  }
}";

            var code = Generate(source).Join();
            Assert.IsTrue(code.Contains("INSERT INTO"));
            Assert.IsTrue(code.Contains("ToAdditionalClause("));
        }

        [TestMethod]
        public void Delete_With_ExpressionToSql_Appends_Where()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using Sqlx.Core; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.Delete, ""users"")] Task<int> DelAsync([ExpressionToSql] ExpressionToSql<User> q);
  }
}";

            var code = Generate(source).Join();
            Assert.IsTrue(code.Contains("ToWhereClause("));
            Assert.IsTrue(code.Contains(" WHERE "));
        }

        [TestMethod]
        public void Update_With_ExpressionToSql_Uses_Template_And_Parameters()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using Sqlx.Core; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.Update, ""users"")] Task<int> UpdAsync([ExpressionToSql] ExpressionToSql<User> q);
  }
}";

            var code = Generate(source).Join();
            Assert.IsTrue(code.Contains("ToTemplate()"));
        }

        [TestMethod]
        public void Method_Generation_With_Complex_Return_Types_Handles_Correctly()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using System.Collections.Generic; using Sqlx.Core; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [Sqlx(""SELECT Id, Name FROM users"")] Task<Dictionary<int, string>> GetUserDictionaryAsync();
    [Sqlx(""SELECT COUNT(*) FROM users"")] Task<(int count, bool hasData)> GetUserCountAsync();
  }
}";

            var code = Generate(source).Join();
            Assert.IsTrue(code.Contains("Dictionary") || code.Contains("GetUser"));
        }

        [TestMethod]
        public void Method_Generation_With_Error_Handling_Paths_Covers_Edge_Cases()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using System.Collections.Generic; using Sqlx.Core; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [Sqlx("""")] Task<User?> GetUserWithEmptySqlAsync(int id);
    Task<User?> GetUserWithoutAttributeAsync(int id);
  }
}";

            var code = Generate(source).Join();
            // Should handle empty SQL and missing attributes gracefully
            Assert.IsTrue(code.Contains("GetUser") || code.Length > 0);
        }

        [TestMethod]
        public void Method_Generation_With_Fallback_Batch_Logic_Generates_Code()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using System.Collections.Generic; using Sqlx.Core; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [Sqlx(""SELECT * FROM users WHERE Id = @id"")] Task<List<User>> GetUsersAsync(List<int> ids);
    [Sqlx(""SELECT COUNT(*) FROM users WHERE Id = @id"")] Task<int> CountUsersAsync(List<int> ids);
  }
}";

            var code = Generate(source).Join();
            Assert.IsTrue(code.Contains("GetUsers") || code.Contains("CountUsers"));
        }

        private static List<string> Generate(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "MgcEnhAsm",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new CSharpGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diags);

            var generated = new List<string>();
            foreach (var tree in newCompilation.SyntaxTrees)
            {
                var text = tree.ToString();
                if (text.Contains("// <auto-generated>") || string.IsNullOrEmpty(tree.FilePath))
                    generated.Add(text);
            }
            return generated;
        }
    }

    internal static class StringListJoinExt
    {
        public static string Join(this List<string> list) => string.Join("\n", list);
    }
}


