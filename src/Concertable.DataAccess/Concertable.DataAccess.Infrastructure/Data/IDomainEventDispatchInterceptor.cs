using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.DataAccess.Infrastructure.Data;

public interface IDomainEventDispatchInterceptor : ISaveChangesInterceptor;
