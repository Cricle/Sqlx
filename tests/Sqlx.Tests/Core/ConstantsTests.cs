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
        [TestMethod]
        public void SqlExecuteTypeValues_AllDefined()
        {
            Assert.AreEqual(0, Constants.SqlExecuteTypeValues.Select);
            Assert.AreEqual(1, Constants.SqlExecuteTypeValues.Update);
            Assert.AreEqual(2, Constants.SqlExecuteTypeValues.Insert);
            Assert.AreEqual(3, Constants.SqlExecuteTypeValues.Delete);
            Assert.AreEqual(4, Constants.SqlExecuteTypeValues.BatchInsert);
            Assert.AreEqual(5, Constants.SqlExecuteTypeValues.BatchUpdate);
            Assert.AreEqual(6, Constants.SqlExecuteTypeValues.BatchDelete);
            Assert.AreEqual(7, Constants.SqlExecuteTypeValues.BatchCommand);
        }

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


