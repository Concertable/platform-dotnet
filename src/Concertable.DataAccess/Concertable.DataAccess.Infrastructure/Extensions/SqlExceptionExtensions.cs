using Microsoft.Data.SqlClient;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class SqlExceptionExtensions
{
    public static bool IsDuplicateKey(this SqlException ex) =>
        ex.Number is 2601 or 2627;
}
