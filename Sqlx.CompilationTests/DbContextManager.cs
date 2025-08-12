// -----------------------------------------------------------------------
// <copyright file="DbContextManager.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.CompilationTests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal partial class DbContextManager
{
    private readonly PersonDbContext context;

    public DbContextManager(PersonDbContext context)
    {
        this.context = context;
    }

    [Sqlx("persons_list")]
    public partial IList<PersonDbContext.Person> GetResult();

    [Sqlx("persons_list")]
    public partial IList<(int Id, string Name)> GetTupleResult();

    [Sqlx("persons_list")]
    public partial IEnumerable<PersonDbContext.Person> GetEnumerableResult();

    [Sqlx("persons_list")]
    public partial Task<IList<PersonDbContext.Person>> GetResultAsync();

    [Sqlx("persons_list")]
    public partial Task<IList<(int Id, string Name)>> GetTupleResultAsync();

    [Sqlx("persons_list")]
    public partial Task<PersonDbContext.Person?> GetFirstOrDefaultAsync();

    [Sqlx("non_existing")]
    public partial int GetScalarResult();

    [Sqlx("non_existing")]
    public partial int? GetNullableScalarResult();

    [Sqlx("non_existing")]
    public partial Task<int> GetScalarResultAsync();

    [Sqlx("non_existing")]
    public partial Task<int?> GetNullableScalarResultAsync();

    [Sqlx("persons_by_id")]
    public partial PersonDbContext.Person GetPersonById(int personId);

    [Sqlx("users_list")]
    public partial IList<PersonDbContext.User> GetUsers();
}
