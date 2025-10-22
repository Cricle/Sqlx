using Microsoft.Data.Sqlite;
using Sqlx.Annotations;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Repositories;

/// <summary>
/// 用户Repository实现 - 由Sqlx源生成器自动生成方法实现
/// </summary>
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(SqliteConnection connection) : IUserRepository
{
    // 所有接口方法实现由Sqlx源生成器在编译时自动生成
    // 生成的代码将在 UserRepository.Repository.g.cs 文件中
}

