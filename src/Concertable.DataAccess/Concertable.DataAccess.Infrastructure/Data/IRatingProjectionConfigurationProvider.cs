using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public interface IRatingProjectionConfigurationProvider
{
    void Configure(ModelBuilder modelBuilder);
}
