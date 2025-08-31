// -----------------------------------------------------------------------
// <copyright file="SimpleExample.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.CompilationTests;

using Sqlx;
using Sqlx.Annotations;
using System.Collections.Generic;
using System.Data.Common;

/// <summary>
/// A simple example demonstrating basic Sqlx usage.
/// </summary>
internal partial class SimpleExample
{
    private readonly DbConnection connection;

    public SimpleExample(DbConnection connection)
    {
        this.connection = connection;
    }

    // Basic query example
    [RawSql("SELECT person_id AS PersonId, person_name AS PersonName FROM person")]
    public partial IList<PersonInformation> GetAllPersons();

    // Parameterized query example
    [RawSql("SELECT person_id AS PersonId, person_name AS PersonName FROM person WHERE person_id = @personId")]
    public partial PersonInformation? GetPersonById(int personId);

    // ExpressionToSql example - SQL generated from expression parameter
    public partial IList<PersonInformation> QueryPersons([ExpressionToSql] ExpressionToSql<PersonInformation> query);
}