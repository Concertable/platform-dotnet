using Concertable.Messaging.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.Infrastructure.Extensions;

public static class InboxServiceCollectionExtensions
{
    public static IServiceCollection AddInbox(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<InboxDbContext>(configureDb);
        return services;
    }
}
