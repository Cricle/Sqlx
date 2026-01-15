using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Repositories;

/// <summary>
/// Benchmark repository interface - combines ICrudRepository with custom query methods.
/// All methods are implemented by source generator.
/// </summary>
public interface IBenchmarkUserRepository : ICrudRepository<BenchmarkUser, long>
{
    // Inherited from ICrudRepository<BenchmarkUser, long>:
    // - GetByIdAsync(id)
    // - GetAllAsync(limit)
    // - GetPagedAsync(pageSize, offset)
    // - InsertAsync(entity)
    // - UpdateAsync(entity)
    // - DeleteAsync(id)
    // - CountAsync()
    // - ExistsAsync(predicate)
    // - BatchInsertAsync(entities)

    /// <summary>Gets users by minimum age.</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<BenchmarkUser>> GetByMinAgeAsync(int minAge, CancellationToken cancellationToken = default);
}

/// <summary>
/// Benchmark repository implementation using source generator.
/// All interface methods are auto-generated.
/// </summary>
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBenchmarkUserRepository))]
public partial class BenchmarkUserRepository(SqliteConnection connection) : IBenchmarkUserRepository
{
    private readonly SqliteConnection _connection = connection;
    public System.Data.Common.DbTransaction? Transaction { get; set; }


}
