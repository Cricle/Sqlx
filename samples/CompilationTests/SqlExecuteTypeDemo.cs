// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypeDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.CompilationTests;

using Sqlx;
using Sqlx.Annotations;
using System;
using System.Collections.Generic;
using System.Data.Common;
using static System.Console;

/// <summary>
/// 演示SqlExecuteTypeAttribute的各种用法，展示如何通过属性指定CRUD操作类型。
/// 这是ExpressionToSql的扩展功能，通过SqlExecuteTypeAttribute来自动生成对应的SQL语句。
/// </summary>
internal static class SqlExecuteTypeDemo
{
    /// <summary>
    /// 演示各种SQL操作类型的用法。
    /// </summary>
    public static void DemoAllOperations()
    {
        WriteLine("=== SqlExecuteTypeAttribute CRUD 操作演示 ===");

        // 这里只演示语法，实际使用时需要有数据库连接
        WriteLine("1. SELECT 操作 - 查询人员信息");
        WriteLine("   [SqlExecuteType(SqlExecuteTypes.Select, \"person\")]");
        WriteLine("   生成的SQL: SELECT person_id AS PersonId, person_name AS PersonName FROM [person]");

        WriteLine("\n2. INSERT 操作 - 插入新人员");
        WriteLine("   [SqlExecuteType(SqlExecuteTypes.Insert, \"person\")]");
        WriteLine("   生成的SQL: INSERT INTO [person](person_id, person_name) VALUES (@person_id, @person_name)");

        WriteLine("\n3. UPDATE 操作 - 更新人员信息");
        WriteLine("   [SqlExecuteType(SqlExecuteTypes.Update, \"person\")]");
        WriteLine("   生成的SQL: UPDATE [person] SET person_id = @person_id, person_name = @person_name");

        WriteLine("\n4. DELETE 操作 - 删除人员");
        WriteLine("   [SqlExecuteType(SqlExecuteTypes.Delete, \"person\")]");
        WriteLine("   生成的SQL: DELETE FROM [person]");

        WriteLine("\n5. 与ExpressionToSql结合使用");
        WriteLine("   ExpressionToSql负责动态查询，SqlExecuteType负责基础CRUD");
    }

    /// <summary>
    /// 演示与ExpressionToSql的区别和互补关系。
    /// </summary>
    public static void DemoComplementaryUsage()
    {
        WriteLine("\n=== SqlExecuteType vs ExpressionToSql 对比 ===");

        WriteLine("ExpressionToSql 用途:");
        WriteLine("- 动态查询条件 (WHERE子句)");
        WriteLine("- 复杂的过滤和排序");
        WriteLine("- 运行时构建的查询");
        WriteLine("- 灵活的查询组合");

        WriteLine("\nSqlExecuteType 用途:");
        WriteLine("- 基础CRUD操作模板");
        WriteLine("- 编译时确定的SQL结构");
        WriteLine("- 简单的增删改查");
        WriteLine("- 基于实体对象的操作");

        WriteLine("\n最佳实践:");
        WriteLine("- 简单CRUD → 使用SqlExecuteType");
        WriteLine("- 复杂查询 → 使用ExpressionToSql");
        WriteLine("- 混合场景 → 两者结合使用");
    }
}

/// <summary>
/// 演示SqlExecuteTypeAttribute的实际使用方法。
/// 每个方法展示不同的CRUD操作。
/// </summary>
internal partial class CrudOperationsManager
{
    private readonly DbConnection connection;

    public CrudOperationsManager(DbConnection connection)
    {
        this.connection = connection;
    }

    /// <summary>
    /// SELECT操作 - 通过SqlExecuteType自动生成SELECT语句。
    /// 生成的SQL: SELECT person_id AS PersonId, person_name AS PersonName FROM [person]
    /// </summary>
    /// <param name="filter">用于过滤的对象</param>
    /// <returns>查询结果列表</returns>
    [SqlExecuteType(SqlExecuteTypes.Select, "person")]
    public partial IList<PersonInformation> GetPersons(PersonInformation filter);

