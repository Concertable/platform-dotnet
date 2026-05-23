using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Data;

public interface IRatingProjectionConfigurationProvider
{
    void Configure(ModelBuilder modelBuilder);
}
