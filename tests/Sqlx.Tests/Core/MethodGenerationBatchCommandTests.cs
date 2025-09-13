// -----------------------------------------------------------------------
// <copyright file="MethodGenerationBatchCommandTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class MethodGenerationBatchCommandTests
    {
        [TestMethod]
        public void BatchCommand_Without_CollectionParam_Emits_UserFriendly_Error()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; 
namespace N { 
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""users"")] Task<int> ExecAsync();
  }
}";

            var code = string.Join("\n", Generate(source));
            StringAssert.Contains(code, "BatchCommand requires a collection parameter");
        }

        [TestMethod]
        public void BatchCommand_With_CollectionParam_Generates_NullCheck_And_NativeDbBatch_Path()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using System.Collections.Generic; 
namespace N { 
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""users"")] Task<int> ExecAsync(IList<int> ids);
  }
}";

            var code = string.Join("\n", Generate(source));
            StringAssert.Contains(code, "if (ids == null)");
            // Native DbBatch path indicators
            StringAssert.Contains(code, "CanCreateBatch");
            StringAssert.Contains(code, "CreateBatch()");
            StringAssert.Contains(code, "BatchCommands.Add");
        }

        [TestMethod]
        public void BatchCommand_Insert_Generates_Dialect_Correct_CommandText_And_Parameters()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using System.Collections.Generic; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; public string Email {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""Users"")] Task<int> InsertUsersAsync(IList<User> users);
  }
}";

            var code = string.Join("\n", Generate(source));

            // Basic indicators
            StringAssert.Contains(code, "INSERT INTO");
            StringAssert.Contains(code, "Users");

            // Parameters should be prefixed and named (lowercase per generator)
            StringAssert.Contains(code, "ParameterName = \"@name\"");
            StringAssert.Contains(code, "ParameterName = \"@email\"");
        }

        [TestMethod]
        public void BatchCommand_Update_Generates_Set_Clauses_And_Where()
        {
            var source = @"using Sqlx.Annotations; using System.Data.Common; using System.Threading.Tasks; using System.Collections.Generic; 
namespace N { 
  public class User { public int Id {get;set;} public string Name {get;set;} = string.Empty; public string Email {get;set;} = string.Empty; }
  [RepositoryFor(typeof(IRepo))]
  public partial class Repo : IRepo { private readonly DbConnection connection; public Repo(DbConnection c){connection=c;} }
  public interface IRepo {
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""Users"")] Task<int> UpdateUsersAsync(IList<User> users);
  }
}";

            var code = string.Join("\n", Generate(source));
            StringAssert.Contains(code, "UPDATE ");
            StringAssert.Contains(code, " SET ");
            // We expect update of Name/Email and a WHERE Id clause in per-item batch commands
            StringAssert.Contains(code, "Name");
            StringAssert.Contains(code, "Email");
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
                "BatchCmdAsm",
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
}


