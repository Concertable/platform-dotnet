using System.Data.Common;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.DataAccess.UnitTests;

public sealed class DbExceptionExtensionsTests
{
    #region IsDuplicateKey

    [Fact]
    public void IsDuplicateKey_PostgresUniqueViolation_IsTrue()
    {
        var exception = PostgresError(PostgresErrorCodes.UniqueViolation);

        Assert.True(exception.IsDuplicateKey());
    }

    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    [InlineData(PostgresErrorCodes.NotNullViolation)]
    [InlineData(PostgresErrorCodes.ExclusionViolation)]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    [InlineData(PostgresErrorCodes.DeadlockDetected)]
    public void IsDuplicateKey_PostgresIntegrityFailureThatIsNotUniqueness_IsFalse(string sqlState)
    {
        var exception = PostgresError(sqlState);

        Assert.False(exception.IsDuplicateKey());
    }

    [Fact]
    public void IsDuplicateKey_ExceptionFromAnotherProvider_IsFalse()
    {
        var exception = new UnknownProviderException();

        Assert.False(exception.IsDuplicateKey());
    }

    #endregion

    #region DbUpdateException.IsDuplicateKey

    [Fact]
    public void IsDuplicateKey_SaveWrappingPostgresUniqueViolation_IsTrue()
    {
        var exception = new DbUpdateException("save failed", PostgresError(PostgresErrorCodes.UniqueViolation));

        Assert.True(exception.IsDuplicateKey());
    }

    [Fact]
    public void IsDuplicateKey_SaveWrappingPostgresForeignKeyViolation_IsFalse()
    {
        var exception = new DbUpdateException("save failed", PostgresError(PostgresErrorCodes.ForeignKeyViolation));

        Assert.False(exception.IsDuplicateKey());
    }

    [Fact]
    public void IsDuplicateKey_SaveWithNoProviderException_IsFalse()
    {
        var exception = new DbUpdateException("save failed", new InvalidOperationException());

        Assert.False(exception.IsDuplicateKey());
    }

    #endregion

    private static PostgresException PostgresError(string sqlState) =>
        new("violation", "ERROR", "ERROR", sqlState);

    private sealed class UnknownProviderException : DbException;
}
