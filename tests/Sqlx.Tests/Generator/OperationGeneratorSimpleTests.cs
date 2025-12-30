// -----------------------------------------------------------------------
// <copyright file="OperationGeneratorSimpleTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;

namespace Sqlx.Tests.Generator;

/// <summary>
/// Simple tests for SqlTemplateEngine class.
/// </summary>
[TestClass]
public class OperationGeneratorSimpleTests : TestBase
{
    private SqlTemplateEngine _templateEngine = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _templateEngine = new SqlTemplateEngine();
    }
}

