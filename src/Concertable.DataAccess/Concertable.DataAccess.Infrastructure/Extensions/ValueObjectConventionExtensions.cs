using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

internal static class ValueObjectConventionExtensions
{
    extension(ModelConfigurationBuilder configurationBuilder)
    {
        public void MapValueObjects() =>
            configurationBuilder.Properties<Sha256Digest>()
                .HaveConversion<Sha256Digest.EfCoreValueConverter>()
                .HaveMaxLength(Sha256Digest.Length)
                .AreFixedLength();
    }
}
