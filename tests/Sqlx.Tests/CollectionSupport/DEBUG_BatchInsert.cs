using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CollectionSupport;

[TestClass]
public class DEBUG_BatchInsert : CodeGenerationTestBase
{
    [TestMethod]
    public void Debug_BatchInsert_Generated_Code()
    {
        var source = @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{values @entities}}"")]
    [BatchOperation(MaxBatchSize = 500)]
    Task<int> BatchInsertAsync(IEnumerable<User> entities);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        var batchInsertMethodIndex = generatedCode.IndexOf("BatchInsertAsync");
        if (batchInsertMethodIndex >= 0)
        {
            var methodSection = generatedCode.Substring(batchInsertMethodIndex, Math.Min(3000, generatedCode.Length - batchInsertMethodIndex));
            Console.WriteLine("=== BATCH INSERT METHOD ===");
            Console.WriteLine(methodSection);
            Console.WriteLine("=== END ===");
        }
        else
        {
            Console.WriteLine("Method not found!");
            Console.WriteLine("Full code:");
            Console.WriteLine(generatedCode);
        }
    }
}

