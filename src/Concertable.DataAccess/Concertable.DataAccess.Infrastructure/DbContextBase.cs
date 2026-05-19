using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public abstract class DbContextBase(DbContextOptions options) : DbContext(options)
{
}
