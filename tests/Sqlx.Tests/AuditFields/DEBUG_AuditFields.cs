using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.AuditFields;

[TestClass]
public class DEBUG_AuditFields : CodeGenerationTestBase
{
    [TestMethod]
    public void Debug_INSERT()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 帮助推断实体类型
    
    [SqlTemplate(""INSERT INTO {{table}} (name) VALUES (@name)"")]
    Task<int> InsertAsync(string name);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // Search for DEBUG comments
        var debugIndex = generatedCode.IndexOf("// DEBUG AuditFields:");
        if (debugIndex >= 0)
        {
            var debugSection = generatedCode.Substring(debugIndex, Math.Min(1000, generatedCode.Length - debugIndex));
            Console.WriteLine("=== DEBUG OUTPUT ===");
            Console.WriteLine(debugSection);
            Console.WriteLine("=== END DEBUG ===");
        }
        else
        {
            Console.WriteLine("⚠️ No DEBUG comments found!");
        }
        
        var insertMethodIndex = generatedCode.IndexOf("public System.Threading.Tasks.Task<int> InsertAsync");
        if (insertMethodIndex >= 0)
        {
            var commandTextIndex = generatedCode.IndexOf("CommandText =", insertMethodIndex);
            if (commandTextIndex > 0)
            {
                var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));
                Console.WriteLine("\n=== INSERT SQL ===");
                Console.WriteLine(sqlPart);
                Console.WriteLine("=== END ===");
            }
        }
    }

    [TestMethod]
    public void Debug_UPDATE()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 帮助推断实体类型
    
    [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
    Task<int> UpdateAsync(long id, string name);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        var updateMethodIndex = generatedCode.IndexOf("public System.Threading.Tasks.Task<int> UpdateAsync");
        if (updateMethodIndex >= 0)
        {
            var commandTextIndex = generatedCode.IndexOf("CommandText =", updateMethodIndex);
            if (commandTextIndex > 0)
            {
                var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));
                Console.WriteLine("=== UPDATE SQL ===");
                Console.WriteLine(sqlPart);
                Console.WriteLine("=== END ===");
            }
        }
    }
}

