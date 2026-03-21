using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests;

[TestClass]
public class ReflectionMetadataQueryTranslationTests
{
    [TableName("reflection_users")]
    public class ReflectionMappedUser
    {
        public int Id { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; } = string.Empty;
    }

    public class ReflectionPlainUser
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;
    }

    public class ReflectionPlainOrder
    {
        public int Id { get; set; }

        public int UserId { get; set; }
    }

    public class ReflectionAcronymEntity
    {
        public int Id { get; set; }

        public int HTTPStatus { get; set; }

        public string URLPath { get; set; } = string.Empty;
    }

    [TestMethod]
    public void SqlQuery_WithReflectionMetadata_UsesTableAndColumnAttributesWithoutSqlxAttribute()
    {
        var sql = SqlQuery<ReflectionMappedUser>.ForSqlite()
            .Where(user => user.DisplayName == "Alice")
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM [reflection_users]"), sql);
        Assert.IsTrue(sql.Contains("[display_name] = 'Alice'"), sql);
        Assert.IsFalse(sql.Contains("[display_name] = @"), sql);
    }

    [TestMethod]
    public void SqlQuery_WithPlainPoco_DefaultSelect_UsesDynamicEntityProviderFallback()
    {
        var sql = SqlQuery<ReflectionPlainUser>.ForSqlite().ToSql();

        Assert.IsTrue(sql.Contains("SELECT [id], [user_name]"), sql);
        Assert.IsTrue(sql.Contains("FROM [ReflectionPlainUser]"), sql);
    }

    [TestMethod]
    public void SqlQuery_WithPlainPoco_SubQuery_UsesDynamicEntityProviderFallback()
    {
        var sql = SqlQuery<ReflectionPlainUser>.ForSqlite()
            .Select(user => new
            {
                user.UserName,
                OrderCount = SubQuery.For<ReflectionPlainOrder>().Where(order => order.UserId == user.Id).Count(),
            })
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM [ReflectionPlainOrder]"), sql);
        Assert.IsTrue(sql.Contains("[user_id] = [id]"), sql);
    }

    [TestMethod]
    public void SqlQuery_WithPlainPoco_Acronyms_UsesGeneratorCompatibleSnakeCase()
    {
        var sql = SqlQuery<ReflectionAcronymEntity>.ForSqlite()
            .Where(entity => entity.HTTPStatus == 200)
            .Select(entity => new { entity.HTTPStatus, entity.URLPath })
            .ToSql();

        Assert.IsTrue(sql.Contains("[http_status]"), sql);
        Assert.IsTrue(sql.Contains("[url_path]"), sql);
        Assert.IsFalse(sql.Contains("[h_t_t_p_status]"), sql);
        Assert.IsFalse(sql.Contains("[u_r_l_path]"), sql);
    }
}
