using Microsoft.Data.Sqlite;
using Sqlx.Annotations;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Repositories;

/// <summary>
/// 用户Repository实现 - Sqlx源生成器
/// 强制启用Activity追踪和Stopwatch计时（性能影响微小<0.1μs）
/// 提供完整的分布式追踪和性能监控能力
/// </summary>
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(SqliteConnection connection) : IUserRepository
{
    // 所有接口方法实现由Sqlx源生成器在编译时自动生成
    // 生成的代码包含：
    // - Activity追踪（OpenTelemetry兼容）
    // - Stopwatch计时（精确性能指标）
    // - 硬编码索引访问（极致性能，reader.GetInt32(0)等）
    // - 智能IsDBNull检查（只对nullable类型检查）
    // - Command自动释放（finally块）
}