    /// <summary>
    /// INSERT操作 - 通过SqlExecuteType自动生成INSERT语句。
    /// 生成的SQL: INSERT INTO [person](person_id, person_name) VALUES (@person_id, @person_name)
    /// </summary>
    /// <param name="person">要插入的人员信息</param>
    /// <returns>插入操作的影响行数</returns>
    [SqlExecuteType(SqlExecuteTypes.Insert, "person")]
    public partial int InsertPerson(PersonInformation person);

    /// <summary>
    /// UPDATE操作 - 通过SqlExecuteType自动生成UPDATE语句。
    /// 生成的SQL: UPDATE [person] SET person_id = @person_id, person_name = @person_name
    /// </summary>
    /// <param name="person">要更新的人员信息</param>
    /// <returns>更新操作的影响行数</returns>
    [SqlExecuteType(SqlExecuteTypes.Update, "person")]
    public partial int UpdatePerson(PersonInformation person);

    /// <summary>
    /// DELETE操作 - 通过SqlExecuteType自动生成DELETE语句。
    /// 生成的SQL: DELETE FROM [person]
    /// </summary>
    /// <param name="person">要删除的人员信息（可能只用ID）</param>
    /// <returns>删除操作的影响行数</returns>
    [SqlExecuteType(SqlExecuteTypes.Delete, "person")]
    public partial int DeletePerson(PersonInformation person);

    /// <summary>
    /// 混合使用示例 - 结合ExpressionToSql进行动态查询。
    /// ExpressionToSql负责复杂的查询逻辑，SqlExecuteType负责基础结构。
    /// </summary>
    /// <param name="dynamicQuery">动态构建的查询表达式</param>
    /// <returns>查询结果</returns>
    public IList<PersonInformation> GetPersonsWithDynamicQuery(
        [ExpressionToSql] ExpressionToSql<PersonInformation> dynamicQuery)
    {
        throw new NotImplementedException("ExpressionToSql feature is not yet implemented in the source generator.");
    }

    /// <summary>
    /// 传统方式对比 - 使用RawSql手动编写SQL。
    /// </summary>
    /// <param name="minId">最小ID值</param>
    /// <returns>查询结果</returns>
    [RawSql("SELECT person_id AS PersonId, person_name AS PersonName FROM person WHERE person_id > @minId")]
    public partial IList<PersonInformation> GetPersonsTraditionalWay(int minId);
}

/// <summary>
/// 扩展方法演示 - 为不同的数据库连接类型提供CRUD操作支持。
/// </summary>
internal static partial class CrudExtensions
{
    /// <summary>
    /// SELECT扩展方法 - 为DbConnection添加类型安全的查询支持。
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Select, "person")]
    public static partial IList<PersonInformation> SelectPersons(
        this DbConnection connection,
        PersonInformation filter);

    /// <summary>
    /// INSERT扩展方法 - 为DbConnection添加插入支持。
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Insert, "person")]
    public static partial int InsertPerson(
        this DbConnection connection,
        PersonInformation person);

    /// <summary>
    /// UPDATE扩展方法 - 为DbConnection添加更新支持。
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Update, "person")]
    public static partial int UpdatePerson(
        this DbConnection connection,
        PersonInformation person);

    /// <summary>
    /// DELETE扩展方法 - 为DbConnection添加删除支持。
    /// </summary>
    [SqlExecuteType(SqlExecuteTypes.Delete, "person")]
    public static partial int DeletePerson(
        this DbConnection connection,
        PersonInformation person);

    /// <summary>
    /// 组合使用示例 - ExpressionToSql + SqlExecuteType的最佳实践。
    /// </summary>
    public static IList<PersonInformation> QueryWithExpressionAndType(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> query)
    {
        throw new NotImplementedException("ExpressionToSql feature is not yet implemented in the source generator.");
    }
}
