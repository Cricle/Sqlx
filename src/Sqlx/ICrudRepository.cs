// -----------------------------------------------------------------------
// <copyright file="ICrudRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// 通用CRUD仓储接口 - 提供标准的增删改查操作
    /// 使用泛型减少重复代码，SQL遵循最佳实践
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型（通常是int、long、Guid等）</typeparam>
    /// <remarks>
    /// 💡 **最佳实践**：
    /// - 所有查询都明确列出列名（不使用SELECT *）
    /// - 使用参数化查询防止SQL注入
    /// - 支持CancellationToken用于取消操作
    /// - 返回受影响的行数便于验证
    /// 
    /// 📖 **使用示例**：
    /// <code>
    /// [RepositoryFor&lt;User&gt;]
    /// public partial interface IUserRepository : ICrudRepository&lt;User, int&gt;
    /// {
    ///     // 继承了所有CRUD方法，可以添加自定义方法
    ///     [Sqlx("SELECT {{columns}} FROM users WHERE email = @email")]
    ///     Task&lt;User?&gt; GetByEmailAsync(string email);
    /// }
    /// </code>
    /// </remarks>
    public interface ICrudRepository<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>
        /// 根据主键获取单个实体
        /// </summary>
        /// <param name="id">主键值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体对象，如果不存在则返回null</returns>
        /// <remarks>
        /// 🎯 **SQL最佳实践**：
        /// - 明确列出所有列名（性能优化）
        /// - 使用主键索引查询（最快）
        /// - 只返回需要的列
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// SELECT id, name, email, created_at, updated_at 
        /// FROM users 
        /// WHERE id = @id
        /// </code>
        /// </remarks>
        [SqlxAttribute("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有实体（分页）
        /// </summary>
        /// <param name="limit">返回的最大行数（默认100，防止一次性加载过多数据）</param>
        /// <param name="offset">跳过的行数（用于分页）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体列表</returns>
        /// <remarks>
        /// ⚠️ **性能提示**：
        /// - 始终使用LIMIT限制返回行数
        /// - 对于大表，建议添加WHERE条件过滤
        /// - 考虑添加ORDER BY确保结果稳定
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// SELECT id, name, email, created_at, updated_at 
        /// FROM users 
        /// ORDER BY id 
        /// LIMIT @limit OFFSET @offset
        /// </code>
        /// </remarks>
        [SqlxAttribute("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit --param limit}} {{offset --param offset}}")]
        Task<List<TEntity>> GetAllAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// 插入新实体
        /// </summary>
        /// <param name="entity">要插入的实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>受影响的行数（成功时为1）</returns>
        /// <remarks>
        /// 🎯 **SQL最佳实践**：
        /// - 明确列出要插入的列名
        /// - 自动排除Id列（假设是自增主键）
        /// - 使用参数化查询
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// INSERT INTO users (name, email, created_at, updated_at) 
        /// VALUES (@name, @email, @created_at, @updated_at)
        /// </code>
        /// </remarks>
        [SqlxAttribute("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据主键更新实体
        /// </summary>
        /// <param name="entity">要更新的实体（必须包含Id）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>受影响的行数（成功时为1，实体不存在时为0）</returns>
        /// <remarks>
        /// 🎯 **SQL最佳实践**：
        /// - 明确列出要更新的列
        /// - 使用主键WHERE条件（最快）
        /// - 排除Id列（主键不应被更新）
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// UPDATE users 
        /// SET name = @name, email = @email, updated_at = @updated_at 
        /// WHERE id = @id
        /// </code>
        /// </remarks>
        [SqlxAttribute("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
        Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <param name="id">要删除的实体主键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>受影响的行数（成功时为1，实体不存在时为0）</returns>
        /// <remarks>
        /// 🎯 **SQL最佳实践**：
        /// - 使用主键索引（最快）
        /// - 参数化查询防止注入
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// DELETE FROM users WHERE id = @id
        /// </code>
        /// 
        /// ⚠️ **注意**：
        /// - 物理删除数据，不可恢复
        /// - 考虑使用软删除（添加is_deleted字段）
        /// </remarks>
        [SqlxAttribute("DELETE FROM {{table}} WHERE id = @id")]
        Task<int> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取实体总数
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>总行数</returns>
        /// <remarks>
        /// 🎯 **SQL最佳实践**：
        /// - COUNT(*)在现代数据库中性能很好
        /// - 对于大表，考虑使用估算值
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// SELECT COUNT(*) FROM users
        /// </code>
        /// </remarks>
        [SqlxAttribute("SELECT COUNT(*) FROM {{table}}")]
        Task<int> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        /// <param name="id">主键值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        /// <remarks>
        /// 🎯 **SQL最佳实践**：
        /// - 使用EXISTS比COUNT(*)更快
        /// - 只检查存在性，不返回数据
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// SELECT CASE WHEN EXISTS(SELECT 1 FROM users WHERE id = @id) 
        ///        THEN 1 ELSE 0 END
        /// </code>
        /// </remarks>
        [SqlxAttribute("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE id = @id) THEN 1 ELSE 0 END")]
        Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量插入实体（高性能）
        /// </summary>
        /// <param name="entities">要插入的实体集合</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>受影响的行数</returns>
        /// <remarks>
        /// 🎯 **性能优化**：
        /// - 一次性插入多行（比循环快10倍以上）
        /// - 使用批量VALUES语法
        /// - 适合初始化数据或导入场景
        /// 
        /// 📝 **生成的SQL示例**：
        /// <code>
        /// INSERT INTO users (name, email, created_at) 
        /// VALUES 
        ///   (@name_0, @email_0, @created_at_0),
        ///   (@name_1, @email_1, @created_at_1),
        ///   (@name_2, @email_2, @created_at_2)
        /// </code>
        /// 
        /// ⚠️ **注意**：
        /// - 注意数据库参数数量限制（通常2100个）
        /// - 大批量时建议分批处理
        /// </remarks>
        [SqlxAttribute("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values --exclude Id}}")]
        [BatchOperation]
        Task<int> BatchInsertAsync(List<TEntity> entities, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 只读仓储接口 - 只包含查询操作
    /// 适用于只需要读取数据的场景
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <remarks>
    /// 💡 **使用场景**：
    /// - 报表查询
    /// - 数据展示
    /// - 只读副本
    /// - 遵循CQRS模式
    /// </remarks>
    public interface IReadOnlyRepository<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>
        /// 根据主键获取单个实体
        /// </summary>
        [SqlxAttribute("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有实体（分页）
        /// </summary>
        [SqlxAttribute("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit --param limit}} {{offset --param offset}}")]
        Task<List<TEntity>> GetAllAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取实体总数
        /// </summary>
        [SqlxAttribute("SELECT COUNT(*) FROM {{table}}")]
        Task<int> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        [SqlxAttribute("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE id = @id) THEN 1 ELSE 0 END")]
        Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    }
}

