using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests;

[TestClass]
public class ColumnAttributeQueryTranslationTests
{
    [Sqlx]
    public partial class ColumnMappedEntity
    {
        public int Id { get; set; }

        [Column("custom_column_name")]
        public string CustomName { get; set; } = string.Empty;
    }

    [TestMethod]
    public void SqlQuery_Where_UsesColumnAttributeName()
    {
        var sql = SqlQuery<ColumnMappedEntity>.ForSqlite()
            .Where(entity => entity.CustomName == "Alice")
            .ToSql();

        Assert.IsTrue(sql.Contains("[custom_column_name]"), sql);
        Assert.IsFalse(sql.Contains("[custom_name]"), sql);
    }

    [TestMethod]
    public void SqlQuery_SelectAndOrderBy_UsesColumnAttributeName()
    {
        var sql = SqlQuery<ColumnMappedEntity>.ForSqlite()
            .Select(entity => new { entity.CustomName })
            .OrderBy(entity => entity.CustomName)
            .ToSql();

        Assert.IsTrue(sql.Contains("[custom_column_name]"), sql);
        Assert.IsFalse(sql.Contains("[custom_name]"), sql);
    }

    [TestMethod]
    public void ExpressionExtensions_ToWhereClause_UsesColumnAttributeName()
    {
        System.Linq.Expressions.Expression<System.Func<ColumnMappedEntity, bool>> predicate =
            entity => entity.CustomName == "Alice";

        var sql = predicate.ToWhereClause(SqlDefine.SQLite);

        Assert.IsTrue(sql.Contains("[custom_column_name]"), sql);
        Assert.IsFalse(sql.Contains("[custom_name]"), sql);
    }

    [TestMethod]
    public void SetExpressionExtensions_ToSetClause_UsesColumnAttributeName()
    {
        System.Linq.Expressions.Expression<System.Func<ColumnMappedEntity, ColumnMappedEntity>> update =
            entity => new ColumnMappedEntity { CustomName = "Updated" };

        var sql = update.ToSetClause(SqlDefine.SQLite);

        Assert.IsTrue(sql.Contains("[custom_column_name]"), sql);
        Assert.IsFalse(sql.Contains("[custom_name]"), sql);
    }
}
