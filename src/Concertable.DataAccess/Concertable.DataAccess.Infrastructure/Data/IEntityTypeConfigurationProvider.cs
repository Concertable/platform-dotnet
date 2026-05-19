using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public interface IEntityTypeConfigurationProvider
{
    void Configure(ModelBuilder modelBuilder);
}
