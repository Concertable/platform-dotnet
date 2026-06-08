using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Data;

public interface IEntityTypeConfigurationProvider
{
    void Configure(ModelBuilder modelBuilder);
}
