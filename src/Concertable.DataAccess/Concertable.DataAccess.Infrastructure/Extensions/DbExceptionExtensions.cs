using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbExceptionExtensions
{
    extension(DbException ex)
    {
        public bool IsDuplicateKey() => ex switch
        {
            SqlException sqlEx => sqlEx.IsDuplicateKey(),
            PostgresException postgresEx => postgresEx.SqlState is PostgresErrorCodes.UniqueViolation,
            _ => false
        };
    }
}
