﻿// -----------------------------------------------------------------------
// <copyright file="PersonDbContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.CompilationTests
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    internal class PersonDbContext : DbContext
    {
        public PersonDbContext(DbContextOptions<PersonDbContext> options)
            : base(options)
        {
        }

        public DbSet<Person> Persons { get; set; } = null!;

        public DbSet<User> Users { get; set; } = null!;

        internal class Person
        {
            [Column("person_id")]
            public int PersonId { get; set; }

            [Column("person_name")]
            public string? PersonName { get; set; }
        }

        internal class User
        {
            [Column("user_id")]
            public int UserId { get; set; }

            [Column("user_name")]
            public string? UserName { get; set; }
        }

        internal class PersonDto
        {
            public PersonDto(int personId)
            {
                PersonId = personId;
            }

            public int PersonId { get; }

            public string? PersonName { get; set; }
        }
    }
}
