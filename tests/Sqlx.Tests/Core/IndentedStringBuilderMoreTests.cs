// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilderMoreTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class IndentedStringBuilderMoreTests
    {
        [TestMethod]
        public void PopIndent_AtZero_Throws()
        {
            var sb = new Sqlx.IndentedStringBuilder(null);
            Assert.ThrowsException<System.InvalidOperationException>(() => sb.PopIndent());
        }

        [TestMethod]
        public void Append_Null_WithIndent_WritesIndentOnly()
        {
            var sb = new Sqlx.IndentedStringBuilder(null);
            sb.PushIndent();
            sb.Append(null);
            sb.AppendLine("X");
            var s = sb.ToString();
            // Expect one indent before X
            Assert.IsTrue(s.Contains("    X"));
        }
    }
}


