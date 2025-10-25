using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests.CollectionSupport;

public class TestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; } = true;
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITestUserRepository))]
public partial class TestUserRepository(IDbConnection connection) : ITestUserRepository { }

public interface ITestUserRepository
{
    [SqlTemplate("INSERT INTO users (name, email, age, is_active) VALUES {{values @users}}")]
    [BatchOperation(MaxBatchSize = 100)]
    Task<int> BatchInsertAsync(IEnumerable<TestUser> users);
    
    [SqlTemplate("SELECT * FROM users ORDER BY id")]
    Task<List<TestUser>> GetAllAsync();
}

