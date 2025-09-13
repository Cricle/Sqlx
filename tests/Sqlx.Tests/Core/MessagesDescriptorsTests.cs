// -----------------------------------------------------------------------
// <copyright file="MessagesDescriptorsTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class MessagesDescriptorsTests
{
    [TestMethod]
    public void Descriptor_Ids_Are_NotNull_And_Sequentially_Named()
    {
        Assert.AreEqual("SP0001", Sqlx.Messages.SP0001.Id);
        Assert.AreEqual("SP0002", Sqlx.Messages.SP0002.Id);
        Assert.AreEqual("SP0003", Sqlx.Messages.SP0003.Id);
        Assert.AreEqual("SP0004", Sqlx.Messages.SP0004.Id);
        Assert.AreEqual("SP0005", Sqlx.Messages.SP0005.Id);
        Assert.AreEqual("SP0006", Sqlx.Messages.SP0006.Id);
        Assert.AreEqual("SP0007", Sqlx.Messages.SP0007.Id);
        Assert.AreEqual("SP0008", Sqlx.Messages.SP0008.Id);
        Assert.AreEqual("SP0009", Sqlx.Messages.SP0009.Id);
        Assert.AreEqual("SP0010", Sqlx.Messages.SP0010.Id);
        Assert.AreEqual("SP0011", Sqlx.Messages.SP0011.Id);
        Assert.AreEqual("SP0012", Sqlx.Messages.SP0012.Id);
        Assert.AreEqual("SP0013", Sqlx.Messages.SP0013.Id);
        Assert.AreEqual("SP0014", Sqlx.Messages.SP0014.Id);
        Assert.AreEqual("SP0015", Sqlx.Messages.SP0015.Id);
    }
}


