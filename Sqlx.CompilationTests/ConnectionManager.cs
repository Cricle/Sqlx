// -----------------------------------------------------------------------
// <copyright file="ConnectionManager.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.CompilationTests;

using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data.Common;

internal partial class ConnectionManager
{
    private readonly DbConnection connection;

    public ConnectionManager(SqlConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("persons_list")]
    public partial IList<PersonInformation> GetResult();

    [Sqlx("persons_list")]
    public partial IList<(int Id, string Name)> GetTupleResult();

    [Sqlx("")]
    public partial IList<PersonInformation> GetResultFromSql([RawSql]string sql, int maxId);

    [Sqlx("persons_by_id")]
    public partial PersonInformation GetPersonById(int personId);

    [Sqlx("persons_list")]
    public partial IEnumerable<PersonInformation> GetResult(DbTransaction tran);
}