using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

[TestClass]
public class EntityProviderResolverTests
{
    public partial class ResolverEntityA
    {
        public int Id { get; set; }
    }

    public partial class ResolverEntityB
    {
        public int Id { get; set; }
    }

    [TestMethod]
    public void EnsureProviderMatches_GenericTypeMismatch_ThrowsArgumentException()
    {
        var mismatchedProvider = new DynamicEntityProvider<ResolverEntityB>();

        Assert.ThrowsException<ArgumentException>(() =>
        {
            EntityProviderResolver.EnsureProviderMatches(typeof(ResolverEntityA), mismatchedProvider);
        });
    }

    [TestMethod]
    public void ResolveOrCreate_Generic_WithMismatchedProvider_AllowsInternalReuse()
    {
        var mismatchedProvider = new DynamicEntityProvider<ResolverEntityB>();

        var resolved = EntityProviderResolver.ResolveOrCreate<ResolverEntityA>(mismatchedProvider);

        Assert.AreSame(mismatchedProvider, resolved);
    }

    [TestMethod]
    public void SqlQuery_ForWithMismatchedProvider_ThrowsArgumentException()
    {
        var mismatchedProvider = new DynamicEntityProvider<ResolverEntityB>();

        Assert.ThrowsException<ArgumentException>(() =>
        {
            _ = SqlQuery<ResolverEntityA>.For(SqlDefine.SQLite, mismatchedProvider);
        });
    }

    [TestMethod]
    public void PlaceholderContext_CreateWithMismatchedProvider_ThrowsArgumentException()
    {
        var mismatchedProvider = new DynamicEntityProvider<ResolverEntityB>();

        Assert.ThrowsException<ArgumentException>(() =>
        {
            _ = PlaceholderContext.Create<ResolverEntityA>(SqlDefine.SQLite, mismatchedProvider);
        });
    }

    [TestMethod]
    public void SqlQuery_EntityProviderSetter_WithMismatchedProvider_ThrowsArgumentException()
    {
        var original = SqlQuery<ResolverEntityA>.EntityProvider;
        var mismatchedProvider = new DynamicEntityProvider<ResolverEntityB>();

        try
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                SqlQuery<ResolverEntityA>.EntityProvider = mismatchedProvider;
            });
        }
        finally
        {
            SqlQuery<ResolverEntityA>.EntityProvider = original;
        }
    }

    [TestMethod]
    public void SqlQuery_EntityProviderSetter_WithMatchingProvider_Succeeds()
    {
        var original = SqlQuery<ResolverEntityA>.EntityProvider;
        var matchingProvider = new DynamicEntityProvider<ResolverEntityA>();

        try
        {
            SqlQuery<ResolverEntityA>.EntityProvider = matchingProvider;
            Assert.AreSame(matchingProvider, SqlQuery<ResolverEntityA>.EntityProvider);
        }
        finally
        {
            SqlQuery<ResolverEntityA>.EntityProvider = original;
        }
    }
}
