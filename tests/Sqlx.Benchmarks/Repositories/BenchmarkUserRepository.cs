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
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge LIMIT @limit")]
    Task<List<BenchmarkUser>> GetByMinAgeAsync(int minAge, int limit, CancellationToken cancellationToken = default);

    /// <summary>Gets users by minimum age (sync).</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge LIMIT @limit")]
    List<BenchmarkUser> GetByMinAge(int minAge, int limit);
}

/// <summary>
/// Benchmark repository implementation using source generator.
/// All interface methods are auto-generated.
/// </summary>
[TableName("users")]
[RepositoryFor(typeof(IBenchmarkUserRepository))]
public partial class BenchmarkUserRepository : IBenchmarkUserRepository
{
    private readonly SqliteConnection _connection;
    public System.Data.Common.DbTransaction? Transaction { get; set; }

    public BenchmarkUserRepository(SqliteConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    /// <summary>
    /// Returns an IQueryable for building complex LINQ queries.
    /// Implements ICrudRepository.AsQueryable().
    /// </summary>
    public IQueryable<BenchmarkUser> AsQueryable()
    {
        return SqlQuery<BenchmarkUser>.For(Dialect).WithConnection(_connection);
    }
}
