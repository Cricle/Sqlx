namespace Sqlx;

using System;

/// <summary>
/// 标记参数为动态 SQL 参数，该参数的值会直接拼接到 SQL 字符串中（非参数化）
/// </summary>
/// <remarks>
/// <para>⚠️ 安全警告：动态 SQL 参数会绕过参数化查询，存在 SQL 注入风险！</para>
/// <para>只在以下场景使用：</para>
/// <list type="bullet">
///   <item>多租户系统（动态表名）</item>
///   <item>分库分表（动态表后缀）</item>
///   <item>动态排序（ORDER BY 子句）</item>
/// </list>
/// <para>使用前必须：</para>
/// <list type="bullet">
///   <item>✅ 验证参数格式（长度、字符集）</item>
///   <item>✅ 使用白名单验证</item>
///   <item>✅ 避免在公共 API 中暴露</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // 示例 1：多租户表名
/// [Sqlx("SELECT {{columns}} FROM {{@tableName}} WHERE id = @id")]
/// Task&lt;User?&gt; GetUserAsync([DynamicSql] string tableName, int id);
/// 
/// // 调用处必须验证
/// var allowedTables = new[] { "users", "admin_users", "guest_users" };
/// if (!allowedTables.Contains(tableName))
///     throw new ArgumentException("Invalid table name");
/// 
/// var user = await repo.GetUserAsync(tableName, userId);
/// 
/// // 示例 2：动态 WHERE 子句
/// [Sqlx("SELECT {{columns}} FROM users WHERE {{@whereClause}}")]
/// Task&lt;List&lt;User&gt;&gt; QueryUsersAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class DynamicSqlAttribute : Attribute
{
    /// <summary>
    /// 动态 SQL 参数的类型
    /// </summary>
    /// <remarks>
    /// 不同类型有不同的验证规则：
    /// <list type="bullet">
    ///   <item><see cref="DynamicSqlType.Identifier"/> - 表名/列名（最严格）</item>
    ///   <item><see cref="DynamicSqlType.Fragment"/> - SQL 片段（中等）</item>
    ///   <item><see cref="DynamicSqlType.TablePart"/> - 表名部分（严格）</item>
    /// </list>
    /// </remarks>
    public DynamicSqlType Type { get; set; } = DynamicSqlType.Identifier;
}

/// <summary>
/// 动态 SQL 参数的类型
/// </summary>
public enum DynamicSqlType
{
    /// <summary>
    /// 标识符（表名、列名）
    /// </summary>
    /// <remarks>
    /// <para>验证规则（最严格）：</para>
    /// <list type="bullet">
    ///   <item>只允许字母、数字、下划线</item>
    ///   <item>必须以字母或下划线开头</item>
    ///   <item>长度限制：1-128 字符</item>
    ///   <item>不能包含 SQL 关键字</item>
    /// </list>
    /// <para>示例：<c>"users"</c>, <c>"tenant1_users"</c>, <c>"user_name"</c></para>
    /// </remarks>
    Identifier = 0,

    /// <summary>
    /// SQL 片段（WHERE、JOIN、ORDER BY 等子句）
    /// </summary>
    /// <remarks>
    /// <para>验证规则（中等）：</para>
    /// <list type="bullet">
    ///   <item>禁止 DDL 操作（DROP、TRUNCATE、ALTER、CREATE）</item>
    ///   <item>禁止危险函数（EXEC、EXECUTE、xp_、sp_executesql）</item>
    ///   <item>禁止注释符号（--, /*）</item>
    ///   <item>长度限制：1-4096 字符</item>
    /// </list>
    /// <para>示例：<c>"age > 18 AND status = 'active'"</c>, <c>"name ASC"</c></para>
    /// </remarks>
    Fragment = 1,

    /// <summary>
    /// 表名部分（前缀、后缀）
    /// </summary>
    /// <remarks>
    /// <para>验证规则（严格）：</para>
    /// <list type="bullet">
    ///   <item>只允许字母和数字</item>
    ///   <item>不允许下划线、空格等特殊字符</item>
    ///   <item>长度限制：1-64 字符</item>
    /// </list>
    /// <para>示例：<c>"2024"</c>, <c>"tenant1"</c>, <c>"shard001"</c></para>
    /// </remarks>
    TablePart = 2
}

