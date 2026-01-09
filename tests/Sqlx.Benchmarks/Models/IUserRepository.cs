using Sqlx.Annotations;
using System.Data;

namespace Sqlx.Benchmarks.Models;

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository
{
}

public interface IUserRepository
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM users WHERE age > @minAge LIMIT @limit")]
    Task<List<User>> GetByAgeAsync(int minAge, int limit);

    [SqlTemplate("INSERT INTO users (name, email, age, is_active) VALUES (@name, @email, @age, @is_active)")]
    Task<int> InsertAsync(string name, string email, int age, bool is_active);

    [SqlTemplate("INSERT INTO users (name, email, age, is_active) VALUES {{batch_values @users}}")]
    [BatchOperation(MaxBatchSize = 100)]
    Task<int> BatchInsertAsync(IEnumerable<User> users);

    [SqlTemplate("UPDATE users SET name = @name WHERE id = @id")]
    Task<int> UpdateAsync(long id, string name);

    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteAsync(long id);
}

