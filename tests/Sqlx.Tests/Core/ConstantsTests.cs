// -----------------------------------------------------------------------
// <copyright file="ConstantsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class ConstantsTests
    {
        // SqlExecuteTypeValues test removed - SqlExecuteType has been deprecated and replaced with SQL templates

        [TestMethod]
        public void GeneratedVariables_AllDefined()
        {
            Assert.AreEqual("__conn__", Constants.GeneratedVariables.Connection);
            Assert.AreEqual("__cmd__", Constants.GeneratedVariables.Command);
            Assert.AreEqual("__reader__", Constants.GeneratedVariables.Reader);
            Assert.AreEqual("__result__", Constants.GeneratedVariables.Result);
            Assert.AreEqual("__data__", Constants.GeneratedVariables.Data);
            Assert.AreEqual("__startTime__", Constants.GeneratedVariables.StartTime);
            Assert.AreEqual("__exception__", Constants.GeneratedVariables.Exception);
            Assert.AreEqual("__elapsed__", Constants.GeneratedVariables.Elapsed);
        }
    }
}


