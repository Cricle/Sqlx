// -----------------------------------------------------------------------
// <copyright file="DbContextManager.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
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
    [DbSetType(typeof(PersonDbContext.Person))]
    public partial IList<PersonDbContext.Person> GetResult();

    // [Sqlx("persons_list")]
    // [DbSetType(typeof(PersonDbContext.Person))]
    // public partial IList<(int PersonId, string PersonName)> GetTupleResult();
    [Sqlx("persons_list")]
    [DbSetType(typeof(PersonDbContext.Person))]
    public partial IEnumerable<PersonDbContext.Person> GetEnumerableResult();

    [Sqlx("persons_list")]
    [DbSetType(typeof(PersonDbContext.Person))]
    public partial Task<IList<PersonDbContext.Person>> GetResultAsync();

    // [Sqlx("persons_list")]
    // [DbSetType(typeof(PersonDbContext.Person))]
    // public partial Task<IList<(int PersonId, string PersonName)>> GetTupleResultAsync();
    [Sqlx("persons_list")]
    [DbSetType(typeof(PersonDbContext.Person))]
    public partial Task<PersonDbContext.Person?> GetFirstOrDefaultAsync();

    [Sqlx("persons_list")]
    [DbSetType(typeof(PersonDbContext.Person))]
    public partial Task<PersonDbContext.PersonDto?> GetFirstOrDefaultDtoAsync();

    [Sqlx("non_existing")]
    public partial int GetScalarResult();

    [Sqlx("non_existing")]
    public partial int? GetNullableScalarResult();

    [Sqlx("non_existing")]
    public partial Task<int> GetScalarResultAsync();

    [Sqlx("non_existing")]
    public partial Task<int?> GetNullableScalarResultAsync();

    [Sqlx("persons_by_id")]
    [DbSetType(typeof(PersonDbContext.Person))]
    public partial PersonDbContext.Person GetPersonById(int personId);

    [Sqlx("users_list")]
    [DbSetType(typeof(PersonDbContext.User))]
    public partial IList<PersonDbContext.User> GetUsers();
}