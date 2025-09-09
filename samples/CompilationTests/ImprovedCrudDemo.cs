// -----------------------------------------------------------------------
// <copyright file="ImprovedCrudDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.CompilationTests;

/// <summary>
/// 改进的 CRUD 操作演示 - 基于用户反馈的正确参数设计。
/// </summary>
internal static partial class ImprovedCrudDemo
{
    // ===== SELECT 操作 =====
    
    /// <summary>
    /// SELECT 操作 - 使用 ExpressionToSql 作为查询条件。
    /// 生成: SELECT * FROM person WHERE {expression}
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Select, "person")]
    public static partial IList<PersonInformation> SelectWithCondition(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> whereCondition);

    /// <summary>
    /// SELECT 操作 - 简单查询所有记录。
    /// 生成: SELECT * FROM person
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Select, "person")]
    public static partial IList<PersonInformation> SelectAll(
        this DbConnection connection);

    // ===== INSERT 操作 =====

    /// <summary>
    /// INSERT 操作 - 插入单个实体。
    /// 生成: INSERT INTO person(...) VALUES (...)
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Insert, "person")]
    public static partial int InsertSingle(
        this DbConnection connection,
        PersonInformation person);

    /// <summary>
    /// INSERT 操作 - 使用 ExpressionToSql 插入。
    /// ExpressionToSql 可以生成完整的 INSERT 语句
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Insert, "person")]
    public static partial int InsertWithExpression(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> insertExpression);

    /// <summary>
    /// INSERT 操作 - 批量插入。
    /// 生成: INSERT INTO person(...) VALUES (...), (...), ...
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Insert, "person")]
    public static partial int InsertBatch(
        this DbConnection connection,
        IEnumerable<PersonInformation> persons);

    /// <summary>
    /// INSERT 操作 - 组合方式：实体 + 表达式补充。
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Insert, "person")]
    public static partial int InsertWithCondition(
        this DbConnection connection,
        PersonInformation person,
        [ExpressionToSql] ExpressionToSql<PersonInformation> additionalClause);

    // ===== UPDATE 操作 =====

    /// <summary>
    /// UPDATE 操作 - 使用 ExpressionToSql 处理整个 UPDATE 语句。
    /// ExpressionToSql 可以包含 SET 子句和 WHERE 子句
    /// 生成: ExpressionToSql.ToTemplate().Sql (完整的 UPDATE 语句)
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "person")]
    public static partial int UpdateWithExpression(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> updateExpression);

    /// <summary>
    /// UPDATE 操作 - 简单实体更新 (需要主键)。
    /// 生成: UPDATE person SET ... WHERE id = @id
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "person")]
    public static partial int UpdateEntity(
        this DbConnection connection,
        PersonInformation person);

    // ===== DELETE 操作 =====

    /// <summary>
    /// DELETE 操作 - 使用 ExpressionToSql 作为 WHERE 条件。
    /// 生成: DELETE FROM person WHERE {expression}
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Delete, "person")]
    public static partial int DeleteWithCondition(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> whereCondition);

    /// <summary>
    /// DELETE 操作 - 危险的全表删除 (通常不推荐)。
    /// 明确使用 Sqlx 属性以表达删除全表的意图
    /// </summary>
    [Sqlx("DELETE FROM person")]
    public static partial int DeleteAll(
        this DbConnection connection);

    // ===== 组合使用示例 =====

    /// <summary>
    /// 复杂查询 - 带分页和排序。
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Select, "person")]
    public static partial IList<PersonInformation> SelectWithPaging(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件更新 - 带事务支持。
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "person")]
    public static partial int UpdateWithTransaction(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> updateQuery,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 演示 ExpressionToSql 的典型用法模式。
/// </summary>
public static class ExpressionToSqlUsageExamples
{
    /// <summary>
    /// 演示如何使用改进的 CRUD 操作。
    /// </summary>
    public static Task DemonstrateCrudUsage(DbConnection connection)
    {
        // SELECT 示例
        var selectCondition = ExpressionToSql<PersonInformation>.Create()
            .Where(p => p.PersonId > 100)
            .And(p => p.PersonName!.Contains("John"));
        
        var persons = connection.SelectWithCondition(selectCondition);

        // INSERT 示例 - 单个实体
        var newPerson = new PersonInformation { PersonName = "Alice" };
        var insertedCount = connection.InsertSingle(newPerson);

        // INSERT 示例 - 使用表达式
        var insertExpression = ExpressionToSql<PersonInformation>.Create()
            .Insert(p => new { p.PersonName, p.PersonId })
            .Values(("Bob", 200));
        
        connection.InsertWithExpression(insertExpression);

        // UPDATE 示例 - 使用表达式
        var updateExpression = ExpressionToSql<PersonInformation>.Create()
            .Set(p => p.PersonName, "Updated Name")
            .Where(p => p.PersonId == 100);
        
        connection.UpdateWithExpression(updateExpression);

        // DELETE 示例
        var deleteCondition = ExpressionToSql<PersonInformation>.Create()
            .Where(p => p.PersonId < 50);
        
        connection.DeleteWithCondition(deleteCondition);
        
        return Task.CompletedTask;
    }
}


