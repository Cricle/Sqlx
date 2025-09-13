// -----------------------------------------------------------------------
// <copyright file="NameMapperTests_Extended.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class NameMapperTests_Extended
    {
        [TestMethod]
        public void MapName_Null_Throws()
        {
            Assert.ThrowsException<System.ArgumentNullException>(() => NameMapper.MapName(null!));
        }

        [TestMethod]
        public void MapNameToSnakeCase_SpecialChars_ToLower()
        {
            Assert.AreEqual("@id", NameMapper.MapNameToSnakeCase("@Id"));
            Assert.AreEqual("#count", NameMapper.MapNameToSnakeCase("#Count"));
        }

        [TestMethod]
        public void MapNameToSnakeCase_PascalCamel_ToSnake()
        {
            Assert.AreEqual("user", NameMapper.MapNameToSnakeCase("User"));
            Assert.AreEqual("user_name", NameMapper.MapNameToSnakeCase("UserName"));
            Assert.AreEqual("http_response_code", NameMapper.MapNameToSnakeCase("HttpResponseCode"));
            Assert.AreEqual("name", NameMapper.MapNameToSnakeCase("name"));
        }
    }
}


