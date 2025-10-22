using Microsoft.Data.Sqlite;
using Sqlx.Annotations;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Repositories;

/// <summary>
/// 用户Repository实现 - 默认配置（启用追踪和指标）
/// 用于测试带有完整追踪的性能
/// </summary>
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
[EnableTracing(true)]  // 启用Activity追踪
[EnableMetrics(true)]  // 启用性能指标
public partial class UserRepositoryWithTracing(SqliteConnection connection) : IUserRepository
{
    // 所有接口方法实现由Sqlx源生成器在编译时自动生成
    // 生成的代码包含Activity追踪和Stopwatch计时
}

/// <summary>
/// 用户Repository实现 - 高性能配置（禁用追踪和指标）
/// 用于测试零开销的极致性能（默认使用硬编码索引）
/// </summary>
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
[EnableTracing(false)]  // 禁用Activity追踪
[EnableMetrics(false)]  // 禁用性能指标
public partial class UserRepositoryNoTracing(SqliteConnection connection) : IUserRepository
{
    // 所有接口方法实现由Sqlx源生成器在编译时自动生成
    // 生成的代码不含Activity追踪和Stopwatch计时（极致性能）
    // 默认使用硬编码索引访问（reader.GetInt32(0)等）
}

/// <summary>
/// 用户Repository实现 - 只启用指标（不含Activity追踪）
/// 用于测试只有Stopwatch计时的性能影响
/// </summary>
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
[EnableTracing(false)]  // 禁用Activity追踪
[EnableMetrics(true)]   // 启用性能指标
public partial class UserRepositoryMetricsOnly(SqliteConnection connection) : IUserRepository
{
    // 生成的代码只包含Stopwatch计时，不含Activity追踪
}
