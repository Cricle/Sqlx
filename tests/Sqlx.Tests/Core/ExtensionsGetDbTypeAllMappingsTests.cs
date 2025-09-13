// -----------------------------------------------------------------------
// <copyright file="ExtensionsGetDbTypeAllMappingsTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class ExtensionsGetDbTypeAllMappingsTests
{
    [TestMethod]
    public void GetDbType_Maps_All_Supported_SpecialTypes()
    {
        var comp = CSharpCompilation.Create("DbTypeAsm");
        Assert.AreEqual("global::System.Data.DbType.Boolean", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Boolean)));
        Assert.AreEqual("global::System.Data.DbType.String", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_String)));
        Assert.AreEqual("global::System.Data.DbType.Char", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Char)));
        Assert.AreEqual("global::System.Data.DbType.Byte", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Byte)));
        Assert.AreEqual("global::System.Data.DbType.SByte", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_SByte)));
        Assert.AreEqual("global::System.Data.DbType.Int16", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Int16)));
        Assert.AreEqual("global::System.Data.DbType.Int32", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Int32)));
        Assert.AreEqual("global::System.Data.DbType.Int64", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Int64)));
        Assert.AreEqual("global::System.Data.DbType.UInt16", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_UInt16)));
        Assert.AreEqual("global::System.Data.DbType.UInt32", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_UInt32)));
        Assert.AreEqual("global::System.Data.DbType.UInt64", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_UInt64)));
        Assert.AreEqual("global::System.Data.DbType.Single", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Single)));
        Assert.AreEqual("global::System.Data.DbType.Double", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Double)));
        Assert.AreEqual("global::System.Data.DbType.Decimal", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_Decimal)));
        Assert.AreEqual("global::System.Data.DbType.DateTime2", Sqlx.Extensions.GetDbType(comp.GetSpecialType(SpecialType.System_DateTime)));
    }
}


