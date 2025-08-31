// -----------------------------------------------------------------------
// <copyright file="ConnectionExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.CompilationTests
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;

    internal static partial class ConnectionExtensions
    {
        [Sqlx("persons_list")]
        public static partial IList<PersonInformation> GetResult(this DbConnection connection);

        [Sqlx("persons_list")]
        public static partial Task<IList<PersonInformation>> GetResultAsync(this DbConnection connection, CancellationToken cancellationToken);
    }
}
