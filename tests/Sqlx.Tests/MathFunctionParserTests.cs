// -----------------------------------------------------------------------
// <copyright file="MathFunctionParserTests.cs" company="Sqlx">
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
    /// Tests for MathFunctionParser to achieve 100% branch coverage.
    /// </summary>
    [TestClass]
    public class MathFunctionParserTests
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
            return new ExpressionParser(dialect, null, parameterized: false);
        }

        [TestMethod]
        public void Parse_Abs_GeneratesAbsFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Abs(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("ABS"));
        }

        [TestMethod]
        public void Parse_Sign_GeneratesSignFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, int>> expr = x => Math.Sign(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("SIGN("));
        }

        [TestMethod]
        public void Parse_RoundOneArg_GeneratesRoundFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Round(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("ROUND("));
        }

        [TestMethod]
        public void Parse_RoundTwoArgs_GeneratesRoundWithPrecision()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, int, double>> expr = (x, p) => Math.Round(x, p);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("ROUND"));
        }

        [TestMethod]
        public void Parse_Floor_GeneratesFloorFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Floor(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("FLOOR"));
        }

        [TestMethod]
        public void Parse_Ceiling_GeneratesCeilingFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Ceiling(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("CEILING"));
        }

        [TestMethod]
        public void Parse_Truncate_SqlServer_GeneratesRoundWithMode()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Truncate(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.Contains("ROUND"));
            Assert.IsTrue(result.Contains("0, 1"));
        }

        [TestMethod]
        public void Parse_Truncate_PostgreSql_GeneratesTruncFunction()
        {
            // Arrange
            var parser = CreateParser("PostgreSql");
            Expression<Func<double, double>> expr = x => Math.Truncate(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("TRUNC("));
        }

        [TestMethod]
        public void Parse_Truncate_Oracle_GeneratesTruncFunction()
        {
            // Arrange
            var parser = CreateParser("Oracle");
            Expression<Func<double, double>> expr = x => Math.Truncate(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("TRUNC("));
        }

        [TestMethod]
        public void Parse_Truncate_MySql_GeneratesTruncateFunction()
        {
            // Arrange
            var parser = CreateParser("MySql");
            Expression<Func<double, double>> expr = x => Math.Truncate(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("TRUNCATE("));
            Assert.IsTrue(result.Contains(", 0)"));
        }

        [TestMethod]
        public void Parse_Sqrt_GeneratesSqrtFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Sqrt(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("SQRT("));
        }

        [TestMethod]
        public void Parse_Pow_SqlServer_GeneratesPowerFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double, double>> expr = (x, y) => Math.Pow(x, y);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("POWER("));
        }

        [TestMethod]
        public void Parse_Pow_MySql_GeneratesPowFunction()
        {
            // Arrange
            var parser = CreateParser("MySql");
            Expression<Func<double, double, double>> expr = (x, y) => Math.Pow(x, y);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("POW("));
        }

        [TestMethod]
        public void Parse_Exp_GeneratesExpFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Exp(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("EXP("));
        }

        [TestMethod]
        public void Parse_LogOneArg_SqlServer_GeneratesLogFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Log(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("LOG("));
        }

        [TestMethod]
        public void Parse_LogOneArg_PostgreSql_GeneratesLnFunction()
        {
            // Arrange
            var parser = CreateParser("PostgreSql");
            Expression<Func<double, double>> expr = x => Math.Log(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("LN("));
        }

        [TestMethod]
        public void Parse_LogTwoArgs_GeneratesLogWithBase()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double, double>> expr = (x, b) => Math.Log(x, b);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("LOG("));
        }

        [TestMethod]
        public void Parse_Log10_GeneratesLog10Function()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Log10(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("LOG10("));
        }

        [TestMethod]
        public void Parse_Sin_GeneratesSinFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Sin(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("SIN("));
        }

        [TestMethod]
        public void Parse_Cos_GeneratesCosFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Cos(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("COS("));
        }

        [TestMethod]
        public void Parse_Tan_GeneratesTanFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Tan(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("TAN("));
        }

        [TestMethod]
        public void Parse_Asin_GeneratesAsinFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Asin(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("ASIN("));
        }

        [TestMethod]
        public void Parse_Acos_GeneratesAcosFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Acos(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("ACOS("));
        }

        [TestMethod]
        public void Parse_Atan_GeneratesAtanFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double>> expr = x => Math.Atan(x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("ATAN("));
        }

        [TestMethod]
        public void Parse_Atan2_SqlServer_GeneratesAtn2Function()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double, double>> expr = (y, x) => Math.Atan2(y, x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("ATN2("));
        }

        [TestMethod]
        public void Parse_Atan2_PostgreSql_GeneratesAtan2Function()
        {
            // Arrange
            var parser = CreateParser("PostgreSql");
            Expression<Func<double, double, double>> expr = (y, x) => Math.Atan2(y, x);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("ATAN2("));
        }

        [TestMethod]
        public void Parse_Min_GeneratesLeastFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double, double>> expr = (x, y) => Math.Min(x, y);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("LEAST("));
        }

        [TestMethod]
        public void Parse_Max_GeneratesGreatestFunction()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            Expression<Func<double, double, double>> expr = (x, y) => Math.Max(x, y);
            var methodCall = (MethodCallExpression)expr.Body;

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert
            Assert.IsTrue(result.StartsWith("GREATEST("));
        }

        [TestMethod]
        public void Parse_UnsupportedFunction_ReturnsOne()
        {
            // Arrange
            var parser = CreateParser("SqlServer");
            // Create a method call expression for an unsupported Math method
            // Using BigMul which is not in the switch statement
            var method = typeof(Math).GetMethod("BigMul", new[] { typeof(long), typeof(long) });
            var param1 = Expression.Parameter(typeof(long), "x");
            var param2 = Expression.Parameter(typeof(long), "y");
            var methodCall = Expression.Call(method!, param1, param2);

            // Act
            var result = MathFunctionParser.Parse(parser, methodCall);

            // Assert - unsupported functions return "1" as fallback
            Assert.AreEqual("1", result);
        }
    }
}
