using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Threading;

namespace Sqlx.Tests;

[TestClass]
public class TableNameQueryTranslationTests
{
    [Sqlx, TableName("app_users")]
    public partial class TableNamedUser
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Sqlx, TableName("sales_orders")]
    public partial class TableNamedOrder
    {
        public int Id { get; set; }

        public int UserId { get; set; }
    }

    [Sqlx, TableName("fallback_users", Method = nameof(GetTableName))]
    public partial class RuntimeTableNamedUser
    {
        public int Id { get; set; }

        public static string GetTableName() => "runtime_users";
    }

    [Sqlx, TableName("fallback_missing_method_users", Method = "MissingMethod")]
    public partial class MissingDynamicMethodUser
    {
        public int Id { get; set; }
    }

    [Sqlx, TableName("fallback_empty_method_users", Method = nameof(GetTableName))]
    public partial class EmptyDynamicMethodUser
    {
        public int Id { get; set; }

        public static string GetTableName() => "   ";
    }

    [Sqlx, TableName("fallback_sequence_users", Method = nameof(GetTableName))]
    public partial class SequenceDynamicMethodUser
    {
        private static int _counter;

        public int Id { get; set; }

        public static void ResetCounter() => _counter = 0;

        public static string GetTableName() => $"runtime_sequence_users_{Interlocked.Increment(ref _counter)}";
    }

    [TestMethod]
    public void SqlQuery_ToSql_UsesTableNameAttribute()
    {
        var sql = SqlQuery<TableNamedUser>.ForSqlite().ToSql();

        Assert.IsTrue(sql.Contains("FROM [app_users]"), sql);
        Assert.IsFalse(sql.Contains("FROM [TableNamedUser]"), sql);
    }

    [TestMethod]
    public void SqlQuery_ToSql_UsesDynamicTableNameMethodWhenAvailable()
    {
        var sql = SqlQuery<RuntimeTableNamedUser>.ForSqlite().ToSql();

        Assert.IsTrue(sql.Contains("FROM [runtime_users]"), sql);
        Assert.IsFalse(sql.Contains("FROM [fallback_users]"), sql);
    }

    [TestMethod]
    public void SqlQuery_ToSql_WithMissingDynamicTableMethod_FallsBackToStaticTableName()
    {
        var sql = SqlQuery<MissingDynamicMethodUser>.ForSqlite().ToSql();

        Assert.IsTrue(sql.Contains("FROM [fallback_missing_method_users]"), sql);
    }

    [TestMethod]
    public void SqlQuery_ToSql_WithWhitespaceDynamicTableName_FallsBackToStaticTableName()
    {
        var sql = SqlQuery<EmptyDynamicMethodUser>.ForSqlite().ToSql();

        Assert.IsTrue(sql.Contains("FROM [fallback_empty_method_users]"), sql);
    }

    [TestMethod]
    public void SqlQuery_ToSql_WithDynamicMethod_EvaluatesOnEachCall()
    {
        SequenceDynamicMethodUser.ResetCounter();

        var firstSql = SqlQuery<SequenceDynamicMethodUser>.ForSqlite().ToSql();
        var secondSql = SqlQuery<SequenceDynamicMethodUser>.ForSqlite().ToSql();

        Assert.IsTrue(firstSql.Contains("FROM [runtime_sequence_users_"), firstSql);
        Assert.IsTrue(secondSql.Contains("FROM [runtime_sequence_users_"), secondSql);
        Assert.AreNotEqual(firstSql, secondSql, "Dynamic table name method should be evaluated on each query translation.");
    }

    [TestMethod]
    public void SqlQuery_Join_UsesTableNameAttributesForBothSides()
    {
        var sql = SqlQuery<TableNamedUser>.ForSqlite()
            .Join(
                SqlQuery<TableNamedOrder>.ForSqlite(),
                user => user.Id,
                order => order.UserId,
                (user, order) => new { user.Name, order.Id })
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM [app_users] AS [t1]"), sql);
        Assert.IsTrue(sql.Contains("JOIN [sales_orders] AS [t2]"), sql);
    }

    [TestMethod]
    public void SqlQuery_SubQuery_UsesInnerTableNameAttribute()
    {
        var sql = SqlQuery<TableNamedUser>.ForSqlite()
            .Select(user => new
            {
                user.Name,
                OrderCount = SubQuery.For<TableNamedOrder>().Where(order => order.UserId == user.Id).Count(),
            })
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM [sales_orders]"), sql);
        Assert.IsFalse(sql.Contains("FROM [TableNamedOrder]"), sql);
    }
}
