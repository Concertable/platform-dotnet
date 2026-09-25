using Microsoft.Data.SqlClient;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class SqlExceptionExtensions
{
    extension(SqlException ex)
    {
        public bool IsDuplicateKey() =>
            ex.Number is 2601 or 2627;
    }
}
