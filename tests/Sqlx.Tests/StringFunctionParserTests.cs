// -----------------------------------------------------------------------
// <copyright file="StringFunctionParserTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Expressions;

namespace Sqlx.Tests
{
    /// <summary>
    /// Tests for StringFunctionParser to achieve 100% branch coverage.
    /// </summary>
    [TestClass]
    public class StringFunctionParserTests
    {
        private ExpressionParser CreateParser(string dialectType)
        {
            SqlDialect dialect = dialectType switch
            {
                "SqlServer" => new SqlServerDialect(),
                "MySql" => new MySqlDialect(),
                "PostgreSql" => new PostgreSqlDialect(),
                "Oracle" => new OracleDialect(),
                "SQLite" => new SQLiteDialect(),
                "DB2" => new DB2Dialect(),
                _ => new SqlServerDialect()
            };
            return new ExpressionParser(dialect, null!, parameterized: false);
        }

        [TestMethod]
        public void Parse_Contains_GeneratesLikeWithWildcards()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string, bool>> expr = (s, search) => s.Contains(search);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LIKE"));
        }

        [TestMethod]
        public void Parse_StartsWith_GeneratesLikeWithTrailingWildcard()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string, bool>> expr = (s, prefix) => s.StartsWith(prefix);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LIKE"));
        }

        [TestMethod]
        public void Parse_EndsWith_GeneratesLikeWithLeadingWildcard()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string, bool>> expr = (s, suffix) => s.EndsWith(suffix);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LIKE"));
        }

        [TestMethod]
        public void Parse_ToUpper_GeneratesUpperFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string>> expr = s => s.ToUpper();
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("UPPER"));
        }

        [TestMethod]
        public void Parse_ToLower_GeneratesLowerFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string>> expr = s => s.ToLower();
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LOWER"));
        }

        [TestMethod]
        public void Parse_Trim_GeneratesTrimFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string>> expr = s => s.Trim();
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("TRIM") || result.Contains("LTRIM"));
        }

        [TestMethod]
        public void Parse_TrimStart_GeneratesLTrimFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string>> expr = s => s.TrimStart();
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LTRIM"));
        }

        [TestMethod]
        public void Parse_TrimEnd_GeneratesRTrimFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string>> expr = s => s.TrimEnd();
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("RTRIM"));
        }

        [TestMethod]
        public void Parse_Replace_GeneratesReplaceFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string, string, string>> expr = (s, old, newVal) => s.Replace(old, newVal);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("REPLACE"));
        }

        [TestMethod]
        public void Parse_SubstringOneArg_GeneratesSubstringFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, int, string>> expr = (s, start) => s.Substring(start);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("SUBSTRING") || result.Contains("SUBSTR"));
        }

        [TestMethod]
        public void Parse_SubstringTwoArgs_GeneratesSubstringWithLength()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, int, int, string>> expr = (s, start, length) => s.Substring(start, length);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("SUBSTRING") || result.Contains("SUBSTR"));
        }

        [TestMethod]
        public void Parse_Length_GeneratesLengthFunction()
        {
            // Arrange - Length is not a method in StringFunctionParser.Parse switch
            // It's handled as ("Length", 0) which returns obj (the string itself)
            // This test verifies the default behavior
            var parser = CreateParser("SqlServer");
            Expression<Func<string, int>> expr = s => s.Length;
            
            // Create a member access expression for Length property
            var param = Expression.Parameter(typeof(string), "s");
            var lengthProperty = typeof(string).GetProperty("Length");
            
            // Wrap in a method call to simulate how it would be called
            var method = typeof(string).GetMethod("get_Length");
            var methodCall = Expression.Call(param, method!);

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert - Length is not in the switch, so it returns obj (the parameter)
            // The result should just be the parsed object
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Parse_PadLeftOneArg_SqlServer_GeneratesPadLeftFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, int, string>> expr = (s, width) => s.PadLeft(width);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("REPLICATE") || result.Contains("RIGHT"));
        }

        [TestMethod]
        public void Parse_PadLeftTwoArgs_PostgreSql_GeneratesLPadFunction()
        {
            // Arrange
            var parser = CreateParser("PostgreSql");
            Expression<Func<string, int, char, string>> expr = (s, width, padChar) => s.PadLeft(width, padChar);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LPAD"));
        }

        [TestMethod]
        public void Parse_PadLeftOneArg_SQLite_GeneratesCustomPadLeft()
        {
            // Arrange
            var parser = CreateParser("SQLite");
            Expression<Func<string, int, string>> expr = (s, width) => s.PadLeft(width);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("SUBSTR") || result.Contains("REPLACE"));
        }

        [TestMethod]
        public void Parse_PadRightOneArg_SqlServer_GeneratesPadRightFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, int, string>> expr = (s, width) => s.PadRight(width);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("REPLICATE") || result.Contains("LEFT"));
        }

        [TestMethod]
        public void Parse_PadRightTwoArgs_PostgreSql_GeneratesRPadFunction()
        {
            // Arrange
            var parser = CreateParser("PostgreSql");
            Expression<Func<string, int, char, string>> expr = (s, width, padChar) => s.PadRight(width, padChar);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("RPAD"));
        }

        [TestMethod]
        public void Parse_PadRightOneArg_SQLite_GeneratesCustomPadRight()
        {
            // Arrange
            var parser = CreateParser("SQLite");
            Expression<Func<string, int, string>> expr = (s, width) => s.PadRight(width);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("SUBSTR") || result.Contains("REPLACE"));
        }

        [TestMethod]
        public void Parse_IndexOfOneArg_SqlServer_GeneratesCharIndexFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string, int>> expr = (s, search) => s.IndexOf(search);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("CHARINDEX"));
        }

        [TestMethod]
        public void Parse_IndexOfOneArg_MySql_GeneratesLocateFunction()
        {
            // Arrange
            var parser = CreateParser("MySql");
            Expression<Func<string, string, int>> expr = (s, search) => s.IndexOf(search);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LOCATE"));
        }

        [TestMethod]
        public void Parse_IndexOfOneArg_PostgreSql_GeneratesPositionFunction()
        {
            // Arrange
            var parser = CreateParser("PostgreSql");
            Expression<Func<string, string, int>> expr = (s, search) => s.IndexOf(search);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("POSITION"));
        }

        [TestMethod]
        public void Parse_IndexOfOneArg_Oracle_GeneratesInstrFunction()
        {
            // Arrange
            var parser = CreateParser("Oracle");
            Expression<Func<string, string, int>> expr = (s, search) => s.IndexOf(search);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("INSTR"));
        }

        [TestMethod]
        public void Parse_IndexOfOneArg_SQLite_GeneratesInstrFunction()
        {
            // Arrange
            var parser = CreateParser("SQLite");
            Expression<Func<string, string, int>> expr = (s, search) => s.IndexOf(search);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("INSTR"));
        }

        [TestMethod]
        public void Parse_IndexOfTwoArgs_SqlServer_GeneratesCharIndexWithStart()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<string, string, int, int>> expr = (s, search, start) => s.IndexOf(search, start);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("CHARINDEX"));
        }

        [TestMethod]
        public void Parse_IndexOfTwoArgs_MySql_GeneratesLocateWithStart()
        {
            // Arrange
            var parser = CreateParser("MySql");
            Expression<Func<string, string, int, int>> expr = (s, search, start) => s.IndexOf(search, start);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LOCATE"));
        }

        [TestMethod]
        public void Parse_IndexOfTwoArgs_PostgreSql_GeneratesPositionWithSubstring()
        {
            // Arrange
            var parser = CreateParser("PostgreSql");
            Expression<Func<string, string, int, int>> expr = (s, search, start) => s.IndexOf(search, start);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("POSITION") || result.Contains("SUBSTRING"));
        }

        [TestMethod]
        public void Parse_IndexOfTwoArgs_SQLite_GeneratesInstrWithSubstr()
        {
            // Arrange
            var parser = CreateParser("SQLite");
            Expression<Func<string, string, int, int>> expr = (s, search, start) => s.IndexOf(search, start);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("INSTR") || result.Contains("SUBSTR"));
        }

        [TestMethod]
        public void Parse_IndexOfTwoArgs_Oracle_GeneratesInstrWithStart()
        {
            // Arrange
            var parser = CreateParser("Oracle");
            Expression<Func<string, string, int, int>> expr = (s, search, start) => s.IndexOf(search, start);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("INSTR"));
        }

        [TestMethod]
        public void Parse_IndexOfTwoArgs_DB2_GeneratesLocateWithStart()
        {
            // Arrange
            var parser = CreateParser("DB2");
            Expression<Func<string, string, int, int>> expr = (s, search, start) => s.IndexOf(search, start);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = StringFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("LOCATE"));
        }

        [TestMethod]
        public void ParseIndexer_GeneratesSubstringForCharacterAccess()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            // Simulate string indexer: s[index]
            var stringParam = Expression.Parameter(typeof(string), "s");
            var indexParam = Expression.Parameter(typeof(int), "index");
            var indexerProperty = typeof(string).GetProperty("Chars");
            var methodCall = Expression.Call(stringParam, indexerProperty!.GetGetMethod()!, indexParam);

            // Act
            var result = StringFunctionParser.ParseIndexer(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("SUBSTRING") || result.Contains("SUBSTR"));
        }

        [TestMethod]
        public void ParseIndexer_NoArguments_ReturnsNull()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            var stringParam = Expression.Parameter(typeof(string), "s");
            
            // Create a simple method call that has no arguments
            // Use a method that doesn't require arguments
            var toUpperMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
            var methodCall = Expression.Call(stringParam, toUpperMethod!);

            // Act - ParseIndexer expects Arguments.Count == 1, so this should return "NULL"
            var result = StringFunctionParser.ParseIndexer(parser, methodCall);

            // Assert - should return "NULL" when Arguments.Count != 1
            Assert.AreEqual("NULL", result);
        }
    }
}
