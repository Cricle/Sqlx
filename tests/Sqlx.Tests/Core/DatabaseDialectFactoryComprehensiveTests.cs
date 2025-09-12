// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectFactoryComprehensiveTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;
using System;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Comprehensive tests for DatabaseDialectFactory to improve coverage.
    /// </summary>
    [TestClass]
    public class DatabaseDialectFactoryComprehensiveTests
    {
        [TestMethod]
        public void GetDialectProvider_WithMySqlType_ReturnsMySqlProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithSqlServerType_ReturnsSqlServerProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SqlServer);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithPostgresqlType_ReturnsPostgreSqlProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.Postgresql);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithSQLiteType_ReturnsSQLiteProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SQLite);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithOracleType_ThrowsUnsupportedDialectException()
        {
            // Act & Assert
            Assert.ThrowsException<UnsupportedDialectException>(() =>
                DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.Oracle));
        }

        [TestMethod]
        public void GetDialectProvider_WithDB2Type_ThrowsUnsupportedDialectException()
        {
            // Act & Assert
            Assert.ThrowsException<UnsupportedDialectException>(() =>
                DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.DB2));
        }

        [TestMethod]
        public void GetDialectProvider_WithInvalidType_ThrowsUnsupportedDialectException()
        {
            // Act & Assert
            Assert.ThrowsException<UnsupportedDialectException>(() =>
                DatabaseDialectFactory.GetDialectProvider((SqlDefineTypes)999));
        }

        [TestMethod]
        public void GetDialectProvider_WithMySqlDefine_ReturnsMySqlProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.MySql);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithSqlServerDefine_ReturnsSqlServerProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.SqlServer);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithPgSqlDefine_ReturnsPostgreSqlProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.PgSql);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithSQLiteDefine_ReturnsSQLiteProvider()
        {
            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.SQLite);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithOracleDefine_ThrowsUnsupportedDialectException()
        {
            // Act & Assert
            Assert.ThrowsException<UnsupportedDialectException>(() =>
                DatabaseDialectFactory.GetDialectProvider(SqlDefine.Oracle));
        }

        [TestMethod]
        public void GetDialectProvider_WithDB2Define_ThrowsUnsupportedDialectException()
        {
            // Act & Assert
            Assert.ThrowsException<UnsupportedDialectException>(() =>
                DatabaseDialectFactory.GetDialectProvider(SqlDefine.DB2));
        }

        [TestMethod]
        public void GetDialectProvider_WithCustomMySqlLikeDefine_ReturnsMySqlProvider()
        {
            // Arrange - Create a custom SqlDefine with MySql characteristics
            var customDefine = new SqlDefine("`", "`", "'", "'", "@");

            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithCustomPostgresqlLikeDefine_ReturnsPostgreSqlProvider()
        {
            // Arrange - Create a custom SqlDefine with Postgresql characteristics
            var customDefine = new SqlDefine("\"", "\"", "'", "'", "$");

            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithCustomOracleLikeDefine_ThrowsUnsupportedDialectException()
        {
            // Arrange - Create a custom SqlDefine with Oracle characteristics
            var customDefine = new SqlDefine("\"", "\"", "'", "'", ":");

            // Act & Assert
            Assert.ThrowsException<UnsupportedDialectException>(() =>
                DatabaseDialectFactory.GetDialectProvider(customDefine));
        }

        [TestMethod]
        public void GetDialectProvider_WithCustomDB2LikeDefine_ThrowsUnsupportedDialectException()
        {
            // Arrange - Create a custom SqlDefine with DB2 characteristics
            var customDefine = new SqlDefine("\"", "\"", "'", "'", "?");

            // Act & Assert
            Assert.ThrowsException<UnsupportedDialectException>(() =>
                DatabaseDialectFactory.GetDialectProvider(customDefine));
        }

        [TestMethod]
        public void GetDialectProvider_WithCustomSQLiteLikeDefine_ReturnsSQLiteProvider()
        {
            // Arrange - Create a custom SqlDefine with SQLite characteristics
            var customDefine = new SqlDefine("[", "]", "'", "'", "@sqlite");

            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithCustomSqlServerLikeDefine_ReturnsSqlServerProvider()
        {
            // Arrange - Create a custom SqlDefine with SqlServer characteristics
            var customDefine = new SqlDefine("[", "]", "'", "'", "@");

            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_WithUnknownCustomDefine_ReturnsSqlServerProviderAsDefault()
        {
            // Arrange - Create a custom SqlDefine with unknown characteristics
            var customDefine = new SqlDefine("~", "~", "'", "'", "#");

            // Act
            var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
        }

        [TestMethod]
        public void GetDialectProvider_CallMultipleTimes_ReturnsNewInstancesEachTime()
        {
            // Act
            var provider1 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);
            var provider2 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);

            // Assert
            Assert.IsNotNull(provider1);
            Assert.IsNotNull(provider2);
            Assert.AreNotSame(provider1, provider2); // Different instances (no caching)
            Assert.AreEqual(provider1.GetType(), provider2.GetType()); // Same type
        }

        [TestMethod]
        public void GetDialectProvider_WithAllSupportedTypes_CreatesCorrectProviders()
        {
            // Arrange
            var supportedTypes = new[]
            {
                SqlDefineTypes.MySql,
                SqlDefineTypes.SqlServer,
                SqlDefineTypes.Postgresql,
                SqlDefineTypes.SQLite
            };

            // Act & Assert
            foreach (var type in supportedTypes)
            {
                var provider = DatabaseDialectFactory.GetDialectProvider(type);
                Assert.IsNotNull(provider, $"Provider for {type} should not be null");
                
                // Verify the provider type matches expectations
                switch (type)
                {
                    case SqlDefineTypes.MySql:
                        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
                        break;
                    case SqlDefineTypes.SqlServer:
                        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
                        break;
                    case SqlDefineTypes.Postgresql:
                        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
                        break;
                    case SqlDefineTypes.SQLite:
                        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
                        break;
                }
            }
        }
    }
}
